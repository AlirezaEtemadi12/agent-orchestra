using Orchestra.Api.Common;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Library;

internal static class LibraryFixture
{
    public static string OrchestraRoot => Path.Combine(FixtureFiles.Root, "library", "orchestra");

    public static string TodayRoot => Path.Combine(FixtureFiles.Root, "library", "today");

    public static ProjectOptions Project(string root, string id = "demo", LibraryOptions? library = null)
    {
        return new ProjectOptions
        {
            Id = id,
            Name = id,
            RunsPath = Path.Combine(root, "orchestrator", "runs"),
            RootPath = root,
            Library = library ?? new LibraryOptions(),
        };
    }

    public static string TempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"orchestra-library-{Guid.NewGuid():n}");
        Directory.CreateDirectory(path);

        return path;
    }

    public static void Copy(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    public static void Write(string root, string relPath, string content)
    {
        var full = Path.Combine(root, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    public static void DeleteTree(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A test's temp folder that cannot be removed must not fail the run.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
