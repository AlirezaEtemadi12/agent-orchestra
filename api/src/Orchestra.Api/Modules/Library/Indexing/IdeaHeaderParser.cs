using System.Globalization;
using System.Text.RegularExpressions;

namespace Orchestra.Api.Modules.Library.Indexing;

public sealed record IdeaHeader(
    string Title,
    string? Status,
    DateTime? CreatedOn,
    string? LinkedPlanNumber);

/// <summary>Reads the ideas' one-line header — <c>**Status:** raw · **Added:** 2026-11-19 · **Plan:** —</c>
/// — so every field is matched anywhere on a line. Only the first 25 lines are read.</summary>
public static partial class IdeaHeaderParser
{
    private const int HeaderLines = 25;

    public static IdeaHeader Parse(string text, string relPath)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').Take(HeaderLines).Select(line => line.TrimEnd('\r')).ToList();

        return new IdeaHeader(
            ReadTitle(lines, MarkdownTitle.FileName(relPath)),
            ReadStatus(lines),
            ReadCreated(lines),
            ReadLinkedPlan(lines));
    }

    private static string ReadTitle(IReadOnlyList<string> lines, string fileName)
    {
        foreach (var line in lines)
        {
            var heading = Heading().Match(line);

            if (heading.Success)
            {
                return heading.Groups[1].Value.Trim();
            }
        }

        return fileName;
    }

    private static string? ReadStatus(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var match = StatusPattern().Match(line);

            if (!match.Success)
            {
                continue;
            }

            return match.Groups[1].Value.ToLowerInvariant() switch
            {
                "raw" => "raw",
                "ready" => "ready",
                "planned" => "planned",
                "dropped" => "dropped",
                _ => null,
            };
        }

        return null;
    }

    private static DateTime? ReadCreated(IReadOnlyList<string> lines)
    {
        var added = ReadDate(lines, "Added")
            ?? ReadDate(lines, "Created");

        return added;
    }

    private static DateTime? ReadDate(IReadOnlyList<string> lines, string field)
    {
        var pattern = $@"\*\*{field}:\*\*\s*(\d{{4}}-\d{{2}}-\d{{2}})";

        foreach (var line in lines)
        {
            var match = Regex.Match(line, pattern, RegexOptions.CultureInvariant);

            if (match.Success)
            {
                return DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed)
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : null;
            }
        }

        return null;
    }

    private static string? ReadLinkedPlan(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var match = LinkedPlan().Match(line);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"\*\*Status:\*\*\s*(\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatusPattern();

    [GeneratedRegex(@"\*\*Plan:\*\*\s*\D*?(\d{4})", RegexOptions.CultureInvariant)]
    private static partial Regex LinkedPlan();
}
