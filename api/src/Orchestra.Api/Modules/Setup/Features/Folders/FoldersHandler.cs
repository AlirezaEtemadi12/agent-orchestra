using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Modules.Setup.Features.Folders;

public sealed record FolderEntryDto(string Name, string Path, bool IsReady, bool IsRepo);

public sealed record FolderListDto(string? Path, string? Parent, IReadOnlyList<FolderEntryDto> Folders);

/// <summary>Plan 0009 D5, plan 0012 D5: folder names only, inside the browse roots, never files,
/// dot-folders skipped, unreadable folders left out, at most 500, sorted case-insensitively. Paths are
/// used as typed — the folder browser and the dashboard see the same disk.</summary>
public sealed class FoldersHandler(IOptions<OrchestraOptions> options)
{
    private const int MaxFolders = 500;

    public (FolderListDto? Page, Error? Error) Handle(string? path)
    {
        var roots = BrowseRoots();

        if (string.IsNullOrWhiteSpace(path))
        {
            return (new FolderListDto(null, null, [.. roots.Select(root => Entry(RootName(root), root))]), null);
        }

        var full = Full(path);

        if (full is null)
        {
            return (null, Error.FolderOutsideRoots);
        }

        var value = LocalPaths.Normalize(full);
        var normalizedRoots = roots.Select(LocalPaths.Normalize).ToList();
        var isRoot = normalizedRoots.Exists(root => string.Equals(root, value, StringComparison.OrdinalIgnoreCase));

        if (!normalizedRoots.Exists(root => LocalPaths.Covers(value, root)))
        {
            return (null, Error.FolderOutsideRoots);
        }

        if (!Directory.Exists(full))
        {
            return (null, Error.FolderMissing);
        }

        var folders = new List<FolderEntryDto>();

        try
        {
            var names = Directory.EnumerateDirectories(full)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(MaxFolders);

            foreach (var name in names)
            {
                folders.Add(Entry(name!, Path.Combine(full, name!)));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A folder that cannot be read is shown empty; its parents stay navigable.
        }

        return (new FolderListDto(full, isRoot ? null : Path.GetDirectoryName(full), folders), null);
    }

    /// <summary><c>Orchestra:BrowseRoots</c>, else every fixed drive — acceptable only because the app
    /// listens on loopback (plan 0012 D13).</summary>
    private IReadOnlyList<string> BrowseRoots()
    {
        var configured = options.Value.BrowseRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .ToList();

        if (configured.Count > 0)
        {
            return configured;
        }

        try
        {
            return
            [
                .. DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
                    .Select(drive => drive.RootDirectory.FullName),
            ];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The absolute form with ".." resolved, or null for a path that is not rooted or not valid.</summary>
    private static string? Full(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static FolderEntryDto Entry(string name, string fullPath)
    {
        return new FolderEntryDto(name, fullPath, FolderCheck.IsReady(fullPath), FolderCheck.IsRepo(fullPath));
    }

    private static string RootName(string root)
    {
        var trimmed = LocalPaths.Normalize(root);
        var separator = trimmed.LastIndexOf('/');
        var name = separator < 0 ? trimmed : trimmed[(separator + 1)..];

        // A drive root (C:/) has no last segment: show the root itself.
        return name.Length == 0 ? root : name;
    }
}
