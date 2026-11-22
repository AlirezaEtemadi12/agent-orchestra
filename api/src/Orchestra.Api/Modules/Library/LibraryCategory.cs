namespace Orchestra.Api.Modules.Library;

public enum LibraryCategory
{
    Plan,
    Preview,
    Idea,
    Research,
    Evidence,
}

/// <summary>The category name in a URL: <c>plan</c>, <c>preview</c>, <c>idea</c>, <c>research</c>,
/// <c>evidence</c>, case-insensitive; anything else — also a number — is unknown.</summary>
public static class LibraryCategoryNames
{
    public static bool TryParse(string? text, out LibraryCategory category)
    {
        category = default;

        if (string.IsNullOrWhiteSpace(text) || text.Any(char.IsDigit))
        {
            return false;
        }

        return Enum.TryParse(text.Trim(), ignoreCase: true, out category)
            && Enum.IsDefined(category);
    }
}
