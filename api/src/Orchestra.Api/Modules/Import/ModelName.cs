namespace Orchestra.Api.Modules.Import;

public static class ModelName
{
    /// <summary>opencode:opencode-go/glm-5.3-flash → (opencode, opencode-go/glm-5.3-flash).</summary>
    public static (string? Adapter, string Model) Split(string? raw)
    {
        var text = raw?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return (null, string.Empty);
        }

        var separator = text.IndexOf(':', StringComparison.Ordinal);

        if (separator <= 0 || separator == text.Length - 1)
        {
            return (null, text);
        }

        return (text[..separator], text[(separator + 1)..]);
    }
}
