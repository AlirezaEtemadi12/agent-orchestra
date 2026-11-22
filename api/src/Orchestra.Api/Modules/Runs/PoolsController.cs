using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Runs.Features.Pools;

namespace Orchestra.Api.Modules.Runs;

/// <summary>D12: its own controller, because anything on <c>RunsController</c> lives under
/// <c>api/runs/…</c> and the route is <c>api/pools</c>.</summary>
[ApiController]
[Route(ApiRoutes.Base + "/pools")]
public sealed class PoolsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] PoolsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }
}
