namespace Orchestra.Api.Modules.Runs.Entities;

public enum RunSource
{
    Ledger,
    Live,
    Archive,
    Both,

    /// <summary>A run the dashboard knows only from events: no file has reached it yet (plan 0004 D7).</summary>
    Events,
}

public enum StageKind
{
    Review,
    ReadBack,
    Implement,
    Verify,
}
