using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import.Archive;

public sealed class ArchiveSnapshot
{
    public string File { get; init; } = string.Empty;

    public string TaskId { get; init; } = string.Empty;

    public int Schema { get; init; }

    public DateTime ArchivedAt { get; init; }

    public TaskSnapshot Task { get; init; } = new();

    public List<StageTextEntity> Texts { get; } = [];
}

public sealed record ArchiveSnapshotResult(ArchiveSnapshot? Snapshot, string? Error);
