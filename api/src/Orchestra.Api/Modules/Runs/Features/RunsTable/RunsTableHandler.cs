using Gridify;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Runs.Features.RunsTable;

/// <summary>Plan 0010 D6: the runs table. The row DTO is the allow-list; the computed fields are
/// enriched for the page only.</summary>
public sealed class RunsTableHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public async Task<(Paging<RunListItemDto>? Page, Error? Error)> HandleAsync(
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

        var (paging, error) = TableMapper<RunEntity, RunListItemDto>.Query(
            source, query, "updatedAt desc", "projectId, runKey", RunsTableMapping.Mapper);

        if (error is not null || paging is null)
        {
            return (null, error);
        }

        var rows = await paging.Query
            .Select(run => new RunRow(
                run.Id,
                run.ProjectId,
                run.Project.Name,
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
                run.CostMicros,
                run.Rounds,
                run.UpdatedAt,
                run.ClosedAt,
                run.Source))
            .ToListAsync(cancellationToken);

        var ids = rows.Select(row => row.Id).ToList();
        var models = await database.StageResults
            .AsNoTracking()
            .Where(stage => ids.Contains(stage.RunId) && stage.IsFinal && stage.Model != null)
            .Select(stage => new { stage.RunId, stage.Model })
            .ToListAsync(cancellationToken);

        var modelsByRun = models
            .GroupBy(stage => stage.RunId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(stage => stage.Model!)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(model => model, StringComparer.Ordinal)
                    .ToList());

        var items = rows
            .Select(row => new RunListItemDto(
                row.ProjectId,
                row.ProjectName,
                row.RunKey,
                row.TaskId,
                row.Repo,
                row.Status,
                row.PlanNumber,
                row.PlanTitle,
                row.Task,
                row.Branch,
                row.Sessions,
                row.Calls,
                row.Requests,
                RunDtos.ToUsd(row.CostMicros),
                row.Rounds,
                modelsByRun.GetValueOrDefault(row.Id, []),
                row.UpdatedAt,
                row.ClosedAt,
                row.Source.ToString()))
            .ToList();

        return (new Paging<RunListItemDto>(paging.Count, items), null);
    }

    private sealed record RunRow(
        Guid Id,
        string ProjectId,
        string ProjectName,
        string RunKey,
        string TaskId,
        string? Repo,
        string Status,
        string? PlanNumber,
        string? PlanTitle,
        string? Task,
        string? Branch,
        int Sessions,
        long Calls,
        long Requests,
        long? CostMicros,
        int? Rounds,
        DateTime? UpdatedAt,
        DateTime? ClosedAt,
        RunSource Source);
}
