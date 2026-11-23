using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Tests.Projects;

/// <summary>A temp root with a config project (<c>config-one</c>), a dashboard store under it and an
/// in-memory database — the shape the project tests ask questions about.</summary>
internal sealed class ProjectsFixture : IDisposable
{
    public const string ConfigProject = "config-one";

    private readonly SqliteConnection _connection;

    public ProjectsFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        Database.Database.EnsureCreated();

        Temp = Path.Combine(Path.GetTempPath(), $"orchestra-projects-{Guid.NewGuid():n}");
        Directory.CreateDirectory(Temp);

        ConfigRuns = Path.Combine(Temp, "config-project", ".orchestra", "runs");
        Directory.CreateDirectory(ConfigRuns);

        StorePath = Path.Combine(Temp, "data", "projects.json");
        Store = new ProjectStore(StorePath);

        Settings = Microsoft.Extensions.Options.Options.Create(new OrchestraOptions
        {
            SourceTimeZone = "Asia/Tehran",
            Projects =
            [
                new ProjectOptions
                {
                    Id = ConfigProject,
                    Name = "Config project",
                    RunsPath = ConfigRuns,
                    RootPath = Path.Combine(Temp, "config-project"),
                },
            ],
        });

        Registry = new ProjectRegistry(Settings, Store, Mapper, NullLogger<ProjectRegistry>.Instance);

        Imports = new ImportService(
            Database,
            Registry,
            Settings,
            TimeProvider.System,
            NullLogger<ImportService>.Instance);
    }

    public string Temp { get; }

    public string ConfigRuns { get; }

    public string StorePath { get; }

    public ProjectStore Store { get; }

    /// <summary>An empty map: on this machine the typed path is the server path (plan 0009 D2).</summary>
    public PathMapper Mapper { get; } = PathMapper.Empty;

    public IOptions<OrchestraOptions> Settings { get; }

    public ProjectRegistry Registry { get; }

    public ImportService Imports { get; }

    public OrchestraDbContext Database { get; }

    /// <summary>A folder with a runs folder inside, ready to be imported.</summary>
    public string NewRoot(string name = "shop")
    {
        var root = Path.Combine(Temp, name);
        Directory.CreateDirectory(Path.Combine(root, ".orchestra", "runs"));

        return root;
    }

    public StoredProject Entry(string id = "shop", string? root = null, string name = "MyShop")
    {
        var folder = root ?? NewRoot(id);

        return new StoredProject
        {
            Id = id,
            Name = name,
            RootPath = folder,
            RunsPath = Path.Combine(folder, ".orchestra", "runs"),
            Repos = [],
            Profile = "default",
            CreatedAt = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc),
        };
    }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();

        if (Directory.Exists(Temp))
        {
            Directory.Delete(Temp, recursive: true);
        }
    }
}
