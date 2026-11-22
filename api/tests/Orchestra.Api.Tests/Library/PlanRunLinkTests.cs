using Orchestra.Api.Common;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Indexing;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Tests.Library;

public sealed class PlanRunLinkTests
{
    private static readonly DateTime Created = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static List<LibraryItemEntity> TodayPlans()
    {
        var project = LibraryFixture.Project(
            LibraryFixture.TodayRoot,
            library: new LibraryOptions { Plans = ["*/.ai/plans/*.md", "*/.ai/plans/done/*.md"] });

        return LibraryIndexer.Build(project, []).Items
            .Where(item => item.Category == LibraryCategory.Plan)
            .ToList();
    }

    private static RunEntity Run(
        string runKey,
        string? branch,
        string? planNumber = null,
        string? repo = null,
        DateTime? updatedAt = null,
        string taskId = "T-1")
    {
        return new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "demo",
            RunKey = runKey,
            TaskId = taskId,
            Branch = branch,
            PlanNumber = planNumber,
            Repo = repo,
            CreatedAt = Created,
            UpdatedAt = updatedAt,
        };
    }

    [Fact]
    public void A_numbered_plan_links_the_run_of_its_own_repo()
    {
        var plans = TodayPlans();
        var runs = new List<RunEntity> { Run("T-1.1", "plan-0007-demo", "0007", "Web") };

        PlanRunLinker.Link(plans, runs);

        plans.Single(plan => plan.RelPath == "Web/.ai/plans/0007-demo.md").RunKey.Should().Be("T-1.1");
        plans.Single(plan => plan.RelPath == "Api/.ai/plans/0007-other.md").RunKey.Should().BeNull();
    }

    [Theory]
    [InlineData("feature/plan-demo-slug")]
    [InlineData("demo-slug")]
    public void An_unnumbered_plan_links_by_the_slug_in_its_branch(string branch)
    {
        var plans = TodayPlans();
        var runs = new List<RunEntity> { Run("T-2.1", branch) };

        PlanRunLinker.Link(plans, runs);

        plans.Single(plan => plan.Slug == "demo-slug").RunKey.Should().Be("T-2.1");
    }

    [Fact]
    public void A_plan_keeps_the_newest_of_its_runs_and_the_detail_keeps_all_of_them()
    {
        var plans = TodayPlans();
        var runs = new List<RunEntity>
        {
            Run("T-3.1", "plan-0007-demo", "0007", "Web", new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)),
            Run("T-3.2", "plan-0007-demo", "0007", "Web", new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc)),
            Run("T-3.3", "plan-0007-demo", "0007", "Web", new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc)),
        };

        PlanRunLinker.Link(plans, runs);

        var plan = plans.Single(item => item.RelPath == "Web/.ai/plans/0007-demo.md");

        // D5: the newest run is the highest n, not the latest UpdatedAt — .3 beats .2 here.
        plan.RunKey.Should().Be("T-3.3");
        PlanRunLinker.MatchesOf(plan, runs).Select(run => run.RunKey)
            .Should().Equal("T-3.3", "T-3.2", "T-3.1");
    }

    [Fact]
    public void Run_numbers_are_compared_as_numbers()
    {
        var plans = TodayPlans();
        var runs = new List<RunEntity>
        {
            Run("T-4.9", "plan-0007-demo", "0007", "Web", taskId: "T-4"),
            Run("T-4.10", "plan-0007-demo", "0007", "Web", taskId: "T-4"),
        };

        PlanRunLinker.Link(plans, runs);

        plans.Single(item => item.RelPath == "Web/.ai/plans/0007-demo.md").RunKey.Should().Be("T-4.10");
    }

    [Fact]
    public void Nothing_matching_leaves_the_plan_without_a_run()
    {
        var plans = TodayPlans();

        PlanRunLinker.Link(plans, [Run("T-5.1", "feature/unrelated", "0099", "Api")]);

        plans.Should().OnlyContain(plan => plan.RunKey == null);
    }
}
