using Microsoft.Extensions.FileProviders;
using Orchestra.Api.Common.Errors;

namespace Orchestra.Api.Common.Ui;

public static class UiHosting
{
    private const string NotFoundFile = "404.html";
    private const string IndexFile = "index.html";

    public static void UseUi(this WebApplication app, OrchestraOptions options)
    {
        var root = options.ResolveUiPath(app.Environment.ContentRootPath);

        if (!Directory.Exists(root))
        {
            app.Logger.LogWarning("The UI folder {UiPath} does not exist; the API still serves.", root);
            return;
        }

        var provider = new PhysicalFileProvider(root);

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
    }

    public static void MapUiFallback(this WebApplication app, OrchestraOptions options)
    {
        var root = options.ResolveUiPath(app.Environment.ContentRootPath);

        app.MapFallback(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/" + ApiRoutes.Base, StringComparison.OrdinalIgnoreCase))
            {
                var problem = ProblemResults.From(Error.NotFound, context);

                context.Response.StatusCode = problem.Status ?? StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(problem);
                return;
            }

            if (!HttpMethods.IsGet(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // The export writes trailingSlash pages as <path>/index.html; a directory request is one of them.
            var index = ResolveInside(root, context.Request.Path.Value, IndexFile);

            if (index is not null && File.Exists(index))
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(index);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/html; charset=utf-8";

            var notFound = Path.Combine(root, NotFoundFile);

            if (File.Exists(notFound))
            {
                await context.Response.SendFileAsync(notFound);
            }
        });
    }

    private static string? ResolveInside(string root, string? requestPath, string fileName)
    {
        var relative = Uri.UnescapeDataString((requestPath ?? "/").TrimStart('/'));

        if (relative.Contains('\0') || relative.Split('/').Any(part => part == ".."))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(root, relative, fileName));

        var rootWithSeparator = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }
}
