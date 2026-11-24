using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Projects.Features.Setup;

namespace Orchestra.Api.Tests.Projects;

public sealed class EngineLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-engine-{Guid.NewGuid():n}");

    [Fact]
    public void The_option_wins_over_the_walk_up()
    {
        var checkout = Engine("checkout");
        var other = Engine("other");
        var content = Path.Combine(_root, "checkout", "api", "src");

        Directory.CreateDirectory(content);

        var locator = new EngineLocator(
            Options.Create(new OrchestraOptions { EnginePath = other }),
            new FakeWebHostEnvironment(content));

        locator.Find().Should().Be(other);
    }

    [Fact]
    public void The_walk_up_finds_the_checkout_above_the_content_root()
    {
        var checkout = Engine("checkout");
        var content = Path.Combine(checkout, "api", "src");

        Directory.CreateDirectory(content);

        var locator = new EngineLocator(
            Options.Create(new OrchestraOptions()),
            new FakeWebHostEnvironment(content));

        locator.Find().Should().Be(checkout);
        // The templates sit inside engine/, beside run.ps1 — where publish.ps1 and the checkout put them.
        locator.TemplatesRoot.Should().Be(Path.Combine(checkout, "engine", "templates", "project"));
    }

    [Fact]
    public void No_engine_anywhere_is_not_found()
    {
        var content = Path.Combine(_root, "plain", "folder");

        Directory.CreateDirectory(content);

        var locator = new EngineLocator(
            Options.Create(new OrchestraOptions()),
            new FakeWebHostEnvironment(content));

        locator.Find().Should().BeNull();
        locator.TemplatesRoot.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string Engine(string name)
    {
        var folder = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.Combine(folder, "engine"));
        File.WriteAllText(Path.Combine(folder, "engine", "run.ps1"), "# shim");

        return folder;
    }
}
