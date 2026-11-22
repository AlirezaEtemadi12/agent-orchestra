using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Runs.Features.Board;
using Orchestra.Api.Modules.Runs.Features.GetRun;
using Orchestra.Api.Modules.Runs.Features.ListRuns;

namespace Orchestra.Api.Modules.Runs;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class RunsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] ListRunsHandler handler,
        [FromQuery] string? project,
        [FromQuery] string? status,
        [FromQuery] string? repo,
        CancellationToken cancellationToken)
    {
        var (runs, error) = await handler.HandleAsync(project, status, repo, cancellationToken);

        return error is null ? Ok(runs) : Failure(error);
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
