namespace Orchestra.Api.Modules.Projects.Features.ListProjects;

public sealed record ProjectDto(
    string Id,
    string Name,
    int RunCount,
    int OpenRunCount,
    long Sessions,
    long Calls,
    long Requests,
    decimal? CostUsd,
    LastImportDto? LastImport,
    string? Origin,
    string? RootPath,
    string? RunsPath,
    IReadOnlyList<string> Repos,
    DateTime? LastEventAt);

public sealed record LastImportDto(DateTime FinishedAt, int ErrorCount);
