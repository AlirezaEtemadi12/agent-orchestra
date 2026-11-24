using Orchestra.Api.Modules.Projects.Features.Setup;

namespace Orchestra.Api.Tests.Projects;

public sealed class VerifyCommandDetectorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-verify-{Guid.NewGuid():n}");

    public VerifyCommandDetectorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void A_solution_means_the_dotnet_commands()
    {
        File.WriteAllText(Path.Combine(Folder("sln"), "App.sln"), string.Empty);
        File.WriteAllText(Path.Combine(Folder("slnx"), "App.slnx"), string.Empty);

        VerifyCommandDetector.Detect(Path.Combine(_root, "sln"))
            .Should().Equal("dotnet build", "dotnet test");
        VerifyCommandDetector.Detect(Path.Combine(_root, "slnx"))
            .Should().Equal("dotnet build", "dotnet test");
    }

    [Fact]
    public void A_package_json_means_one_command_per_script_it_has_in_order()
    {
        Package("npm", "{\"scripts\":{\"build\":\"x\",\"test\":\"x\",\"typecheck\":\"x\"}}");

        VerifyCommandDetector.Detect(Path.Combine(_root, "npm"))
            .Should().Equal("npm run typecheck", "npm run test", "npm run build");
    }

    [Fact]
    public void Both_means_dotnet_first()
    {
        var folder = Folder("both");
        File.WriteAllText(Path.Combine(folder, "App.sln"), string.Empty);
        File.WriteAllText(Path.Combine(folder, "package.json"), "{\"scripts\":{\"test\":\"x\"}}");

        VerifyCommandDetector.Detect(folder)
            .Should().Equal("dotnet build", "dotnet test", "npm run test");
    }

    [Fact]
    public void Nothing_found_and_an_unreadable_package_give_an_empty_list()
    {
        VerifyCommandDetector.Detect(Folder("empty")).Should().BeEmpty();

        Package("broken", "not json");
        VerifyCommandDetector.Detect(Path.Combine(_root, "broken")).Should().BeEmpty();

        Package("noscripts", "{\"name\":\"x\"}");
        VerifyCommandDetector.Detect(Path.Combine(_root, "noscripts")).Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string Folder(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);

        return path;
    }

    private void Package(string folder, string text)
    {
        File.WriteAllText(Path.Combine(Folder(folder), "package.json"), text);
    }
}
