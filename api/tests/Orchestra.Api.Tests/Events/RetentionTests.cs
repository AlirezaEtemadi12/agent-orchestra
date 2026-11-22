using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Tests.Events;

public sealed class RetentionTests : IDisposable
{
    private readonly EventsFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Progress_older_than_seven_days_goes_and_everything_else_stays()
    {
        var now = _fixture.Clock.GetUtcNow().UtcDateTime;

        await Add(EventKinds.Progress, now.AddDays(-8));
        await Add(EventKinds.Progress, now.AddDays(-6));
        await Add(EventKinds.History, now.AddDays(-30));
        await Add(EventKinds.Status, now.AddDays(-365));

        var deleted = await EventRetentionService.DeleteExpiredProgressAsync(
            _fixture.Database, now, CancellationToken.None);

        deleted.Should().Be(1);

        var left = await _fixture.Database.RunEvents.Select(stored => stored.Kind).ToListAsync();
        left.Should().BeEquivalentTo([EventKinds.Progress, EventKinds.History, EventKinds.Status]);
    }

    private Task Add(string kind, DateTime at)
    {
        _fixture.Database.RunEvents.Add(new RunEventEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = EventsFixture.Project,
            TaskId = "T-EV-002",
            At = at,
            Seq = 1,
            Kind = kind,
            DataJson = "{}",
            ReceivedAt = at,
        });

        return _fixture.Database.SaveChangesAsync();
    }
}
