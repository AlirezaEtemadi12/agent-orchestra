using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Runs.Features.Pools;

namespace Orchestra.Api.Tests.Runs;

public sealed class PoolsTests : IDisposable
{
    private readonly LiveFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task A_held_slot_and_a_waiting_task_are_both_shown()
    {
        _fixture.AddEvent(
            EventKinds.PoolAcquired,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-LIVE-001")),
            _fixture.Now.AddMinutes(-10),
            "T-LIVE-001");

        _fixture.AddEvent(
            EventKinds.PoolWaiting,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-LIVE-001")),
            _fixture.Now.AddMinutes(-5),
            "T-LIVE-002");

        var pool = (await PoolsAsync()).Should().ContainSingle().Subject;

        pool.Pool.Should().Be("agent");

        var slot = pool.Slots.Should().ContainSingle().Subject;
        slot.N.Should().Be(1);
        slot.HeldBy.Should().Be("T-LIVE-001");
        slot.Unknown.Should().BeFalse();

        pool.Waiting.Should().ContainSingle().Which.TaskId.Should().Be("T-LIVE-002");
    }

    [Fact]
    public async Task A_release_frees_the_slot_and_ends_the_wait()
    {
        _fixture.AddEvent(
            EventKinds.PoolAcquired,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-LIVE-001")),
            _fixture.Now.AddMinutes(-20),
            "T-LIVE-001");

        _fixture.AddEvent(
            EventKinds.PoolWaiting,
            Data(("pool", "agent"), ("slot", 1)),
            _fixture.Now.AddMinutes(-15),
            "T-LIVE-002");

        _fixture.AddEvent(
            EventKinds.PoolAcquired,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-LIVE-002")),
            _fixture.Now.AddMinutes(-10),
            "T-LIVE-002");

        var pool = (await PoolsAsync()).Should().ContainSingle().Subject;

        pool.Slots.Should().ContainSingle().Which.HeldBy.Should().Be("T-LIVE-002");
        pool.Waiting.Should().BeEmpty();
    }

    [Fact]
    public async Task Cooldown_and_exhaustion_come_from_the_latest_event()
    {
        _fixture.AddEvent(
            EventKinds.PoolReleased,
            Data(("pool", "agent"), ("slot", 1), ("cooldownUntil", "2026-11-20T08:30:00+00:00"), ("exhaustedUntil", "2026-11-21T00:00:00+00:00")),
            _fixture.Now.AddMinutes(-1));

        var pool = (await PoolsAsync()).Should().ContainSingle().Subject;

        pool.CooldownUntil.Should().Be(new DateTime(2026, 11, 20, 8, 30, 0, DateTimeKind.Utc));
        pool.ExhaustedUntil.Should().Be(new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task A_slot_nobody_reported_on_for_six_hours_is_unknown_not_held()
    {
        _fixture.AddEvent(
            EventKinds.PoolAcquired,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-LIVE-001")),
            _fixture.Now.AddHours(-8));

        var slot = (await PoolsAsync()).Should().ContainSingle().Subject.Slots.Single();

        slot.Unknown.Should().BeTrue();
        slot.HeldBy.Should().BeNull();
        slot.Since.Should().BeNull();
    }

    private async Task<IReadOnlyList<PoolDto>> PoolsAsync()
    {
        return await _fixture.Pools.HandleAsync(CancellationToken.None);
    }

    private static Dictionary<string, object?> Data(params (string Name, object? Value)[] pairs)
    {
        return pairs.ToDictionary(pair => pair.Name, pair => pair.Value);
    }
}
