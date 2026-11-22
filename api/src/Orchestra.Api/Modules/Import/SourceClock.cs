namespace Orchestra.Api.Modules.Import;

public sealed class SourceClock
{
    private readonly TimeZoneInfo _zone;

    public SourceClock(TimeZoneInfo zone)
    {
        _zone = zone;
    }

    public static SourceClock From(string? timeZoneId)
    {
        var zone = string.IsNullOrWhiteSpace(timeZoneId)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        return new SourceClock(zone);
    }

    /// <summary>The ledger and task.json carry no offset: they are read in the source zone.</summary>
    public DateTime ToUtc(DateTime local)
    {
        if (local.Kind == DateTimeKind.Utc)
        {
            return local;
        }

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _zone);
    }
}
