using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Plan 0012 D12: the engine folder is found, not asked for — the option wins, otherwise the
/// first folder above the content root that holds <c>engine/run.ps1</c>. The published app finds its own
/// copy beside the exe; <c>dotnet run</c> finds the checkout.</summary>
public sealed class EngineLocator(IOptions<OrchestraOptions> options, IWebHostEnvironment environment)
{
    public string? Find()
    {
        var configured = options.Value.EnginePath;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            var folder = SafeFull(configured);

            if (folder is not null && HasRun(folder))
            {
                return folder;
            }
        }

        var current = SafeFull(environment.ContentRootPath);

        while (current is not null)
        {
            if (HasRun(current))
            {
                return current;
            }

            var parent = Path.GetDirectoryName(current);

            if (string.IsNullOrEmpty(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = parent;
        }

        return null;
    }

    /// <summary>Where the project templates of phase C live: <c>&lt;found folder&gt;/engine/templates/project</c>
    /// (<see cref="Find"/> answers the folder that holds <c>engine/</c>, as the shim's <c>enginePath</c> does).</summary>
    public string? TemplatesRoot => Find() is { } engine ? Path.Combine(engine, "engine", "templates", "project") : null;

    private static bool HasRun(string folder)
    {
        return File.Exists(Path.Combine(folder, "engine", "run.ps1"));
    }

    private static string? SafeFull(string path)
    {
        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
