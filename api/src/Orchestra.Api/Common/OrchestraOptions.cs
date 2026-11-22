namespace Orchestra.Api.Common;

public sealed class OrchestraOptions
{
    public const string SectionName = "Orchestra";

    public string DatabasePath { get; init; } = "data/orchestra.db";

    public string UiPath { get; init; } = "wwwroot";

    public string? SourceTimeZone { get; init; }

    public bool ImportOnStart { get; init; } = true;

    /// <summary>The bearer token <c>POST api/events</c> accepts, and nothing else (plan 0004 D1). Unset
    /// means the reporter is off and the route answers 404 (D2).</summary>
    public string? ReportToken { get; init; }

    public List<ProjectOptions> Projects { get; init; } = [];

    public string ResolveDatabasePath(string contentRootPath)
    {
        return Path.GetFullPath(Path.Combine(contentRootPath, DatabasePath));
    }

    public string ResolveUiPath(string contentRootPath)
    {
        return Path.GetFullPath(Path.Combine(contentRootPath, UiPath));
    }
}

public sealed class ProjectOptions
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string RunsPath { get; init; } = string.Empty;

    /// <summary>The project folder the library globs are relative to. Optional: derived from a
    /// <c>orchestrator/runs</c> or <c>.orchestra/runs</c> RunsPath when it is not set.</summary>
    public string? RootPath { get; init; }

    /// <summary>The git repositories inside the root; empty when the project does not name any
    /// (plan 0003 D9).</summary>
    public List<string> Repos { get; init; } = [];

    public LibraryOptions Library { get; init; } = new();

    /// <summary>The configured root, else the one derived from RunsPath, else null. Never a filesystem
    /// root — the indexer refuses those.</summary>
    public string? ResolveRootPath()
    {
        return string.IsNullOrWhiteSpace(RootPath) ? DeriveRootPath(RunsPath) : RootPath;
    }

    /// <summary>Only <c>&lt;root&gt;/orchestrator/runs</c> and <c>&lt;root&gt;/.orchestra/runs</c> imply a
    /// root (the folder above the orchestrator folder). Any other RunsPath — a Docker mount at
    /// <c>/projects/p1</c>, a <c>&lt;root&gt;/runs</c> — implies none, because the derived root would be
    /// wider than the mount and the library would read outside it.</summary>
    public static string? DeriveRootPath(string runsPath)
    {
        if (string.IsNullOrWhiteSpace(runsPath))
        {
            return null;
        }

        var trimmed = runsPath.TrimEnd('/', '\\');
        var segments = trimmed.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            return null;
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        var orchestrator = segments[^2];
        var runs = segments[^1];

        var known = orchestrator.Equals("orchestrator", comparison)
            || orchestrator.Equals(".orchestra", comparison);

        if (!known || !runs.Equals("runs", comparison))
        {
            return null;
        }

        var lastSeparator = trimmed.LastIndexOfAny(['/', '\\']);
        var above = trimmed[..lastSeparator];
        var separatorAbove = above.LastIndexOfAny(['/', '\\']);

        return separatorAbove < 0 ? null : above[..(separatorAbove + 1)];
    }
}

/// <summary>Per-category globs, relative to <see cref="ProjectOptions.RootPath"/>. A configured list
/// replaces the default for that category.</summary>
public sealed class LibraryOptions
{
    public List<string>? Plans { get; init; }

    public List<string>? Previews { get; init; }

    public List<string>? Ideas { get; init; }

    public List<string>? Research { get; init; }

    public List<string>? Evidence { get; init; }
}
