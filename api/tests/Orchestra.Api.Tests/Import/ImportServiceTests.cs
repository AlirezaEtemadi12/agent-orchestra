using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class ImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly string _runsPath;

    public ImportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        _runsPath = Path.Combine(Path.GetTempPath(), $"orchestra-fixture-{Guid.NewGuid():n}");
        Copy(FixtureFiles.RunsRoot, _runsPath);

        File.Copy(
            Path.Combine(FixtureFiles.Root, "ledger.lifecycle.md"),
            Path.Combine(_runsPath, "_ledger.md"),
            overwrite: true);
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

    private ImportService CreateService(string? runsPath = null)
    {
        var options = Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects =
            [
                new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = runsPath ?? _runsPath },
            ],
        });

        return new ImportService(
            _database,
            TestRegistries.ConfigOnly(options),
            options,
            TimeProvider.System,
            NullLogger<ImportService>.Instance);
    }

    [Fact]
    public async Task Importing_twice_keeps_the_same_runs_and_rows()
    {
        var service = CreateService();

        var first = await service.ImportAllAsync(CancellationToken.None);
        var keys = await _database.Runs.OrderBy(run => run.RunKey).Select(run => run.RunKey).ToListAsync();
        var sessions = await _database.SessionUsages.CountAsync();

        var second = await service.ImportAllAsync(CancellationToken.None);

        first.Should().ContainSingle().Which.RunsBuilt.Should().Be(5);
        second.Should().ContainSingle().Which.RunsBuilt.Should().Be(5);

        var again = await _database.Runs.OrderBy(run => run.RunKey).Select(run => run.RunKey).ToListAsync();

        again.Should().Equal(keys);
        (await _database.SessionUsages.CountAsync()).Should().Be(sessions);
    }

    [Fact]
    public async Task A_deleted_run_folder_disappears_from_the_database()
    {
        var service = CreateService();
        await service.ImportAllAsync(CancellationToken.None);

        Directory.Delete(Path.Combine(_runsPath, "T-DEMO-003"), recursive: true);

        await service.ImportAllAsync(CancellationToken.None);

        (await _database.Runs.AnyAsync(run => run.TaskId == "T-DEMO-003")).Should().BeFalse();
    }

    [Fact]
    public async Task A_missing_runs_path_is_one_error_and_leaves_the_rows_alone()
    {
        var service = CreateService();
        await service.ImportAllAsync(CancellationToken.None);

        var before = await _database.Runs.CountAsync();
        var missing = Path.Combine(_runsPath, "does-not-exist");

        var result = await CreateService(missing).ImportAllAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ErrorCount.Should().Be(1);
        result[0].RunsBuilt.Should().Be(0);
        (await _database.Runs.CountAsync()).Should().Be(before);

        var log = await _database.ImportLogs.Include(item => item.Errors).OrderByDescending(item => item.StartedAt).FirstAsync();
        log.Errors.Should().ContainSingle().Which.Message.Should().Contain("runs folder");
    }

    [Fact]
    public async Task A_corrupt_block_is_logged_and_the_rest_is_imported()
    {
        File.WriteAllText(Path.Combine(_runsPath, "_ledger.md"), FixtureFiles.LedgerCorrupt);

        var result = await CreateService().ImportAllAsync(CancellationToken.None);

        // The broken ledger block, the schema-2 archive, and — since plan 0002 — the one "library root
        // not set" error of a project whose RunsPath is not an orchestrator/runs shape. One bad file or
        // block never stops the rest.
        result[0].ErrorCount.Should().Be(3);
        (await _database.Runs.AnyAsync(run => run.TaskId == "T-DEMO-003")).Should().BeTrue();
    }

    [Fact]
    public void A_second_import_cannot_start_while_one_is_running()
    {
        var service = CreateService();

        service.TryEnter().Should().BeTrue();
        service.TryEnter().Should().BeFalse();

        service.Leave();
        service.TryEnter().Should().BeTrue();
        service.Leave();
    }

    [Fact]
    public async Task The_log_is_trimmed_to_the_last_fifty()
    {
        var service = CreateService();

        for (var index = 0; index < 51; index++)
        {
            await service.ImportAllAsync(CancellationToken.None);
        }

        (await _database.ImportLogs.CountAsync()).Should().Be(50);
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
