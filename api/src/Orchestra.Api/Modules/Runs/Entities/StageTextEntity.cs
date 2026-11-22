namespace Orchestra.Api.Modules.Runs.Entities;

public sealed class StageTextEntity
{
    public const int MaxTextLength = 200_000;

    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public string Stage { get; set; } = string.Empty;

    public int? Round { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public RunEntity Run { get; set; } = null!;
}
