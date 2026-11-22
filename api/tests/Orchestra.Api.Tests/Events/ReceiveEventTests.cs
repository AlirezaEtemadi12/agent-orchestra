using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Tests.Events;

public sealed class ReceiveEventTests : IDisposable
{
    private readonly EventsFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    public static IEnumerable<object[]> Kinds() => EventKinds.All.Select(kind => new object[] { kind });

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Every_known_kind_is_stored(string kind)
    {
        var id = Guid.NewGuid();

        var error = await _fixture.Handler.HandleAsync(Envelope(kind, id, data: "{\"round\":2}"), CancellationToken.None);

        error.Should().BeNull();

        var stored = await _fixture.Database.RunEvents.SingleAsync();
        stored.Id.Should().Be(id);
        stored.Kind.Should().Be(kind);
        stored.ProjectId.Should().Be(EventsFixture.Project);
        stored.TaskId.Should().Be("T-EV-001");
        stored.Branch.Should().Be("alireza/events");
        stored.DataJson.Should().Be("{\"round\":2}");
        stored.Seq.Should().Be(7);
    }

    [Fact]
    public async Task An_unknown_project_is_422_and_stores_nothing()
    {
        var error = await _fixture.Handler.HandleAsync(
            Envelope(EventKinds.Status, Guid.NewGuid(), project: "no-such-project"), CancellationToken.None);

        error.Should().Be(Error.EventProjectUnknown);
        error!.Status.Should().Be(422);
        (await _fixture.Database.RunEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task An_unknown_kind_is_422_and_stores_nothing()
    {
        var error = await _fixture.Handler.HandleAsync(
            Envelope("stage.exploded", Guid.NewGuid()), CancellationToken.None);

        error.Should().Be(Error.EventKindUnknown);
        (await _fixture.Database.RunEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_body_that_is_not_schema_1_is_400()
    {
        var old = Envelope(EventKinds.Status, Guid.NewGuid(), schema: 2);

        var error = await _fixture.Handler.HandleAsync(old, CancellationToken.None);

        error.Should().Be(Error.EventBodyInvalid);
        (await _fixture.Database.RunEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task An_offset_time_is_stored_as_utc()
    {
        var envelope = Envelope(
            EventKinds.Progress,
            Guid.NewGuid(),
            at: new DateTimeOffset(2026, 11, 20, 10, 2, 11, TimeSpan.FromHours(3.5)));

        await _fixture.Handler.HandleAsync(envelope, CancellationToken.None);

        var stored = await _fixture.Database.RunEvents.SingleAsync();
        stored.At.Kind.Should().Be(DateTimeKind.Utc);
        stored.At.Should().Be(new DateTime(2026, 11, 20, 6, 32, 11, DateTimeKind.Utc));
        stored.ReceivedAt.Should().Be(new DateTime(2026, 11, 20, 6, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task A_duplicate_id_is_stored_once_and_accepted_again()
    {
        var id = Guid.NewGuid();

        var first = await _fixture.Handler.HandleAsync(Envelope(EventKinds.History, id), CancellationToken.None);
        var second = await _fixture.Handler.HandleAsync(Envelope(EventKinds.History, id), CancellationToken.None);

        first.Should().BeNull();
        second.Should().BeNull();
        (await _fixture.Database.RunEvents.CountAsync()).Should().Be(1);
    }

    private static EventEnvelope Envelope(
        string kind,
        Guid id,
        string project = EventsFixture.Project,
        string taskId = "T-EV-001",
        string data = "{}",
        int schema = 1,
        DateTimeOffset? at = null)
    {
        return new EventEnvelope
        {
            Schema = schema,
            Id = id,
            Project = project,
            TaskId = taskId,
            Branch = "alireza/events",
            At = at ?? new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(3.5)),
            Seq = 7,
            Kind = kind,
            Data = System.Text.Json.JsonDocument.Parse(data).RootElement.Clone(),
        };
    }
}
