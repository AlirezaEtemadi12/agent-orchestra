using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events.Features.ReceiveEvent;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Events;

/// <summary>An in-memory database with one config project and a clock the test controls — the shape
/// the event tests ask questions about.</summary>
internal sealed class EventsFixture : IDisposable
{
    public const string Project = "events-project";

    private readonly SqliteConnection _connection;

    public EventsFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        Database.Database.EnsureCreated();

        Settings = Options.Create(new OrchestraOptions
        {
            Projects =
            [
                new ProjectOptions
                {
                    Id = Project,
                    Name = "Events project",
                    RunsPath = Path.Combine(Path.GetTempPath(), "orchestra-events-runs"),
                },
            ],
        });

        Registry = TestRegistries.ConfigOnly(Settings);
        Clock = new FixedClock(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(3.5)));
        Handler = new ReceiveEventHandler(Database, Registry, Clock);
    }

    public OrchestraDbContext Database { get; }

    public IOptions<OrchestraOptions> Settings { get; }

    public ProjectRegistry Registry { get; }

    public FixedClock Clock { get; }

    public ReceiveEventHandler Handler { get; }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
}
