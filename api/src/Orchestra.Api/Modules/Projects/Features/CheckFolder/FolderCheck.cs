namespace Orchestra.Api.Modules.Projects.Features.CheckFolder;

public sealed record FolderCheckItemDto(string Name, string Kind, bool Found);

public sealed record RepoKnowledgeDto(string Repo, bool Found);

public sealed record FolderCheckDto(
    bool Exists,
    bool IsAbsolute,
    IReadOnlyList<FolderCheckItemDto> Items,
    IReadOnlyList<string> Repos,
    string SuggestedRunsPath,
    IReadOnlyList<RepoKnowledgeDto> RepoKnowledge);

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

    public static FolderCheckDto Run(string? rootPath)
    {
        var path = (rootPath ?? string.Empty).Trim();

        if (path.Length == 0 || !Path.IsPathRooted(path))
        {
            return Missing(isAbsolute: false);
        }

        string full;

        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Missing(isAbsolute: true);
        }

        if (!Directory.Exists(full))
        {
            return new FolderCheckDto(
                Exists: false,
                IsAbsolute: true,
                Items: [.. KnownItems.Select(item => new FolderCheckItemDto(item.Name, item.Kind, Found: false))],
                Repos: [],
                SuggestedRunsPath: Path.Combine(full, ".orchestra", "runs"),
                RepoKnowledge: []);
        }

        var items = KnownItems
            .Select(item => new FolderCheckItemDto(item.Name, item.Kind, Exists(full, item.Name, item.Kind)))
            .ToList();

        var repos = Repos(full);

        var orchestratorRuns = Path.Combine(full, "orchestrator", "runs");
        var suggested = Directory.Exists(orchestratorRuns)
            ? orchestratorRuns
            : Path.Combine(full, ".orchestra", "runs");

        var knowledge = repos
            .Select(repo => new RepoKnowledgeDto(repo, Directory.Exists(KnowledgeFolder(full, repo))))
            .ToList();

        return new FolderCheckDto(true, true, items, repos, suggested, knowledge);
    }

    private static FolderCheckDto Missing(bool isAbsolute)
    {
        return new FolderCheckDto(
            Exists: false,
            IsAbsolute: isAbsolute,
            Items: [.. KnownItems.Select(item => new FolderCheckItemDto(item.Name, item.Kind, Found: false))],
            Repos: [],
            SuggestedRunsPath: Path.Combine(".orchestra", "runs"),
            RepoKnowledge: []);
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
