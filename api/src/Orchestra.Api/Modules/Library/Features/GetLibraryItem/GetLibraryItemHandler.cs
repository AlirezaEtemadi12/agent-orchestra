using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Library.Features.ListLibrary;
using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Modules.Library.Features.GetLibraryItem;

public sealed class GetLibraryItemHandler(OrchestraDbContext database)
{
    public async Task<(LibraryItemDetailDto? Item, Error? Error)> HandleAsync(
        string project,
        string category,
        string path,
        CancellationToken cancellationToken)
    {
        if (!LibraryCategoryNames.TryParse(category, out var parsed))
        {
            return (null, Error.LibraryItemNotFound);
        }

        var item = await database.LibraryItems
            .AsNoTracking()
            .Include(row => row.Project)
            .FirstOrDefaultAsync(
                row => row.ProjectId == project && row.Category == parsed && row.RelPath == path,
                cancellationToken);

        if (item is null)
        {
            return (null, Error.LibraryItemNotFound);
        }

        var plans = await database.LibraryItems
            .AsNoTracking()
            .Where(row => row.ProjectId == project && row.Category == LibraryCategory.Plan)
            .ToListAsync(cancellationToken);

        var runs = parsed == LibraryCategory.Plan
            ? PlanRunLinker.MatchesOf(
                    item,
                    await database.Runs
                        .AsNoTracking()
                        .Where(run => run.ProjectId == project)
                        .ToListAsync(cancellationToken))
                .Select(run => new LibraryRunDto(run.RunKey, run.Status, run.UpdatedAt))
                .ToList()
            : [];

        var evidence = parsed == LibraryCategory.Plan
            ? await EvidenceOfAsync(item, plans, cancellationToken)
            : [];

        var detail = new LibraryItemDetailDto(
            LibraryDtos.ToDto(item, new Dictionary<(string, string), string>(), plans),
            LibraryDtos.PreviewUrl(item),
            runs,
            evidence);

        return (detail, null);
    }

    /// <summary>Evidence links to a plan when its group starts with the plan's number (or, for a
    /// numbered-less plan, its slug) and a dash (plan 0002 §6.2).</summary>
    private async Task<List<LibraryItemDto>> EvidenceOfAsync(
        LibraryItemEntity plan,
        IReadOnlyList<LibraryItemEntity> plans,
        CancellationToken cancellationToken)
    {
        var prefix = plan.Number ?? plan.Slug;

        if (prefix.Length == 0)
        {
            return [];
        }

        var rows = await database.LibraryItems
            .AsNoTracking()
            .Include(row => row.Project)
            .Where(row => row.ProjectId == plan.ProjectId && row.Category == LibraryCategory.Evidence)
            .ToListAsync(cancellationToken);

        var matching = rows
            .Where(row => row.Group is not null && row.Group.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.RelPath, StringComparer.Ordinal)
            .ToList();

        return matching
            .Select(row => LibraryDtos.ToDto(row, new Dictionary<(string, string), string>(), plans))
            .ToList();
    }
}
