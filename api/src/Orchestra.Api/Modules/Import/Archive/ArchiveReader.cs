using System.Text.Json;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;
using static Orchestra.Api.Modules.Import.Tasks.JsonFields;

namespace Orchestra.Api.Modules.Import.Archive;

public static class ArchiveReader
{
    public const int SupportedSchema = 1;

    public static ArchiveSnapshotResult Read(string json, string file, SourceClock clock)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException exception)
        {
            return new ArchiveSnapshotResult(null, $"{file}: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ArchiveSnapshotResult(null, $"{file}: the archive is not an object.");
            }

            var schema = Number(Property(root, "schema")) ?? 0;

            if (schema != SupportedSchema)
            {
                return new ArchiveSnapshotResult(
                    null,
                    $"{file}: schema {schema} is not supported (only {SupportedSchema}).");
            }

            var archivedAt = Utc(Property(root, "archivedAt"), clock);
            var task = Property(root, "task") ?? Property(root, "data");
            var taskSnapshot = TaskJsonReader.Map(task ?? default, file, clock);

            var snapshot = new ArchiveSnapshot
            {
                File = file,
                TaskId = TaskIdOf(file),
                Schema = schema,
                ArchivedAt = archivedAt ?? DateTime.UtcNow,
                Task = taskSnapshot,
            };

            AddTexts(snapshot, Property(root, "reviews"), "Review");
            AddTexts(snapshot, Property(root, "readbacks"), "ReadBack");
            AddTexts(snapshot, Property(root, "verify"), "Verify");

            return new ArchiveSnapshotResult(snapshot, null);
        }
    }

    /// <summary>The file is named by task id; anything after the first "." is accepted.</summary>
    public static string TaskIdOf(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var dot = name.IndexOf('.', StringComparison.Ordinal);

        return dot < 0 ? name : name[..dot];
    }

    private static void AddTexts(ArchiveSnapshot snapshot, JsonElement? rows, string stage)
    {
        if (rows is not { ValueKind: JsonValueKind.Array } array)
        {
            return;
        }

        foreach (var row in array.EnumerateArray())
        {
            var text = Text(row, "text");

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.Length > StageTextEntity.MaxTextLength)
            {
                text = text[..StageTextEntity.MaxTextLength] + "\n… (cut)";
            }

            snapshot.Texts.Add(new StageTextEntity
            {
                Id = Guid.NewGuid(),
                Stage = stage,
                Round = Number(Property(row, "round")),
                Name = Text(row, "name") ?? stage,
                Text = text,
            });
        }
    }
}
