using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Projects;

/// <summary>Config projects first, then the dashboard's own; the configuration wins a shared id. One
/// immutable snapshot serves every request and is swapped only after a successful write (plan 0003 D8).
/// The snapshot keeps each dashboard entry's stored paths beside the options it builds from them.</summary>
public sealed class ProjectRegistry
{
    private sealed record Snapshot(
        IReadOnlyDictionary<string, ProjectOptions> Projects,
        IReadOnlyDictionary<string, ProjectOrigin> Origins,
        IReadOnlyDictionary<string, StoredProject> Stored);

    private readonly object _write = new();
    private readonly IOptions<OrchestraOptions> _options;
    private readonly ProjectStore _store;
    private readonly ILogger<ProjectRegistry> _logger;
    private Snapshot _snapshot;

    public ProjectRegistry(
        IOptions<OrchestraOptions> options,
        ProjectStore store,
        ILogger<ProjectRegistry> logger)
    {
        _options = options;
        _store = store;
        _logger = logger;

        var read = store.Read();

        if (read.Error is not null)
        {
            logger.LogWarning("The dashboard's project list is unreadable: {Error}", read.Error);
        }

        _snapshot = Build(read.Projects);
    }

    public IReadOnlyList<ProjectOptions> All => [.. Current.Projects.Values];

    public ProjectOptions? Find(string id)
    {
        return Current.Projects.GetValueOrDefault(id);
    }

    /// <summary>The entry as the owner typed it, for the edit form and the list. Null for a config
    /// project (its paths are already what the file says).</summary>
    public StoredProject? Stored(string id)
    {
        return Current.Stored.GetValueOrDefault(id);
    }

    public ProjectOrigin? OriginOf(string id)
    {
        return Current.Origins.TryGetValue(id, out var origin) ? origin : null;
    }

    /// <summary>Writes the entry, then swaps the snapshot; a failed write leaves both unchanged.</summary>
    public void Create(StoredProject project)
    {
        lock (_write)
        {
            var entries = Entries();

            if (entries.Exists(entry => entry.Id == project.Id))
            {
                throw new InvalidOperationException($"Project '{project.Id}' is already in the dashboard's list.");
            }

            entries.Add(project);

            Commit(entries);
        }
    }

    public bool Update(string id, Func<StoredProject, StoredProject> change)
    {
        lock (_write)
        {
            var entries = Entries();
            var index = entries.FindIndex(entry => entry.Id == id);

            if (index < 0)
            {
                return false;
            }

            entries[index] = change(entries[index]);

            Commit(entries);

            return true;
        }
    }

    public bool Remove(string id)
    {
        lock (_write)
        {
            var entries = Entries();
            var removed = entries.RemoveAll(entry => entry.Id == id) > 0;

            if (removed)
            {
                Commit(entries);
            }

            return removed;
        }
    }

    private Snapshot Current => Volatile.Read(ref _snapshot);

    private List<StoredProject> Entries()
    {
        var read = _store.Read();

        if (read.Error is not null)
        {
            throw new ProjectStoreUnreadableException(read.Error);
        }

        return [.. read.Projects];
    }

    private void Commit(List<StoredProject> entries)
    {
        _store.Write(entries);
        Volatile.Write(ref _snapshot, Build(entries));
    }

    private Snapshot Build(IReadOnlyList<StoredProject> stored)
    {
        var projects = new Dictionary<string, ProjectOptions>(StringComparer.Ordinal);
        var origins = new Dictionary<string, ProjectOrigin>(StringComparer.Ordinal);
        var entries = new Dictionary<string, StoredProject>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var project in _options.Value.Projects)
        {
            if (projects.TryAdd(project.Id, project))
            {
                origins[project.Id] = ProjectOrigin.Config;
            }
            else
            {
                duplicates.Add(project.Id);
            }
        }

        foreach (var entry in stored)
        {
            if (projects.TryAdd(entry.Id, entry.ToOptions()))
            {
                origins[entry.Id] = ProjectOrigin.Dashboard;
                entries[entry.Id] = entry;

            }
            else
            {
                duplicates.Add(entry.Id);
            }
        }

        foreach (var id in duplicates.Distinct(StringComparer.Ordinal))
        {
            _logger.LogWarning("Project id {Id} appears more than once; the configuration wins.", id);
        }

        return new Snapshot(projects, origins, entries);
    }
}
