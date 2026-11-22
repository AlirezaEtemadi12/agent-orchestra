namespace Orchestra.Api.Modules.Import;

public sealed class ImportLogEntity
{
    public Guid Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime FinishedAt { get; set; }

    public int FilesRead { get; set; }

    public int BlocksRead { get; set; }

    public int RunsBuilt { get; set; }

    public int LibraryItems { get; set; }

    public int ErrorCount { get; set; }

    public List<ImportErrorEntity> Errors { get; set; } = [];
}
