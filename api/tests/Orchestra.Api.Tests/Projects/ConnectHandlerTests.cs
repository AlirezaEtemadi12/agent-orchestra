using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.Connect;

namespace Orchestra.Api.Tests.Projects;

public sealed class ConnectHandlerTests : IDisposable
{
    private readonly ProjectsFixture _fixture = new();

    [Fact]
    public void An_unknown_project_is_a_not_found()
    {
        var (connect, error) = Handler(_fixture.Settings).Handle("nope", "http://localhost:3500");

        connect.Should().BeNull();
        error!.Code.Should().Be(Error.ProjectNotFound.Code);
    }

    [Fact]
    public void The_block_names_the_project_and_the_request_origin_and_the_prompt_holds_no_token()
    {
        _fixture.Registry.Create(_fixture.Entry("shop"));

        var (connect, error) = Handler(_fixture.Settings).Handle("shop", "http://localhost:3500");

        error.Should().BeNull();

        using var block = JsonDocument.Parse(connect!.ReportBlock);
        var report = block.RootElement.GetProperty("report");

        report.GetProperty("project").GetString().Should().Be("shop");
        report.GetProperty("url").GetString().Should().Be("http://localhost:3500/api/events");

        using var keys = JsonDocument.Parse(connect.LocalKeys);
        var token = keys.RootElement.GetProperty("reportToken").GetString();

        token.Should().HaveLength(64);
        connect.AgentPrompt.Should().NotContain(token!);
        connect.AgentPrompt.Should().Contain("Add this \"report\" block");
        connect.AgentPrompt.Should().Contain("Do not commit.");
        connect.AgentPrompt.Should().Contain("shop");
    }

    [Fact]
    public void A_configured_public_url_wins_over_the_request_origin()
    {
        _fixture.Registry.Create(_fixture.Entry("shop"));

        var options = Microsoft.Extensions.Options.Options.Create(new OrchestraOptions
        {
            PublicUrl = "https://orchestra.example/",
            Projects = _fixture.Settings.Value.Projects,
        });

        using var block = JsonDocument.Parse(Handler(options).Handle("shop", "http://localhost:3500").Connect!.ReportBlock);

        block.RootElement.GetProperty("report").GetProperty("url").GetString()
            .Should().Be("https://orchestra.example/api/events");
    }

    [Fact]
    public void Profiles_path_is_omitted_without_a_map_and_included_when_the_map_moves_it()
    {
        _fixture.Registry.Create(_fixture.Entry("shop"));

        using var plain = JsonDocument.Parse(Handler(_fixture.Settings).Handle("shop", "http://x").Connect!.LocalKeys);
        plain.RootElement.TryGetProperty("profilesPath", out _).Should().BeFalse();

        var resolved = _fixture.Settings.Value.ResolveProfilesPath(_fixture.Temp);
        var serverParent = Path.GetDirectoryName(resolved)!;

        var mapped = Microsoft.Extensions.Options.Options.Create(new OrchestraOptions
        {
            PathMap = [new PathMapEntry { Host = Path.Combine(_fixture.Temp, "vhosts"), Server = serverParent }],
            Projects = _fixture.Settings.Value.Projects,
        });

        using var withMap = JsonDocument.Parse(Handler(mapped).Handle("shop", "http://x").Connect!.LocalKeys);

        withMap.RootElement.TryGetProperty("profilesPath", out var profilesPath).Should().BeTrue();
        profilesPath.GetString().Should().Contain("vhosts");
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private ConnectHandler Handler(IOptions<OrchestraOptions> options)
    {
        var hosting = new HostingInfo(options, new FakeReader(), NullLogger<HostingInfo>.Instance);

        var tokens = new ReportTokenStore(
            Path.Combine(_fixture.Temp, $"token-{Guid.NewGuid():n}"),
            options.Value.ReportToken,
            NullLogger<ReportTokenStore>.Instance);

        return new ConnectHandler(_fixture.Registry, tokens, hosting, options, new FakeEnvironment(_fixture.Temp));
    }

    private sealed class FakeReader : IHostingReader
    {
        public bool RunningInContainer => false;
    }

    private sealed class FakeEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Orchestra.Api.Tests";

        public string WebRootPath { get; set; } = contentRoot;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
