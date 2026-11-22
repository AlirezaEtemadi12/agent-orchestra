using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Events.Features.ReceiveEvent;

namespace Orchestra.Api.Modules.Events;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class EventsController : ControllerBase
{
    /// <summary>D1: the runner's one write. 202 with no body, whatever the engine sent (D4).</summary>
    [HttpPost]
    [ReportTokenFilter]
    public async Task<IActionResult> Post(
        [FromBody] EventEnvelope? envelope,
        [FromServices] ReceiveEventHandler handler,
        CancellationToken cancellationToken)
    {
        if (Request.ContentLength > ReceiveEventHandler.MaxBodyBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var error = await handler.HandleAsync(envelope, cancellationToken);

        if (error is null)
        {
            return Accepted();
        }

        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
