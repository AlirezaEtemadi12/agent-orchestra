using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.CreateProject;
using Orchestra.Api.Modules.Projects.Features.DeleteProject;
using Orchestra.Api.Modules.Projects.Features.EffectiveConfig;
using Orchestra.Api.Modules.Projects.Features.ListProjects;
using Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;
using Orchestra.Api.Modules.Projects.Features.StoreState;
using Orchestra.Api.Modules.Projects.Features.UpdateProject;

namespace Orchestra.Api.Modules.Projects;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class ProjectsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] ListProjectsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpGet("store")]
    public IActionResult Store([FromServices] StoreStateHandler handler)
    {
        return Ok(handler.Handle());
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromServices] CreateProjectHandler handler,
        [FromBody] ProjectInput input,
        CancellationToken cancellationToken)
    {
        var (saved, error) = await handler.HandleAsync(input, cancellationToken);

        return error is null ? StatusCode(StatusCodes.Status201Created, saved) : Failure(error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        [FromServices] UpdateProjectHandler handler,
        string id,
        [FromBody] ProjectInput input,
        CancellationToken cancellationToken)
    {
        var (saved, error) = await handler.HandleAsync(id, input, cancellationToken);

        return error is null ? Ok(saved) : Failure(error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteProjectHandler handler,
        string id,
        CancellationToken cancellationToken)
    {
        var error = await handler.HandleAsync(id, cancellationToken);

        return error is null ? NoContent() : Failure(error);
    }

    /// <summary>Plan 0006 §6.2: the project's effective values, read-only.</summary>
    [HttpGet("{id}/effective")]
    public async Task<IActionResult> Effective(
        [FromServices] EffectiveConfigHandler handler,
        string id,
        CancellationToken cancellationToken)
    {
        var (config, error, detail) = await handler.HandleAsync(id, cancellationToken);

        return error is null ? Ok(config) : Failure(error, detail);
    }

    [HttpPost("check")]
    public IActionResult Check(
        [FromServices] CheckFolderHandler handler,
        [FromBody] FolderCheckRequest input)
    {
        return Ok(handler.Handle(input?.RootPath));
    }

    [HttpPost("scaffold-prompt")]
    public IActionResult Prompt(
        [FromServices] ScaffoldPromptHandler handler,
        [FromBody] ScaffoldPromptRequest input)
    {
        var (prompt, error) = handler.Handle(input);

        return error is null ? Ok(prompt) : Failure(error);
    }

    private IActionResult Failure(Error error, string? detail = null)
    {
        var problem = ProblemResults.From(error, HttpContext, detail);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}

public sealed record FolderCheckRequest(string? RootPath);
