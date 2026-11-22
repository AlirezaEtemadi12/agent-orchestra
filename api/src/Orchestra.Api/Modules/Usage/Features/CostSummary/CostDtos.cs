namespace Orchestra.Api.Modules.Usage.Features.CostSummary;

public sealed record CostRowDto(
    string Key,
    decimal? CostUsd,
    long Runs,
    long Sessions,
    long Calls,
    long Input,
    long Output,
    long CacheRead);
