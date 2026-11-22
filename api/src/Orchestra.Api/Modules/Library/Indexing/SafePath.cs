namespace Orchestra.Api.Modules.Library.Indexing;

/// <summary>D10: a path is inside the root only when its text is under the root <b>and</b> no existing
/// segment below the root is a symbolic link or junction — <c>GetFullPath</c> alone does not resolve
/// those. Used at indexing time and again on every file request.</summary>
public static class SafePath
{
    private static StringComparison Comparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static bool IsInside(string root, string fullPath)
    {
        string rootFull;
        string pathFull;

        try
        {
            rootFull = Path.GetFullPath(root);
            pathFull = Path.GetFullPath(fullPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        var trimmedRoot = Path.TrimEndingDirectorySeparator(rootFull);
        var prefix = trimmedRoot + Path.DirectorySeparatorChar;

        if (!pathFull.StartsWith(prefix, Comparison))
        {
            return false;
        }

        var rest = pathFull[prefix.Length..];
        var segments = rest.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        var current = trimmedRoot;

        for (var index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);

            FileSystemInfo info = index == segments.Length - 1
                ? new FileInfo(current)
                : new DirectoryInfo(current);

            string? linkTarget;

            try
            {
                linkTarget = info.LinkTarget;
            }
            catch (IOException)
            {
                return false;
            }

            if (linkTarget is not null)
            {
                return false;
            }

            if (!info.Exists)
            {
                // Nothing below a missing segment exists, so nothing below it can be a link.
                return true;
            }

            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return false;
            }
        }

        return true;
    }
}
