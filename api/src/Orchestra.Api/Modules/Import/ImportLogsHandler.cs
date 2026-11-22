using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Import;

public sealed class ImportLogsHandler(OrchestraDbContext database)
{
    public async Task<IReadOnlyList<ImportLogDto>> HandleAsync(
        string? project,
        int take,
        CancellationToken cancellationToken)
    {
        var query = database.ImportLogs
            .AsNoTracking()
            .Include(log => log.Errors)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            query = query.Where(log => log.ProjectId == project);
        }

        return await query
            .OrderByDescending(log => log.StartedAt)
            .Take(Math.Clamp(take, 1, 50))
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
                    .OrderBy(error => error.File)
                    .Select(error => new ImportErrorDto(error.File, error.Location, error.Message))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
