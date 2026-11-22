using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Tests.Fixtures;
using Orchestra.Api.Tests.Library;

namespace Orchestra.Api.Tests.Import;

public sealed class ImportServiceLibraryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly string _root;
    private readonly string _runsPath;

    public ImportServiceLibraryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        _root = LibraryFixture.TempRoot();
        _runsPath = Path.Combine(_root, "orchestrator", "runs");

        LibraryFixture.Copy(FixtureFiles.RunsRoot, _runsPath);
        File.Copy(
            Path.Combine(FixtureFiles.Root, "ledger.lifecycle.md"),
            Path.Combine(_runsPath, "_ledger.md"),
            overwrite: true);

        LibraryFixture.Copy(
            Path.Combine(LibraryFixture.OrchestraRoot, ".orchestra"),
            Path.Combine(_root, ".orchestra"));
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();
        LibraryFixture.DeleteTree(_root);
    }

    private ImportService CreateService()
    {
        var options = Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects =
            [
                // RootPath is left out on purpose: an orchestrator/runs path derives it.
                new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = _runsPath },
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
    public async Task Runs_and_library_rebuild_together_and_the_log_counts_them()
    {
        var result = await CreateService().ImportAllAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].RunsBuilt.Should().Be(5);
        result[0].LibraryItems.Should().Be(8);

        (await _database.LibraryItems.AsNoTracking().CountAsync()).Should().Be(8);

        var log = await _database.ImportLogs.AsNoTracking()
            .Include(item => item.Errors)
            .OrderByDescending(item => item.StartedAt)
            .FirstAsync();

        log.LibraryItems.Should().Be(8);

        // The runs fixture ships one known schema-2 archive; the library contributes no error of its own.
        log.Errors.Should().ContainSingle().Which.File.Should().Be("_archive/T-DEMO-010.json");
    }

    [Fact]
    public async Task A_deleted_idea_disappears_from_the_library()
    {
        var service = CreateService();
        await service.ImportAllAsync(CancellationToken.None);

        File.Delete(Path.Combine(_root, ".orchestra", "ideas", "second-idea.md"));

        await service.ImportAllAsync(CancellationToken.None);

        var paths = await _database.LibraryItems.AsNoTracking().Select(item => item.RelPath).ToListAsync();

        paths.Should().NotContain(".orchestra/ideas/second-idea.md");
        paths.Should().HaveCount(7);
    }

    [Fact]
    public async Task Importing_twice_keeps_the_same_items()
    {
        var service = CreateService();
        await service.ImportAllAsync(CancellationToken.None);

        var first = await _database.LibraryItems.AsNoTracking()
            .OrderBy(item => item.RelPath)
            .Select(item => item.RelPath)
            .ToListAsync();

        await service.ImportAllAsync(CancellationToken.None);

        var second = await _database.LibraryItems.AsNoTracking()
            .OrderBy(item => item.RelPath)
            .Select(item => item.RelPath)
            .ToListAsync();

        second.Should().Equal(first);
        second.Should().HaveCount(8);
    }

    [Fact]
    public async Task Removing_the_project_takes_its_library_with_it()
    {
        await CreateService().ImportAllAsync(CancellationToken.None);

        _database.ChangeTracker.Clear();

        var project = await _database.Projects.SingleAsync();
        _database.Projects.Remove(project);
        await _database.SaveChangesAsync();

        (await _database.LibraryItems.AsNoTracking().CountAsync()).Should().Be(0);
    }
}
