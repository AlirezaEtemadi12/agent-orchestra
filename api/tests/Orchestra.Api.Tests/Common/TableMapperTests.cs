using Gridify;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Library.Features.ListLibrary;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Modules.Runs.Features;
using Orchestra.Api.Modules.Runs.Features.RunsTable;

namespace Orchestra.Api.Tests.Common;

public sealed class TableMapperTests
{
    [Fact]
    public void The_runs_map_is_the_row_allow_list()
    {
        var keys = RunsTableMapping.Mapper
            .GetCurrentMaps()
            .Select(map => map.From)
            .ToHashSet(StringComparer.Ordinal);

        keys.Should().BeEquivalentTo(new[]
        {
            "projectId", "runKey", "taskId", "repo", "status", "planNumber", "planTitle",
            "task", "branch", "rounds", "updatedAt", "closedAt", "costUsd",
        });

        keys.Should().NotContain(new[]
        {
            "id", "projectName", "sessions", "calls", "requests", "models", "source",
        });
    }

    [Fact]
    public void An_empty_order_is_the_page_default_followed_by_the_tiebreak()
    {
        var query = new GridifyQuery();

        TableQueryDefaults.Apply(query, "updatedAt desc", "projectId, runKey");

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.OrderBy.Should().Be("updatedAt desc, projectId, runKey");
        query.IsValid(RunsTableMapping.Mapper).Should().BeTrue();
    }

    [Fact]
    public void An_order_that_ends_in_the_whole_tiebreak_is_not_doubled()
    {
        var query = new GridifyQuery { Page = 2, PageSize = 500, OrderBy = "updatedAt desc, projectId, runKey" };

        TableQueryDefaults.Apply(query, "updatedAt desc", "projectId, runKey");

        query.Page.Should().Be(2);
        query.PageSize.Should().Be(100);
        query.OrderBy.Should().Be("updatedAt desc, projectId, runKey");
    }

    [Fact]
    public void An_order_by_one_tiebreak_key_gets_the_whole_tiebreak()
    {
        var query = new GridifyQuery { OrderBy = "runKey" };

        TableQueryDefaults.Apply(query, "updatedAt desc", "projectId, runKey");

        query.OrderBy.Should().Be("runKey, projectId, runKey");
    }

    [Fact]
    public void The_cost_key_orders_by_micros()
    {
        RunsTableMapping.Mapper.GetExpression("costUsd").Body.ToString().Should().Contain("CostMicros");
    }

    [Fact]
    public void The_import_map_contains_id_without_an_extra_map()
    {
        var keys = TableMapper<ImportLogEntity, ImportLogDto>.Build()
            .GetCurrentMaps()
            .Select(map => map.From);

        keys.Should().Contain("id");
    }

    [Fact]
    public void The_library_map_has_project_name_and_its_tiebreak_is_valid()
    {
        var keys = LibraryTableMapping.Mapper
            .GetCurrentMaps()
            .Select(map => map.From)
            .ToHashSet(StringComparer.Ordinal);

        keys.Should().Contain("projectName");

        var query = new GridifyQuery { OrderBy = "projectName" };
        TableQueryDefaults.Apply(query, "updatedOn desc", "projectId, relPath");

        query.IsValid(LibraryTableMapping.Mapper).Should().BeTrue();
    }

    [Theory]
    [InlineData("passwordHash", null)]
    [InlineData("sessions", null)]
    [InlineData(null, "foo=1")]
    public void An_unmapped_sort_or_filter_is_table_query_invalid(string? orderBy, string? filter)
    {
        var (paging, error) = TableMapper<RunEntity, RunListItemDto>.Query(
            Array.Empty<RunEntity>().AsQueryable(),
            new GridifyQuery { OrderBy = orderBy, Filter = filter },
            "updatedAt desc",
            "projectId, runKey");

        paging.Should().BeNull();
        error!.Code.Should().Be(Error.TableQueryInvalid.Code);
    }
}
