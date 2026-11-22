using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import.Tasks;

public sealed class TaskSnapshot
{
    public string File { get; init; } = string.Empty;

    public string? TaskId { get; set; }

    public string? Repo { get; set; }

    public string? Task { get; set; }

    public string? Status { get; set; }

    public string? Branch { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? Round { get; set; }

    public int? PlanVersion { get; set; }

    public string? PlanNumber { get; set; }

    public string? PlanTitle { get; set; }

    public StageSnapshot? Review { get; set; }

    public StageSnapshot? ReadBack { get; set; }

    public StageSnapshot? Implement { get; set; }

    public StageSnapshot? Verify { get; set; }

    public List<StageSnapshot> Phases { get; } = [];

    public List<StageSnapshot> History { get; } = [];
}

public sealed class StageSnapshot
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

    public DateTime? At { get; init; }
}

public sealed record TaskSnapshotResult(TaskSnapshot? Snapshot, string? Error);
