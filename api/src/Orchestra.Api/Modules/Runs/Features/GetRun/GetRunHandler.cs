using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Indexing;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Modules.Runs.Features.ListRuns;

namespace Orchestra.Api.Modules.Runs.Features.GetRun;

public sealed class GetRunHandler(OrchestraDbContext database, TimeProvider clock)
{
    public async Task<(RunDetailDto? Run, Error? Error)> HandleAsync(
        string project,
        string runKey,
        CancellationToken cancellationToken)
    {
        var run = await database.Runs
            .AsNoTracking()
            .Include(item => item.Project)
            .Include(item => item.Sessions)
            .Include(item => item.Stages)
            .Include(item => item.StageUsage)
            .Include(item => item.Texts)
            .FirstOrDefaultAsync(item => item.ProjectId == project && item.RunKey == runKey, cancellationToken);

        if (run is null)
        {
            return (null, Error.RunNotFound);
        }

        var stages = Order(run.Stages.Where(stage => stage.IsFinal));
        var rounds = run.Stages
            .Where(stage => !stage.IsFinal)
            .OrderBy(stage => stage.At)
            .ThenBy(stage => stage.Round)
            .Select(ToDto)
            .ToList();

        var sessions = run.Sessions
            .OrderBy(session => session.SessionId)
            .Select(session => new SessionUsageDto(
                session.SessionId,
                session.FirstRole,
                session.Model,
                session.Calls,
                session.Requests,
                session.Input,
                session.Output,
                session.Reasoning,
                session.CacheRead,
                ListRunsHandler.ToUsd(session.CostMicros)))
            .ToList();

        var stageUsage = run.StageUsage
            .OrderBy(usage => usage.Stage)
            .ThenBy(usage => usage.Model)
            .Select(usage => new StageUsageDto(
                usage.Stage,
                usage.Model,
                usage.Sessions,
                usage.Calls,
                usage.Requests,
                usage.Input,
                usage.Output,
                usage.Reasoning,
                usage.CacheRead,
                usage.Seconds,
                ListRunsHandler.ToUsd(usage.CostMicros)))
            .ToList();

        var texts = run.Texts
            .OrderBy(text => text.Round)
            .ThenBy(text => text.Name)
            .Select(text => new StageTextDto(text.Stage, text.Round, text.Name, text.Text))
            .ToList();

        var detail = new RunDetailDto(
            run.ProjectId,
            run.Project?.Name ?? run.ProjectId,
            run.RunKey,
            run.TaskId,
            run.Repo,
            run.Status,
            run.PlanNumber,
            run.PlanTitle,
            run.Task,
            run.Branch,
            run.PlanVersion,
            run.Sessions.Count,
            run.Sessions.Sum(session => (long?)session.Calls) ?? 0,
            run.Sessions.Sum(session => (long?)session.Requests) ?? 0,
            ListRunsHandler.ToUsd(run.CostMicros),
            run.Rounds,
            stages.Where(stage => stage.Model is not null).Select(stage => stage.Model!).Distinct(StringComparer.Ordinal).ToList(),
            run.CreatedAt,
            run.UpdatedAt,
            run.ClosedAt,
            run.Source.ToString(),
            stages.Select(ToDto).ToList(),
            rounds,
            sessions,
            stageUsage,
            texts,
            ListRunsHandler.ToUsd(run.CostMicros),
            await PlanOfAsync(run, cancellationToken),
            await LiveOfAsync(run, cancellationToken));

        return (detail, null);
    }

    /// <summary>The plan whose <c>RunKey</c> is this run; else the plans this run matches by D5 — a
    /// number match first, then the lowest path, so repeated calls answer the same (plan 0002 §6.3).</summary>
    private async Task<RunPlanDto?> PlanOfAsync(RunEntity run, CancellationToken cancellationToken)
    {
        var plans = await database.LibraryItems
            .AsNoTracking()
            .Where(item => item.ProjectId == run.ProjectId && item.Category == LibraryCategory.Plan)
            .ToListAsync(cancellationToken);

        var plan = plans.FirstOrDefault(item => item.RunKey == run.RunKey)
            ?? plans
                .Where(item => PlanRunLinker.Matches(item, run))
                .OrderByDescending(item => item.Number is null ? 0 : 1)
                .ThenBy(item => item.RelPath, StringComparer.Ordinal)
                .FirstOrDefault();

        return plan is null
            ? null
            : new RunPlanDto(plan.RelPath, plan.Number, plan.Title, plan.Status ?? "Unknown");
    }

    /// <summary>The run's newest <c>progress</c> event, when it is from the last 24 hours (plan 0004 D14).</summary>
    private async Task<RunLiveDto?> LiveOfAsync(RunEntity run, CancellationToken cancellationToken)
    {
        var since = clock.GetUtcNow().UtcDateTime.AddHours(-24);

        var item = await database.RunEvents
            .AsNoTracking()
            .Where(stored => stored.ProjectId == run.ProjectId
                && stored.TaskId == run.TaskId
                && stored.Kind == EventKinds.Progress
                && stored.At >= since)
            .Where(stored => string.IsNullOrEmpty(run.Branch)
                || string.IsNullOrEmpty(stored.Branch)
                || stored.Branch == run.Branch)
            .OrderByDescending(stored => stored.At)
            .ThenByDescending(stored => stored.Seq)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        var data = EventData.Read(item.DataJson);

        return new RunLiveDto(
            data.Text("phase") ?? data.Text("state"),
            data.Text("model"),
            data.Text("lastLine"),
            data.Int("lastOutputAgoSec"),
            data.Text("pool"),
            data.Text("heldBy"),
            item.At);
    }

    private static IEnumerable<StageResultEntity> Order(IEnumerable<StageResultEntity> stages)
    {
        return stages
            .OrderBy(stage => stage.Stage switch
            {
                StageKind.Review => 0,
                StageKind.ReadBack => 1,
                StageKind.Implement => 2,
                _ => 3,
            })
            .ThenBy(stage => stage.Phase)
            .ThenBy(stage => stage.Round);
    }

    private static StageResultDto ToDto(StageResultEntity stage)
    {
        return new StageResultDto(
            stage.Stage.ToString(),
            stage.Round,
            stage.Phase,
            stage.Adapter,
            stage.Model,
            stage.Verdict,
            stage.Blockers,
            stage.Should,
            stage.Nits,
            stage.Changed,
            stage.OutsidePlan,
            stage.Violations,
            stage.GatesGreen,
            stage.GatesNote,
            stage.ModelVerdict,
            stage.At);
    }
}
