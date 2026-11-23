namespace Orchestra.Api.Common.Hosting;

/// <summary>Plan 0009 D2: a pure translation between the paths the owner sees (Windows, the
/// <c>Host</c> side) and the paths this process can open (inside the container, the <c>Server</c> side).
/// An empty map — a direct install, IIS — is the identity in both directions.</summary>
public sealed class PathMapper
{
    private readonly (string Host, string Server)[] _byHost;
    private readonly (string Host, string Server)[] _byServer;

    public static PathMapper Empty { get; } = new([]);

    public PathMapper(IEnumerable<PathMapEntry>? entries)
    {
        var valid = (entries ?? [])
            .Select(entry => (Host: Normalize(entry.Host), Server: Normalize(entry.Server)))
            .Where(entry => entry.Host.Length > 0 && entry.Server.Length > 0)
            .ToArray();

        _byHost = [.. valid.OrderByDescending(entry => entry.Host.Length)];
        _byServer = [.. valid.OrderByDescending(entry => entry.Server.Length)];
    }

    public IReadOnlyList<PathMapEntry> Entries
        => [.. _byHost.Select(entry => new PathMapEntry { Host = entry.Host, Server = entry.Server })];

    /// <summary>The longest <c>Host</c> that is a whole-segment prefix wins; no match returns the
    /// typed value unchanged, so a server path that was already correct is never rewritten.</summary>
    public string ToServer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var typed = path.Trim();
        var value = Normalize(typed);

        foreach (var (host, server) in _byHost)
        {
            if (Covers(value, host))
            {
                return server + value[host.Length..];
            }
        }

        return typed;
    }

    /// <summary>The inverse, case-sensitive on <c>Server</c>. A Windows host comes back with
    /// backslashes, the way the owner types it.</summary>
    public string ToHost(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var typed = path.Trim();
        var value = Normalize(typed);

        foreach (var (host, server) in _byServer)
        {
            if (!Covers(value, server, ignoreCase: false))
            {
                continue;
            }

            var combined = host + value[server.Length..];

            return host.Contains(':') ? combined.Replace('/', '\\') : combined;
        }

        return typed;
    }

    /// <summary>Absolute as the owner means it, whatever this process runs on: a drive path
    /// (<c>F:\x</c>, <c>F:/x</c>), a UNC path, or a rooted path of this OS. <c>Path.IsPathRooted</c> alone
    /// says false for <c>F:\x</c> inside a Linux container.</summary>
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
    internal static string Normalize(string? path)
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
