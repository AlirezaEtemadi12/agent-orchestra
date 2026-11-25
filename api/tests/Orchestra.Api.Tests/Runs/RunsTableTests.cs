using FluentAssertions;
using Gridify;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Modules.Runs.Features.RunsTable;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Runs;

public sealed class RunsTableTests : IDisposable
{
    private const int Count = 53;

    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly ProjectRegistry _registry;

    public RunsTableTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        var project = new ProjectEntity { Id = "demo", Name = "Demo", RunsPath = "demo/runs" };
        _database.Projects.Add(project);

        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var index = 1; index <= Count; index++)
        {
            _database.Runs.Add(new RunEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                RunKey = $"T-DEMO-{index:000}.1",
                TaskId = $"T-DEMO-{index:000}",
                Repo = index % 2 == 0 ? "Web" : "Ui",
                Status = index % 3 == 0 ? "Verified" : "Running",
                PlanNumber = $"{index:0000}",
                PlanTitle = $"Plan {index}",
                Task = $"Task {index}",
                Branch = $"plan-{index:0000}-demo",
                Rounds = 1,
                CreatedAt = start,
                UpdatedAt = start.AddMinutes(index),
                CostMicros = index * 1_000_000,
                Source = RunSource.Ledger,
                Sessions =
                [
                    new SessionUsageEntity
                    {
                        Id = Guid.NewGuid(),
                        SessionId = $"session-{index:000}",
                        Calls = index,
                        Requests = index,
                        CostMicros = index * 1_000_000,
                    },
                ],
                Stages =
                [
                    new StageResultEntity
                    {
                        Id = Guid.NewGuid(),
                        Stage = StageKind.Verify,
                        IsFinal = true,
                        Model = "model-x",
                    },
                ],
            });
        }

        _database.SaveChanges();

        var options = Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = "demo/runs" }],
        });

        _registry = TestRegistries.ConfigOnly(options);
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task The_default_order_is_the_newest_updated_run_first()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery(), CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(Count);
        page.Data.Should().HaveCount(20);
        page.Data.First().RunKey.Should().Be("T-DEMO-053.1");
    }

    [Fact]
    public async Task The_third_page_holds_the_last_thirteen_rows()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery { Page = 3, PageSize = 20 }, CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(Count);
        page.Data.Should().HaveCount(13);
        page.Data.Last().RunKey.Should().Be("T-DEMO-001.1");
    }

    [Fact]
    public async Task The_repo_filter_runs_on_the_server()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery { Filter = "repo=Web", PageSize = 100 }, CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(26);
        page.Data.Should().OnlyContain(run => run.Repo == "Web");
    }

    [Fact]
    public async Task Cost_sorts_by_the_most_expensive_run()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery { OrderBy = "costUsd desc" }, CancellationToken.None);

        error.Should().BeNull();
        page!.Data.First().CostUsd.Should().Be(53m);
    }

    [Fact]
    public async Task Search_finds_the_task_id_case_insensitively()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery { Filter = "taskId=*t-demo-001/i" }, CancellationToken.None);

        error.Should().BeNull();
        page!.Data.Should().ContainSingle();
        page.Data.Single().TaskId.Should().Be("T-DEMO-001");
    }

    [Fact]
    public async Task The_summary_counts_the_whole_filtered_set_and_the_dropdowns_ignore_the_filter()
    {
        var (summary, error) = await new RunsSummaryHandler(_database, _registry)
            .HandleAsync("demo", new GridifyQuery { Filter = "repo=Web" }, CancellationToken.None);

        error.Should().BeNull();
        summary!.Runs.Should().Be(26);
        summary.Sessions.Should().Be(26);
        summary.Calls.Should().Be(702);
        summary.CostUsd.Should().Be(702m);
        summary.Statuses.Should().Equal("Running", "Verified");
        summary.Repos.Should().Equal("Ui", "Web");
    }
}
