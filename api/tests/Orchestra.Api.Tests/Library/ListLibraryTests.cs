using Gridify;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library.Features.ListLibrary;

namespace Orchestra.Api.Tests.Library;

public sealed class ListLibraryTests
{
    [Fact]
    public async Task A_category_lists_every_project()
    {
        using var fixture = new LibraryApiFixture();

        var (page, error) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery(), CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(2);
        page.Data.Select(item => item.ProjectId).Should().OnlyContain(id => id == LibraryApiFixture.Demo);
    }

    [Fact]
    public async Task A_category_keeps_only_its_own_rows()
    {
        using var fixture = new LibraryApiFixture();

        var (plans, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery(), CancellationToken.None);

        plans!.Data.Select(item => item.RelPath).Should().BeEquivalentTo(
            [".orchestra/plans/0001-alpha.md", ".orchestra/plans/0002-beta.md"]);

        (await Handler(fixture).HandleAsync(null, "nope", new GridifyQuery(), CancellationToken.None)).Error!
            .Code.Should().Be(Error.LibraryItemNotFound.Code);
    }

    [Fact]
    public async Task An_unknown_project_is_a_not_found()
    {
        using var fixture = new LibraryApiFixture();

        var (page, error) = await Handler(fixture)
            .HandleAsync("nope", "plan", new GridifyQuery(), CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.ProjectNotFound.Code);
    }

    [Fact]
    public async Task A_status_filter_applies_inside_one_category()
    {
        using var fixture = new LibraryApiFixture();

        var (page, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { Filter = "status=Done" }, CancellationToken.None);

        page!.Data.Should().ContainSingle().Which.Number.Should().Be("0001");
    }

    [Fact]
    public async Task The_order_keys_order_the_rows()
    {
        using var fixture = new LibraryApiFixture();

        var (byNumber, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "number" }, CancellationToken.None);

        byNumber!.Data.Select(item => item.Number).Should().Equal("0001", "0002");

        var (byNumberDesc, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "number desc" }, CancellationToken.None);

        byNumberDesc!.Data.Select(item => item.Number).Should().Equal("0002", "0001");

        var (byProject, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "projectName" }, CancellationToken.None);

        byProject!.Count.Should().Be(2);
        byProject.Data.Select(item => item.ProjectName).Should().OnlyContain(name => name == "Demo");
    }

    [Fact]
    public async Task The_default_order_is_updated_on_desc()
    {
        using var fixture = new LibraryApiFixture();

        var (page, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery(), CancellationToken.None);

        page!.Data.First().UpdatedOn.Should().Be(page.Data.Max(item => item.UpdatedOn));
    }

    [Fact]
    public async Task An_idea_carries_its_linked_plans_title()
    {
        using var fixture = new LibraryApiFixture();

        var (ideas, _) = await Handler(fixture)
            .HandleAsync(null, "idea", new GridifyQuery(), CancellationToken.None);

        var linked = ideas!.Data.Single(idea => idea.RelPath == ".orchestra/ideas/0001-first-idea.md");

        linked.LinkedPlanNumber.Should().Be("0001");
        linked.LinkedPlanTitle.Should().Be("Alpha: the first synthetic plan");
        ideas!.Data.Single(idea => idea.LinkedPlanNumber is null).LinkedPlanTitle.Should().BeNull();
    }

    private static LibraryTableHandler Handler(LibraryApiFixture fixture)
    {
        return new LibraryTableHandler(fixture.Database, fixture.Registry);
    }
}
