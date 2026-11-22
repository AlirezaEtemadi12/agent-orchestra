namespace Orchestra.Api.Modules.Events;

/// <summary>An event the dashboard received (plan 0004 D3): its own record, never derived from a file
/// and never deleted by an import. <see cref="Id"/> is the engine's GUID, so a resume or a hook firing
/// twice stores one row (D11).</summary>
public sealed class RunEventEntity
{
    public Guid Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public string TaskId { get; set; } = string.Empty;

    public string? Branch { get; set; }

    /// <summary>UTC.</summary>
    public DateTime At { get; set; }

    public long Seq { get; set; }

    public string Kind { get; set; } = string.Empty;

    /// <summary>The event's <c>data</c> object, as sent: the API never interprets it.</summary>
    public string DataJson { get; set; } = "{}";

    public DateTime ReceivedAt { get; set; }
}
