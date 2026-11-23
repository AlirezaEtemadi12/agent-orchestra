using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Modules.Setup.Features.Hosting;

public sealed record HostingDto(
    string Mode,
    IReadOnlyList<string> Roots,
    bool Mapped,
    string TokenSource);

public sealed class HostingHandler(HostingInfo hosting, ReportTokenStore tokens)
{
    public HostingDto Handle()
    {
        return new HostingDto(
            hosting.Mode,
            [.. hosting.BrowseRoots()],
            hosting.Mapped,
            tokens.Source);
    }
}
