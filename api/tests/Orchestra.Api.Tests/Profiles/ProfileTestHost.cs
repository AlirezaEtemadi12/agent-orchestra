using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Profiles;

namespace Orchestra.Api.Tests.Profiles;

/// <summary>The same shape the app wires: a migrated database and a profiles folder beside it.</summary>
internal sealed class ProfileTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public ProfileTestHost()
    {
        Folder = Path.Combine(Path.GetTempPath(), $"orchestra-profiles-{Guid.NewGuid():n}");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);
        Database.Database.EnsureCreated();

        Options = Microsoft.Extensions.Options.Options.Create(new OrchestraOptions
        {
            DatabasePath = Path.Combine(Folder, "data", "orchestra.db"),
            ProfilesPath = "profiles",
        });

        var environment = new TestEnvironment { ContentRootPath = Folder };

        Exporter = new ProfileExporter(Options, environment);

        var services = new ServiceCollection();
        services.AddSingleton(Database);

        _services = services.BuildServiceProvider();
        Scopes = _services.GetRequiredService<IServiceScopeFactory>();
    }

    public string Folder { get; }

    public OrchestraDbContext Database { get; }

    public IOptions<OrchestraOptions> Options { get; }

    public ProfileExporter Exporter { get; }

    public IServiceScopeFactory Scopes { get; }

    public string ProfilesFolder => Options.Value.ResolveProfilesPath(Folder);

    public ProfileEntity AddProfile(string id, string json, int version = 1)
    {
        var profile = new ProfileEntity
        {
            Id = id,
            Name = id,
            CurrentVersion = version,
            Versions =
            [
                new ProfileVersionEntity
                {
                    Id = Guid.NewGuid(),
                    ProfileId = id,
                    Version = version,
                    Json = json,
                    Reason = "test",
                    CreatedAt = DateTime.UtcNow,
                },
            ],
        };

        Database.Profiles.Add(profile);
        Database.SaveChanges();

        return profile;
    }

    public void Dispose()
    {
        _services.Dispose();
        Database.Dispose();
        _connection.Dispose();

        if (Directory.Exists(Folder))
        {
            Directory.Delete(Folder, recursive: true);
        }
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Orchestra.Api.Tests";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
