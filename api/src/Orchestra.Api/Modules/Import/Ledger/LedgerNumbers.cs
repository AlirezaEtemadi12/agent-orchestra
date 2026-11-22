using System.Globalization;

namespace Orchestra.Api.Modules.Import.Ledger;

public static class LedgerNumbers
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>529.9k → 529900, 2.2m → 2200000, — → null.</summary>
    public static long? Tokens(string? raw)
    {
        var text = Clean(raw);

        if (text is null)
        {
            return null;
        }

        var multiplier = 1m;

        if (text.EndsWith('k') || text.EndsWith('K'))
        {
            multiplier = 1_000m;
            text = text[..^1];
        }
        else if (text.EndsWith('m') || text.EndsWith('M'))
        {
            multiplier = 1_000_000m;
            text = text[..^1];
        }

        var value = ParseDecimal(text);

        return value is null
            ? null
            : (long)decimal.Round(value.Value * multiplier, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>22:05 → 1325 s, 2:38:01 → 9481 s.</summary>
    public static int? Duration(string? raw)
    {
        var text = Clean(raw);

        if (text is null)
        {
            return null;
        }

        var parts = text.Split(':');

        if (parts.Length is < 2 or > 3 || parts.Any(part => !int.TryParse(part, NumberStyles.Integer, Invariant, out _)))
        {
            return null;
        }

        var numbers = parts.Select(part => int.Parse(part, Invariant)).ToArray();

        return numbers.Length == 2
            ? (numbers[0] * 60) + numbers[1]
            : (numbers[0] * 3600) + (numbers[1] * 60) + numbers[2];
    }

    /// <summary>$0.170 → 170 000 micro-dollars.</summary>
    public static long? Money(string? raw)
    {
        var text = Clean(raw);

        if (text is null)
        {
            return null;
        }

        text = text.TrimStart('$').Trim();

        var value = ParseDecimal(text);

        return value is null
            ? null
            : (long)decimal.Round(value.Value * 1_000_000m, 0, MidpointRounding.AwayFromZero);
    }

    public static int? Count(string? raw)
    {
        var text = Clean(raw);

        return text is not null && int.TryParse(text, NumberStyles.Integer, Invariant, out var value)
            ? value
            : null;
    }

    private static string? Clean(string? raw)
    {
        var text = raw?.Trim();

        return string.IsNullOrEmpty(text) || text == "—" || text == "-" ? null : text;
    }

    private static decimal? ParseDecimal(string text)
    {
        return decimal.TryParse(text, NumberStyles.Number, Invariant, out var value) ? value : null;
    }
}
