using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Modules.Projects.Features.CheckFolder;

public sealed record FolderCheckItemDto(string Name, string Kind, bool Found, bool InEachRepo = false);

public sealed record RepoKnowledgeDto(string Repo, bool Found);

/// <summary>Plan 0009 D4/D7: the check never fails, it answers <c>visible: false</c>; the paths it
/// echoes back are in host form, and <c>isReady</c> is the config-file rule.</summary>
public sealed record FolderCheckDto(
    bool Exists,
    bool IsAbsolute,
    IReadOnlyList<FolderCheckItemDto> Items,
    IReadOnlyList<string> Repos,
    string SuggestedRunsPath,
    IReadOnlyList<RepoKnowledgeDto> RepoKnowledge,
    string HostPath,
    string SuggestedName,
    bool IsReady,
    bool Visible);

/// <summary>Existence only — never a file's content, never a write (plan 0003 §0 rule 2, §6.2).</summary>
public static class FolderCheck
{
    private const int ReposMax = 20;

    private static readonly (string Name, string Kind)[] KnownItems =
    [
        ("AGENTS.md", "file"),
        ("CLAUDE.md", "file"),
        (".claude/", "folder"),
        (".cursor/", "folder"),
        (".cursorrules", "file"),
        (".github/copilot-instructions.md", "file"),
        (".ai/", "folder"),
        (".orchestra/", "folder"),
        ("orchestrator/", "folder"),
    ];

    /// <summary>Plan 0009 D7: ready means the config file exists, in either layout.</summary>
    public static bool IsReady(string serverFolder)
    {
        return File.Exists(Path.Combine(serverFolder, ".orchestra", "orchestrator.json"))
            || File.Exists(Path.Combine(serverFolder, "orchestrator", "orchestrator.json"));
    }

    public static bool IsRepo(string serverFolder)
    {
        return Directory.Exists(Path.Combine(serverFolder, ".git"))
            || File.Exists(Path.Combine(serverFolder, ".git"));
    }

    public static FolderCheckDto Run(string? rootPath, PathMapper? mapper = null)
    {
        mapper ??= PathMapper.Empty;

        var typed = (rootPath ?? string.Empty).Trim();
        var server = mapper.ToServer(typed);
        var isAbsolute = PathMapper.LooksAbsolute(typed);

        if (typed.Length == 0 || !Path.IsPathRooted(server))
        {
            return Invisible(isAbsolute, mapper.ToHost(typed), SuggestedName(typed));
        }

        string full;

        try
        {
            full = Path.GetFullPath(server);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Invisible(isAbsolute, mapper.ToHost(typed), SuggestedName(typed));
        }

        if (!Directory.Exists(full))
        {
            return new FolderCheckDto(
                Exists: false,
                IsAbsolute: isAbsolute,
                Items: [.. KnownItems.Select(item => new FolderCheckItemDto(item.Name, item.Kind, Found: false))],
                Repos: [],
                SuggestedRunsPath: mapper.ToHost(Path.Combine(full, ".orchestra", "runs")),
                RepoKnowledge: [],
                HostPath: mapper.ToHost(full),
                SuggestedName: SuggestedName(typed),
                IsReady: false,
                Visible: true);
        }

        var repos = Repos(full);

        var knowledge = repos
            .Select(repo => new RepoKnowledgeDto(repo, Directory.Exists(KnowledgeFolder(full, repo))))
            .ToList();

        var rootKnowledge = Directory.Exists(Path.Combine(full, ".ai"));
        var eachRepoKnowledge = repos.Count > 0
            && repos.All(repo => Directory.Exists(KnowledgeFolder(full, repo)));

        var rootAgents = File.Exists(Path.Combine(full, "AGENTS.md"));
        var eachRepoAgents = repos.Count > 0
            && repos.All(repo => File.Exists(Path.Combine(full, repo == "." ? "AGENTS.md" : Path.Combine(repo, "AGENTS.md"))));

        var items = KnownItems
            .Select(item => item.Name switch
            {
                ".ai/" => new FolderCheckItemDto(
                    item.Name,
                    item.Kind,
                    rootKnowledge || eachRepoKnowledge,
                    InEachRepo: !rootKnowledge && eachRepoKnowledge),
                "AGENTS.md" => new FolderCheckItemDto(
                    item.Name,
                    item.Kind,
                    rootAgents || eachRepoAgents,
                    InEachRepo: !rootAgents && eachRepoAgents),
                _ => new FolderCheckItemDto(item.Name, item.Kind, Exists(full, item.Name, item.Kind)),
            })
            .ToList();

        var orchestratorRuns = Path.Combine(full, "orchestrator", "runs");
        var suggested = Directory.Exists(orchestratorRuns)
            ? orchestratorRuns
            : Path.Combine(full, ".orchestra", "runs");

        return new FolderCheckDto(
            Exists: true,
            IsAbsolute: isAbsolute,
            Items: items,
            Repos: repos,
            SuggestedRunsPath: mapper.ToHost(suggested),
            RepoKnowledge: knowledge,
            HostPath: mapper.ToHost(full),
            SuggestedName: SuggestedName(typed),
            IsReady: IsReady(full),
            Visible: true);
    }

    private static FolderCheckDto Invisible(bool isAbsolute, string hostPath, string suggestedName)
    {
        return new FolderCheckDto(
            Exists: false,
            IsAbsolute: isAbsolute,
            Items: [.. KnownItems.Select(item => new FolderCheckItemDto(item.Name, item.Kind, Found: false))],
            Repos: [],
            SuggestedRunsPath: string.Empty,
            RepoKnowledge: [],
            HostPath: hostPath,
            SuggestedName: suggestedName,
            IsReady: false,
            Visible: false);
    }

    private static string SuggestedName(string typed)
    {
        var normalized = PathMapper.Normalize(typed);

        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var separator = normalized.LastIndexOf('/');

        return separator < 0 ? normalized : normalized[(separator + 1)..];
    }

    private static bool Exists(string full, string name, string kind)
    {
        var target = Path.Combine(full, name.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar));

        return kind == "folder" ? Directory.Exists(target) : File.Exists(target);
    }

    /// <summary><c>.git</c> in the root means the root is the repo; otherwise every direct subfolder that
    /// holds <c>.git</c>, sorted, at most 20.</summary>
    private static List<string> Repos(string full)
    {
        if (Directory.Exists(Path.Combine(full, ".git")))
        {
            return ["."];
        }

        try
        {
            return [.. Directory.EnumerateDirectories(full)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Where(folder => Directory.Exists(Path.Combine(folder, ".git")) || File.Exists(Path.Combine(folder, ".git")))
                .Select(folder => Path.GetFileName(folder))
                .Take(ReposMax)];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string KnowledgeFolder(string full, string repo)
    {
        return repo == "."
            ? Path.Combine(full, ".ai")
            : Path.Combine(full, repo, ".ai");
    }
}
