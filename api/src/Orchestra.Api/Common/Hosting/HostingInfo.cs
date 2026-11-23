using Microsoft.Extensions.Options;

namespace Orchestra.Api.Common.Hosting;

/// <summary>Whether this process runs inside a container — the one thing the dashboard detects instead
/// of asking (plan 0009 D1). Injectable so a test can decide.</summary>
public interface IHostingReader
{
    bool RunningInContainer { get; }
}

public sealed class EnvironmentHostingReader : IHostingReader
{
    public bool RunningInContainer
        => string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>Plan 0009 D1/D5: the mode, the validated map, and the folders the browser may list.</summary>
public sealed class HostingInfo
{
    public const string ContainerMode = "container";
    public const string DirectMode = "direct";

    private readonly OrchestraOptions _settings;

    public HostingInfo(IOptions<OrchestraOptions> options, IHostingReader reader, ILogger<HostingInfo> logger)
    {
        _settings = options.Value;
        Mode = reader.RunningInContainer ? ContainerMode : DirectMode;

        var entries = new List<PathMapEntry>();

        foreach (var entry in _settings.PathMap)
        {
            if (string.IsNullOrWhiteSpace(entry.Host)
                || string.IsNullOrWhiteSpace(entry.Server)
                || !Path.IsPathRooted(entry.Server))
            {
                logger.LogWarning(
                    "Orchestra:PathMap entry '{Host}' -> '{Server}' is dropped: both sides must be set and Server must be rooted.",
                    entry.Host,
                    entry.Server);

                continue;
            }

            entries.Add(entry);
        }

        Entries = entries;
        Mapper = entries.Count == 0 ? PathMapper.Empty : new PathMapper(entries);
    }

    public string Mode { get; }

    public IReadOnlyList<PathMapEntry> Entries { get; }

    public PathMapper Mapper { get; }

    public bool Mapped => Entries.Count > 0;

    /// <summary>Container: the map's host side. Direct: <c>Orchestra:BrowseRoots</c>, else every fixed
    /// drive — acceptable only because a direct install listens on loopback until plan 0008 (D16).</summary>
    public IReadOnlyList<string> BrowseRoots()
    {
        if (Mode == ContainerMode)
        {
            return [.. Entries.Select(entry => entry.Host)];
        }

        var configured = _settings.BrowseRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .ToList();

        if (configured.Count > 0)
        {
            return configured;
        }

        try
        {
            return
            [
                .. DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
                    .Select(drive => drive.RootDirectory.FullName),
            ];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
