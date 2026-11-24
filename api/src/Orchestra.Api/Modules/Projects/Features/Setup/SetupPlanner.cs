using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Plan 0012 §6.3.2: reads the file system and answers the checklist. It never writes.</summary>
public sealed class SetupPlanner(
    IOptions<OrchestraOptions> options,
    IWebHostEnvironment environment,
    EngineLocator engine,
    TemplateSet templates,
    ReportTokenStore tokens)
{
    public const string GitInit = "git-init";

    public const string ReportBlock = "report-block";

    public const string LocalJson = "local-json";

    public const string OrchestratorJson = "orchestra/orchestrator.json";

    public SetupPlanDto Plan(SetupRequest request, string root)
    {
        var repos = NormalizeRepos(request.Repos);
        var single = repos.Count == 1 && repos[0] == ".";
        var items = new List<SetupItemDto>();

        // 1. git init only in a workspace whose root is not itself a repo (D8).
        if (!IsRepo(root))
        {
            var git = Path.Combine(root, ".orchestra", ".git");

            items.Add(new SetupItemDto(
                GitInit,
                ".orchestra/.git",
                Directory.Exists(git) ? SetupActions.Exists : SetupActions.Create,
                false,
                null));
        }

        // 2. Every template file, plus one plans folder per repo in the multi-repo shape.
        foreach (var file in templates.Files)
        {
            items.Add(FileItem(root, Path.Combine(".orchestra", file).Replace('\\', '/'), file, false));
        }

        if (!single)
        {
            foreach (var repo in repos)
            {
                var relative = $".orchestra/plans/{repo}/.gitkeep";

                items.Add(FileItem(root, relative, $"plans/{repo}/.gitkeep", false));
            }
        }

        // 3. orchestrator.json: create it, or add only the report block to an existing one.
        var configPath = Path.Combine(root, ".orchestra", "orchestrator.json");

        if (!File.Exists(configPath))
        {
            items.Add(new SetupItemDto(OrchestratorJson, ".orchestra/orchestrator.json", SetupActions.Create, false, null));
        }
        else
        {
            items.Add(new SetupItemDto(OrchestratorJson, ".orchestra/orchestrator.json", SetupActions.Exists, false, null));

            if (!HasReportBlock(configPath))
            {
                items.Add(new SetupItemDto(
                    ReportBlock,
                    ".orchestra/orchestrator.json",
                    SetupActions.Add,
                    false,
                    "report"));
            }
        }

        // 4. orchestrator.local.json: three keys this machine owns.
        items.Add(LocalJsonItem(root));

        // 5. Knowledge: the root in a single repo, each repo otherwise. Optional and ticked by default.
        if (single)
        {
            items.Add(FileItem(root, "AGENTS.md", "knowledge/AGENTS.md", true));
            items.Add(FileItem(root, ".ai/README.md", "knowledge/.ai/README.md", true));
        }
        else
        {
            foreach (var repo in repos)
            {
                items.Add(FileItem(root, $"{repo}/AGENTS.md", $"knowledge/{repo}/AGENTS.md", true));
                items.Add(FileItem(root, $"{repo}/.ai/README.md", $"knowledge/{repo}/.ai/README.md", true));
            }
        }

        return new SetupPlanDto(
            engine.Find() is not null && templates.IsAvailable,
            single,
            items,
            VerifyCommands(request, root, repos, single));
    }

    /// <summary>The engine's rules, so a plan and an apply read the same file the same way.</summary>
    public static (JsonObject? Value, string? Error) ReadLocalJson(string path)
    {
        if (!File.Exists(path))
        {
            return (new JsonObject(), null);
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(path));

            return node is JsonObject value
                ? (value, null)
                : (null, "The file is not a JSON object.");
        }
        catch (JsonException exception)
        {
            return (null, exception.Message);
        }
    }

    public static bool HasTopLevelReport(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("report", out _);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool HasReportBlock(string path)
    {
        return HasTopLevelReport(path);
    }

    private SetupItemDto LocalJsonItem(string root)
    {
        var path = Path.Combine(root, ".orchestra", "orchestrator.local.json");
        var desired = DesiredLocalJson();

        if (!File.Exists(path))
        {
            return new SetupItemDto(
                LocalJson,
                ".orchestra/orchestrator.local.json",
                SetupActions.Create,
                false,
                string.Join(", ", desired.Select(pair => pair.Key)));
        }

        var (existing, error) = ReadLocalJson(path);

        if (existing is null || error is not null)
        {
            return new SetupItemDto(LocalJson, ".orchestra/orchestrator.local.json", SetupActions.Update, false, null);
        }

        var changed = desired
            .Where(pair => AsString(existing[pair.Key]) != pair.Value)
            .Select(pair => pair.Key)
            .ToList();

        return new SetupItemDto(
            LocalJson,
            ".orchestra/orchestrator.local.json",
            changed.Count == 0 ? SetupActions.Exists : SetupActions.Update,
            false,
            changed.Count == 0 ? null : string.Join(", ", changed));
    }

    /// <summary>The three values this machine owns; the token's value never appears in a DTO.</summary>
    public List<KeyValuePair<string, string>> DesiredLocalJson()
    {
        var engineRoot = engine.Find() ?? string.Empty;

        return
        [
            new("enginePath", engineRoot),
            new("profilesPath", options.Value.ResolveProfilesPath(environment.ContentRootPath)),
            new("reportToken", tokens.Get()),
        ];
    }

    private static string? AsString(JsonNode? node)
    {
        try
        {
            return node?.GetValue<string>();
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    private static SetupItemDto FileItem(string root, string relative, string changeId, bool optional)
    {
        var exists = File.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

        return new SetupItemDto(changeId, relative, exists ? SetupActions.Exists : SetupActions.Create, optional, null);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> VerifyCommands(
        SetupRequest request,
        string root,
        IReadOnlyList<string> repos,
        bool single)
    {
        if (request.VerifyCommands is { Count: > 0 } given)
        {
            return given.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)[.. pair.Value],
                StringComparer.Ordinal);
        }

        var detected = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var repo in repos)
        {
            // The single-repo shape keys repos and commands by the project's name (plan 0012 §6.3.4).
            var folder = single ? root : Path.Combine(root, repo);

            detected[single ? request.Name : repo] = VerifyCommandDetector.Detect(folder);
        }

        return detected;
    }

    public static List<string> NormalizeRepos(IReadOnlyList<string>? repos)
    {
        var list = (repos ?? [])
            .Select(repo => (repo ?? string.Empty).Trim())
            .Where(repo => repo.Length > 0)
            // A project saved before FolderCheck left it out may list .orchestra itself (a repo after D8);
            // it is the orchestrator's folder, not a repo to plan for.
            .Where(repo => !string.Equals(repo, ".orchestra", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return list.Count == 0 ? ["."] : list;
    }

    private static bool IsRepo(string root)
    {
        return Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git"));
    }
}
