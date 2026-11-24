namespace Orchestra.Api.Common.Hosting;

/// <summary>Plan 0012 D5: the dashboard and the engine run on the same machine, so a path is used as
/// typed. These three helpers are what the folder browser and setup still need.</summary>
public static class LocalPaths
{
    /// <summary>Absolute as the owner means it, whatever this process runs on: a drive path
    /// (<c>F:\x</c>, <c>F:/x</c>), a UNC path, or a rooted path of this OS.</summary>
    public static bool LooksAbsolute(string? path)
    {
        var value = (path ?? string.Empty).Trim();

        return value.Length > 0
            && (Path.IsPathRooted(value)
                || (value.Length >= 3 && char.IsAsciiLetter(value[0]) && value[1] == ':' && value[2] is '\\' or '/')
                || value.StartsWith(@"\\", StringComparison.Ordinal));
    }

    /// <summary>Whole segments only: <c>F:/Proj</c> does not cover <c>F:/Projects</c>. Both sides must be
    /// <see cref="Normalize"/>d; a root that keeps its slash (<c>F:/</c>, <c>/</c>) covers everything under it.</summary>
    public static bool Covers(string value, string root, bool ignoreCase = true)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (value.Equals(root, comparison))
        {
            return true;
        }

        if (root.Length == 0 || !value.StartsWith(root, comparison))
        {
            return false;
        }

        return root[^1] == '/' || (value.Length > root.Length && value[root.Length] == '/');
    }

    /// <summary>Trim, <c>\</c> to <c>/</c>, and drop a trailing slash — except on a drive root like
    /// <c>F:/</c>, where the slash is part of the path.</summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var value = path.Trim().Replace('\\', '/');

        if (value.Length > 3 && value[^1] == '/')
        {
            value = value.TrimEnd('/');
        }

        return value;
    }
}
