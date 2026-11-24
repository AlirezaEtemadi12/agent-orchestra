using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Setup.Features.Folders;

namespace Orchestra.Api.Modules.Setup;

/// <summary>Plan 0009, plan 0012 D5: which folders the browser may list. The module folder is
/// <c>Setup</c>, not <c>System</c>, which would shadow <c>System.*</c> for sibling namespaces.</summary>
[ApiController]
[Route(ApiRoutes.System)]
public sealed class SetupController : ControllerBase
{
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
