using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import.Ledger;

public sealed class LedgerBlock
{
    public string File { get; init; } = string.Empty;

    public string Heading { get; init; } = string.Empty;

    /// <summary>As written in the file: local time, no offset.</summary>
    public DateTime LocalTime { get; init; }

    public string TaskId { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? Repo { get; set; }

    public string? Status { get; set; }

    public int? PlanVersion { get; set; }

    public string? Branch { get; set; }

    public List<StageRow> Stages { get; } = [];

    public List<SessionUsageEntity> Sessions { get; } = [];

    public List<StageUsageEntity> StageUsage { get; } = [];

    public long? TotalCostMicros { get; set; }

    public bool IsClose => Kind == "close";
}

public sealed class StageRow
{
    public StageKind Stage { get; init; }

    public int? Round { get; init; }

    public int? Phase { get; init; }

    public string? Adapter { get; init; }

    public string? Model { get; init; }

    public string? Verdict { get; init; }

    public int? Blockers { get; init; }

    public int? Should { get; init; }

    public int? Nits { get; init; }

    public int? Changed { get; init; }

    public int? OutsidePlan { get; init; }

    public int? Violations { get; init; }

    public bool? GatesGreen { get; init; }

    public string? GatesNote { get; init; }

    public string? ModelVerdict { get; init; }
}

public sealed record LedgerError(string File, string Heading, int Line, string Message);

public sealed class LedgerParseResult
{
    public List<LedgerBlock> Blocks { get; } = [];

    public List<LedgerError> Errors { get; } = [];
}
