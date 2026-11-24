using System.Text;
using System.Text.Json;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Projects;

/// <summary>One entry of the dashboard's own <c>projects.json</c>. Never the database: the database is
/// rebuildable, this file is not (plan 0003 D2).</summary>
public sealed record StoredProject
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public string RunsPath { get; init; } = string.Empty;

    public List<string> Repos { get; init; } = [];

    public string Profile { get; init; } = "default";

    public DateTime CreatedAt { get; init; }

    /// <summary>Plan 0012 D5: the stored path is the path on disk — there is no second form.</summary>
    public ProjectOptions ToOptions()
    {
        return new ProjectOptions
        {
            Id = Id,
            Name = Name,
            RootPath = RootPath,
            RunsPath = RunsPath,
            Repos = [.. Repos],
            Library = new(),
        };
    }
}

public sealed record ProjectStoreRead(IReadOnlyList<StoredProject> Projects, string? Error);

public sealed class ProjectStoreUnreadableException(string message) : Exception(message);

/// <summary>The dashboard's own project list: atomic writes (temp file, then replace), and a file that
/// does not parse is reported and never overwritten (plan 0003 §6.1).</summary>
public sealed class ProjectStore
{
    private const string SchemaError = "projects.json has an unknown schema.";
    private const string ParseError = "projects.json cannot be parsed.";
    private const string EntryError = "projects.json has an invalid project.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly object _gate = new();

    public ProjectStore(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public ProjectStoreRead Read()
    {
        lock (_gate)
        {
            return ReadUnlocked();
        }
    }

    /// <summary>Refuses to replace a file that does not parse, so a broken list is never lost.</summary>
    public void Write(IReadOnlyList<StoredProject> projects)
    {
        lock (_gate)
        {
            var current = ReadUnlocked();

            if (current.Error is not null)
            {
                throw new ProjectStoreUnreadableException(current.Error);
            }

            var folder = Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var text = JsonSerializer.Serialize(new ProjectFile { Schema = 1, Projects = [.. projects] }, Json);
            var temporary = FilePath + ".tmp";

            File.WriteAllText(temporary, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, FilePath, overwrite: true);
        }
    }

    private ProjectStoreRead ReadUnlocked()
    {
        if (!File.Exists(FilePath))
        {
            return new ProjectStoreRead([], null);
        }

        string text;

        try
        {
            text = File.ReadAllText(FilePath, new UTF8Encoding(false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new ProjectStoreRead([], "projects.json cannot be read.");
        }

        ProjectFile? file;

        try
        {
            file = JsonSerializer.Deserialize<ProjectFile>(text, Json);
        }
        catch (JsonException)
        {
            return new ProjectStoreRead([], ParseError);
        }

        if (file is null || file.Schema != 1)
        {
            return new ProjectStoreRead([], SchemaError);
        }

        foreach (var project in file.Projects)
        {
            if (!IsValid(project))
            {
                return new ProjectStoreRead([], EntryError);
            }
        }

        return new ProjectStoreRead(file.Projects, null);
    }

    private static bool IsValid(StoredProject project)
    {
        return !string.IsNullOrWhiteSpace(project.Id)
            && !string.IsNullOrWhiteSpace(project.Name)
            && !string.IsNullOrWhiteSpace(project.RootPath)
            && !string.IsNullOrWhiteSpace(project.RunsPath)
            && project.Profile == "default";
    }

    private sealed class ProjectFile
    {
        public int Schema { get; init; }

        public List<StoredProject> Projects { get; init; } = [];
    }
}
