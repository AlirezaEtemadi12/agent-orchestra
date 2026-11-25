using Gridify;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Library.Features.ListLibrary;

/// <summary>Plan 0010 D12: one category at a time, paged and sorted on the server; the page's run
/// statuses and linked plan titles are looked up for the page only.</summary>
public sealed class LibraryTableHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public async Task<(Paging<LibraryItemDto>? Page, Error? Error)> HandleAsync(
        string? project,
        string? category,
        GridifyQuery query,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(project) && registry.Find(project) is null)
        {
            return (null, Error.ProjectNotFound);
        }

        if (!LibraryCategoryNames.TryParse(category, out var wanted))
        {
            return (null, Error.LibraryItemNotFound);
        }

        var source = database.LibraryItems
            .AsNoTracking()
            .Where(item => item.Category == wanted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            source = source.Where(item => item.ProjectId == project);
        }

        var (paging, error) = TableMapper<LibraryItemEntity, LibraryItemDto>.Query(
            source, query, "updatedOn desc", "projectId, relPath", LibraryTableMapping.Mapper);

        if (error is not null || paging is null)
        {
            return (null, error);
        }

        var items = await paging.Query
            .Include(item => item.Project)
            .ToListAsync(cancellationToken);

        var projects = items.Select(item => item.ProjectId).Distinct(StringComparer.Ordinal).ToList();
        var runKeys = items
            .Where(item => item.RunKey is not null)
            .Select(item => item.RunKey!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var runStatus = runKeys.Count == 0
            ? new Dictionary<(string ProjectId, string RunKey), string>()
            : (await database.Runs
                .AsNoTracking()
                .Where(run => projects.Contains(run.ProjectId) && runKeys.Contains(run.RunKey))
                .Select(run => new { run.ProjectId, run.RunKey, run.Status })
                .ToListAsync(cancellationToken))
                .ToDictionary(run => (run.ProjectId, run.RunKey), run => run.Status);

        var linkedNumbers = items
            .Where(item => item.Category == LibraryCategory.Idea && item.LinkedPlanNumber is not null)
            .Select(item => item.LinkedPlanNumber!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var plans = linkedNumbers.Count == 0
            ? []
            : await database.LibraryItems
                .AsNoTracking()
                .Where(item => item.Category == LibraryCategory.Plan
                    && projects.Contains(item.ProjectId)
                    && item.Number != null
                    && linkedNumbers.Contains(item.Number))
                .ToListAsync(cancellationToken);

        var rows = items
            .Select(item => LibraryDtos.ToDto(item, runStatus, plans))
            .ToList();

        return (new Paging<LibraryItemDto>(paging.Count, rows), null);
    }
}
