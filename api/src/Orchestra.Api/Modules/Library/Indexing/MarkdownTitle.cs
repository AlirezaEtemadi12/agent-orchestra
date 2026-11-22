using System.Text.RegularExpressions;

namespace Orchestra.Api.Modules.Library.Indexing;

/// <summary>A document's title: for HTML the <c>&lt;title&gt;</c>, else the first <c># </c> heading, else
/// the file name.</summary>
public static partial class MarkdownTitle
{
    public static string Title(string? text, string relPath)
    {
        var fileName = FileName(relPath);

        if (IsHtml(relPath))
        {
            var match = text is null ? Match.Empty : HtmlTitle().Match(text);

            return match.Success && match.Groups[1].Value.Trim().Length > 0
                ? match.Groups[1].Value.Trim()
                : fileName;
        }

        if (text is not null)
        {
            foreach (var line in text.Split('\n'))
            {
                var match = Heading().Match(line.TrimEnd('\r'));

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }

        return fileName;
    }

    public static bool IsHtml(string relPath)
    {
        return relPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || relPath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);
    }

    public static string FileName(string relPath)
    {
        var normalized = relPath.Replace('\\', '/');
        var last = normalized.LastIndexOf('/');

        return last < 0 ? normalized : normalized[(last + 1)..];
    }

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTitle();
}
