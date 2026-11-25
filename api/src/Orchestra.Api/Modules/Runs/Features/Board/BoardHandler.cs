using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Runs.Features.Board;

/// <summary>D13–D15: the lanes come from events, the plan name and run key from the imported run. A run
/// nothing has reported in 24 hours is not running; a card is shown only while its lane's window holds.
/// Plan 0013 D12: one card per run, so the branch is not part of the grouping key.</summary>
public sealed class BoardHandler(
    OrchestraDbContext database,
    ProjectRegistry registry,
    TimeProvider clock)
{
    private static readonly TimeSpan NeedsYouWindow = TimeSpan.FromDays(30);

    private static readonly TimeSpan DoneWindow = TimeSpan.FromDays(7);

    private static readonly TimeSpan RunningWindow = TimeSpan.FromHours(24);

    public async Task<(BoardDto? Board, Error? Error)> HandleAsync(
        string? project,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(project) && registry.Find(project) is null)
        {
            return (null, Error.ProjectNotFound);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var since = now - NeedsYouWindow;

        var runsQuery = database.Runs.AsNoTracking().AsQueryable();
        var eventsQuery = database.RunEvents
            .AsNoTracking()
            .Where(item => item.At >= since)
            // Progress is the only kind that arrives every few seconds; the board needs the newest one.
            .Where(item => item.Kind != EventKinds.Progress || item.At >= now - RunningWindow);

        if (!string.IsNullOrWhiteSpace(project))
        {
            runsQuery = runsQuery.Where(run => run.ProjectId == project);
            eventsQuery = eventsQuery.Where(item => item.ProjectId == project);
        }

        var runs = await runsQuery.ToListAsync(cancellationToken);
        var events = await eventsQuery.ToListAsync(cancellationToken);

        var cards = new List<Card>();

        var groups = events
            .OrderBy(item => item.At)
            .ThenBy(item => item.Seq)
            .ThenBy(item => item.ReceivedAt)
            .GroupBy(item => (item.ProjectId, item.TaskId));

        foreach (var group in groups)
        {
            cards.AddRange(ToCards(group.Key.ProjectId, group.Key.TaskId, [.. group], runs, now));
        }

        var lanes = new BoardLanesDto(
            Lane(cards, "running"),
            Lane(cards, "needsYou"),
            Lane(cards, "done"),
            Lane(cards, "failed"));

        return (new BoardDto(lanes), null);
    }

    private static IReadOnlyList<BoardCardDto> Lane(List<Card> cards, string lane)
    {
        return cards
            .Where(card => card.Lane == lane)
            .OrderByDescending(card => card.LastAt)
            .Select(card => card.Dto)
            .ToList();
    }

    /// <summary>Plan 0013 D12: one card per run instance. The events of one task id are split at each
    /// <c>run.created</c>, and a run row goes to the newest instance that had started when it was created —
    /// so a closed run and the new run reusing its id keep a card each.</summary>
    private static List<Card> ToCards(
        string projectId,
        string taskId,
        List<RunEventEntity> events,
        List<RunEntity> runs,
        DateTime now)
    {
        var instances = Instances(events);
        MatchRuns(instances, runs, projectId, taskId);

        return [.. instances.Select(instance => ToCard(projectId, taskId, instance, now))];
    }

    /// <summary>A new instance starts at every <c>run.created</c> but the first one of the current instance;
    /// events the window cut from before that first one stay with it, as the run's own history.</summary>
    private static List<RunInstance> Instances(List<RunEventEntity> events)
    {
        var instances = new List<RunInstance>();
        var opened = false;

        foreach (var item in events)
        {
            if (instances.Count == 0)
            {
                instances.Add(new RunInstance());
            }
            else if (item.Kind == EventKinds.RunCreated && opened)
            {
                instances.Add(new RunInstance());
                opened = false;
            }

            if (item.Kind == EventKinds.RunCreated)
            {
                opened = true;
            }

            instances[^1].Add(item);
        }

        return instances;
    }

    /// <summary>A run row is matched to at most one instance, and an instance keeps the newest run that
    /// belongs to it.</summary>
    private static void MatchRuns(
        List<RunInstance> instances,
        List<RunEntity> runs,
        string projectId,
        string taskId)
    {
        var candidates = runs
            .Where(run => run.ProjectId == projectId && run.TaskId == taskId)
            .OrderBy(run => run.CreatedAt)
            .ToList();

        foreach (var run in candidates)
        {
            var instance = instances[NewestInstanceBefore(instances, run.CreatedAt)];

            if (instance.Run is null || instance.Run.CreatedAt <= run.CreatedAt)
            {
                instance.Run = run;
            }
        }
    }

    /// <summary>The newest instance that had started by <paramref name="at"/>; the first one when the run
    /// predates every event the board still has (its <c>run.created</c> fell out of the window).</summary>
    private static int NewestInstanceBefore(List<RunInstance> instances, DateTime at)
    {
        for (var index = instances.Count - 1; index > 0; index--)
        {
            if (instances[index].StartedAt <= at)
            {
                return index;
            }
        }

        return 0;
    }

    private static Card ToCard(string projectId, string taskId, RunInstance instance, DateTime now)
    {
        var events = instance.Events;
        var run = instance.Run;
        var first = events[0];
        var last = events[^1];

        // D12: a run.closed counts only when it comes after the instance's own run.created. An event from
        // before that is the run's earlier history (the window cut its run.created), not this end.
        var created = events.FindIndex(item => item.Kind == EventKinds.RunCreated);
        var closedByEvent = events
            .Select((item, index) => (item, index))
            .Any(row => row.item.Kind == EventKinds.RunClosed && (created < 0 || row.index > created));
        var closed = closedByEvent || (run?.ClosedAt is not null && run.CreatedAt >= first.At);

        var status = run?.Status ?? string.Empty;
        string? historyVerdict = null;
        string? verifyVerdict = null;
        EventData? progress = null;
        EventData? pool = null;
        string? poolKind = null;
        // D12: one chip per repo of the plan, from the last status/progress event that names one.
        var repos = new List<BoardRepoDto>();

        foreach (var item in events)
        {
            var data = EventData.Read(item.DataJson);

            if ((item.Kind == EventKinds.Status || item.Kind == EventKinds.Progress)
                && data.Text("repo") is { } chipRepo)
            {
                var chip = item.Kind == EventKinds.Status
                    ? data.Text("to")
                    : data.Text("phase") ?? data.Text("state");
                var existing = repos.FindIndex(entry => string.Equals(entry.Name, chipRepo, StringComparison.Ordinal));

                if (existing >= 0)
                {
                    repos[existing] = new BoardRepoDto(chipRepo, chip);
                }
                else
                {
                    repos.Add(new BoardRepoDto(chipRepo, chip));
                }
            }

            switch (item.Kind)
            {
                case EventKinds.RunCreated:
                    status = data.Text("status") ?? status;
                    break;

                case EventKinds.Status:
                    status = data.Text("to") ?? status;
                    break;

                case EventKinds.Progress:
                    progress = data;
                    break;

                case EventKinds.History:
                    historyVerdict = data.Text("verdict") ?? historyVerdict;

                    if (data.Text("event") == "verify")
                    {
                        verifyVerdict = data.Text("verdict");
                    }

                    break;

                case EventKinds.PoolWaiting:
                case EventKinds.PoolAcquired:
                case EventKinds.PoolReleased:
                    pool = data;
                    poolKind = item.Kind;
                    break;
            }
        }

        var startedAt = run is not null && run.CreatedAt != default ? run.CreatedAt : first.At;
        var elapsed = (long)Math.Max(0, (last.At - startedAt).TotalSeconds);

        var (number, title) = PlanName.From(run?.Task, instance.Branch, taskId);

        // Plan 0013 §6.3.1: gate 2 and gate 2.5 read "In review" on the card. The engine keeps `Planning`
        // for the whole review (its own state machine depends on that), so the label comes from the hop the
        // progress event names: a plan-reviewer or a read-back hop is a review in flight.
        var label = status == "Planning" && progress?.Text("role") is "plan-reviewer" or "readback"
            ? "Reviewing"
            : status;

        var card = new BoardCardDto(
            projectId,
            run?.RunKey,
            taskId,
            label,
            run?.PlanNumber ?? number,
            run?.PlanTitle ?? title,
            progress?.Text("phase") ?? progress?.Text("state"),
            progress?.Text("model"),
            startedAt,
            elapsed,
            progress?.Text("lastLine"),
            WaitingFor(status, historyVerdict, verifyVerdict, progress, poolKind),
            progress?.Text("pool") ?? pool?.Text("pool"),
            progress?.Text("heldBy") ?? pool?.Text("heldBy"),
            repos);

        return new Card(card, LaneOf(status, card.WaitingFor, last.At, closed, now), last.At);
    }

    /// <summary>Why the owner is needed (plan §6.2). <c>pool</c> is a wait, not a question, so it never
    /// puts a card in <c>needsYou</c>.</summary>
    private static string? WaitingFor(
        string status,
        string? historyVerdict,
        string? verifyVerdict,
        EventData? progress,
        string? poolKind)
    {
        if (status == "AwaitingApproval")
        {
            return "approval";
        }

        if (status == "NeedsInput" || historyVerdict == "NEED_INPUT")
        {
            return "needInput";
        }

        if (verifyVerdict is "FAIL" or "Fail" or "Failed")
        {
            return "verifyFail";
        }

        if (progress?.Text("state") == "inbox")
        {
            return "manual";
        }

        return poolKind == EventKinds.PoolWaiting ? "pool" : null;
    }

    private static string? LaneOf(string status, string? waitingFor, DateTime lastAt, bool closed, DateTime now)
    {
        if (status is "VerifyFailed" or "Violation")
        {
            return "failed";
        }

        if (closed || status == "Verified")
        {
            return lastAt >= now - DoneWindow ? "done" : null;
        }

        if (waitingFor is "approval" or "manual" or "needInput" or "verifyFail"
            && lastAt >= now - NeedsYouWindow)
        {
            return "needsYou";
        }

        return lastAt >= now - RunningWindow ? "running" : null;
    }

    /// <summary>One run's events. <see cref="Branch"/> is the first one that names a branch: the run does
    /// not have one while it plans (§6.3.3 keeps reading the branch).</summary>
    private sealed class RunInstance
    {
        public List<RunEventEntity> Events { get; } = [];

        public DateTime StartedAt => Events.Count == 0 ? DateTime.MinValue : Events[0].At;

        public string? Branch =>
            Events.Select(item => item.Branch).FirstOrDefault(branch => !string.IsNullOrEmpty(branch));

        public RunEntity? Run { get; set; }

        public void Add(RunEventEntity item) => Events.Add(item);
    }

    private sealed record Card(BoardCardDto Dto, string? Lane, DateTime LastAt);
}
