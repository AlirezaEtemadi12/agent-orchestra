using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

public sealed record ScaffoldPromptRequest(
    string? Name,
    string? RootPath,
    List<string>? Repos,
    FolderCheckDto? Check);

public sealed record ScaffoldPromptDto(string Text);

public sealed class ScaffoldPromptHandler
{
    public (ScaffoldPromptDto? Prompt, Error? Error) Handle(ScaffoldPromptRequest? request)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.RootPath)
            || request.Check is null)
        {
            return (null, Error.ProjectInvalid);
        }

        var text = ScaffoldPromptBuilder.Build(
            request.Name.Trim(),
            request.RootPath.Trim(),
            request.Repos ?? [],
            request.Check);

        return (new ScaffoldPromptDto(text), null);
    }
}
