using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

/// <summary>The prompt the owner may hand to their own agent after setup (plan 0003 §6.4, plan 0012 D14).
/// English on purpose: the agent reads it. The config is no longer part of it — setup writes that.</summary>
public static class ScaffoldPromptBuilder
{
    private const string NothingMissing = "Nothing to create: .ai/ and AGENTS.md are already there.";

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

        if (hasAi && hasAgents)
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

        lines.Add(string.Empty);
        lines.Add("4. Commit nothing. Tell me what you created and what you could not verify.");

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
}
