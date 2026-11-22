namespace Orchestra.Api.Modules.Runs.Features.Board;

/// <summary><c>GET api/runs/board</c>: the lanes and whether the reporter is configured (D13).</summary>
public sealed record BoardDto(string Reporter, BoardLanesDto Lanes);

public sealed record BoardLanesDto(
    IReadOnlyList<BoardCardDto> Running,
    IReadOnlyList<BoardCardDto> NeedsYou,
    IReadOnlyList<BoardCardDto> Done,
    IReadOnlyList<BoardCardDto> Failed);

/// <summary>One card. <see cref="RunKey"/> is null until the import has seen the run, so the UI links
/// only what it can open. Status is the engine's own word (D15).</summary>
public sealed record BoardCardDto(
    string ProjectId,
    string? RunKey,
    string TaskId,
    string Status,
    string? PlanNumber,
    string? PlanTitle,
    string? Stage,
    string? Model,
    DateTime StartedAt,
    long ElapsedSec,
    string? LastLine,
    string? WaitingFor,
    string? Pool,
    string? HeldBy);
