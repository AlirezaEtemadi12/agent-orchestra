using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

namespace Orchestra.Api.Tests.Projects;

public sealed class ScaffoldPromptBuilderTests
{
    [Fact]
    public void Nothing_missing_is_one_line()
    {
        // Plan 0012 D14: the config no longer counts; .ai/ and AGENTS.md are the whole question.
        var check = Check(".ai/", "AGENTS.md");

        ScaffoldPromptBuilder.Build("MyShop", "/shop", [], check)
            .Should().Be("Nothing to create: .ai/ and AGENTS.md are already there.");
    }

    [Fact]
    public void A_folder_with_the_config_but_no_ai_asks_only_for_ai()
    {
        var check = Check(".orchestra/", "AGENTS.md");

        var text = ScaffoldPromptBuilder.Build("MyShop", "/shop", [], check);

        text.Should().Contain("2. Create .ai/ — the project's knowledge tree, for any agent:");
        text.Should().Contain("1. Read what already instructs agents here: AGENTS.md.");
        text.Should().NotContain("3. Create AGENTS.md");
        text.Should().NotContain("orchestrator.json");
        text.Should().EndWith("4. Commit nothing. Tell me what you created and what you could not verify.");
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
    public void The_last_line_is_numbered_4()
    {
        ScaffoldPromptBuilder.Build("MyShop", "/shop", ["."], Check())
            .Should().EndWith("\n4. Commit nothing. Tell me what you created and what you could not verify.");
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
