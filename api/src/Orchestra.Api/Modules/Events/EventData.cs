using System.Globalization;
using System.Text.Json;

namespace Orchestra.Api.Modules.Events;

/// <summary>Reads an event's <c>data</c> object. The API never interprets the payload as a contract of
/// its own, so a missing or malformed field is null here, never an exception.</summary>
public sealed class EventData
{
    private readonly JsonElement _root;

    private EventData(JsonElement root)
    {
        _root = root;
    }

    public static EventData Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new EventData(default);
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return new EventData(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return new EventData(default);
        }
    }

    public string? Text(string name)
    {
        if (_root.ValueKind != JsonValueKind.Object || !_root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
            _ => null,
        };
    }

    public int? Int(string name)
    {
        var text = Text(name);

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public bool? Bool(string name)
    {
        var text = Text(name);

        if (bool.TryParse(text, out var value))
        {
            return value;
        }

        return text is "1" ? true : text is "0" ? false : null;
    }

    /// <summary>An ISO time with its offset (<c>2026-11-20T10:02:11+03:30</c>) as UTC; null when absent.</summary>
    public DateTime? Time(string name)
    {
        var text = Text(name);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value)
            ? value.UtcDateTime
            : null;
    }
}
