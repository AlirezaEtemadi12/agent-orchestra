using Gridify;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Runs.Features.RunsTable;

/// <summary>The KPI cards and the two dropdowns read this over the whole filtered set. Plan 0010 D7.</summary>
public sealed record RunsSummaryDto(
    int Runs,
    int Sessions,
    long Calls,
    decimal CostUsd,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> Repos);

public sealed class RunsSummaryHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public async Task<(RunsSummaryDto? Summary, Error? Error)> HandleAsync(
        string? project,
        GridifyQuery query,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(project) && registry.Find(project) is null)
        {
            return (null, Error.ProjectNotFound);
        }

        var source = database.Runs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            source = source.Where(run => run.ProjectId == project);
        }

        TableQueryDefaults.Apply(query, "updatedAt desc", "projectId, runKey");

        if (!query.IsValid(RunsTableMapping.Mapper))
        {
            return (null, Error.TableQueryInvalid);
        }

        var filtered = source.ApplyFiltering(query, RunsTableMapping.Mapper);

        var runs = await filtered.CountAsync(cancellationToken);
        var sessions = await filtered.SelectMany(run => run.Sessions).CountAsync(cancellationToken);
        var calls = await filtered.SelectMany(run => run.Sessions)
            .SumAsync(session => (long?)session.Calls, cancellationToken) ?? 0;
        var costMicros = await filtered.SumAsync(run => (long?)run.CostMicros, cancellationToken) ?? 0;

        // The dropdowns never empty themselves: they see the project's values, not the filtered ones.
        var statuses = (await source.Select(run => run.Status).Distinct().ToListAsync(cancellationToken))
            .OrderBy(status => status, StringComparer.Ordinal)
            .ToList();
        var repos = (await source.Where(run => run.Repo != null).Select(run => run.Repo!).Distinct().ToListAsync(cancellationToken))
            .OrderBy(repo => repo, StringComparer.Ordinal)
            .ToList();

        return (new RunsSummaryDto(runs, sessions, calls, costMicros / 1_000_000m, statuses, repos), null);
    }
}
