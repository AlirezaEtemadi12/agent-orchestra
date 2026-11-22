using System.Globalization;
using System.Text.Json;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import.Tasks;

internal static class JsonFields
{
    public static JsonElement? Property(JsonElement? element, string name)
    {
        if (element is not { ValueKind: JsonValueKind.Object })
        {
            return null;
        }

        foreach (var property in element.Value.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    public static string? Text(JsonElement? element, string name)
    {
        return Text(Property(element, name));
    }

    public static string? Text(JsonElement? value)
    {
        return value is { ValueKind: JsonValueKind.String } ? value.Value.GetString() : null;
    }

    public static int? Number(JsonElement? value)
    {
        return value is { ValueKind: JsonValueKind.Number } && value.Value.TryGetInt32(out var number)
            ? number
            : null;
    }

    public static bool? Flag(JsonElement? value)
    {
        return value?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    /// <summary>An explicit offset (or Z) is absolute; a value without one is in the source zone.</summary>
    public static DateTime? Utc(JsonElement? value, SourceClock clock)
    {
        return Utc(Text(value), clock);
    }

    public static DateTime? Utc(string? text, SourceClock clock)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return null;
        }

        return HasOffset(text)
            ? parsed.UtcDateTime
            : clock.ToUtc(DateTime.SpecifyKind(parsed.DateTime, DateTimeKind.Unspecified));
    }

    private static bool HasOffset(string text)
    {
        return text.EndsWith('Z') || text.EndsWith('z') || text.Contains('+', StringComparison.Ordinal)
            || text.LastIndexOf('-') > 10;
    }

    public static StageKind Stage(string? name)
    {
        return name?.Replace("-", string.Empty, StringComparison.Ordinal) switch
        {
            "review" => StageKind.Review,
            "readback" => StageKind.ReadBack,
            "implement" or "phase" => StageKind.Implement,
            "verify" => StageKind.Verify,
            _ => StageKind.Implement,
        };
    }
}
