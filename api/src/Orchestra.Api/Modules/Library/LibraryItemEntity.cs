using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Library;

/// <summary>One indexed work product. Metadata only — the content is read from disk on request
/// (plan 0002 D2).</summary>
public sealed class LibraryItemEntity
{
    public Guid Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public LibraryCategory Category { get; set; }

    /// <summary>Forward slashes, relative to the project's root.</summary>
    public string RelPath { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>The <c>NNNN</c> of a plan — from the title, else from the file name.</summary>
    public string? Number { get; set; }

    /// <summary>File name without its extension and without a leading <c>NNNN-</c>.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Plans only: the folder the glob's first whole-segment <c>*</c> matched (D11).</summary>
    public string? Repo { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public DateTime? DoneOn { get; set; }

    public string? PlannedBy { get; set; }

    public string? PreviewRelPath { get; set; }

    public string? LinkedPlanNumber { get; set; }

    /// <summary>Plans only: the newest run that matches this plan (D5).</summary>
    public string? RunKey { get; set; }

    /// <summary>Evidence only: <c>NNNN-topic/phase-X</c>, or the first folder below the glob's prefix.</summary>
    public string? Group { get; set; }

    public long SizeBytes { get; set; }

    public DateTime ModifiedAt { get; set; }

    public ProjectEntity Project { get; set; } = null!;
}
