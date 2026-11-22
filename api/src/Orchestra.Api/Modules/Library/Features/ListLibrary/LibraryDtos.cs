namespace Orchestra.Api.Modules.Library.Features.ListLibrary;

/// <summary>Every field of the indexed item except its size, plus the project's name, the run's status
/// and an idea's linked plan's title (plan 0002 §6.3).</summary>
public sealed record LibraryItemDto(
    string ProjectId,
    string ProjectName,
    string Category,
    string RelPath,
    string Title,
    string? Number,
    string Slug,
    string? Repo,
    string? Status,
    DateTime? CreatedOn,
    DateTime? UpdatedOn,
    DateTime? DoneOn,
    string? PlannedBy,
    string? PreviewRelPath,
    string? LinkedPlanNumber,
    string? RunKey,
    string? Group,
    DateTime ModifiedAt,
    string? RunStatus,
    string? LinkedPlanTitle);

public sealed record LibraryRunDto(string RunKey, string Status, DateTime? UpdatedAt);

/// <summary>The item, its preview URL (only when the preview is indexed), every run of a plan and the
/// plan's evidence.</summary>
public sealed record LibraryItemDetailDto(
    LibraryItemDto Item,
    string? PreviewUrl,
    IReadOnlyList<LibraryRunDto> Runs,
    IReadOnlyList<LibraryItemDto> Evidence);

public static class LibraryDtos
{
    public static LibraryItemDto ToDto(
        LibraryItemEntity item,
        IReadOnlyDictionary<(string ProjectId, string RunKey), string> runStatus,
        IReadOnlyList<LibraryItemEntity> plans)
    {
        var status = item.RunKey is null
            ? null
            : runStatus.GetValueOrDefault((item.ProjectId, item.RunKey));

        string? linkedPlanTitle = null;

        if (item.Category == LibraryCategory.Idea && item.LinkedPlanNumber is not null)
        {
            var linked = plans
                .Where(plan => string.Equals(plan.Number, item.LinkedPlanNumber, StringComparison.Ordinal)
                    && string.Equals(plan.ProjectId, item.ProjectId, StringComparison.Ordinal))
                .OrderBy(plan => plan.RelPath, StringComparer.Ordinal)
                .FirstOrDefault();

            linkedPlanTitle = linked?.Title;
        }

        return new LibraryItemDto(
            item.ProjectId,
            item.Project?.Name ?? item.ProjectId,
            item.Category.ToString(),
            item.RelPath,
            item.Title,
            item.Number,
            item.Slug,
            item.Repo,
            item.Status,
            item.CreatedOn,
            item.UpdatedOn,
            item.DoneOn,
            item.PlannedBy,
            item.PreviewRelPath,
            item.LinkedPlanNumber,
            item.RunKey,
            item.Group,
            item.ModifiedAt,
            status,
            linkedPlanTitle);
    }

    public static string? PreviewUrl(LibraryItemEntity item)
    {
        return item.PreviewRelPath is null
            ? null
            : $"library/{Uri.EscapeDataString(item.ProjectId)}/preview/file"
                + $"?path={Uri.EscapeDataString(item.PreviewRelPath)}";
    }
}
