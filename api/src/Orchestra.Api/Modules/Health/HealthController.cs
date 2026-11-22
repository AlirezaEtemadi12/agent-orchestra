using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Health;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
