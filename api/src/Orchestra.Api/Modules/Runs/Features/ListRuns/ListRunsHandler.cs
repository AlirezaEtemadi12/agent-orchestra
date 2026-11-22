using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Runs.Features.ListRuns;

public sealed class ListRunsHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public async Task<(IReadOnlyList<RunListItemDto>? Runs, Error? Error)> HandleAsync(
        string? project,
        string? status,
        string? repo,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(project) && registry.Find(project) is null)
        {
            return (null, Error.ProjectNotFound);
        }

        var query = database.Runs
            .AsNoTracking()
            .Include(run => run.Sessions)
            .Include(run => run.Stages)
            .Include(run => run.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            query = query.Where(run => run.ProjectId == project);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var wanted = status.ToLowerInvariant();
            query = query.Where(run => run.Status.ToLower() == wanted);
        }

        if (!string.IsNullOrWhiteSpace(repo))
        {
            var wanted = repo.ToLowerInvariant();
            query = query.Where(run => run.Repo != null && run.Repo.ToLower() == wanted);
        }

        var runs = await query
            .OrderByDescending(run => run.UpdatedAt)
            .ThenBy(run => run.RunKey)
            .ToListAsync(cancellationToken);

        var result = runs.Select(run => new RunListItemDto(
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
            run.Sessions.Count,
            run.Sessions.Sum(session => (long?)session.Calls) ?? 0,
            run.Sessions.Sum(session => (long?)session.Requests) ?? 0,
            ToUsd(run.CostMicros),
            run.Rounds,
            run.Stages
                .Where(stage => stage.IsFinal && stage.Model is not null)
                .Select(stage => stage.Model!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(model => model, StringComparer.Ordinal)
                .ToList(),
            run.UpdatedAt,
            run.ClosedAt,
            run.Source.ToString())).ToList();

        return (result, null);
    }

    internal static decimal? ToUsd(long? micros)
    {
        return micros is null ? null : micros.Value / 1_000_000m;
    }
}
