using System.Text.Json;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

/// <summary>The prompt the owner hands to their own agent (plan 0003 §6.4). English on purpose: the
/// agent reads it. The page around it is localized (D4). Plan 0009 D7: it creates the config file the
/// engine reads — <c>.orchestra/orchestrator.json</c> — so a project that ran it is ready.</summary>
public static class ScaffoldPromptBuilder
{
    private const string NothingMissing = "Nothing to create: .ai/, AGENTS.md and .orchestra/orchestrator.json are already there.";

    private static readonly string[] InstructionFiles =
    [
        "CLAUDE.md",
        ".claude/",
        ".cursor/",
        ".cursorrules",
        ".github/copilot-instructions.md",
        "AGENTS.md",
    ];

    public static string Build(string name, string rootPath, IReadOnlyList<string> repos, FolderCheckDto check)
    {
        var found = check.Items
            .Where(item => item.Found)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);

        var hasAi = found.Contains(".ai/");
        var hasAgents = found.Contains("AGENTS.md");
        var hasOrchestraFolder = found.Contains(".orchestra/");
        var hasConfig = check.IsReady;

        if (hasAi && hasAgents && hasConfig)
        {
            return NothingMissing;
        }

        var lines = new List<string>
        {
            "This folder is being added to Orchestra, a dashboard for a gated multi-agent orchestrator.",
            $"Project: {name}. Root: {rootPath}. Repos: {(repos.Count == 0 ? "none" : string.Join(", ", repos))}.",
            "Work only inside this root. Show me the file tree you will create and wait for my yes before writing.",
        };

        var existing = InstructionFiles.Where(found.Contains).ToList();

        if (existing.Count > 0)
        {
            lines.Add(string.Empty);
            lines.AddRange(Section1(existing));
        }

        if (!hasAi)
        {
            lines.Add(string.Empty);
            lines.AddRange(Section2());

            var missing = check.RepoKnowledge
                .Where(repo => !repo.Found)
                .Select(repo => repo.Repo)
                .ToList();

            if (repos.Count > 1 && missing.Count > 0)
            {
                lines.Add($"   Each repo without .ai/ gets its own: {string.Join(", ", missing)}.");
            }
        }

        if (!hasAgents)
        {
            lines.Add(string.Empty);
            lines.AddRange(Section3());

            if (found.Contains("CLAUDE.md"))
            {
                lines.Add("Make CLAUDE.md one line that points to AGENTS.md.");
            }
        }

        if (!hasConfig)
        {
            lines.Add(string.Empty);
            lines.AddRange(Section4(OrchestratorJson(name, repos), hasOrchestraFolder));
        }

        lines.Add(string.Empty);
        lines.Add("5. Commit nothing. Tell me what you created and what you could not verify.");

        return string.Join('\n', lines);
    }

    private static string[] Section1(IReadOnlyList<string> existing)
    {
        return
        [
            $"1. Read what already instructs agents here: {string.Join(", ", existing)}. Keep every rule; move each into the place below",
            "   that owns it; never keep the same rule in two files.",
        ];
    }

    private static string[] Section2()
    {
        return
        [
            "2. Create .ai/ — the project's knowledge tree, for any agent:",
            "   - .ai/README.md: an index \"doing X -> read Y\".",
            "   - .ai/architecture/overview.md: the pieces and how they talk, read from the code.",
            "   - .ai/decisions/README.md: an empty ADR index (one decision per file, never rewritten).",
            "   - one folder per area that has its own rules (for example .ai/api/, .ai/ui/), each rule in one doc.",
            "   - .ai/context/README.md: one session log per session, newest read first.",
            "   Write only what you verified in the code; mark anything else as a question for me.",
        ];
    }

    private static string[] Section3()
    {
        return
        [
            "3. Create AGENTS.md: a pointer, not a rule book — what the project is, where to start (.ai/README.md,",
            "   the newest session log, .orchestra/workflow.md), and the few rules that bite first, each linked.",
        ];
    }

    private static string[] Section4(string config, bool hasOrchestraFolder)
    {
        var lines = new List<string>();

        if (hasOrchestraFolder)
        {
            lines.Add("4. Create .orchestra/orchestrator.json — the engine reads this file:");
        }
        else
        {
            lines.Add("4. Create .orchestra/ — the orchestrator's working folder, in git:");
            lines.Add("   - README.md (what each folder is), workflow.md (the gates: understanding -> HTML preview -> plan ->");
            lines.Add("     review by another model family -> my approval -> branch -> implement -> verify -> hand over");
            lines.Add("     uncommitted),");
            lines.Add("   - ideas/README.md, research/, previews/, plans/README.md, plans/TEMPLATE.md, prompts/,");
            lines.Add("   - .orchestra/orchestrator.json:");
        }

        lines.Add(config);
        lines.Add("   Fill verifyCommands with each repo's build and test commands.");

        if (!hasOrchestraFolder)
        {
            lines.Add("   - add .orchestra/evidence/ and .orchestra/runs/ to .gitignore.");
        }

        return [.. lines];
    }

    /// <summary>Plan 0009 D7: the shape Project A uses. The engine reads this file, not <c>project.json</c>.</summary>
    private static string OrchestratorJson(string name, IReadOnlyList<string> repos)
    {
        var shape = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["profile"] = "default",
            ["root"] = "..",
            ["repos"] = repos.ToDictionary(repo => repo, repo => repo, StringComparer.Ordinal),
            ["plansDir"] = ".orchestra/plans/{repo}",
            ["description"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["title"] = name,
                ["repos"] = repos.Count == 0 ? "none" : string.Join(", ", repos),
            },
            ["workflow"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["gatesPath"] = ".orchestra/workflow.md",
                ["rolesPath"] = ".orchestra/roles/{repo}",
                ["rolesIndex"] = ".orchestra/roles/{repo}/README.md",
                ["promptsPath"] = ".orchestra/prompts/{repo}",
                ["agentsDoc"] = "{repo}/AGENTS.md",
                ["indexDoc"] = "{repo}/.ai/README.md",
                ["planTemplate"] = ".orchestra/plans/TEMPLATE.md",
            },
            ["verifyCommands"] = new Dictionary<string, object?>(StringComparer.Ordinal),
        };

        return JsonSerializer.Serialize(shape, new JsonSerializerOptions { WriteIndented = true });
    }
}
