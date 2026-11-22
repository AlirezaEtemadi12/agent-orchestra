using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Usage.Features.CostSummary;
using Orchestra.Api.Modules.Usage.Features.ModelQuality;

namespace Orchestra.Api.Modules.Usage;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class UsageController : ControllerBase
{
    [HttpGet("cost")]
    public async Task<IActionResult> Cost(
        [FromServices] CostSummaryHandler handler,
        [FromQuery] string? project,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? groupBy,
        [FromQuery] string? tz,
        CancellationToken cancellationToken)
    {
        var (rows, error) = await handler.HandleAsync(project, from, to, groupBy, tz, cancellationToken);

        return error is null ? Ok(rows) : Failure(error);
    }

    [HttpGet("quality")]
    public async Task<IActionResult> Quality(
        [FromServices] ModelQualityHandler handler,
        [FromQuery] string? project,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(project, cancellationToken));
    }

    private IActionResult Failure(Error error)
    {
        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
