using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Modules.Setup.Features.Folders;

public sealed record FolderEntryDto(string Name, string Path, bool IsReady, bool IsRepo);

public sealed record FolderListDto(string? Path, string? Parent, IReadOnlyList<FolderEntryDto> Folders);

/// <summary>Plan 0009 D5: folder names only, inside the browse roots, never files, dot-folders
/// skipped, unreadable folders left out, at most 500, sorted case-insensitively.</summary>
public sealed class FoldersHandler(HostingInfo hosting)
{
    private const int MaxFolders = 500;

    public (FolderListDto? Page, Error? Error) Handle(string? path)
    {
        var mapper = hosting.Mapper;
        var roots = hosting.BrowseRoots();

        if (string.IsNullOrWhiteSpace(path))
        {
            return (new FolderListDto(
                null,
                null,
                [.. roots.Select(root => Entry(RootName(root), root, mapper))]), null);
        }

        // Resolve ".." before the root check, and compare in one form (forward slashes): a direct
        // install keeps Windows backslashes, a container map produces forward slashes.
        var server = Full(mapper.ToServer(path));

        if (server is null)
        {
            return (null, Error.FolderOutsideRoots);
        }

        var value = PathMapper.Normalize(server);
        var serverRoots = roots
            .Select(root => Full(mapper.ToServer(root)))
            .OfType<string>()
            .Select(root => PathMapper.Normalize(root))
            .ToList();
        var isRoot = serverRoots.Exists(root => string.Equals(root, value, StringComparison.OrdinalIgnoreCase));

        if (!serverRoots.Exists(root => PathMapper.Covers(value, root)))
        {
            return (null, Error.FolderOutsideRoots);
        }

        if (!Directory.Exists(server))
        {
            return (null, Error.FolderMissing);
        }

        var folders = new List<FolderEntryDto>();

        try
        {
            var names = Directory.EnumerateDirectories(server)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(MaxFolders);

            foreach (var name in names)
            {
                folders.Add(Entry(name!, mapper.ToHost(Path.Combine(server, name!)), mapper));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A folder that cannot be read is shown empty; its parents stay navigable.
        }

        return (new FolderListDto(mapper.ToHost(server), isRoot ? null : Parent(server, mapper), folders), null);
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

    private static FolderEntryDto Entry(string name, string hostPath, PathMapper mapper)
    {
        var server = mapper.ToServer(hostPath);

        return new FolderEntryDto(name, hostPath, FolderCheck.IsReady(server), FolderCheck.IsRepo(server));
    }

    private static string? Parent(string server, PathMapper mapper)
    {
        var parent = Path.GetDirectoryName(server);

        return string.IsNullOrEmpty(parent) ? null : mapper.ToHost(parent);
    }

    private static string RootName(string root)
    {
        var trimmed = PathMapper.Normalize(root);
        var separator = trimmed.LastIndexOf('/');
        var name = separator < 0 ? trimmed : trimmed[(separator + 1)..];

        // A drive root (C:/) has no last segment: show the root itself.
        return name.Length == 0 ? root : name;
    }
}
