using Gridify;
using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Runs.Features.Board;
using Orchestra.Api.Modules.Runs.Features.GetRun;
using Orchestra.Api.Modules.Runs.Features.RunsTable;

namespace Orchestra.Api.Modules.Runs;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class RunsController : ControllerBase
{
    [HttpGet("table")]
    public async Task<IActionResult> Table(
        [FromServices] RunsTableHandler handler,
        [FromQuery] string? project,
        [FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {
        var (page, error) = await handler.HandleAsync(project, query, cancellationToken);

        return error is null ? Ok(page) : Failure(error);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromServices] RunsSummaryHandler handler,
        [FromQuery] string? project,
        [FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {
        var (summary, error) = await handler.HandleAsync(project, query, cancellationToken);

        return error is null ? Ok(summary) : Failure(error);
    }

    [HttpGet("board")]
    public async Task<IActionResult> Board(
        [FromServices] BoardHandler handler,
        [FromQuery] string? project,
        CancellationToken cancellationToken)
    {
        var (board, error) = await handler.HandleAsync(project, cancellationToken);

        return error is null ? Ok(board) : Failure(error);
    }

    [HttpGet("{project}/{runKey}")]
    public async Task<IActionResult> Get(
        [FromServices] GetRunHandler handler,
        string project,
        string runKey,
        CancellationToken cancellationToken)
    {
        var (run, error) = await handler.HandleAsync(project, runKey, cancellationToken);

        return error is null ? Ok(run) : Failure(error);
    }

    private IActionResult Failure(Error error)
    {
        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
