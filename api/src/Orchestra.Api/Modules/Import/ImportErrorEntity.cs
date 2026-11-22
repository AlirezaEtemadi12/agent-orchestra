namespace Orchestra.Api.Modules.Import;

public sealed class ImportErrorEntity
{
    public Guid Id { get; set; }

    public Guid ImportLogId { get; set; }

    public string File { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string Message { get; set; } = string.Empty;

    public ImportLogEntity ImportLog { get; set; } = null!;
}
