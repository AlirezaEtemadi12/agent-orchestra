using Gridify;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Import;

/// <summary>Plan 0010 D11: the import log is a server-side table; the errors of the page load with it.</summary>
public sealed class ImportLogsTableHandler(OrchestraDbContext database)
{
    public async Task<(Paging<ImportLogDto>? Page, Error? Error)> HandleAsync(
        string? project,
        GridifyQuery query,
        CancellationToken cancellationToken)
    {
        var source = database.ImportLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            source = source.Where(log => log.ProjectId == project);
        }

        var (paging, error) = TableMapper<ImportLogEntity, ImportLogDto>.Query(
            source, query, "startedAt desc", "id");

        if (error is not null || paging is null)
        {
            return (null, error);
        }

        var logs = await paging.Query
            .Include(log => log.Errors)
            .ToListAsync(cancellationToken);

        var items = logs
            .Select(log => new ImportLogDto(
                log.Id,
                log.ProjectId,
                log.StartedAt,
                log.FinishedAt,
                log.FilesRead,
                log.BlocksRead,
                log.RunsBuilt,
                log.LibraryItems,
                log.ErrorCount,
                log.Errors
                    .OrderBy(failure => failure.File)
                    .Select(failure => new ImportErrorDto(failure.File, failure.Location, failure.Message))
                    .ToList()))
            .ToList();

        return (new Paging<ImportLogDto>(paging.Count, items), null);
    }
}
