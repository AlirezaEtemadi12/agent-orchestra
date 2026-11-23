using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Setup.Features.Folders;
using Orchestra.Api.Modules.Setup.Features.Hosting;

namespace Orchestra.Api.Modules.Setup;

/// <summary>Plan 0009: where the dashboard is running and which folders it can see. The module folder
/// is <c>Setup</c>, not <c>System</c>, which would shadow <c>System.*</c> for sibling namespaces.</summary>
[ApiController]
[Route(ApiRoutes.System)]
public sealed class SetupController : ControllerBase
{
    [HttpGet("hosting")]
    public IActionResult Hosting([FromServices] HostingHandler handler)
    {
        return Ok(handler.Handle());
    }

    [HttpGet("folders")]
    public IActionResult Folders(
        [FromServices] FoldersHandler handler,
        [FromQuery] string? path)
    {
        var (page, error) = handler.Handle(path);

        if (error is null)
        {
            return Ok(page);
        }

        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
