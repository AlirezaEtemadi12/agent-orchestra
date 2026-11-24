namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Plan 0012 §6.3.1: what the setup screen sends, for a plan and for an apply alike. The server
/// recomputes the plan on apply, so a plan the browser kept is never trusted.</summary>
public sealed record SetupRequest(
    string RootPath,
    string Name,
    string Id,
    IReadOnlyList<string> Repos,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? VerifyCommands,
    IReadOnlyList<string>? Skip);

/// <summary>One row of the checklist. <c>Id</c> is stable across calls, so a tick the owner removed stays
/// removed while the rest of the plan is recomputed.</summary>
public sealed record SetupItemDto(
    string Id,
    string Path,
    string Action,
    bool Optional,
    string? Detail);

public sealed record SetupPlanDto(
    bool EngineFound,
    bool IsSingleRepo,
    IReadOnlyList<SetupItemDto> Items,
    IReadOnlyDictionary<string, IReadOnlyList<string>> VerifyCommands);

public sealed record SetupResultItemDto(string Id, bool Ok, string? Error, string? Detail);

public sealed record SetupResultDto(
    IReadOnlyList<SetupResultItemDto> Items,
    ProjectSavedDto Project,
    string KnowledgePrompt);

/// <summary>The four actions and the four row errors, spelled once (plan 0012 §6.3.1, §6.6).</summary>
public static class SetupActions
{
    public const string Create = "create";
    public const string Add = "add";
    public const string Update = "update";
    public const string Exists = "exists";
}

public static class SetupItemErrors
{
    public const string WriteFailed = "Setup.Item.WriteFailed";
    public const string GitInitFailed = "Setup.Item.GitInitFailed";
    public const string InvalidJson = "Setup.Item.InvalidJson";
    public const string Skipped = "Setup.Item.Skipped";
}
