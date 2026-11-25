using FluentAssertions;
using Gridify;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.ListProjects;
using Orchestra.Api.Modules.Runs.Features.GetRun;
using Orchestra.Api.Modules.Runs.Features.RunsTable;
using Orchestra.Api.Modules.Usage.Features.CostSummary;
using Orchestra.Api.Modules.Usage.Features.ModelQuality;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Dashboard;

public sealed class DashboardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly string _runsPath;
    private readonly ProjectRegistry _registry;

    public DashboardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        _runsPath = Path.Combine(Path.GetTempPath(), $"orchestra-dashboard-{Guid.NewGuid():n}");
        Copy(FixtureFiles.RunsRoot, _runsPath);

        File.Copy(
            Path.Combine(FixtureFiles.Root, "ledger.lifecycle.md"),
            Path.Combine(_runsPath, "_ledger.md"),
            overwrite: true);

        var options = Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects =
            [
                new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = _runsPath },
            ],
        });

        _registry = TestRegistries.ConfigOnly(options);

        new ImportService(
            _database,
            _registry,
            options,
            TimeProvider.System,
            NullLogger<ImportService>.Instance)
            .ImportAllAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();

        if (Directory.Exists(_runsPath))
        {
            Directory.Delete(_runsPath, recursive: true);
        }
    }

    [Fact]
    public async Task Projects_carry_their_totals_and_last_import()
    {
        var rows = await new ListProjectsHandler(_database, _registry)
            .HandleAsync(CancellationToken.None);

        var project = rows.Should().ContainSingle().Subject;

        project.Id.Should().Be("demo");
        project.RunCount.Should().Be(5);
        project.OpenRunCount.Should().BeGreaterThan(0);
        project.CostUsd.Should().NotBeNull();
        project.LastImport.Should().NotBeNull();
    }

    [Fact]
    public async Task The_status_filter_is_exact()
    {
        var handler = new RunsTableHandler(_database, _registry);

        var (page, error) = await handler.HandleAsync(
            "demo", new GridifyQuery { Filter = "status=Verified" }, CancellationToken.None);

        error.Should().BeNull();
        page!.Data.Should().NotBeEmpty();
        page.Data.Select(run => run.Status).Should().OnlyContain(status => status == "Verified");
    }

    [Fact]
    public async Task An_unknown_project_is_a_not_found()
    {
        var (page, error) = await new RunsTableHandler(_database, _registry)
            .HandleAsync("nope", new GridifyQuery(), CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.ProjectNotFound.Code);
    }

    [Fact]
    public async Task A_run_detail_orders_review_read_back_phases_and_verify()
    {
        var (run, error) = await new GetRunHandler(_database, TimeProvider.System)
            .HandleAsync("demo", "T-DEMO-001.2", CancellationToken.None);

        error.Should().BeNull();

        run!.Stages.Should().NotBeEmpty();
        run.Stages[0].Stage.Should().Be("Review");
        run.Stages[^1].Stage.Should().Be("Verify");
        run.Stages.Count(stage => stage.Stage == "Review").Should().BeGreaterThanOrEqualTo(1);
        run.Texts.Should().HaveCount(4);
        run.SessionRows.Should().NotBeEmpty();
        run.TotalCostUsd.Should().NotBeNull();
    }

    [Fact]
    public async Task An_unknown_run_is_a_not_found()
    {
        var (run, error) = await new GetRunHandler(_database, TimeProvider.System)
            .HandleAsync("demo", "T-DEMO-999.1", CancellationToken.None);

        run.Should().BeNull();
        error!.Code.Should().Be(Error.RunNotFound.Code);
    }

    [Fact]
    public async Task Cost_groups_by_model_and_project()
    {
        var handler = new CostSummaryHandler(_database);

        var (byModel, _) = await handler.HandleAsync("demo", null, null, "model", "Asia/Tehran", CancellationToken.None);
        var (byProject, _) = await handler.HandleAsync("demo", null, null, "project", "Asia/Tehran", CancellationToken.None);

        byModel!.Should().NotBeEmpty();
        byModel!.Select(row => row.Key).Should().OnlyContain(key => key.Length > 0);
        byProject!.Should().ContainSingle().Which.Key.Should().Be("demo");
    }

    [Fact]
    public async Task Cost_by_day_uses_the_requested_zone_and_range()
    {
        var handler = new CostSummaryHandler(_database);

        var (allDays, _) = await handler.HandleAsync("demo", null, null, "day", "UTC", CancellationToken.None);
        var day = allDays!.First().Key;

        var date = DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture);
        var (narrow, _) = await handler.HandleAsync("demo", date, date, "day", "UTC", CancellationToken.None);

        narrow!.Should().ContainSingle().Which.Key.Should().Be(day);
        narrow![0].CostUsd.Should().Be(allDays![0].CostUsd);
    }

    [Fact]
    public async Task Bad_cost_queries_are_rejected()
    {
        var handler = new CostSummaryHandler(_database);

        (await handler.HandleAsync(null, null, null, "week", null, CancellationToken.None)).Error!.Code
            .Should().Be(Error.InvalidGroupBy.Code);

        (await handler.HandleAsync(null, new DateOnly(2026, 2, 2), new DateOnly(2026, 1, 1), "day", null, CancellationToken.None)).Error!.Code
            .Should().Be(Error.InvalidRange.Code);

        (await handler.HandleAsync(null, null, null, "day", "Mars/Olympus", CancellationToken.None)).Error!.Code
            .Should().Be(Error.InvalidTimeZone.Code);
    }

    [Fact]
    public async Task Quality_lists_the_models_that_did_the_work()
    {
        var quality = await new ModelQualityHandler(_database).HandleAsync("demo", CancellationToken.None);

        quality.Reviewers.Should().NotBeEmpty();
        quality.Implementers.Should().NotBeEmpty();
        quality.Cache.Should().NotBeEmpty();
        quality.Reviewers.Should().OnlyContain(row => row.Runs > 0);
    }

    private static void Copy(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }
}
