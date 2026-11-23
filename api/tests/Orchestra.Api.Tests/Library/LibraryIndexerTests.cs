using Orchestra.Api.Common;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Tests.Library;

public sealed class LibraryIndexerTests : IDisposable
{
    private readonly List<string> _tempRoots = [];

    public void Dispose()
    {
        foreach (var root in _tempRoots)
        {
            LibraryFixture.DeleteTree(root);
        }
    }

    private string TempRoot()
    {
        var root = LibraryFixture.TempRoot();
        _tempRoots.Add(root);

        return root;
    }

    [Fact]
    public void The_default_globs_index_the_orchestra_layout()
    {
        var result = LibraryIndexer.Build(LibraryFixture.Project(LibraryFixture.OrchestraRoot), []);

        result.Errors.Should().BeEmpty();
        result.Items.Should().HaveCount(8);
        result.Items.Count(item => item.Category == LibraryCategory.Plan).Should().Be(2);
        result.Items.Count(item => item.Category == LibraryCategory.Preview).Should().Be(1);
        result.Items.Count(item => item.Category == LibraryCategory.Idea).Should().Be(2);
        result.Items.Count(item => item.Category == LibraryCategory.Research).Should().Be(1);
        result.Items.Count(item => item.Category == LibraryCategory.Evidence).Should().Be(2);
    }

    [Fact]
    public void A_run_draft_preview_is_indexed_from_the_new_layout()
    {
        // Plan 0007 D4b: the preview is written to runs/_drafts first and moved to previews/ only when the
        // plan is filed, so the in-flight one has to stay visible after the projects drop their own globs.
        var root = TempRoot();
        LibraryFixture.Copy(LibraryFixture.OrchestraRoot, root);
        LibraryFixture.Write(root, ".orchestra/runs/_drafts/T-X-001.preview.html", "<html></html>");

        var items = LibraryIndexer.Build(LibraryFixture.Project(root), []).Items;

        items.Should().Contain(item =>
            item.Category == LibraryCategory.Preview
            && item.RelPath == ".orchestra/runs/_drafts/T-X-001.preview.html");
    }

    [Fact]
    public void A_plan_carries_its_header_its_repo_and_its_indexed_preview()
    {
        var items = LibraryIndexer.Build(LibraryFixture.Project(LibraryFixture.OrchestraRoot), []).Items;

        var alpha = items.Single(item => item.RelPath == ".orchestra/plans/0001-alpha.md");

        alpha.Title.Should().Be("Alpha: the first synthetic plan");
        alpha.Number.Should().Be("0001");
        alpha.Slug.Should().Be("alpha");
        alpha.Status.Should().Be("Done");
        alpha.DoneOn.Should().Be(new DateTime(2026, 11, 4));
        alpha.PlannedBy.Should().Be("Claude Opus 5.5");
        alpha.PreviewRelPath.Should().Be(".orchestra/previews/0001-alpha/index.html");
        alpha.Repo.Should().BeNull();
    }

    [Fact]
    public void A_plan_whose_preview_is_not_indexed_has_no_preview()
    {
        var items = LibraryIndexer.Build(LibraryFixture.Project(LibraryFixture.OrchestraRoot), []).Items;

        items.Single(item => item.RelPath == ".orchestra/plans/0002-beta.md")
            .PreviewRelPath.Should().BeNull();
    }

    [Fact]
    public void The_walk_never_enters_node_modules()
    {
        var project = LibraryFixture.Project(
            LibraryFixture.OrchestraRoot,
            library: new LibraryOptions { Plans = ["**/*.md"] });

        var result = LibraryIndexer.Build(project, []);

        result.Items.Should().NotBeEmpty();
        result.Items.Should().NotContain(item => item.RelPath.Contains("node_modules", StringComparison.Ordinal));
        result.Items.Should().NotContain(item => item.RelPath.Contains("0099-trap", StringComparison.Ordinal));
    }

