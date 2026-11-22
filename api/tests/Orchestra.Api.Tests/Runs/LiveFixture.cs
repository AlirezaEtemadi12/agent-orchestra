using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Modules.Runs.Features.Board;
using Orchestra.Api.Modules.Runs.Features.Pools;
using Orchestra.Api.Tests.Events;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Runs;

/// <summary>An in-memory database with one config project, a fixed clock and the two live handlers —
/// the shape the board and pools tests ask questions about.</summary>
internal sealed class LiveFixture : IDisposable
{
    public const string Project = "live-project";

    private readonly SqliteConnection _connection;

    public LiveFixture(string? reportToken = null, DateTime? now = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        Database.Database.EnsureCreated();

        Settings = Options.Create(new OrchestraOptions
        {
            ReportToken = reportToken,
            Projects =
            [
                new ProjectOptions
                {
                    Id = Project,
                    Name = "Live project",
                    RunsPath = Path.Combine(Path.GetTempPath(), "orchestra-live-runs"),
                },
            ],
        });

        Registry = TestRegistries.ConfigOnly(Settings);
        Now = now ?? new DateTime(2026, 11, 20, 7, 0, 0, DateTimeKind.Utc);
        Clock = new FixedClock(new DateTimeOffset(Now, TimeSpan.Zero));

        Board = new BoardHandler(Database, Registry, Settings, Clock);
        Pools = new PoolsHandler(Database, Clock);
    }

    public OrchestraDbContext Database { get; }

    public IOptions<OrchestraOptions> Settings { get; }

    public ProjectRegistry Registry { get; }

    public FixedClock Clock { get; }

    public DateTime Now { get; }

    public BoardHandler Board { get; }

    public PoolsHandler Pools { get; }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();
    }

    public ProjectEntity AddProject()
    {
        var project = new ProjectEntity { Id = Project, Name = "Live project", RunsPath = "runs" };

        Database.Projects.Add(project);
        Database.SaveChanges();

        return project;
    }

    public RunEntity AddRun(
        string taskId,
        string? branch,
        string status,
        string runKey,
        DateTime createdAt,
        string? planNumber = null,
        string? planTitle = null)
    {
        var run = new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = Project,
            RunKey = runKey,
            TaskId = taskId,
            Branch = branch,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            PlanNumber = planNumber,
            PlanTitle = planTitle,
        };

        Database.Runs.Add(run);
        Database.SaveChanges();

        return run;
    }

    public RunEventEntity AddEvent(
        string kind,
        Dictionary<string, object?> data,
        DateTime at,
        string taskId = "T-LIVE-001",
        string? branch = "plan-0159-live",
        long seq = 1)
    {
        var entity = new RunEventEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = Project,
            TaskId = taskId,
            Branch = branch,
            At = at,
            Seq = seq,
            Kind = kind,
            DataJson = JsonSerializer.Serialize(data),
            ReceivedAt = at,
        };

        Database.RunEvents.Add(entity);
        Database.SaveChanges();

        return entity;
    }
}
