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
/// nothing has reported in 24 hours is not running; a card is shown only while its lane's window holds.</summary>
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
            .GroupBy(item => (item.ProjectId, item.TaskId, Branch: item.Branch ?? string.Empty));

        foreach (var group in groups)
        {
            cards.Add(ToCard(group.Key.ProjectId, group.Key.TaskId, [.. group], runs, now));
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

    private static Card ToCard(
        string projectId,
        string taskId,
        List<RunEventEntity> events,
        List<RunEntity> runs,
        DateTime now)
    {
        var run = MatchRun(runs, projectId, taskId, events[0].Branch);
        var first = events[0];
        var last = events[^1];
        var closed = run?.ClosedAt is not null;

        var status = run?.Status ?? string.Empty;
        string? historyVerdict = null;
        string? verifyVerdict = null;
        EventData? progress = null;
        EventData? pool = null;
        string? poolKind = null;

        foreach (var item in events)
        {
            var data = EventData.Read(item.DataJson);

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

                case EventKinds.RunClosed:
                    closed = true;
                    break;
            }
        }

        var startedAt = run is not null && run.CreatedAt != default ? run.CreatedAt : first.At;
        var elapsed = (long)Math.Max(0, (last.At - startedAt).TotalSeconds);

        var (number, title) = PlanName.From(run?.Task, events[0].Branch);

        var card = new BoardCardDto(
            projectId,
            run?.RunKey,
            taskId,
            status,
            run?.PlanNumber ?? number,
            run?.PlanTitle ?? title,
            progress?.Text("phase") ?? progress?.Text("state"),
            progress?.Text("model"),
            startedAt,
            elapsed,
            progress?.Text("lastLine"),
            WaitingFor(status, historyVerdict, verifyVerdict, progress, poolKind),
            progress?.Text("pool") ?? pool?.Text("pool"),
            progress?.Text("heldBy") ?? pool?.Text("heldBy"));

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

    /// <summary>D7: the same task id on the same branch; a run with no branch takes any branch, and the
    /// newest run wins when several match.</summary>
    private static RunEntity? MatchRun(
        List<RunEntity> runs,
        string projectId,
        string taskId,
        string? branch)
    {
        var candidates = runs
            .Where(run => run.ProjectId == projectId && run.TaskId == taskId)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(branch))
        {
            var exact = candidates.LastOrDefault(run =>
                string.Equals(run.Branch, branch, StringComparison.Ordinal));

            if (exact is not null)
            {
                return exact;
            }
        }

        return candidates.LastOrDefault(run => string.IsNullOrEmpty(run.Branch))
            ?? candidates.OrderByDescending(run => run.UpdatedAt ?? run.CreatedAt).First();
    }

    private sealed record Card(BoardCardDto Dto, string? Lane, DateTime LastAt);
}
