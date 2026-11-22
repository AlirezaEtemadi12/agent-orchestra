using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;

namespace Orchestra.Api.Modules.Import;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class ImportController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromServices] ImportService service,
        CancellationToken cancellationToken)
    {
        if (!service.TryEnter())
        {
            var problem = ProblemResults.From(Error.ImportAlreadyRunning, HttpContext);

            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        try
        {
            return Ok(await service.ImportAllAsync(cancellationToken));
        }
        finally
        {
            service.Leave();
        }
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs(
        [FromServices] ImportLogsHandler handler,
        [FromQuery] string? project,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await handler.HandleAsync(project, take, cancellationToken));
    }
}