    [Fact]
    public void Configured_globs_replace_the_defaults()
    {
        var project = LibraryFixture.Project(
            LibraryFixture.TodayRoot,
            library: new LibraryOptions { Plans = ["*/.ai/plans/*.md"] });

        var result = LibraryIndexer.Build(project, []);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.Category == LibraryCategory.Plan);
        result.Items.Should().NotContain(item => item.RelPath.EndsWith("demo-slug.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Today_layout_plans_get_their_repo_and_the_done_folder_gets_none()
    {
        var project = LibraryFixture.Project(
            LibraryFixture.TodayRoot,
            library: new LibraryOptions
            {
                Plans = ["*/.ai/plans/*.md", "*/.ai/plans/done/*.md"],
                Previews = ["*.html"],
            });

        var items = LibraryIndexer.Build(project, []).Items;

        items.Single(item => item.RelPath == "Web/.ai/plans/0007-demo.md").Repo.Should().Be("Web");
        items.Single(item => item.RelPath == "Api/.ai/plans/0007-other.md").Repo.Should().Be("Api");

        var done = items.Single(item => item.RelPath == "Web/.ai/plans/done/demo-slug.md");

        // The repo star matched Web; `done` is literal in this glob, so it is not the repo (D11).
        done.Repo.Should().Be("Web");
        done.Number.Should().BeNull();
        done.Slug.Should().Be("demo-slug");
        done.Status.Should().Be("Done");

        items.Single(item => item.RelPath == "preview.html").Repo.Should().BeNull();
    }

    [Fact]
    public void A_plan_in_a_done_folder_still_links_a_run_by_number()
    {
        var root = TempRoot();
        LibraryFixture.Write(root, ".orchestra/plans/done/0003-x.md", "# Plan 0003 — done folder\n\n**Status:** Done\n");

        var run = new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "demo",
            RunKey = "T-1.1",
            TaskId = "T-1",
            PlanNumber = "0003",
            Branch = "plan-0003-x",
            CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var item = LibraryIndexer.Build(LibraryFixture.Project(root), [run]).Items
            .Single(candidate => candidate.Category == LibraryCategory.Plan);

        item.Repo.Should().BeNull();
        item.RunKey.Should().Be("T-1.1");
    }

    [Fact]
    public void Evidence_is_grouped_by_the_two_folders_below_the_prefix()
    {
        var root = TempRoot();
        LibraryFixture.Write(root, "screenshots/shot.png", "x");
        LibraryFixture.Write(root, "screenshots/a/shot.png", "x");
        LibraryFixture.Write(root, "screenshots/notes.txt", "x");
        LibraryFixture.Write(root, "screenshots/dump.zip", "x");

        var project = LibraryFixture.Project(
            root,
            library: new LibraryOptions { Evidence = ["screenshots/**/*"] });

        var items = LibraryIndexer.Build(project, []).Items;

        items.Should().HaveCount(2);
        items.Single(item => item.RelPath == "screenshots/shot.png").Group.Should().BeNull();
        items.Single(item => item.RelPath == "screenshots/a/shot.png").Group.Should().Be("a");
    }

    [Fact]
    public void A_link_below_the_root_is_skipped_and_logged()
    {
        var root = TempRoot();
        var outside = TempRoot();

        LibraryFixture.Write(root, ".orchestra/plans/0001-a.md", "# Plan 0001 — a\n\n**Status:** Draft\n");
        LibraryFixture.Write(outside, "0002-b.md", "# Plan 0002 — b\n\n**Status:** Draft\n");

        var link = Path.Combine(root, ".orchestra", "plans", "linked");

        try
        {
            Directory.CreateSymbolicLink(link, outside);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return;
        }

        var result = LibraryIndexer.Build(LibraryFixture.Project(root), []);

        result.Items.Should().NotContain(item => item.RelPath.Contains("linked", StringComparison.Ordinal));
        result.Errors.Should().Contain(error => error.Message == "link skipped");
    }

    [Fact]
    public void A_preview_in_another_projects_folder_is_not_a_preview()
    {
        var root = TempRoot();
        LibraryFixture.Copy(LibraryFixture.TodayRoot, root);
        LibraryFixture.Write(
            root,
            "Web/.ai/plans/0007-demo.md",
            """
            # Plan 0007 — Demo

            **Status:** Done
            **Preview:** ../../Api/.ai/plans/0007-other.md
            """);

        var project = LibraryFixture.Project(
            root,
            library: new LibraryOptions
            {
                Plans = ["*/.ai/plans/*.md"],
                Previews = ["*/*.png"],
            });

        var plan = LibraryIndexer.Build(project, []).Items
            .Single(item => item.RelPath == "Web/.ai/plans/0007-demo.md");

        plan.PreviewRelPath.Should().BeNull();
    }

    [Fact]
    public void The_cap_stops_the_index_and_logs_one_error()
    {
        var root = TempRoot();
        var folder = Path.Combine(root, ".orchestra", "evidence", "big");
        Directory.CreateDirectory(folder);

        for (var index = 0; index < LibraryIndexer.MaxItems + 1; index++)
        {
            File.WriteAllText(Path.Combine(folder, $"shot-{index:D5}.png"), string.Empty);
        }

        var result = LibraryIndexer.Build(LibraryFixture.Project(root), []);

        result.Items.Should().HaveCount(LibraryIndexer.MaxItems);
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("library truncated");
    }

    [Fact]
    public void An_unset_root_is_one_error_and_no_items()
    {
        var root = TempRoot();
        var project = new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = Path.Combine(root, "runs") };

        var result = LibraryIndexer.Build(project, []);

        result.Items.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("library root not set");
    }

    [Fact]
    public void A_docker_mount_never_derives_a_root()
    {
        var project = new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = "/projects/p1" };

        LibraryIndexer.Build(project, []).Errors.Should().ContainSingle()
            .Which.Message.Should().Be("library root not set");
    }

    [Fact]
    public void An_orchestrator_runs_path_derives_the_project_root()
    {
        var root = TempRoot();

        var forward = new ProjectOptions
        {
            Id = "demo",
            Name = "Demo",
            RunsPath = Path.Combine(root, "orchestrator", "runs"),
        };

        var backslash = new ProjectOptions
        {
            Id = "demo",
            Name = "Demo",
            RunsPath = root + @"\orchestrator\runs",
        };

        Path.TrimEndingDirectorySeparator(forward.ResolveRootPath()!).Should().Be(root);
        Path.TrimEndingDirectorySeparator(backslash.ResolveRootPath()!).Should().Be(root);
    }

    [Fact]
    public void A_bare_runs_folder_derives_no_root()
    {
        var root = TempRoot();

        new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = Path.Combine(root, "runs") }
            .ResolveRootPath().Should().BeNull();
    }

    [Fact]
    public void A_filesystem_root_is_not_usable()
    {
        var root = TempRoot();
        var systemRoot = Path.GetPathRoot(Path.GetFullPath(root))!;

        var project = new ProjectOptions
        {
            Id = "demo",
            Name = "Demo",
            RunsPath = Path.Combine(root, "orchestrator", "runs"),
            RootPath = systemRoot,
        };

        var result = LibraryIndexer.Build(project, []);

        result.Items.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Message.Should().StartWith("library root not usable");
    }
}
