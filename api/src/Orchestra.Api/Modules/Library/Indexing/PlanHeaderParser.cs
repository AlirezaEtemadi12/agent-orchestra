using System.Globalization;
using System.Text.RegularExpressions;

namespace Orchestra.Api.Modules.Library.Indexing;

public sealed record PlanHeader(
    string Title,
    string? Number,
    string Status,
    DateTime? CreatedOn,
    DateTime? UpdatedOn,
    DateTime? DoneOn,
    string? PlannedBy,
    string? PreviewRelPath);

/// <summary>Reads the plan header tolerantly — the three styles in the real projects: Orchestra's
/// template, Project B's <c>**done**</c> + <c>**Author:**</c>, and Project A's
/// <c># Plan — title</c> + <c>**Status:** Done</c>. Only the title line and the first 25 lines are read.</summary>
public static partial class PlanHeaderParser
{
    private const int HeaderLines = 25;

    public static PlanHeader Parse(string text, string relPath)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').Take(HeaderLines).Select(line => line.TrimEnd('\r')).ToList();
        var fileName = MarkdownTitle.FileName(relPath);

        var (title, numberFromTitle) = ReadTitle(lines, fileName);

        return new PlanHeader(
            title,
            numberFromTitle ?? NumberFromFileName(fileName),
            ReadStatus(lines, relPath, out var doneFromStatus),
            ReadDate(lines, "Created"),
            ReadDate(lines, "Updated"),
            ReadDate(lines, "Done") ?? doneFromStatus,
            ReadPlannedBy(lines),
            ReadPreview(lines, relPath));
    }

    private static (string Title, string? Number) ReadTitle(IReadOnlyList<string> lines, string fileName)
    {
        foreach (var line in lines)
        {
            var numbered = PlanWithNumber().Match(line);

            if (numbered.Success)
            {
                return (numbered.Groups[2].Value.Trim(), numbered.Groups[1].Value);
            }

            var unnumbered = PlanWithoutNumber().Match(line);

            if (unnumbered.Success)
            {
                return (unnumbered.Groups[1].Value.Trim(), null);
            }

            var heading = Heading().Match(line);

            if (heading.Success)
            {
                return (heading.Groups[1].Value.Trim(), null);
            }
        }

        return (fileName, null);
    }

    private static string? NumberFromFileName(string fileName)
    {
        var match = NumberPrefix().Match(fileName);

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ReadStatus(IReadOnlyList<string> lines, string relPath, out DateTime? doneOn)
    {
        doneOn = null;

        foreach (var line in lines)
        {
            var status = StatusPattern().Match(line);

            if (!status.Success)
            {
                continue;
            }

            return MapStatus(status.Groups[1].Value);
        }

        foreach (var line in lines)
        {
            var legacyDone = LegacyDone().Match(line);

            if (legacyDone.Success)
            {
                doneOn = ParseDate(legacyDone.Groups[1].Value);

                return "Done";
            }
        }

        return relPath.Replace('\\', '/').Contains("/done/", StringComparison.OrdinalIgnoreCase)
            ? "Done"
            : "Unknown";
    }

    private static string MapStatus(string word)
    {
        return word.ToLowerInvariant() switch
        {
            "draft" => "Draft",
            "approved" => "Approved",
            "running" => "Running",
            "done" => "Done",
            "dropped" => "Dropped",
            _ => "Unknown",
        };
    }

    private static DateTime? ReadDate(IReadOnlyList<string> lines, string field)
    {
        var pattern = $@"\*\*{field}:\*\*\s*(\d{{4}}-\d{{2}}-\d{{2}})";

        foreach (var line in lines)
        {
            var match = Regex.Match(line, pattern, RegexOptions.CultureInvariant);

            if (match.Success)
            {
                return ParseDate(match.Groups[1].Value);
            }
        }

        return null;
    }

    private static string? ReadPlannedBy(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var match = PlannedByPattern().Match(line);

            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups[1].Value.Trim().TrimEnd('.');

            return name.Length == 0 ? null : name;
        }

        return null;
    }

    /// <summary>The resolved preview path — a folder target gains <c>index.html</c> — or null when the
    /// header names none or the target escapes the root.</summary>
    private static string? ReadPreview(IReadOnlyList<string> lines, string relPath)
    {
        foreach (var line in lines)
        {
            var header = PreviewHeader().Match(line);

            if (!header.Success)
            {
                continue;
            }

            var rest = header.Groups[1].Value.Trim();

            if (rest.Length == 0)
            {
                return null;
            }

            var link = MarkdownLink().Match(rest);

            if (link.Success)
            {
                return Resolve(relPath, link.Groups[1].Value);
            }

            var ticked = Backtick().Match(rest);

            if (ticked.Success)
            {
                return Resolve(relPath, ticked.Groups[1].Value);
            }

            return Resolve(relPath, rest.Split([' ', '\t'], 2)[0]);
        }

        return null;
    }

    /// <summary>A target relative to the document's own folder → a root-relative path with forward
    /// slashes. Null when it is absolute or climbs above the root.</summary>
    public static string? Resolve(string fromRelPath, string target)
    {
        var clean = target.Trim().Trim('"', '\'', '<', '>');

        if (clean.Length == 0)
        {
            return null;
        }

        var isFolder = clean.EndsWith('/') || clean.EndsWith('\\');
        var normalized = clean.Replace('\\', '/');

        if (normalized.StartsWith('/') || (normalized.Length > 1 && normalized[1] == ':'))
        {
            return null;
        }

        var stack = fromRelPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (stack.Count == 0)
        {
            return null;
        }

        stack.RemoveAt(stack.Count - 1);

        foreach (var segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (stack.Count == 0)
                {
                    return null;
                }

                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            stack.Add(segment);
        }

        if (isFolder)
        {
            stack.Add("index.html");
        }

        return stack.Count == 0 ? null : string.Join('/', stack);
    }

    private static DateTime? ParseDate(string value)
    {
        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    [GeneratedRegex(@"^#\s+Plan\s+(\d{4})\s*[—–-]\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanWithNumber();

    [GeneratedRegex(@"^#\s+Plan\s*[—–-]\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanWithoutNumber();

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"^(\d{4})-", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPrefix();

    [GeneratedRegex(@"^\*\*Status:\*\*\s*(\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatusPattern();

    [GeneratedRegex(@"^\*\*done\*\*\s+(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyDone();

    [GeneratedRegex(@"^\*\*(?:Planned by|Author):\*\*\s*([^,(]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlannedByPattern();

    [GeneratedRegex(@"^\*\*Preview:\*\*\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreviewHeader();

    [GeneratedRegex(@"\[[^\]]*\]\(([^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^`([^`]+)`", RegexOptions.CultureInvariant)]
    private static partial Regex Backtick();
}
