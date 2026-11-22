namespace Orchestra.Api.Modules.Import;

public sealed record ImportResultDto(
    string ProjectId,
    DateTime StartedAt,
    DateTime FinishedAt,
    int FilesRead,
    int BlocksRead,
    int RunsBuilt,
    int LibraryItems,
    int ErrorCount);

public sealed record ImportLogDto(
    Guid Id,
    string ProjectId,
    DateTime StartedAt,
    DateTime FinishedAt,
    int FilesRead,
    int BlocksRead,
    int RunsBuilt,
    int LibraryItems,
    int ErrorCount,
    IReadOnlyList<ImportErrorDto> Errors);

public sealed record ImportErrorDto(string File, string? Location, string Message);
