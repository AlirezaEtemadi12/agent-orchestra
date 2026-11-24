using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Tests.Projects;

public sealed class FolderCheckTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-check-{Guid.NewGuid():n}");

    [Fact]
    public void Each_known_item_is_found_or_missing()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "CLAUDE.md"), "x");
        Directory.CreateDirectory(Path.Combine(_root, ".git"));

        var check = FolderCheck.Run(_root);

        check.Exists.Should().BeTrue();
        check.IsAbsolute.Should().BeTrue();
        check.Items.Select(item => item.Name).Should().Equal(
            ["AGENTS.md", "CLAUDE.md", ".claude/", ".cursor/", ".cursorrules", ".github/copilot-instructions.md", ".ai/", ".orchestra/", "orchestrator/"]);
        check.Items.Single(item => item.Name == "CLAUDE.md").Found.Should().BeTrue();
        check.Items.Single(item => item.Name == "AGENTS.md").Found.Should().BeFalse();
    }

    [Fact]
    public void Repos_come_from_the_root_git_or_its_subfolders()
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, ".git"));

        FolderCheck.Run(_root).Repos.Should().Equal(".");

        Directory.Delete(Path.Combine(_root, ".git"));

        foreach (var name in new[] { "web", "api" })
        {
            Directory.CreateDirectory(Path.Combine(_root, name, ".git"));
        }

        FolderCheck.Run(_root).Repos.Should().Equal("api", "web");

        // Setup makes .orchestra a repo in a workspace (plan 0012 D8); it never counts as one of the repos.
        Directory.CreateDirectory(Path.Combine(_root, ".orchestra", ".git"));

        FolderCheck.Run(_root).Repos.Should().Equal("api", "web");
    }

    [Fact]
    public void The_suggested_runs_path_prefers_orchestrator_then_orchestra()
    {
        Directory.CreateDirectory(_root);
        FolderCheck.Run(_root).SuggestedRunsPath.Should().Be(Path.Combine(_root, ".orchestra", "runs"));

        Directory.CreateDirectory(Path.Combine(_root, "orchestrator", "runs"));
        FolderCheck.Run(_root).SuggestedRunsPath.Should().Be(Path.Combine(_root, "orchestrator", "runs"));
    }

    [Fact]
    public void A_missing_folder_and_a_relative_path_are_not_errors()
    {
        var missing = FolderCheck.Run(Path.Combine(_root, "nope"));

        missing.Exists.Should().BeFalse();
        missing.IsAbsolute.Should().BeTrue();
        missing.Items.Should().OnlyContain(item => !item.Found);

        var relative = FolderCheck.Run(".orchestra/runs");

        relative.Exists.Should().BeFalse();
        relative.IsAbsolute.Should().BeFalse();
        relative.Items.Should().HaveCount(9);
    }

    [Fact]
    public void Repo_knowledge_is_reported_per_repo()
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "api", ".git"));
        Directory.CreateDirectory(Path.Combine(_root, "api", ".ai"));
        Directory.CreateDirectory(Path.Combine(_root, "web", ".git"));

        var check = FolderCheck.Run(_root);

        check.RepoKnowledge.Should().BeEquivalentTo(
        [
            new RepoKnowledgeDto("api", true),
            new RepoKnowledgeDto("web", false),
        ]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
