using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Projects.Features.ListProjects;

public sealed class ListProjectsHandler(OrchestraDbContext database, ProjectRegistry registry)
{
    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var known = await database.Projects
            .AsNoTracking()
            .Select(project => project.Id)
            .ToListAsync(cancellationToken);

        var ids = registry.All.Select(project => project.Id)
            .Concat(known)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var projects = await database.Projects
            .AsNoTracking()
            .Include(project => project.Runs)
                .ThenInclude(run => run.Sessions)
            .ToListAsync(cancellationToken);

        var errors = await database.ImportLogs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Plan 0009 D11: the connect and start pages call a project connected after its first event.
        var lastEvents = await database.RunEvents
            .AsNoTracking()
            .GroupBy(item => item.ProjectId)
            .Select(group => new { ProjectId = group.Key, Last = group.Max(item => item.ReceivedAt) })
            .ToListAsync(cancellationToken);

        var result = new List<ProjectDto>();

        foreach (var id in ids)
        {
            var project = projects.FirstOrDefault(item => item.Id == id);
            var runs = project?.Runs ?? [];
            var configured = registry.Find(id);

            // Plan 0009 D3: the form and the list show the paths as the owner typed them, never
            // the container's /host/... form.
            var stored = registry.Stored(id);

            var lastLog = project is null
                ? null
                : errors
                    .Where(log => log.ProjectId == id)
                    .OrderByDescending(log => log.StartedAt)
                    .FirstOrDefault();

            result.Add(new ProjectDto(
                id,
                configured?.Name ?? project?.Name ?? id,
                runs.Count,
                runs.Count(run => run.ClosedAt is null),
                runs.Sum(run => (long)run.Sessions.Count),
                runs.Sum(run => run.Sessions.Sum(session => (long?)session.Calls) ?? 0),
                runs.Sum(run => run.Sessions.Sum(session => (long?)session.Requests) ?? 0),
                Cost(runs.Sum(run => run.CostMicros)),
                lastLog is null ? null : new LastImportDto(lastLog.FinishedAt, lastLog.ErrorCount),
                Origin(registry.OriginOf(id)),
                stored?.RootPath ?? configured?.ResolveRootPath(),
                stored?.RunsPath ?? configured?.RunsPath,
                configured?.Repos ?? [],
                lastEvents.FirstOrDefault(item => item.ProjectId == id)?.Last));
        }

        return result;
    }

    private static string? Origin(ProjectOrigin? origin)
    {
        return origin switch
        {
            ProjectOrigin.Config => "config",
            ProjectOrigin.Dashboard => "dashboard",
            _ => null,
        };
    }

    private static decimal? Cost(long? micros)
    {
        return micros is null ? null : micros.Value / 1_000_000m;
    }
}
