using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library.Features.GetLibraryItem;
using Orchestra.Api.Modules.Library.Features.ListLibrary;
using Orchestra.Api.Modules.Library.Features.PlanPrompt;
using Orchestra.Api.Modules.Library.Features.ReadLibraryFile;

namespace Orchestra.Api.Modules.Library;

[ApiController]
[Route(ApiRoutes.Controller)]
public sealed class LibraryController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] ListLibraryHandler handler,
        [FromQuery] string? project,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? dir,
        CancellationToken cancellationToken)
    {
        var (items, error) = await handler.HandleAsync(project, category, status, sort, dir, cancellationToken);

        return error is null ? Ok(items) : Failure(error);
    }

    [HttpGet("{project}/{category}/item")]
    public async Task<IActionResult> Item(
        [FromServices] GetLibraryItemHandler handler,
        string project,
        string category,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        var (item, error) = await handler.HandleAsync(project, category, path ?? string.Empty, cancellationToken);

        return error is null ? Ok(item) : Failure(error);
    }

    [HttpGet("{project}/{category}/file")]
    public async Task<IActionResult> File(
        [FromServices] ReadLibraryFileHandler handler,
        string project,
        string category,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        var (file, error) = await handler.HandleAsync(project, category, path ?? string.Empty, cancellationToken);

        if (error is not null)
        {
            return Failure(error);
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (file!.ContentSecurityPolicy is not null)
        {
            Response.Headers["Content-Security-Policy"] = file.ContentSecurityPolicy;
        }

        if (file.ContentDisposition is not null)
        {
            Response.Headers["Content-Disposition"] = file.ContentDisposition;
        }

        // Opened read-only and shared, so an import or the owner's editor never blocks a read.
        var stream = new FileStream(
            file.FullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return File(stream, file.ContentType);
    }

    [HttpGet("{project}/plan/prompt")]
    public async Task<IActionResult> Prompt(
        [FromServices] PlanPromptHandler handler,
        string project,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        var (text, error) = await handler.HandleAsync(project, path ?? string.Empty, cancellationToken);

        return error is null ? Ok(new PlanPromptDto(text!)) : Failure(error);
    }

    private IActionResult Failure(Error error)
    {
        var problem = ProblemResults.From(error, HttpContext);

        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}

public sealed record PlanPromptDto(string Text);
