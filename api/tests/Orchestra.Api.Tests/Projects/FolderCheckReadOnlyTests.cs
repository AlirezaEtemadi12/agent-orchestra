using Orchestra.Api.Modules.Projects.Features.CheckFolder;

namespace Orchestra.Api.Tests.Projects;

public sealed class FolderCheckReadOnlyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-check-{Guid.NewGuid():n}");

    [Fact]
    public void A_check_never_writes_or_touches_the_tree()
    {
        Directory.CreateDirectory(Path.Combine(_root, "api", ".git"));
        File.WriteAllText(Path.Combine(_root, "CLAUDE.md"), "rule");
        File.WriteAllText(Path.Combine(_root, "api", "readme.md"), "text");

        var before = Snapshot();

        FolderCheck.Run(_root);

        Snapshot().Should().Equal(before);
    }

    private List<string> Snapshot()
    {
        return [.. Directory.EnumerateFileSystemEntries(_root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => $"{path}|{File.GetLastWriteTimeUtc(path):O}")];
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
