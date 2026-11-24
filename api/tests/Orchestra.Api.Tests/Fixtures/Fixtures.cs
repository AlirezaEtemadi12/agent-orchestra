using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Tests.Fixtures;

internal static class FixtureFiles
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Text(string name) => File.ReadAllText(Path.Combine(Root, name));

    public static string RunsRoot => Path.Combine(Root, "runs");

    public static string LedgerShapes => Text("ledger.shapes.md");

    public static string LedgerLifecycle => Text("ledger.lifecycle.md");

    public static string LedgerCorrupt => Text("ledger.corrupt.md");
}

internal static class TestRegistries
{
    /// <summary>Config projects only: the store file does not exist, so the dashboard owns nothing.</summary>
    public static ProjectRegistry ConfigOnly(IOptions<OrchestraOptions> options)
    {
        var path = Path.Combine(Path.GetTempPath(), $"orchestra-projects-{Guid.NewGuid():n}", "projects.json");

        return new ProjectRegistry(options, new ProjectStore(path), NullLogger<ProjectRegistry>.Instance);
    }
}
