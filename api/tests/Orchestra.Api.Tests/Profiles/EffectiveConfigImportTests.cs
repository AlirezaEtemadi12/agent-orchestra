using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Import.Archive;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Tests.Fixtures;
using Microsoft.Data.Sqlite;

namespace Orchestra.Api.Tests.Profiles;

public sealed class EffectiveConfigImportTests : IDisposable
{
    private const string Effective = """
        { "profile": "default", "profileVersion": 3, "writtenAt": "2026-11-22T10:00:00", "config": { "roles": { } } }
        """;

    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"orchestra-effective-{Guid.NewGuid():n}");

    public EffectiveConfigImportTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);
        _database.Database.EnsureCreated();

        Directory.CreateDirectory(_folder);
    }

    [Fact]
    public void An_archive_carries_its_effective_config_onto_the_run()
    {
        var json = $$"""
            {
              "schema": 1,
              "archivedAt": "2026-11-22T10:05:00",
              "task": { "id": "T-1", "repo": "Ui", "status": "Verified", "createdAt": "2026-11-22T10:01:00" },
              "effectiveConfig": {{Effective}}
            }
            """;

        var read = ArchiveReader.Read(json, "_archive/T-1.202611221005.json", SourceClock.From(null));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.EffectiveConfig.Should().NotBeNull();

        var runs = RunAssembler.Build(
            "demo",
            [],
            [],
            [read.Snapshot],
            [],
            SourceClock.From(null));

        runs.Should().ContainSingle();
        runs[0].EffectiveConfigJson.Should().Contain("\"profileVersion\": 3");
    }

    [Fact]
    public void A_live_task_carries_its_effective_config_onto_the_run()
    {
        var task = new TaskSnapshot
        {
            File = "runs/T-2/task.json",
            TaskId = "T-2",
            Repo = "Ui",
            Status = "Planning",
            CreatedAt = DateTime.UtcNow,
            EffectiveConfig = Effective,
        };

        var runs = RunAssembler.Build("demo", [], [task], [], [], SourceClock.From(null));

        runs.Should().ContainSingle();
        runs[0].EffectiveConfigJson.Should().Be(Effective);
    }

    [Fact]
    public void A_run_without_one_stays_null()
    {
        var task = new TaskSnapshot
        {
            File = "runs/T-3/task.json",
            TaskId = "T-3",
            Repo = "Ui",
            Status = "Planning",
            CreatedAt = DateTime.UtcNow,
        };

        RunAssembler.Build("demo", [], [task], [], [], SourceClock.From(null))
            .Single()
            .EffectiveConfigJson
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task The_importer_reads_the_live_file_and_reports_a_broken_one()
    {
        var runs = Path.Combine(_folder, "runs");
        var good = Path.Combine(runs, "T-4");
        var broken = Path.Combine(runs, "T-5");

        Directory.CreateDirectory(good);
        Directory.CreateDirectory(broken);

        const string task = """
            { "id": "T-4", "repo": "Ui", "task": "x", "status": "Planning", "createdAt": "2026-11-22T10:00:00" }
            """;

        await File.WriteAllTextAsync(Path.Combine(good, "task.json"), task);
        await File.WriteAllTextAsync(Path.Combine(good, "config.effective.json"), Effective);

        await File.WriteAllTextAsync(Path.Combine(broken, "task.json"), task.Replace("T-4", "T-5"));
        await File.WriteAllTextAsync(Path.Combine(broken, "config.effective.json"), "{ \"oops\": ");

        var options = Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = runs }],
        });

        var service = new ImportService(
            _database,
            TestRegistries.ConfigOnly(options),
            options,
            TimeProvider.System,
            NullLogger<ImportService>.Instance);

        var result = await service.ImportAllAsync(CancellationToken.None);

        var stored = await _database.Runs.SingleAsync(run => run.TaskId == "T-4");

        stored.EffectiveConfigJson.Should().Contain("\"profileVersion\": 3");
        result.Single().ErrorCount.Should().BeGreaterThanOrEqualTo(1);

        var errors = await _database.ImportErrors.ToListAsync();

        errors.Should().ContainSingle(error => error.Message.Contains("config.effective.json"));
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
