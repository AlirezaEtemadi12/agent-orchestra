using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Modules.Runs.Features.Pools;

/// <summary>D10: pool state is the replay of the project's <c>pool.*</c> events, newest last.</summary>
public sealed class PoolsHandler(OrchestraDbContext database, TimeProvider clock)
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(6);

    private static readonly string[] Kinds = [EventKinds.PoolWaiting, EventKinds.PoolAcquired, EventKinds.PoolReleased];

    public async Task<IReadOnlyList<PoolDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var events = await database.RunEvents
            .AsNoTracking()
            .Where(item => Kinds.Contains(item.Kind))
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        var pools = new Dictionary<string, MutablePool>(StringComparer.Ordinal);

        foreach (var item in events
                     .OrderBy(stored => stored.At)
                     .ThenBy(stored => stored.Seq)
                     .ThenBy(stored => stored.ReceivedAt))
        {
            var data = EventData.Read(item.DataJson);
            var name = data.Text("pool") ?? "default";

            if (!pools.TryGetValue(name, out var pool))
            {
                pool = new MutablePool();
                pools[name] = pool;
            }

            pool.Tier ??= data.Text("tier");
            pool.CooldownUntil = data.Time("cooldownUntil");
            pool.ExhaustedUntil = data.Time("exhaustedUntil");

            var slot = data.Int("slot");

            switch (item.Kind)
            {
                case EventKinds.PoolWaiting:
                    pool.Waiting.RemoveAll(entry => string.Equals(entry.TaskId, item.TaskId, StringComparison.Ordinal));
                    pool.Waiting.Add((item.TaskId, item.At));
                    break;

                case EventKinds.PoolAcquired when slot is { } acquired:
                    var held = pool.Slot(acquired);
                    held.HeldBy = data.Text("heldBy") ?? item.TaskId;
                    held.Since = item.At;
                    held.LastAt = item.At;
                    pool.Waiting.RemoveAll(entry => string.Equals(entry.TaskId, item.TaskId, StringComparison.Ordinal));
                    break;

                case EventKinds.PoolReleased when slot is { } released:
                    var freed = pool.Slot(released);
                    freed.HeldBy = null;
                    freed.Since = null;
                    freed.LastAt = item.At;
                    break;
            }
        }

        return pools
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => ToDto(pair.Key, pair.Value, now))
            .ToList();
    }

    private static PoolDto ToDto(string name, MutablePool pool, DateTime now)
    {
        var slots = pool.Slots
            .OrderBy(pair => pair.Key)
            .Select(pair =>
            {
                var stale = now - pair.Value.LastAt > StaleAfter;

                return new PoolSlotDto(
                    pair.Key,
                    stale ? null : pair.Value.HeldBy,
                    stale ? null : pair.Value.Since,
                    stale && pair.Value.HeldBy is not null);
            })
            .ToList();

        var waiting = pool.Waiting
            .OrderBy(entry => entry.Since)
            .Select(entry => new PoolWaitingDto(entry.TaskId, entry.Since))
            .ToList();

        return new PoolDto(name, pool.Tier, slots, waiting, pool.CooldownUntil, pool.ExhaustedUntil);
    }

    private sealed class MutablePool
    {
        public string? Tier { get; set; }

        public Dictionary<int, MutableSlot> Slots { get; } = [];

        public List<(string TaskId, DateTime Since)> Waiting { get; } = [];

        public DateTime? CooldownUntil { get; set; }

        public DateTime? ExhaustedUntil { get; set; }

        public MutableSlot Slot(int number)
        {
            if (!Slots.TryGetValue(number, out var slot))
            {
                slot = new MutableSlot { LastAt = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
                Slots[number] = slot;
            }

            return slot;
        }
    }

    private sealed class MutableSlot
    {
        public string? HeldBy { get; set; }

        public DateTime? Since { get; set; }

        public DateTime LastAt { get; set; }
    }
}
