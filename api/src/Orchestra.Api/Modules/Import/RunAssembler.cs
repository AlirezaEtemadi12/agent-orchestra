using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import.Archive;
using Orchestra.Api.Modules.Import.Ledger;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import;

/// <summary>Implements data-model § "How a run is identified", steps 1–5, and step 4a for archives.</summary>
public static class RunAssembler
{
    public static IReadOnlyList<RunEntity> Build(
        string projectId,
        IReadOnlyList<LedgerBlock> blocks,
        IReadOnlyList<TaskSnapshot> liveTasks,
        IReadOnlyList<ArchiveSnapshot> archives,
        IReadOnlyList<RunEventEntity> events,
        SourceClock clock)
    {
        var runs = BuildLedgerRuns(projectId, blocks, clock);

        // The rows task.json carries, so an event can replace exactly its own round and no final row.
        var fromTask = new HashSet<StageResultEntity>();

        MergeLiveRuns(runs, projectId, liveTasks, clock, fromTask);
        MergeArchives(runs, projectId, archives, clock, fromTask);
        MergeEvents(runs, projectId, events, fromTask);

        NumberRunKeys(runs);

        return runs;
    }

    private static List<RunEntity> BuildLedgerRuns(
        string projectId,
        IReadOnlyList<LedgerBlock> blocks,
        SourceClock clock)
    {
        var runs = new List<RunEntity>();
        var byTask = new Dictionary<string, List<LedgerBlock>>(StringComparer.Ordinal);

        foreach (var block in blocks)
        {
            if (!byTask.TryGetValue(block.TaskId, out var list))
            {
                list = [];
                byTask[block.TaskId] = list;
            }

            list.Add(block);
        }

        foreach (var (taskId, taskBlocks) in byTask)
        {
            List<LedgerBlock>? current = null;
            var branch = string.Empty;

            foreach (var block in taskBlocks)
            {
                if (current is null)
                {
                    current = [];
                    runs.Add(NewRun(projectId, taskId, RunSource.Ledger));
                    branch = block.Branch ?? string.Empty;
                }
                else if (current.Exists(candidate => candidate.IsClose)
                    && !string.Equals(branch, block.Branch ?? string.Empty, StringComparison.Ordinal))
                {
                    current = [];
                    runs.Add(NewRun(projectId, taskId, RunSource.Ledger));
                    branch = block.Branch ?? string.Empty;
                }

                current.Add(block);

                if (block.IsClose)
                {
                    branch = block.Branch ?? branch;
                }

                ApplyLedgerBlock(runs[^1], block, clock);
            }
        }

        return runs;
    }

    private static void ApplyLedgerBlock(RunEntity run, LedgerBlock block, SourceClock clock)
    {
        var at = clock.ToUtc(Reinterpret(block.LocalTime));

        run.Source = RunSource.Ledger;
        run.SourceRef = block.File;
        run.Status = block.Status ?? run.Status;
        run.Repo = block.Repo ?? run.Repo;
        run.Branch = block.Branch ?? run.Branch;
        run.PlanVersion = block.PlanVersion ?? run.PlanVersion;
        run.CreatedAt = run.CreatedAt == default ? at : run.CreatedAt;
        run.UpdatedAt = at;
        run.ClosedAt = block.IsClose ? at : run.ClosedAt;
        run.CostMicros = block.TotalCostMicros ?? run.CostMicros;
        run.Rounds = block.Stages.Where(stage => stage.Stage == StageKind.Review).Max(stage => stage.Round) ?? run.Rounds;

        var (number, title) = PlanName.From(null, run.Branch);
        run.PlanNumber = number ?? run.PlanNumber;
        run.PlanTitle = title ?? run.PlanTitle;

        // A later block replaces the snapshot: stages and usage are never summed across blocks.
        run.Stages.Clear();
        run.Sessions.Clear();
        run.StageUsage.Clear();

        foreach (var row in block.Stages)
        {
            run.Stages.Add(new StageResultEntity
            {
                Id = Guid.NewGuid(),
                Stage = row.Stage,
                Round = row.Round,
                Phase = row.Phase,
                Adapter = row.Adapter,
                Model = row.Model,
                Verdict = row.Verdict,
                Blockers = row.Blockers,
                Should = row.Should,
                Nits = row.Nits,
                Changed = row.Changed,
                OutsidePlan = row.OutsidePlan,
                Violations = row.Violations,
                GatesGreen = row.GatesGreen,
                GatesNote = row.GatesNote,
                ModelVerdict = row.ModelVerdict,
                IsFinal = true,
                At = at,
            });
        }

        foreach (var session in block.Sessions)
        {
            session.RunId = run.Id;
            run.Sessions.Add(session);
        }

        foreach (var usage in block.StageUsage)
        {
            usage.RunId = run.Id;
            run.StageUsage.Add(usage);
        }
    }

