using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import.Archive;
using Orchestra.Api.Modules.Import.Ledger;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Import;

public sealed class ImportService
{
    private const int LogsKept = 50;

    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly OrchestraDbContext _database;
    private readonly ProjectRegistry _projects;
    private readonly IOptions<OrchestraOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        OrchestraDbContext database,
        ProjectRegistry projects,
        IOptions<OrchestraOptions> options,
        TimeProvider clock,
        ILogger<ImportService> logger)
    {
        _database = database;
        _projects = projects;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ImportResultDto>> ImportAllAsync(CancellationToken cancellationToken)
    {
        var results = new List<ImportResultDto>();

        foreach (var project in _projects.All)
        {
            results.Add(await ImportProjectAsync(project, cancellationToken));
        }

        return results;
    }

    /// <summary>One project, waiting for the gate instead of refusing (plan 0003 D11): a create or an
    /// update must not fail because a full import is running.</summary>
    public async Task<ImportResultDto> ImportOneAsync(ProjectOptions project, CancellationToken cancellationToken)
    {
        await EnterAsync(cancellationToken);

        try
        {
            return await ImportProjectAsync(project, cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Waits for the import gate; the caller releases with <see cref="Leave"/>.</summary>
    public Task EnterAsync(CancellationToken cancellationToken)
    {
        return Gate.WaitAsync(cancellationToken);
    }

    public bool TryEnter()
    {
        return Gate.Wait(0);
    }

    public void Leave()
    {
        Gate.Release();
    }

    private async Task<ImportResultDto> ImportProjectAsync(ProjectOptions project, CancellationToken cancellationToken)
    {
        var startedAt = _clock.GetUtcNow().UtcDateTime;
        var errors = new List<(string File, string? Location, string Message)>();
        var filesRead = 0;

        var runsPath = project.RunsPath;

        if (string.IsNullOrWhiteSpace(runsPath) || !Directory.Exists(runsPath))
        {
            errors.Add((runsPath, null, "The runs folder does not exist."));

            return await SaveLogAsync(project, startedAt, 0, 0, 0, 0, errors, cancellationToken);
        }

        var clock = SourceClock.From(_options.Value.SourceTimeZone);

        var ledgerText = string.Empty;
        var ledgerFile = Path.Combine(runsPath, "_ledger.md");

        if (File.Exists(ledgerFile))
        {
            ledgerText = ReadText(ledgerFile);
            filesRead++;
        }

        var ledger = LedgerParser.Parse(ledgerText, "_ledger.md");

        foreach (var error in ledger.Errors)
        {
            errors.Add((error.File, error.Heading, error.Message));
        }

        var tasks = new List<TaskSnapshot>();

        foreach (var taskFile in Directory.EnumerateFiles(runsPath, "task.json", SearchOption.AllDirectories))
        {
            var relative = Relative(runsPath, taskFile);
            var result = TaskJsonReader.Read(ReadText(taskFile), relative, clock);

            filesRead++;

            if (result.Snapshot is null)
            {
                errors.Add((relative, null, result.Error ?? "The file could not be read."));
            }
            else
            {
                // Plan 0006 D10: a live run records the config it started with beside task.json. A file
                // that is unreadable is one import error, never a failed import.
                var effectiveFile = Path.Combine(Path.GetDirectoryName(taskFile)!, "config.effective.json");

                if (File.Exists(effectiveFile))
                {
                    filesRead++;
                    result.Snapshot.EffectiveConfig = ReadEffectiveConfig(
                        effectiveFile,
                        Relative(runsPath, effectiveFile),
                        errors);
                }

                tasks.Add(result.Snapshot);
            }
        }

        var archives = new List<ArchiveSnapshot>();
        var archiveFolder = Path.Combine(runsPath, "_archive");

        if (Directory.Exists(archiveFolder))
        {
            foreach (var archiveFile in Directory.EnumerateFiles(archiveFolder, "*.json", SearchOption.TopDirectoryOnly))
            {
                var relative = Relative(runsPath, archiveFile);
                var result = ArchiveReader.Read(ReadText(archiveFile), relative, clock);

                filesRead++;

                if (result.Snapshot is null)
                {
                    errors.Add((relative, null, result.Error ?? "The file could not be read."));
                }
                else
                {
                    archives.Add(result.Snapshot);
                }
            }
        }

        // Plan 0004 §6.3: the import reads the project's events and never deletes them (ADR 0006).
        var events = await _database.RunEvents
            .AsNoTracking()
            .Where(stored => stored.ProjectId == project.Id)
            .ToListAsync(cancellationToken);

        var runs = RunAssembler.Build(project.Id, ledger.Blocks, tasks, archives, events, clock);

        // The library is indexed in the same rebuild as the runs (plan 0002 D1): metadata only, and a
        // root that cannot be used is one error while the runs still import.
        var library = LibraryIndexer.Build(project, runs);

        foreach (var (file, message) in library.Errors)
        {
            errors.Add((file, null, message));
        }

        await SaveProjectAsync(project, runs, library.Items, cancellationToken);

        return await SaveLogAsync(
            project,
            startedAt,
            filesRead,
            ledger.Blocks.Count,
            runs.Count,
            library.Items.Count,
            errors,
            cancellationToken);
    }

    private async Task SaveProjectAsync(
        ProjectOptions project,
        IReadOnlyList<Runs.Entities.RunEntity> runs,
        IReadOnlyList<LibraryItemEntity> libraryItems,
        CancellationToken cancellationToken)
    {
        var entity = await _database.Projects.FirstOrDefaultAsync(item => item.Id == project.Id, cancellationToken);

        if (entity is null)
        {
            entity = new ProjectEntity { Id = project.Id };
            _database.Projects.Add(entity);
        }

        entity.Name = project.Name;
        entity.RunsPath = project.RunsPath;

        var existing = await _database.Runs
            .Where(run => run.ProjectId == project.Id)
            .ToListAsync(cancellationToken);

        _database.Runs.RemoveRange(existing);

        foreach (var run in runs)
        {
            foreach (var stage in run.Stages)
            {
                stage.RunId = run.Id;
            }

            foreach (var text in run.Texts)
            {
                text.RunId = run.Id;
            }

            foreach (var session in run.Sessions)
            {
                session.RunId = run.Id;
            }

            foreach (var usage in run.StageUsage)
            {
                usage.RunId = run.Id;
            }

            _database.Runs.Add(run);
        }

        var existingItems = await _database.LibraryItems
            .Where(item => item.ProjectId == project.Id)
            .ToListAsync(cancellationToken);

        _database.LibraryItems.RemoveRange(existingItems);
        _database.LibraryItems.AddRange(libraryItems);

        // One save: the runs and the library of a project are rebuilt together or not at all.
        await _database.SaveChangesAsync(cancellationToken);
    }

    private async Task<ImportResultDto> SaveLogAsync(
        ProjectOptions project,
        DateTime startedAt,
        int filesRead,
        int blocksRead,
        int runsBuilt,
        int libraryItems,
        IReadOnlyList<(string File, string? Location, string Message)> errors,
        CancellationToken cancellationToken)
    {
        var finishedAt = _clock.GetUtcNow().UtcDateTime;

        var log = new ImportLogEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            FilesRead = filesRead,
            BlocksRead = blocksRead,
            RunsBuilt = runsBuilt,
            LibraryItems = libraryItems,
            ErrorCount = errors.Count,
        };

        foreach (var (file, location, message) in errors)
        {
            log.Errors.Add(new ImportErrorEntity
            {
                Id = Guid.NewGuid(),
                ImportLogId = log.Id,
                File = file,
                Location = location,
                Message = message,
            });
        }

        _database.ImportLogs.Add(log);
        await _database.SaveChangesAsync(cancellationToken);

        await TrimLogsAsync(project.Id, cancellationToken);

        _logger.LogInformation(
            "Imported {Runs} runs from {Project} with {Errors} errors.",
            runsBuilt,
            project.Id,
            errors.Count);

        return new ImportResultDto(
            project.Id,
            startedAt,
            finishedAt,
            filesRead,
            blocksRead,
            runsBuilt,
            libraryItems,
            errors.Count);
    }

    private async Task TrimLogsAsync(string projectId, CancellationToken cancellationToken)
    {
        var stale = await _database.ImportLogs
            .Where(log => log.ProjectId == projectId)
            .OrderByDescending(log => log.StartedAt)
            .Skip(LogsKept)
            .ToListAsync(cancellationToken);

        if (stale.Count > 0)
        {
            _database.ImportLogs.RemoveRange(stale);
            await _database.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Plan 0006 D10: a live run's `config.effective.json`, validated just enough to be a config.</summary>
    private static string? ReadEffectiveConfig(
        string file,
        string relative,
        List<(string File, string? Location, string Message)> errors)
    {
        try
        {
            var text = ReadText(file);

            using var document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("config", out var config)
                || config.ValueKind != JsonValueKind.Object)
            {
                errors.Add((relative, null, "config.effective.json has no config object."));
                return null;
            }

            return text;
        }
        catch (JsonException exception)
        {
            errors.Add((relative, null, $"config.effective.json: {exception.Message}"));
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add((relative, null, $"config.effective.json: {exception.Message}"));
            return null;
        }
    }

    private static string ReadText(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static string Relative(string root, string file)
    {
        return Path.GetRelativePath(root, file).Replace('\\', '/');
    }
}
