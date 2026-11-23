using System.Text.Json;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

namespace Orchestra.Api.Tests.Projects;

public sealed class ScaffoldPromptBuilderTests
{
    [Fact]
    public void Nothing_missing_is_one_line()
    {
        var check = Check(".ai/", ".orchestra/", "AGENTS.md");

        ScaffoldPromptBuilder.Build("MyShop", "/shop", [], check)
            .Should().Be("Nothing to create: .ai/, AGENTS.md and .orchestra/orchestrator.json are already there.");
    }

    [Fact]
    public void A_folder_with_the_config_but_no_ai_asks_only_for_ai()
    {
        var check = Check(".orchestra/", "AGENTS.md");

        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", [], check);

        text.Should().Contain("2. Create .ai/ — the project's knowledge tree, for any agent:");
        text.Should().Contain("1. Read what already instructs agents here: AGENTS.md.");
        text.Should().NotContain("3. Create AGENTS.md");
        text.Should().NotContain("4. Create .orchestra/");
        text.Should().NotContain("orchestrator.json — the engine reads this file");
        text.Should().EndWith("5. Commit nothing. Tell me what you created and what you could not verify.");
    }

    [Fact]
    public void An_existing_claude_file_asks_for_a_read_and_a_pointer_line()
    {
        var check = Check("CLAUDE.md");

        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", [], check);

        text.Should().Contain("1. Read what already instructs agents here: CLAUDE.md.");
        text.Should().Contain("Make CLAUDE.md one line that points to AGENTS.md.");
    }

    [Fact]
    public void A_multi_repo_project_lists_the_repos_without_knowledge()
    {
        var check = Check("AGENTS.md", ".orchestra/") with
        {
            RepoKnowledge = [new RepoKnowledgeDto("api", true), new RepoKnowledgeDto("web", false)],
        };

        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", ["api", "web"], check);

        text.Should().Contain("Repos: api, web.");
        text.Should().Contain("   Each repo without .ai/ gets its own: web.");
    }

    [Fact]
    public void The_embedded_config_is_the_engines_orchestrator_json()
    {
        // The folder exists but the config file does not: the one case that asks for the file alone.
        var check = Check("AGENTS.md", ".orchestra/") with { IsReady = false };

        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", ["api"], check);

        var start = text.IndexOf('{');
        var json = text[start..(text.LastIndexOf('}') + 1)];

        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("profile").GetString().Should().Be("default");
        document.RootElement.GetProperty("root").GetString().Should().Be("..");
        document.RootElement.GetProperty("plansDir").GetString().Should().Be(".orchestra/plans/{repo}");
        document.RootElement.GetProperty("repos").GetProperty("api").GetString().Should().Be("api");
        document.RootElement.GetProperty("verifyCommands").ValueKind.Should().Be(JsonValueKind.Object);
        text.Should().Contain("Create .orchestra/orchestrator.json — the engine reads this file:");
        text.Should().Contain("Fill verifyCommands with each repo's build and test commands.");
        text.Should().NotContain("project.json");
    }

    [Fact]
    public void A_missing_orchestra_folder_embeds_the_config_and_the_folder_work()
    {
        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", ["api"], Check(".ai/", "AGENTS.md"));

        text.Should().Contain("4. Create .orchestra/ — the orchestrator's working folder, in git:");
        text.Should().Contain("\"verifyCommands\": {");
        text.Should().Contain("add .orchestra/evidence/ and .orchestra/runs/ to .gitignore.");
    }

    private static FolderCheckDto Check(params string[] found)
    {
        var items = new[]
        {
            "AGENTS.md", "CLAUDE.md", ".claude/", ".cursor/", ".cursorrules",
            ".github/copilot-instructions.md", ".ai/", ".orchestra/", "orchestrator/",
        };

        return new FolderCheckDto(
            Exists: true,
            IsAbsolute: true,
            Items: [.. items.Select(name => new FolderCheckItemDto(name, name.EndsWith('/') ? "folder" : "file", found.Contains(name)))],
            Repos: [],
            SuggestedRunsPath: "/shop/.orchestra/runs",
            RepoKnowledge: [],
            HostPath: "/shop",
            SuggestedName: "shop",
            IsReady: found.Contains(".orchestra/"),
            Visible: true);
    }
}
