namespace Orchestra.Api.Modules.Runs.Features.Pools;

/// <summary><c>GET api/pools</c>, derived from the latest pool events (D10): there is no pool table.</summary>
public sealed record PoolDto(
    string Pool,
    string? Tier,
    IReadOnlyList<PoolSlotDto> Slots,
    IReadOnlyList<PoolWaitingDto> Waiting,
    DateTime? CooldownUntil,
    DateTime? ExhaustedUntil);

/// <summary><see cref="Unknown"/> is true when the slot's last event is older than six hours: a slot
/// nobody has reported on is not shown as held.</summary>
public sealed record PoolSlotDto(int N, string? HeldBy, DateTime? Since, bool Unknown);

public sealed record PoolWaitingDto(string TaskId, DateTime Since);
