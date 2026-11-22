using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Library.Indexing;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Library.Features.ListLibrary;

public sealed class ListLibraryHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    private static readonly StringComparer Text = StringComparer.Ordinal;

    public async Task<(IReadOnlyList<LibraryItemDto>? Items, Error? Error)> HandleAsync(
        string? project,
        string? category,
        string? status,
        string? sort,
        string? dir,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(project) && registry.Find(project) is null)
        {
            return (null, Error.ProjectNotFound);
        }

        LibraryCategory? wanted = null;

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!LibraryCategoryNames.TryParse(category, out var parsed))
            {
                return (null, Error.LibraryItemNotFound);
            }

            wanted = parsed;
        }

        var query = database.LibraryItems
            .AsNoTracking()
            .Include(item => item.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            query = query.Where(item => item.ProjectId == project);
        }

        var items = await query.ToListAsync(cancellationToken);

        var runs = await database.Runs
            .AsNoTracking()
            .Where(run => string.IsNullOrWhiteSpace(project) || run.ProjectId == project)
            .Select(run => new { run.ProjectId, run.RunKey, run.Status })
            .ToListAsync(cancellationToken);

        var runStatus = runs.ToDictionary(
            run => (run.ProjectId, run.RunKey),
            run => run.Status);

        var plans = items.Where(item => item.Category == LibraryCategory.Plan).ToList();

        var rows = items
            .Where(item => wanted is null || item.Category == wanted)
            .Select(item => LibraryDtos.ToDto(item, runStatus, plans))
            .ToList();

        // Status is a plan and an idea field; a status filter never hides anything else.
        if (!string.IsNullOrWhiteSpace(status))
        {
            var wantedStatus = status.Trim();

            rows = rows.Where(row =>
                row.Category is nameof(LibraryCategory.Plan) or nameof(LibraryCategory.Idea)
                    ? string.Equals(row.Status, wantedStatus, StringComparison.OrdinalIgnoreCase)
                    : true).ToList();
        }

        var descending = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

        return (Sort(rows, sort, descending).ToList(), null);
    }

    private static IOrderedEnumerable<LibraryItemDto> Sort(
        IReadOnlyList<LibraryItemDto> rows,
        string? sort,
        bool descending)
    {
        return (sort ?? "updated").Trim().ToLowerInvariant() switch
        {
            "number" => NullsLast(rows, row => row.Number, descending),
            "title" => texts(rows, row => row.Title, descending),
            "project" => texts(rows, row => row.ProjectName, descending),
            "status" => NullsLast(rows, row => row.Status, descending),
            "created" => dates(rows, row => row.CreatedOn, descending),
            "done" => dates(rows, row => row.DoneOn, descending),
            "run" => runs(rows, descending),
            _ => dates(rows, row => row.UpdatedOn, descending),
        };

        static IOrderedEnumerable<LibraryItemDto> texts(
            IReadOnlyList<LibraryItemDto> rows,
            Func<LibraryItemDto, string> selector,
            bool descending)
        {
            var ordered = descending
                ? rows.OrderByDescending(selector, Text)
                : rows.OrderBy(selector, Text);

            return ordered.ThenBy(row => row.RelPath, Text);
        }

        // Nulls last in both directions (plan 0002 §6.3).
        static IOrderedEnumerable<LibraryItemDto> NullsLast(
            IReadOnlyList<LibraryItemDto> rows,
            Func<LibraryItemDto, string?> selector,
            bool descending)
        {
            var ordered = rows.OrderBy(row => selector(row) is null ? 1 : 0);

            var value = descending
                ? ordered.ThenByDescending(selector, Text)
                : ordered.ThenBy(selector, Text);

            return value.ThenBy(row => row.RelPath, Text);
        }

        static IOrderedEnumerable<LibraryItemDto> dates(
            IReadOnlyList<LibraryItemDto> rows,
            Func<LibraryItemDto, DateTime?> selector,
            bool descending)
        {
            var ordered = rows.OrderBy(row => selector(row) is null ? 1 : 0);

            var value = descending
                ? ordered.ThenByDescending(selector)
                : ordered.ThenBy(selector);

            return value.ThenBy(row => row.RelPath, Text);
        }

        // `n` is compared as a number, so .10 beats .9 (D5).
        static IOrderedEnumerable<LibraryItemDto> runs(IReadOnlyList<LibraryItemDto> rows, bool descending)
        {
            var ordered = rows.OrderBy(row => row.RunKey is null ? 1 : 0);

            var byTask = descending
                ? ordered.ThenByDescending(row => TaskId(row.RunKey), Text)
                : ordered.ThenBy(row => TaskId(row.RunKey), Text);

            return descending
                ? byTask.ThenByDescending(row => PlanRunLinker.RunNumber(row.RunKey ?? string.Empty))
                : byTask.ThenBy(row => PlanRunLinker.RunNumber(row.RunKey ?? string.Empty));
        }
    }

    private static string TaskId(string? runKey)
    {
        if (runKey is null)
        {
            return string.Empty;
        }

        var dot = runKey.LastIndexOf('.');

        return dot < 0 ? runKey : runKey[..dot];
    }
}
