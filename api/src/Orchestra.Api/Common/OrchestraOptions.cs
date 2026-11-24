namespace Orchestra.Api.Common;

public sealed class OrchestraOptions
{
    public const string SectionName = "Orchestra";

    /// <summary>Plan 0012 D3: empty means <c>%LOCALAPPDATA%\Orchestra\orchestra.db</c> — the app's own
    /// folder. A relative value resolves against the content root, which is how development keeps
    /// <c>data/orchestra.db</c>.</summary>
    public string DatabasePath { get; init; } = string.Empty;

    /// <summary>Where profile exports live (plan 0006 D5). A relative value sits beside the database —
    /// <c>&lt;data&gt;/profiles</c> — so the profiles follow the database wherever it is.</summary>
    public string ProfilesPath { get; init; } = "profiles";

    public string UiPath { get; init; } = "wwwroot";

    public string? SourceTimeZone { get; init; }

    public bool ImportOnStart { get; init; } = true;

    /// <summary>The bearer token <c>POST api/events</c> accepts, and nothing else (plan 0004 D1). When it
    /// is unset the dashboard makes and keeps one (plan 0009 D6): the reporter is always on.</summary>
    public string? ReportToken { get; init; }

    /// <summary>Plan 0009 D5: the folders the browser may list in a direct install. Empty means every
    /// fixed drive — acceptable only on loopback until plan 0008 (D16).</summary>
    public List<string> BrowseRoots { get; init; } = [];

    /// <summary>Plan 0012 D12: the engine folder, when it is not the one found by walking up from the
    /// content root. A developer editing the engine sets this so the published app does not repoint
    /// existing projects to its own copy.</summary>
    public string? EnginePath { get; init; }

    public List<ProjectOptions> Projects { get; init; } = [];

    public string ResolveDatabasePath(string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Orchestra");

            return Path.GetFullPath(Path.Combine(local, "orchestra.db"));
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, DatabasePath));
    }

    public string ResolveUiPath(string contentRootPath)
    {
        return Path.GetFullPath(Path.Combine(contentRootPath, UiPath));
    }

    /// <summary>A relative ProfilesPath resolves against the database file's folder, not the content
    /// root: the app's data folder is the one place its own files live.</summary>
    public string ResolveProfilesPath(string contentRootPath)
    {
        if (Path.IsPathRooted(ProfilesPath))
        {
            return Path.GetFullPath(ProfilesPath);
        }

        var database = ResolveDatabasePath(contentRootPath);
        var folder = Path.GetDirectoryName(database) ?? contentRootPath;

        return Path.GetFullPath(Path.Combine(folder, ProfilesPath));
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
