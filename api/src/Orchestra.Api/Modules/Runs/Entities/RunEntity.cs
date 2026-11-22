using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Runs.Entities;

public sealed class RunEntity
{
    public Guid Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public string RunKey { get; set; } = string.Empty;

    public string TaskId { get; set; } = string.Empty;

    public string? Repo { get; set; }

    public string? Task { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Branch { get; set; }

    public int? PlanVersion { get; set; }

    public string? PlanNumber { get; set; }

    public string? PlanTitle { get; set; }

    public int? Rounds { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public long? CostMicros { get; set; }

    public RunSource Source { get; set; }

    public string? SourceRef { get; set; }

    /// <summary>Plan 0006 D10: the profile-merged config the run started with, from its archive or its live
    /// <c>config.effective.json</c>. Null when the run never recorded one.</summary>
    public string? EffectiveConfigJson { get; set; }

    public ProjectEntity Project { get; set; } = null!;

    public List<StageResultEntity> Stages { get; set; } = [];

    public List<StageTextEntity> Texts { get; set; } = [];

    public List<SessionUsageEntity> Sessions { get; set; } = [];

    public List<StageUsageEntity> StageUsage { get; set; } = [];
}
