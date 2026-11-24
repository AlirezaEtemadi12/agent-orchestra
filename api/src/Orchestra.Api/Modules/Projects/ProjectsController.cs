using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.CreateProject;
using Orchestra.Api.Modules.Projects.Features.DeleteProject;
using Orchestra.Api.Modules.Projects.Features.EffectiveConfig;
using Orchestra.Api.Modules.Projects.Features.ListProjects;
using Orchestra.Api.Modules.Projects.Features.Setup;
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

    /// <summary>Plan 0012 D6: the checklist, without writing anything.</summary>
    [HttpPost("setup/plan")]
    public IActionResult SetupPlan(
        [FromServices] SetupPlanner planner,
        [FromServices] SetupGuard guard,
        [FromBody] SetupRequest input)
    {
        if (guard.CheckOrigin(Request) is { } origin)
        {
            return Failure(origin);
        }

        var check = guard.CheckRoot(input?.RootPath);

        if (check.Refusal is { } refusal)
        {
            return Failure(Error.SetupPathRefused, SetupRefusalText.For(refusal, Cultures()));
        }

        return Ok(planner.Plan(input!, check.Root!));
    }

    /// <summary>Plan 0012 D6: the same input, recomputed and executed.</summary>
    [HttpPost("setup/apply")]
    public async Task<IActionResult> SetupApply(
        [FromServices] SetupApplier applier,
        [FromServices] SetupGuard guard,
        [FromBody] SetupRequest input,
        CancellationToken cancellationToken)
    {
        if (guard.CheckOrigin(Request) is { } origin)
        {
            return Failure(origin);
        }

        var (result, error, refusal) = await applier.HandleAsync(input, cancellationToken);

        return error is null
            ? Ok(result)
            : Failure(error, refusal is { } value ? SetupRefusalText.For(value, Cultures()) : null);
    }

    private IReadOnlyList<System.Globalization.CultureInfo> Cultures()
    {
        return ErrorMessages.CulturesFrom(Request.Headers.AcceptLanguage.ToString());
    }

    private IActionResult Failure(Error error, string? detail = null)
    {
        var problem = ProblemResults.From(error, HttpContext, detail);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}

public sealed record FolderCheckRequest(string? RootPath);
