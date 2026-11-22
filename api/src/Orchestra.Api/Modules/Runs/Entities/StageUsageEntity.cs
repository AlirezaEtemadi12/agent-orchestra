namespace Orchestra.Api.Modules.Runs.Entities;

public sealed class StageUsageEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string? Model { get; set; }

    public int? Sessions { get; set; }

    public int? Calls { get; set; }

    public int? Requests { get; set; }

    public long? Input { get; set; }

    public long? Output { get; set; }

    public long? Reasoning { get; set; }

    public long? CacheRead { get; set; }

    public int? Seconds { get; set; }

    public long? CostMicros { get; set; }

    public RunEntity Run { get; set; } = null!;
}
