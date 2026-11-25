using Gridify;

namespace Orchestra.Api.Common.Paging;

/// <summary>Plan 0010 D4: the page defaults and the stable tiebreak every table order ends with.</summary>
public static class TableQueryDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Clamps the page and the page size, fills an empty order with the endpoint's default,
    /// then appends the tiebreak unless the order already ends with the whole suffix.</summary>
    public static void Apply(GridifyQuery query, string defaultOrderBy, string tiebreak)
    {
        if (query.Page <= 0)
        {
            query.Page = 1;
        }

        if (query.PageSize <= 0)
        {
            query.PageSize = DefaultPageSize;
        }

        if (query.PageSize > MaxPageSize)
        {
            query.PageSize = MaxPageSize;
        }

        var order = string.IsNullOrWhiteSpace(query.OrderBy) ? defaultOrderBy : query.OrderBy.Trim();

        if (string.IsNullOrWhiteSpace(order))
        {
            query.OrderBy = tiebreak;
            return;
        }

        query.OrderBy = EndsWith(order, tiebreak) ? order : $"{order}, {tiebreak}";
    }

    private static bool EndsWith(string order, string tiebreak)
    {
        var keys = Keys(order);
        var suffix = Keys(tiebreak);

        if (suffix.Count == 0 || keys.Count < suffix.Count)
        {
            return false;
        }

        for (var index = 0; index < suffix.Count; index++)
        {
            if (!string.Equals(keys[keys.Count - suffix.Count + index], suffix[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> Keys(string order)
    {
        return order
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0])
            .Where(field => field.Length > 0)
            .ToList();
    }
}
