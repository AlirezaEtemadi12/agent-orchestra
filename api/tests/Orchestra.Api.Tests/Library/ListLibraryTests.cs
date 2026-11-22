using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Features.ListLibrary;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Tests.Library;

public sealed class ListLibraryTests
{
    [Fact]
    public async Task Without_a_project_every_project_is_listed()
    {
        using var fixture = new LibraryApiFixture();

        var (items, error) = await Handler(fixture).HandleAsync(null, null, null, null, null, CancellationToken.None);

        error.Should().BeNull();
        items!.Should().HaveCount(8);
        items!.Select(item => item.ProjectId).Should().OnlyContain(id => id == LibraryApiFixture.Demo);
    }

    [Fact]
    public async Task A_category_keeps_only_its_own_rows()
    {
        using var fixture = new LibraryApiFixture();

        var (plans, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, null, null, CancellationToken.None);

        plans!.Select(item => item.RelPath).Should().BeEquivalentTo(
            [".orchestra/plans/0001-alpha.md", ".orchestra/plans/0002-beta.md"]);

        (await Handler(fixture).HandleAsync(null, "nope", null, null, null, CancellationToken.None)).Error!
            .Code.Should().Be(Error.LibraryItemNotFound.Code);
    }

    [Fact]
    public async Task An_unknown_project_is_a_not_found()
    {
        using var fixture = new LibraryApiFixture();

        var (items, error) = await Handler(fixture)
            .HandleAsync("nope", null, null, null, null, CancellationToken.None);

        items.Should().BeNull();
        error!.Code.Should().Be(Error.ProjectNotFound.Code);
    }

    [Fact]
    public async Task A_plan_status_filter_applies_and_a_research_status_filter_is_ignored()
    {
        using var fixture = new LibraryApiFixture();

        var (done, _) = await Handler(fixture)
            .HandleAsync(null, "plan", "done", null, null, CancellationToken.None);

        done!.Should().ContainSingle().Which.Number.Should().Be("0001");

        // Research has no status; the filter must not hide it.
        var (research, _) = await Handler(fixture)
            .HandleAsync(null, "research", "Done", null, null, CancellationToken.None);

        research!.Should().ContainSingle().Which.Category.Should().Be("Research");
    }

    [Fact]
    public async Task Every_sort_key_orders_the_rows()
    {
        using var fixture = new LibraryApiFixture();

        var (byNumber, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "number", "asc", CancellationToken.None);

        byNumber!.Select(item => item.Number).Should().Equal("0001", "0002");

        var (byNumberDesc, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "number", "desc", CancellationToken.None);

        byNumberDesc!.Select(item => item.Number).Should().Equal("0002", "0001");

        var (byTitle, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "title", "asc", CancellationToken.None);

        byTitle!.Select(item => item.Title).Should().BeInAscendingOrder(StringComparer.Ordinal);

        var (byProject, _) = await Handler(fixture)
            .HandleAsync(null, null, null, "project", "asc", CancellationToken.None);

        byProject!.Select(item => item.ProjectName).Should().OnlyContain(name => name == "Demo");

        var (byDone, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "done", "desc", CancellationToken.None);

        byDone!.First().Number.Should().Be("0001");

        // Default: updated desc, with the null dates last.
        var (byUpdated, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, null, null, CancellationToken.None);

        byUpdated!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Nulls_are_last_in_both_directions()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddItem(".orchestra/plans/0003-null.md", LibraryCategory.Plan, number: "0003", status: "Draft");

        var (ascending, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "created", "asc", CancellationToken.None);

        var (descending, _) = await Handler(fixture)
            .HandleAsync(null, "plan", null, "created", "desc", CancellationToken.None);

        ascending![^1].Number.Should().Be("0003");
        descending![^1].Number.Should().Be("0003");
    }

    [Fact]
    public async Task An_idea_carries_its_linked_plans_title()
    {
        using var fixture = new LibraryApiFixture();

        var (ideas, _) = await Handler(fixture)
            .HandleAsync(null, "idea", null, null, null, CancellationToken.None);

        var linked = ideas!.Single(idea => idea.RelPath == ".orchestra/ideas/0001-first-idea.md");

        linked.LinkedPlanNumber.Should().Be("0001");
        linked.LinkedPlanTitle.Should().Be("Alpha: the first synthetic plan");
        ideas!.Single(idea => idea.LinkedPlanNumber is null).LinkedPlanTitle.Should().BeNull();
    }

    private static ListLibraryHandler Handler(LibraryApiFixture fixture)
    {
        return new ListLibraryHandler(fixture.Database, fixture.Registry);
    }
}
