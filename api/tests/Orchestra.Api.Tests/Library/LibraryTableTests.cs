using Gridify;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Features.ListLibrary;

namespace Orchestra.Api.Tests.Library;

public sealed class LibraryTableTests
{
    [Fact]
    public async Task The_page_has_the_total_and_the_page_size()
    {
        using var fixture = new LibraryApiFixture();

        var (page, error) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { Page = 1, PageSize = 1 }, CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(2);
        page.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task An_unknown_order_is_table_query_invalid()
    {
        using var fixture = new LibraryApiFixture();

        var (page, error) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "passwordHash" }, CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.TableQueryInvalid.Code);
    }

    [Fact]
    public async Task Project_sorts_through_the_extra_map()
    {
        using var fixture = new LibraryApiFixture();

        var (page, error) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "projectName" }, CancellationToken.None);

        error.Should().BeNull();
        page!.Data.Should().OnlyContain(item => item.ProjectName == "Demo");
    }

    [Fact]
    public async Task Nulls_follow_the_sqlite_order()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddItem(".orchestra/plans/0003-null.md", LibraryCategory.Plan, number: "0003", status: "Draft");

        var (ascending, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "createdOn" }, CancellationToken.None);

        var (descending, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { OrderBy = "createdOn desc" }, CancellationToken.None);

        ascending!.Data.First().Number.Should().Be("0003");
        descending!.Data.Last().Number.Should().Be("0003");
    }

    [Fact]
    public async Task The_status_filter_works_inside_one_category()
    {
        using var fixture = new LibraryApiFixture();

        var (page, _) = await Handler(fixture)
            .HandleAsync(null, "plan", new GridifyQuery { Filter = "status=Done" }, CancellationToken.None);

        page!.Data.Should().ContainSingle().Which.Number.Should().Be("0001");
    }

    private static LibraryTableHandler Handler(LibraryApiFixture fixture)
    {
        return new LibraryTableHandler(fixture.Database, fixture.Registry);
    }
}
