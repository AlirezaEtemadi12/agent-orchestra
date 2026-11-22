namespace Orchestra.Api.Modules.Usage.Features.ModelQuality;

public sealed record ReviewerQualityDto(
    string Model,
    long Runs,
    double? AvgRounds,
    double? PassFirstRound,
    double? FindingsShare,
    long FinalBlockers,
    long FinalShould,
    long FinalNits);

public sealed record ReadBackQualityDto(
    string Model,
    long Runs,
    double? FinalFindingsShare,
    long FinalBlockers,
    long FinalShould,
    long FinalNits);

public sealed record ImplementerQualityDto(
    string Model,
    long Phases,
    long NeedInput,
    double? ChangedAvg,
    long OutsidePlan,
    long Violations,
    long VerifyRuns,
    double? VerifyFailRate);

public sealed record StageTimeDto(string Stage, string Model, long Runs, double? AvgSeconds);

public sealed record CacheQualityDto(string Model, long Input, long CacheRead, double? HitRate);

public sealed record ModelQualityDto(
    IReadOnlyList<ReviewerQualityDto> Reviewers,
    IReadOnlyList<ReadBackQualityDto> ReadBacks,
    IReadOnlyList<ImplementerQualityDto> Implementers,
    IReadOnlyList<StageTimeDto> StageTimes,
    IReadOnlyList<CacheQualityDto> Cache);
