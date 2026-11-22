using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class RunAssemblerEventsTests : IDisposable
{
    private static readonly SourceClock Clock = SourceClock.From("Asia/Tehran");

    private static readonly DateTime At = new(2026, 11, 20, 6, 0, 0, DateTimeKind.Utc);

    private readonly string _runs = Path.Combine(Path.GetTempPath(), $"orchestra-import-{Guid.NewGuid():n}");

    public RunAssemblerEventsTests()
    {
        Directory.CreateDirectory(_runs);
    }

    public void Dispose()
    {
        if (Directory.Exists(_runs))
        {
            Directory.Delete(_runs, recursive: true);
        }
    }

    [Fact]
    public void A_history_event_carries_the_counts_and_a_run_only_events_know()
    {
        var runs = RunAssembler.Build(
            "demo",
            [],
            [],
            [],
            [
                Event(EventKinds.History, new()
                {
                    ["event"] = "review",
                    ["round"] = 1,
                    ["verdict"] = "FINDINGS",
                    ["model"] = "opencode:opencode-go/glm-5.3-flash",
                    ["blockers"] = 2,
                    ["should"] = 1,
                    ["nits"] = 3,
                }),
            ],
            Clock);

        var run = runs.Should().ContainSingle().Subject;
        run.Source.Should().Be(RunSource.Events);
        run.Rounds.Should().Be(1);

        var stage = run.Stages.Should().ContainSingle().Subject;
        stage.Stage.Should().Be(StageKind.Review);
        stage.Blockers.Should().Be(2);
        stage.Should.Should().Be(1);
        stage.Nits.Should().Be(3);
        stage.Adapter.Should().Be("opencode");
        stage.Model.Should().Be("opencode-go/glm-5.3-flash");
        stage.IsFinal.Should().BeFalse();
    }

    [Fact]
    public void An_event_wins_over_the_task_json_row_of_the_same_round()
    {
        var task = new TaskSnapshot
        {
            File = "T-EV-001/task.json",
            TaskId = "T-EV-001",
            Repo = "Api",
            Task = "Plan 0200: the event round",
            Status = "Reviewing",
            Branch = "plan-0200-event-round",
            CreatedAt = At.AddHours(-3),
            UpdatedAt = At.AddHours(-2),
        };

        task.History.Add(new StageSnapshot
        {
            Stage = StageKind.Review,
            Round = 1,
            Verdict = "FINDINGS",
            Blockers = 1,
            Should = 0,
            Nits = 0,
            At = At.AddHours(-2),
        });

        var runs = RunAssembler.Build(
            "demo",
            [],
            [task],
            [],
            [
                Event(EventKinds.History, new()
                {
                    ["event"] = "review",
                    ["round"] = 1,
                    ["verdict"] = "FINDINGS",
                    ["blockers"] = 7,
                    ["should"] = 4,
                    ["nits"] = 0,
                }),
            ],
            Clock);

        var run = runs.Should().ContainSingle().Subject;
        var stage = run.Stages.Should().ContainSingle().Subject;

        stage.Blockers.Should().Be(7);
        stage.Should.Should().Be(4);
        stage.IsFinal.Should().BeFalse();
    }

    [Fact]
    public void A_status_event_newer_than_the_file_wins()
    {
        var task = new TaskSnapshot
        {
            File = "T-EV-002/task.json",
            TaskId = "T-EV-002",
            Status = "Implementing",
            Branch = "plan-0201-status",
            CreatedAt = At.AddHours(-6),
            UpdatedAt = At.AddHours(-4),
        };

        var runs = RunAssembler.Build(
            "demo",
            [],
            [task],
            [],
            [
                Event(EventKinds.Status, new() { ["from"] = "Planning", ["to"] = "Reviewing" }, At.AddHours(-5), "T-EV-002", branch: "plan-0201-status"),
                Event(EventKinds.Status, new() { ["from"] = "Reviewing", ["to"] = "AwaitingApproval" }, At.AddHours(-3), "T-EV-002", branch: "plan-0201-status", seq: 2),
            ],
            Clock);

        runs.Should().ContainSingle().Which.Status.Should().Be("AwaitingApproval");
    }

    [Fact]
    public void A_status_event_older_than_the_file_is_ignored()
    {
        var task = new TaskSnapshot
        {
            File = "T-EV-004/task.json",
            TaskId = "T-EV-004",
            Status = "Implementing",
            Branch = "plan-0202-older",
            CreatedAt = At.AddHours(-6),
            UpdatedAt = At.AddHours(-1),
        };

        var runs = RunAssembler.Build(
            "demo",
            [],
            [task],
            [],
            [
                Event(EventKinds.Status, new() { ["to"] = "Reviewing" }, At.AddHours(-3), "T-EV-004", branch: "plan-0202-older"),
            ],
            Clock);

        runs.Should().ContainSingle().Which.Status.Should().Be("Implementing");
    }

    [Fact]
    public async Task An_import_deletes_no_event()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(connection).Options);

        database.Database.EnsureCreated();

        var options = Options.Create(new OrchestraOptions
        {
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RunsPath = _runs }],
        });

        var imports = new ImportService(
            database,
            TestRegistries.ConfigOnly(options),
            options,
            TimeProvider.System,
            NullLogger<ImportService>.Instance);

        database.RunEvents.Add(new RunEventEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "demo",
            TaskId = "T-EV-003",
            At = At,
            Seq = 1,
            Kind = EventKinds.Status,
            DataJson = "{\"to\":\"Implementing\"}",
            ReceivedAt = At,
        });

        await database.SaveChangesAsync();

        await imports.ImportAllAsync(CancellationToken.None);

        database.RunEvents.Should().ContainSingle();
    }

    private static RunEventEntity Event(
        string kind,
        Dictionary<string, object?> data,
        DateTime? at = null,
        string taskId = "T-EV-001",
        string? branch = "plan-0200-event-round",
        long seq = 1)
    {
        return new RunEventEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "demo",
            TaskId = taskId,
            Branch = branch,
            At = at ?? At,
            Seq = seq,
            Kind = kind,
            DataJson = JsonSerializer.Serialize(data),
            ReceivedAt = at ?? At,
        };
    }
}
