namespace Orchestra.Api.Modules.Events;

/// <summary>The event kinds the engine sends (plan 0004 §6.1). An unknown kind is refused, never
/// stored: the schema is the contract, and a typo must not become a silent new kind.</summary>
public static class EventKinds
{
    public const string RunCreated = "run.created";

    public const string History = "history";

    public const string Status = "status";

    public const string Progress = "progress";

    public const string PoolWaiting = "pool.waiting";

    public const string PoolAcquired = "pool.acquired";

    public const string PoolReleased = "pool.released";

    public const string LedgerWritten = "ledger.written";

    public const string RunClosed = "run.closed";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        RunCreated,
        History,
        Status,
        Progress,
        PoolWaiting,
        PoolAcquired,
        PoolReleased,
        LedgerWritten,
        RunClosed,
    };

    public static bool IsKnown(string? kind) => kind is not null && All.Contains(kind);
}
