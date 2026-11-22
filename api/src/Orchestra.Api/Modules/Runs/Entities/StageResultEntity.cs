namespace Orchestra.Api.Modules.Runs.Entities;

public sealed class StageResultEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public StageKind Stage { get; set; }

    public int? Round { get; set; }

    public int? Phase { get; set; }

    public string? Adapter { get; set; }

    public string? Model { get; set; }

    public string? Verdict { get; set; }

    public int? Blockers { get; set; }

    public int? Should { get; set; }

    public int? Nits { get; set; }

    public int? Changed { get; set; }

    public int? OutsidePlan { get; set; }

    public int? Violations { get; set; }

    public bool? GatesGreen { get; set; }

    public string? GatesNote { get; set; }

    public string? ModelVerdict { get; set; }

    public bool IsFinal { get; set; }

    public DateTime? At { get; set; }

    public RunEntity Run { get; set; } = null!;
}
