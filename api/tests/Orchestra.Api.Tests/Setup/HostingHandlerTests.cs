using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Setup.Features.Hosting;

namespace Orchestra.Api.Tests.Setup;

public sealed class HostingHandlerTests
{
    [Fact]
    public void The_mode_comes_from_the_container_variable_and_the_roots_from_the_map()
    {
        var options = Options.Create(new OrchestraOptions
        {
            ReportToken = "pinned-token",
            PathMap = [new PathMapEntry { Host = "F:/Projects", Server = "/host" }],
        });

        var container = Handle(options, container: true);

        container.Mode.Should().Be("container");
        container.Roots.Should().Equal("F:/Projects");
        container.Mapped.Should().BeTrue();
        container.TokenSource.Should().Be("settings");
    }

    [Fact]
    public void A_direct_install_reports_its_configured_roots()
    {
        var options = Options.Create(new OrchestraOptions
        {
            BrowseRoots = ["C:/Work"],
        });

        var direct = Handle(options, container: false);

        direct.Mode.Should().Be("direct");
        direct.Roots.Should().Equal("C:/Work");
        direct.Mapped.Should().BeFalse();
    }

    [Fact]
    public void An_invalid_map_entry_is_dropped()
    {
        var options = Options.Create(new OrchestraOptions
        {
            PathMap = [new PathMapEntry { Host = "F:/Projects", Server = string.Empty }],
        });

        Handle(options, container: true).Mapped.Should().BeFalse();
    }

    private static HostingDto Handle(IOptions<OrchestraOptions> options, bool container)
    {
        var hosting = new HostingInfo(options, new FakeReader(container), NullLogger<HostingInfo>.Instance);

        var tokens = new ReportTokenStore(
            Path.Combine(Path.GetTempPath(), $"orchestra-hosting-{Guid.NewGuid():n}", "report-token"),
            options.Value.ReportToken,
            NullLogger<ReportTokenStore>.Instance);

        return new HostingHandler(hosting, tokens).Handle();
    }

    private sealed class FakeReader(bool container) : IHostingReader
    {
        public bool RunningInContainer { get; } = container;
    }
}
