using Gridify;
using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Profiles.Features.GetProfile;
using Orchestra.Api.Modules.Profiles.Features.GetVersions;
using Orchestra.Api.Modules.Profiles.Features.ListProfiles;
using Orchestra.Api.Modules.Profiles.Features.RevertProfile;
using Orchestra.Api.Modules.Profiles.Features.SaveProfile;

namespace Orchestra.Api.Modules.Profiles;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class ProfilesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] ListProfilesHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(
        [FromServices] GetProfileHandler handler,
        string id,
        [FromQuery] int? version,
        CancellationToken cancellationToken)
    {
        var (profile, error) = await handler.HandleAsync(id, version, cancellationToken);

        return error is null ? Ok(profile) : Failure(error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Save(
        [FromServices] SaveProfileHandler handler,
        string id,
        [FromBody] SaveProfileInput? input,
        CancellationToken cancellationToken)
    {
        var (saved, error, detail) = await handler.HandleAsync(id, input, cancellationToken);

        return error is null ? Ok(saved) : Failure(error, detail);
    }

    [HttpPost("{id}/revert")]
    public async Task<IActionResult> Revert(
        [FromServices] RevertProfileHandler handler,
        string id,
        [FromBody] RevertProfileInput? input,
        CancellationToken cancellationToken)
    {
        var (saved, error, detail) = await handler.HandleAsync(id, input, cancellationToken);

        return error is null ? Ok(saved) : Failure(error, detail);
    }

    [HttpGet("{id}/versions")]
    public async Task<IActionResult> Versions(
        [FromServices] GetVersionDiffHandler handler,
        string id,
        [FromQuery] string? diff,
        CancellationToken cancellationToken)
    {
        var (changes, error, detail) = await handler.HandleAsync(id, diff, cancellationToken);

        return error is null ? Ok(changes) : Failure(error, detail);
    }

    [HttpGet("{id}/versions/table")]
    public async Task<IActionResult> VersionsTable(
        [FromServices] ProfileVersionsTableHandler handler,
        string id,
        [FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {
        var (page, error) = await handler.HandleAsync(id, query, cancellationToken);

        return error is null ? Ok(page) : Failure(error);
    }

    private IActionResult Failure(Error error, string? detail = null)
    {
        var problem = ProblemResults.From(error, HttpContext, detail);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