    private static RunEntity NewRun(string projectId, string taskId, RunSource source)
    {
        return new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            TaskId = taskId,
            Source = source,
        };
    }

    private static void MergeLiveRuns(
        List<RunEntity> runs,
        string projectId,
        IReadOnlyList<TaskSnapshot> liveTasks,
        SourceClock clock,
        HashSet<StageResultEntity> fromTask)
    {
        foreach (var task in liveTasks)
        {
            var taskId = task.TaskId ?? Path.GetFileName(Path.GetDirectoryName(task.File));

            if (string.IsNullOrEmpty(taskId))
            {
                continue;
            }

            var branch = task.Branch ?? string.Empty;

            var target = runs.LastOrDefault(run =>
                run.TaskId == taskId
                && run.ClosedAt is null
                && (string.IsNullOrEmpty(branch)
                    || string.IsNullOrEmpty(run.Branch)
                    || string.Equals(run.Branch, branch, StringComparison.Ordinal)));

            if (target is null)
            {
                target = NewRun(projectId, taskId, RunSource.Live);
                runs.Add(target);
            }
            else
            {
                target.Source = RunSource.Both;
            }

            target.SourceRef = task.File;
            target.Status = task.Status ?? target.Status;
            target.Repo = task.Repo ?? target.Repo;
            target.Task ??= task.Task;
            target.Branch = string.IsNullOrEmpty(task.Branch) ? target.Branch : task.Branch;
            target.Rounds ??= task.Round;
            target.PlanVersion ??= task.PlanVersion;
            target.PlanNumber ??= task.PlanNumber;
            target.PlanTitle ??= task.PlanTitle;

            var created = task.CreatedAt ?? target.CreatedAt;
            var updated = task.UpdatedAt ?? target.UpdatedAt;

            target.CreatedAt = target.CreatedAt == default ? created : target.CreatedAt;
            target.UpdatedAt = updated ?? target.UpdatedAt;

            var (number, title) = PlanName.From(task.Task, task.Branch);
            target.PlanNumber ??= number;
            target.PlanTitle ??= title;

            AddTaskStages(target, task, clock, fromTask);
        }
    }

    private static void MergeArchives(
        List<RunEntity> runs,
        string projectId,
        IReadOnlyList<ArchiveSnapshot> archives,
        SourceClock clock,
        HashSet<StageResultEntity> fromTask)
    {
        foreach (var archive in archives)
        {
            var branch = archive.Task.Branch ?? string.Empty;

            var target = runs
                .Where(run => run.TaskId == archive.TaskId && run.ClosedAt is not null)
                .Where(run => string.Equals(run.Branch ?? string.Empty, branch, StringComparison.Ordinal))
                .OrderBy(run => Math.Abs((run.ClosedAt!.Value - archive.ArchivedAt).TotalSeconds))
                .FirstOrDefault();

            if (target is null)
            {
                target = NewRun(projectId, archive.TaskId, RunSource.Archive);
                target.Status = archive.Task.Status ?? string.Empty;
                target.CreatedAt = archive.Task.CreatedAt ?? archive.ArchivedAt;
                runs.Add(target);
            }
            else
            {
                target.Source = RunSource.Both;
            }

            target.SourceRef = archive.File;
            target.Status = archive.Task.Status ?? target.Status;
            target.Repo = archive.Task.Repo ?? target.Repo;
            target.Task ??= archive.Task.Task;
            target.Branch = string.IsNullOrEmpty(archive.Task.Branch) ? target.Branch : archive.Task.Branch;
            target.ClosedAt ??= archive.ArchivedAt;
            target.UpdatedAt = archive.Task.UpdatedAt ?? target.UpdatedAt ?? archive.ArchivedAt;
            target.Rounds ??= archive.Task.Round;
            target.PlanVersion ??= archive.Task.PlanVersion;
            target.PlanNumber ??= archive.Task.PlanNumber;
            target.PlanTitle ??= archive.Task.PlanTitle;

            var (number, title) = PlanName.From(archive.Task.Task, archive.Task.Branch);
            target.PlanNumber ??= number;
            target.PlanTitle ??= title;

            AddTaskStages(target, archive.Task, clock, fromTask);

            foreach (var text in archive.Texts)
            {
                text.RunId = target.Id;
                target.Texts.Add(text);
            }
        }
    }

    /// <summary>Plan 0004 §6.3 and D7: events fill in what no file carries — every review round's counts
    /// and the status the engine last wrote — and a task only events know about becomes a run with
    /// <see cref="RunSource.Events"/>. Events are read, never written back and never deleted.</summary>
    private static void MergeEvents(
        List<RunEntity> runs,
        string projectId,
        IReadOnlyList<RunEventEntity> events,
        HashSet<StageResultEntity> fromTask)
    {
        var ordered = events
            .OrderBy(item => item.At)
            .ThenBy(item => item.Seq)
            .ThenBy(item => item.ReceivedAt);

        foreach (var group in ordered.GroupBy(item => (item.TaskId, Branch: item.Branch ?? string.Empty)))
        {
            var branch = group.Key.Branch;

            var run = runs.LastOrDefault(candidate =>
                candidate.TaskId == group.Key.TaskId
                && (branch.Length == 0
                    || string.IsNullOrEmpty(candidate.Branch)
                    || string.Equals(candidate.Branch, branch, StringComparison.Ordinal)));

            if (run is null)
            {
                run = NewRun(projectId, group.Key.TaskId, RunSource.Events);
                run.Branch = branch.Length == 0 ? null : branch;
                runs.Add(run);
            }

            foreach (var item in group)
            {
                ApplyEvent(run, item, fromTask);
            }

            var (number, title) = PlanName.From(run.Task, run.Branch);

            run.PlanNumber ??= number;
            run.PlanTitle ??= title;
        }
    }

    private static void ApplyEvent(RunEntity run, RunEventEntity item, HashSet<StageResultEntity> fromTask)
    {
        var data = EventData.Read(item.DataJson);
        var previous = run.UpdatedAt;

        run.CreatedAt = run.CreatedAt == default ? item.At : run.CreatedAt;

        if (previous is null || item.At >= previous)
        {
            run.UpdatedAt = item.At;
        }

        switch (item.Kind)
        {
            case EventKinds.RunCreated:
                run.Repo = data.Text("repo") ?? run.Repo;
                run.Task = data.Text("task") ?? run.Task;

                if (run.Status.Length == 0)
                {
                    run.Status = data.Text("status") ?? run.Status;
                }

                run.PlanNumber ??= data.Text("planFamily");
                break;

            case EventKinds.Status:
                if (previous is null || item.At >= previous)
                {
                    run.Status = data.Text("to") ?? run.Status;
                }

                break;

            case EventKinds.History:
                AddEventStage(run, item, data, fromTask);
                break;

            case EventKinds.RunClosed:
                run.ClosedAt ??= item.At;
                break;
        }
    }

    private static void AddEventStage(
        RunEntity run,
        RunEventEntity item,
        EventData data,
        HashSet<StageResultEntity> fromTask)
    {
        var stage = data.Text("event") switch
        {
            "review" => StageKind.Review,
            "readback" => StageKind.ReadBack,
            "implement" => StageKind.Implement,
            "verify" => StageKind.Verify,
            _ => (StageKind?)null,
        };

        if (stage is null)
        {
            return;
        }

        var round = data.Int("round");
        var phase = data.Int("phase");

        // The event carries the counts task.json only summarises, so it replaces that row — and only a
        // row task.json gave us: the ledger's final rows are never touched.
        run.Stages.RemoveAll(row =>
            fromTask.Contains(row)
            && row.Stage == stage
            && row.Round == round
            && row.Phase == phase);

        var (adapter, model) = ModelName.Split(data.Text("model"));

        run.Stages.Add(new StageResultEntity
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Stage = stage.Value,
            Round = round,
            Phase = phase,
            Adapter = adapter,
            Model = model.Length == 0 ? null : model,
            Verdict = data.Text("verdict"),
            Blockers = data.Int("blockers"),
            Should = data.Int("should"),
            Nits = data.Int("nits"),
            Changed = data.Int("changed"),
            OutsidePlan = data.Int("outsidePlan"),
            Violations = data.Int("violations"),
            GatesGreen = data.Bool("gates"),
            ModelVerdict = data.Text("modelVerdict"),
            IsFinal = false,
            At = item.At,
        });

        if (stage == StageKind.Review
            && round is { } reviewed
            && (run.Rounds is null || reviewed > run.Rounds))
        {
            run.Rounds = reviewed;
        }
    }

    private static void AddTaskStages(
        RunEntity run,
        TaskSnapshot task,
        SourceClock clock,
        HashSet<StageResultEntity> fromTask)
    {
        var lastPerStage = task.History
            .GroupBy(row => row.Stage)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var row in task.History)
        {
            if (HasRow(run, row.Stage, row.Phase, row.Round))
            {
                continue;
            }

            Add(run, ToEntity(run, row, clock, isFinal: ReferenceEquals(lastPerStage[row.Stage], row)), fromTask);
        }

        foreach (var phase in task.Phases)
        {
            if (!HasRow(run, StageKind.Implement, phase.Phase, null))
            {
                Add(run, ToEntity(run, phase, clock, isFinal: true), fromTask);
            }
        }

        if (task.Verify is not null && !HasRow(run, StageKind.Verify, null, null))
        {
            Add(run, ToEntity(run, task.Verify, clock, isFinal: true), fromTask);
        }
    }

    private static void Add(RunEntity run, StageResultEntity stage, HashSet<StageResultEntity> fromTask)
    {
        run.Stages.Add(stage);
        fromTask.Add(stage);
    }

    private static bool HasRow(RunEntity run, StageKind stage, int? phase, int? round)
    {
        return run.Stages.Any(row =>
            row.Stage == stage
            && (round is null || row.Round == round)
            && (phase is null || row.Phase == phase));
    }

    private static StageResultEntity ToEntity(RunEntity run, StageSnapshot stage, SourceClock clock, bool isFinal)
    {
        return new StageResultEntity
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Stage = stage.Stage,
            Round = stage.Round,
            Phase = stage.Phase,
            Adapter = stage.Adapter,
            Model = stage.Model,
            Verdict = stage.Verdict,
            Blockers = stage.Blockers,
            Should = stage.Should,
            Nits = stage.Nits,
            Changed = stage.Changed,
            OutsidePlan = stage.OutsidePlan,
            Violations = stage.Violations,
            GatesGreen = stage.GatesGreen,
            GatesNote = stage.GatesNote,
            ModelVerdict = stage.ModelVerdict,
            IsFinal = isFinal,
            At = stage.At is null ? null : clock.ToUtc(DateTime.SpecifyKind(stage.At.Value, DateTimeKind.Unspecified)),
        };
    }

    private static void NumberRunKeys(List<RunEntity> runs)
    {
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var run in runs)
        {
            counters[run.TaskId] = counters.GetValueOrDefault(run.TaskId) + 1;
            run.RunKey = $"{run.TaskId}.{counters[run.TaskId]}";
        }
    }

    private static DateTime Reinterpret(DateTime local)
    {
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }
}
