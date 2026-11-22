using System.Text.Json;

namespace Orchestra.Api.Modules.Events;

/// <summary>One event as the engine sends it (schema 1, plan 0004 §6.1). Every field is optional here
/// so a malformed body fails validation with a code, not a deserialization stack trace.</summary>
public sealed class EventEnvelope
{
    public int Schema { get; init; }

    public Guid Id { get; init; }

    public string? Project { get; init; }

    public string? TaskId { get; init; }

    public string? Branch { get; init; }

    /// <summary>Carries its offset (<c>2026-11-20T10:02:11+03:30</c>); stored as UTC (D8).</summary>
    public DateTimeOffset At { get; init; }

    /// <summary>Per-run counter kept in memory by the engine; orders events that share an `at`.</summary>
    public long Seq { get; init; }

    public string? Kind { get; init; }

    public JsonElement Data { get; init; }
}
