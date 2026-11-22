namespace Orchestra.Api.Modules.Library.Indexing;

/// <summary>Library globs: <c>*</c> is one segment, <c>**</c> is any depth (zero or more), and the match
/// is case-insensitive on Windows only. <c>README.md</c> and <c>TEMPLATE.md</c> are never items.</summary>
public static class GlobMatcher
{
    private static readonly string[] NeverItems = ["README.md", "TEMPLATE.md"];

    private static StringComparison Comparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static bool Matches(string pattern, string relPath)
    {
        var segments = Segments(relPath);

        if (segments.Length > 0 && NeverItems.Any(name => name.Equals(segments[^1], Comparison)))
        {
            return false;
        }

        return MatchFrom(Segments(pattern), 0, segments, 0).Ok;
    }

    /// <summary>The path segment matched by the glob's first whole-segment <c>*</c> — the repo folder
    /// (D11). Null when the glob has no such segment, when the segment is the file name, when the file
    /// does not match, or when the matched segment is <c>done</c> (any case).</summary>
    public static string? RepoOf(string pattern, string relPath)
    {
        if (!Matches(pattern, relPath))
        {
            return null;
        }

        var captured = MatchFrom(Segments(pattern), 0, Segments(relPath), 0).Captured;

        return captured is not null && "done".Equals(captured, Comparison) ? null : captured;
    }

    /// <summary>The segments before the first one carrying a wildcard — where the walk starts.</summary>
    public static string FixedPrefix(string pattern)
    {
        var fixedSegments = new List<string>();

        foreach (var segment in Segments(pattern))
        {
            if (segment.Contains('*') || segment.Contains('?'))
            {
                break;
            }

            fixedSegments.Add(segment);
        }

        return string.Join('/', fixedSegments);
    }

    private static (bool Ok, string? Captured) MatchFrom(string[] pattern, int patternIndex, string[] path, int pathIndex)
    {
        if (patternIndex == pattern.Length)
        {
            return (pathIndex == path.Length, null);
        }

        var segment = pattern[patternIndex];

        if (segment == "**")
        {
            for (var take = pathIndex; take <= path.Length; take++)
            {
                var (ok, captured) = MatchFrom(pattern, patternIndex + 1, path, take);

                if (ok)
                {
                    return (true, captured);
                }
            }

            return (false, null);
        }

        if (pathIndex == path.Length)
        {
            return (false, null);
        }

        if (segment == "*")
        {
            var (ok, captured) = MatchFrom(pattern, patternIndex + 1, path, pathIndex + 1);

            if (!ok)
            {
                return (false, null);
            }

            // A whole * is a folder only when something follows it; as the last segment it is a file name.
            return patternIndex < pattern.Length - 1 && captured is null
                ? (true, path[pathIndex])
                : (true, captured);
        }

        if (!SegmentMatches(segment, path[pathIndex]))
        {
            return (false, null);
        }

        return MatchFrom(pattern, patternIndex + 1, path, pathIndex + 1);
    }

    private static bool SegmentMatches(string pattern, string value)
    {
        var parts = pattern.Split('*');

        if (parts.Length == 1)
        {
            return pattern.Equals(value, Comparison);
        }

        var position = 0;

        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];

            if (part.Length == 0)
            {
                continue;
            }

            if (index == 0)
            {
                if (!value.AsSpan().StartsWith(part, Comparison))
                {
                    return false;
                }

                position = part.Length;
                continue;
            }

            var found = value.IndexOf(part, position, Comparison);

            if (found < 0)
            {
                return false;
            }

            position = found + part.Length;
        }

        // A trailing * allows any remainder; without one the value must end at the last part.
        return pattern.EndsWith('*') || position == value.Length;
    }

    private static string[] Segments(string path)
    {
        return path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}
