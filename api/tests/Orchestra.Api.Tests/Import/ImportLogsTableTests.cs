using Gridify;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Tests.Import;

public sealed class ImportLogsTableTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;

    public ImportLogsTableTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var index = 1; index <= 3; index++)
        {
            _database.ImportLogs.Add(new ImportLogEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = index == 3 ? "other" : "demo",
                StartedAt = start.AddHours(index),
                FinishedAt = start.AddHours(index).AddMinutes(5),
                FilesRead = index,
                BlocksRead = index,
                RunsBuilt = index,
                LibraryItems = index,
                ErrorCount = index,
                Errors = index == 2
                    ? [new ImportErrorEntity { Id = Guid.NewGuid(), File = "b.md", Location = "1", Message = "bad" }]
                    : [],
            });
        }

        _database.SaveChanges();
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task The_default_order_is_started_at_desc()
    {
        var (page, error) = await new ImportLogsTableHandler(_database)
            .HandleAsync(null, new GridifyQuery(), CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(3);
        page.Data.First().StartedAt.Should().Be(new DateTime(2026, 1, 1, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task The_page_has_the_total_and_the_page_size()
    {
        var (page, _) = await new ImportLogsTableHandler(_database)
            .HandleAsync(null, new GridifyQuery { Page = 2, PageSize = 2 }, CancellationToken.None);

        page!.Count.Should().Be(3);
        page.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task A_project_filter_runs_on_the_server()
    {
        var (page, _) = await new ImportLogsTableHandler(_database)
            .HandleAsync("demo", new GridifyQuery(), CancellationToken.None);

        page!.Count.Should().Be(2);
        page.Data.Should().OnlyContain(log => log.ProjectId == "demo");
    }

    [Fact]
    public async Task An_unknown_field_is_table_query_invalid()
    {
        var (page, error) = await new ImportLogsTableHandler(_database)
            .HandleAsync(null, new GridifyQuery { OrderBy = "foo" }, CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.TableQueryInvalid.Code);
    }

    [Fact]
    public async Task The_errors_of_the_page_are_included()
    {
        var (page, _) = await new ImportLogsTableHandler(_database)
            .HandleAsync(null, new GridifyQuery { PageSize = 3 }, CancellationToken.None);

        page!.Data.Single(log => log.ErrorCount == 2).Errors.Should()
            .ContainSingle().Which.File.Should().Be("b.md");
    }
}
