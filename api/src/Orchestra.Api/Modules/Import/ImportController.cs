using Gridify;
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

    [HttpGet("logs/table")]
    public async Task<IActionResult> LogsTable(
        [FromServices] ImportLogsTableHandler handler,
        [FromQuery] string? project,
        [FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {
        var (page, error) = await handler.HandleAsync(project, query, cancellationToken);

        return error is null ? Ok(page) : Failure(error);
    }

    private IActionResult Failure(Error error)
    {
        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
