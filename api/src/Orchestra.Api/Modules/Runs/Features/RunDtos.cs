namespace Orchestra.Api.Modules.Runs.Features;

public sealed record RunListItemDto(
    string ProjectId,
    string ProjectName,
    string RunKey,
    string TaskId,
    string? Repo,
    string Status,
    string? PlanNumber,
    string? PlanTitle,
    string? Task,
    string? Branch,
    long Sessions,
    long Calls,
    long Requests,
    decimal? CostUsd,
    int? Rounds,
    IReadOnlyList<string> Models,
    DateTime? UpdatedAt,
    DateTime? ClosedAt,
    string Source);

public sealed record StageResultDto(
    string Stage,
    int? Round,
    int? Phase,
    string? Adapter,
    string? Model,
    string? Verdict,
    int? Blockers,
    int? Should,
    int? Nits,
    int? Changed,
    int? OutsidePlan,
    int? Violations,
    bool? GatesGreen,
    string? GatesNote,
    string? ModelVerdict,
    DateTime? At);

public sealed record SessionUsageDto(
    string SessionId,
    string? FirstRole,
    string? Model,
    int? Calls,
    int? Requests,
    long? Input,
    long? Output,
    long? Reasoning,
    long? CacheRead,
    decimal? CostUsd);

public sealed record StageUsageDto(
    string Stage,
    string? Model,
    int? Sessions,
    int? Calls,
    int? Requests,
    long? Input,
    long? Output,
    long? Reasoning,
    long? CacheRead,
    int? Seconds,
    decimal? CostUsd);

public sealed record StageTextDto(string Stage, int? Round, string Name, string Text);

/// <summary>The library plan this run belongs to (plan 0002 §6.3).</summary>
public sealed record RunPlanDto(string RelPath, string? Number, string Title, string Status);

/// <summary>What the run is doing now, from its latest <c>progress</c> event (plan 0004 §6.1). Null when
/// nothing has reported in the last 24 hours: a run that stopped is not live.</summary>
public sealed record RunLiveDto(
    string? Stage,
    string? Model,
    string? LastLine,
    int? LastOutputAgoSec,
    string? Pool,
    string? HeldBy,
    DateTime At);

public sealed record RunDetailDto(
    string ProjectId,
    string ProjectName,
    string RunKey,
    string TaskId,
    string? Repo,
    string Status,
    string? PlanNumber,
    string? PlanTitle,
    string? Task,
    string? Branch,
    int? PlanVersion,
    long Sessions,
    long Calls,
    long Requests,
    decimal? CostUsd,
    int? Rounds,
    IReadOnlyList<string> Models,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ClosedAt,
    string Source,
    IReadOnlyList<StageResultDto> Stages,
    IReadOnlyList<StageResultDto> RoundRows,
    IReadOnlyList<SessionUsageDto> SessionRows,
    IReadOnlyList<StageUsageDto> StageUsage,
    IReadOnlyList<StageTextDto> Texts,
    decimal? TotalCostUsd,
    RunPlanDto? Plan,
    RunLiveDto? Live,
    string? EffectiveConfig);

public static class RunDtos
{
    /// <summary>Money is <c>long CostMicros</c> in the database; DTOs carry decimal USD
    /// (`.ai/api/conventions.md`). The runs table's <c>costUsd</c> grid key maps to <c>CostMicros</c> for
    /// ordering only, so a hand-typed <c>filter=costUsd&gt;…</c> compares micros, not dollars.</summary>
    public static decimal? ToUsd(long? micros)
    {
        return micros is null ? null : micros.Value / 1_000_000m;
    }
}
