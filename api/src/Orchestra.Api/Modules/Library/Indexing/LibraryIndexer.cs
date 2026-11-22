using System.Text;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Library.Indexing;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Library;

public sealed class LibraryIndexResult
{
    public List<LibraryItemEntity> Items { get; } = [];

    /// <summary>File-relative messages for the import log; the import records them as errors.</summary>
    public List<(string File, string Message)> Errors { get; } = [];
}

/// <summary>Builds a project's library from disk. Metadata only, never content in the database, and
/// never a write into the project folder (plan 0002 D2, ADR 0003).</summary>
public static class LibraryIndexer
{
    public const int MaxItems = 5000;

    private static readonly string[] SkippedFolders =
        [".git", "node_modules", "bin", "obj", ".next", "out", "graphify-out"];

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];

    private static StringComparison Comparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static LibraryIndexResult Build(ProjectOptions project, IReadOnlyList<RunEntity> runs)
    {
        var result = new LibraryIndexResult();
        var root = project.ResolveRootPath();

        if (string.IsNullOrWhiteSpace(root))
        {
            result.Errors.Add((project.RunsPath, "library root not set"));

            return result;
        }

        var fullRoot = FullPath(root);

        if (fullRoot is null || !Directory.Exists(fullRoot) || IsFileSystemRoot(fullRoot))
        {
            result.Errors.Add((root, $"library root not usable: {root}"));

            return result;
        }

        var plans = new List<LibraryItemEntity>();
        var seen = new HashSet<string>(Comparison == StringComparison.OrdinalIgnoreCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);
        var truncated = false;

        foreach (var category in Enum.GetValues<LibraryCategory>())
        {
            foreach (var pattern in LibraryDefaults.For(category, project.Library))
            {
                foreach (var (relPath, matchedPattern) in Files(fullRoot, pattern))
                {
                    if (result.Items.Count >= MaxItems)
                    {
                        truncated = true;

                        break;
                    }

                    if (category == LibraryCategory.Evidence && !IsEvidence(relPath))
                    {
                        continue;
                    }

                    var full = Path.Combine(fullRoot, relPath.Replace('/', Path.DirectorySeparatorChar));

                    if (!File.Exists(full))
                    {
                        continue;
                    }

                    if (!SafePath.IsInside(fullRoot, full))
                    {
                        result.Errors.Add((relPath, "link skipped"));

                        continue;
                    }

                    // One row per (category, path), however many globs match it.
                    if (!seen.Add($"{category}\u0001{relPath}"))
                    {
                        continue;
                    }

                    var item = CreateItem(project, category, relPath, matchedPattern, full, result);

                    if (item is null)
                    {
                        continue;
                    }

                    if (category == LibraryCategory.Plan)
                    {
                        plans.Add(item);
                    }

                    result.Items.Add(item);
                }

                if (truncated)
                {
                    break;
                }
            }

            if (truncated)
            {
                break;
            }
        }

        if (truncated)
        {
            result.Errors.Add((root, "library truncated"));
        }

        ResolvePreviews(result.Items);
        PlanRunLinker.Link(plans, runs);

        return result;
    }

    private static LibraryItemEntity? CreateItem(
        ProjectOptions project,
        LibraryCategory category,
        string relPath,
        string pattern,
        string fullPath,
        LibraryIndexResult result)
    {
        var info = new FileInfo(fullPath);

        var item = new LibraryItemEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Category = category,
            RelPath = relPath,
            Slug = SlugOf(relPath),
            SizeBytes = info.Length,
            ModifiedAt = info.LastWriteTimeUtc,
            Title = MarkdownTitle.FileName(relPath),
        };

        switch (category)
        {
            case LibraryCategory.Plan:
            {
                var header = PlanHeaderParser.Parse(ReadText(fullPath, relPath, result), relPath);

                item.Title = Trim(header.Title, 300);
                item.Number = header.Number;
                item.Status = header.Status;
                item.CreatedOn = header.CreatedOn;
                item.UpdatedOn = header.UpdatedOn;
                item.DoneOn = header.DoneOn;
                item.PlannedBy = Trim(header.PlannedBy, 120);
                item.PreviewRelPath = header.PreviewRelPath;
                item.Repo = GlobMatcher.RepoOf(pattern, relPath);

                return item;
            }

            case LibraryCategory.Idea:
            {
                var header = IdeaHeaderParser.Parse(ReadText(fullPath, relPath, result), relPath);

                item.Title = Trim(header.Title, 300);
                item.Status = header.Status;
                item.CreatedOn = header.CreatedOn;
                item.LinkedPlanNumber = header.LinkedPlanNumber;

                return item;
            }

            case LibraryCategory.Preview:
            case LibraryCategory.Research:
                item.Title = Trim(MarkdownTitle.Title(ReadText(fullPath, relPath, result), relPath), 300);

                return item;

            case LibraryCategory.Evidence:
                item.Title = Trim(IsReport(relPath)
                    ? MarkdownTitle.Title(ReadText(fullPath, relPath, result), relPath)
                    : MarkdownTitle.FileName(relPath), 300);
                item.Group = GroupOf(relPath, pattern);

                return item;

            default:
                return item;
        }
    }

    /// <summary>D9: evidence is images and <c>report.md</c>; anything else a glob matches is skipped
    /// silently.</summary>
    private static bool IsEvidence(string relPath)
    {
        return IsReport(relPath)
            || ImageExtensions.Any(extension => relPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsReport(string relPath)
    {
        return MarkdownTitle.FileName(relPath).Equals("report.md", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The folder segments below the glob's fixed prefix, at most two; a file directly under
    /// the prefix is ungrouped (null).</summary>
    private static string? GroupOf(string relPath, string pattern)
    {
        var prefix = GlobMatcher.FixedPrefix(pattern);
        var below = relPath;

        if (prefix.Length > 0 && below.StartsWith(prefix + "/", Comparison))
        {
            below = below[(prefix.Length + 1)..];
        }

        var segments = below.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var folders = segments.Take(Math.Max(0, segments.Length - 1)).Take(2).ToArray();

        return folders.Length == 0 ? null : string.Join('/', folders);
    }

    /// <summary>A plan's preview is only stored when it is an indexed preview of the same project, so
    /// <c>previewUrl</c> can never point at a file the file endpoint refuses.</summary>
    private static void ResolvePreviews(List<LibraryItemEntity> items)
    {
        var previews = items
            .Where(item => item.Category == LibraryCategory.Preview)
            .Select(item => item.RelPath)
            .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        foreach (var plan in items.Where(item => item.Category == LibraryCategory.Plan))
        {
            if (plan.PreviewRelPath is not null && !previews.Contains(plan.PreviewRelPath))
            {
                plan.PreviewRelPath = null;
            }
        }
    }

    /// <summary>The files under a glob's fixed prefix, with the pattern that matched each. The walk
    /// never enters a build or VCS folder, and a <c>*</c>-leading glob also checks the root's own files.</summary>
    private static IEnumerable<(string RelPath, string Pattern)> Files(string root, string pattern)
    {
        var prefix = GlobMatcher.FixedPrefix(pattern);
        var start = prefix.Length == 0
            ? root
            : Path.Combine(root, prefix.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(start))
        {
            yield break;
        }

        foreach (var file in Walk(root, start))
        {
            if (GlobMatcher.Matches(pattern, file))
            {
                yield return (file, pattern);
            }
        }
    }

    private static IEnumerable<string> Walk(string root, string directory)
    {
        string[] files;
        string[] folders;

        try
        {
            files = Directory.GetFiles(directory);
            folders = Directory.GetDirectories(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            yield return Path.GetRelativePath(root, file).Replace('\\', '/');
        }

        foreach (var folder in folders)
        {
            var name = Path.GetFileName(folder);

            if (SkippedFolders.Any(skipped => skipped.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var file in Walk(root, folder))
            {
                yield return file;
            }
        }
    }

    private static string ReadText(string fullPath, string relPath, LibraryIndexResult result)
    {
        try
        {
            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            return reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add((relPath, "the file could not be read"));

            return string.Empty;
        }
    }

    private static string SlugOf(string relPath)
    {
        var fileName = MarkdownTitle.FileName(relPath);
        var dot = fileName.LastIndexOf('.');
        var name = dot > 0 ? fileName[..dot] : fileName;

        var numbered = name.Length > 5 && name[4] == '-' && name[..4].All(char.IsDigit);

        return numbered ? name[5..] : name;
    }

    private static string? FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>A filesystem root (<c>/</c>, <c>C:\</c>) is never a project folder.</summary>
    private static bool IsFileSystemRoot(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath);

        return root is not null
            && Path.TrimEndingDirectorySeparator(root)
                .Equals(Path.TrimEndingDirectorySeparator(fullPath), Comparison);
    }

    private static string Trim(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
