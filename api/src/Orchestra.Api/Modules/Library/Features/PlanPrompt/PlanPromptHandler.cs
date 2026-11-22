using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Library.Features.PlanPrompt;

/// <summary>The approved mock's copy-prompt text, filled server-side (plan 0002 §6.5, D8).</summary>
public sealed class PlanPromptHandler(OrchestraDbContext database)
{
    public async Task<(string? Text, Error? Error)> HandleAsync(
        string project,
        string path,
        CancellationToken cancellationToken)
    {
        var plan = await database.LibraryItems
            .AsNoTracking()
            .Include(item => item.Project)
            .FirstOrDefaultAsync(
                item => item.ProjectId == project
                    && item.Category == LibraryCategory.Plan
                    && item.RelPath == path,
                cancellationToken);

        if (plan is null)
        {
            return (null, Error.LibraryItemNotFound);
        }

        var repo = string.IsNullOrWhiteSpace(plan.Repo)
            ? plan.Project?.Name ?? plan.ProjectId
            : plan.Repo;

        var status = string.IsNullOrWhiteSpace(plan.Status) ? "Unknown" : plan.Status;

        var text =
            $"Run plan {plan.RelPath}\n"
            + $"through the orchestrator (repo: {repo}).\n"
            + $"Status: {status}. Preview: {PreviewFolder(plan.PreviewRelPath)}\n"
            + "Read the plan header first; decide nothing new.\n";

        return (text, null);
    }

    /// <summary><c>.orchestra/previews/0001-x/index.html</c> → <c>.orchestra/previews/0001-x/</c>;
    /// a null preview → <c>none</c>; anything else as it is.</summary>
    public static string PreviewFolder(string? previewRelPath)
    {
        if (string.IsNullOrWhiteSpace(previewRelPath))
        {
            return "none";
        }

        const string index = "index.html";

        return previewRelPath.EndsWith(index, StringComparison.OrdinalIgnoreCase)
            ? previewRelPath[..^index.Length]
            : previewRelPath;
    }
}
