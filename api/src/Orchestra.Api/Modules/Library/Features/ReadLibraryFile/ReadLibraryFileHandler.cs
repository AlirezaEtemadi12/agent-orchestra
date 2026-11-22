using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Library.Indexing;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Library.Features.ReadLibraryFile;

/// <summary>What the controller needs to stream a checked file; the handler never opens the stream
/// itself, so a refusal is a plain result.</summary>
public sealed record LibraryFile(
    string FullPath,
    string ContentType,
    string? ContentSecurityPolicy,
    string? ContentDisposition);

/// <summary>Plan 0002 §6.4: only an indexed item of that project is servable, only while it is still
/// inside the root with no link below it, and never over 20 MB.</summary>
public sealed class ReadLibraryFileHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public const long MaxBytes = 20L * 1024 * 1024;

    /// <summary>The approved sandbox: scripts allowed, no network, no forms, no base, only Google
    /// Fonts and the scripts of cdnjs, jsDelivr and unpkg (D6).</summary>
    public const string HtmlPolicy =
        "sandbox allow-scripts; default-src 'none'; "
        + "script-src 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://unpkg.com; "
        + "style-src 'unsafe-inline' https://fonts.googleapis.com; "
        + "font-src https://fonts.gstatic.com; "
        + "img-src data: blob:; media-src data: blob:; "
        + "connect-src 'none'; form-action 'none'; base-uri 'none'; frame-src 'none'";

    public const string NonePolicy = "default-src 'none'";

    public async Task<(LibraryFile? File, Error? Error)> HandleAsync(
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
            .FirstOrDefaultAsync(
                row => row.ProjectId == project && row.Category == parsed && row.RelPath == path,
                cancellationToken);

        if (item is null)
        {
            return (null, Error.LibraryItemNotFound);
        }

        var root = registry.Find(project)?.ResolveRootPath();

        if (string.IsNullOrWhiteSpace(root))
        {
            return (null, Error.LibraryItemNotFound);
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(Path.Combine(root, item.RelPath.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return (null, Error.LibraryItemNotFound);
        }

        // Checked again now: a file replaced by a link after indexing is not served (D10).
        if (!SafePath.IsInside(root, fullPath) || !File.Exists(fullPath))
        {
            return (null, Error.LibraryItemNotFound);
        }

        var length = new FileInfo(fullPath).Length;

        if (length > MaxBytes)
        {
            return (null, Error.LibraryFileTooLarge);
        }

        return (Describe(fullPath), null);
    }

    private static LibraryFile Describe(string fullPath)
    {
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();

        return extension switch
        {
            ".html" => new LibraryFile(fullPath, "text/html; charset=utf-8", HtmlPolicy, null),
            ".md" => new LibraryFile(fullPath, "text/markdown; charset=utf-8", NonePolicy, null),
            ".png" => new LibraryFile(fullPath, "image/png", NonePolicy, null),
            ".jpg" or ".jpeg" => new LibraryFile(fullPath, "image/jpeg", NonePolicy, null),
            ".webp" => new LibraryFile(fullPath, "image/webp", NonePolicy, null),
            ".gif" => new LibraryFile(fullPath, "image/gif", NonePolicy, null),
            _ => new LibraryFile(fullPath, "application/octet-stream", NonePolicy, "attachment"),
        };
    }
}
