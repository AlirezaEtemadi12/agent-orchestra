using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Usage.Features.CostSummary;

public sealed class CostSummaryHandler(OrchestraDbContext database)
{
    private static readonly string[] Groups = ["day", "month", "model", "project"];

    public async Task<(IReadOnlyList<CostRowDto>? Rows, Error? Error)> HandleAsync(
        string? project,
        DateOnly? from,
        DateOnly? to,
        string? groupBy,
        string? tz,
        CancellationToken cancellationToken)
    {
        var group = (groupBy ?? "day").Trim().ToLowerInvariant();

        if (!Groups.Contains(group, StringComparer.Ordinal))
        {
            return (null, Error.InvalidGroupBy);
        }

        if (from is not null && to is not null && to < from)
        {
            return (null, Error.InvalidRange);
        }

        TimeZoneInfo zone;

        if (string.IsNullOrWhiteSpace(tz))
        {
            zone = TimeZoneInfo.Utc;
        }
        else
        {
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            }
            catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return (null, Error.InvalidTimeZone);
            }
        }

        var query = database.Runs
            .AsNoTracking()
            .Include(run => run.Sessions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            query = query.Where(run => run.ProjectId == project);
        }

        // A run is dated by its last block; SQLite cannot do the zone arithmetic, so it happens here.
        var runs = await query.ToListAsync(cancellationToken);

        var dated = runs
            .Where(run => run.UpdatedAt is not null)
            .Select(run => (
                Run: run,
                Date: DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(run.UpdatedAt!.Value, zone))))
            .Where(item => from is null || item.Date >= from)
            .Where(item => to is null || item.Date <= to)
            .ToList();

        IReadOnlyList<CostRowDto> rows = group switch
        {
            "model" => ByModel(dated.Select(item => item.Run)),
            "project" => GroupByRuns(dated, item => item.Run.ProjectId),
            "month" => GroupByRuns(dated, item => item.Date.ToString("yyyy-MM", CultureInfo.InvariantCulture)),
            _ => GroupByRuns(dated, item => item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        };

        return (rows, null);
    }

    private static IReadOnlyList<CostRowDto> GroupByRuns(
        IEnumerable<(RunEntity Run, DateOnly Date)> items,
        Func<(RunEntity Run, DateOnly Date), string> key)
    {
        return items
            .GroupBy(key, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var runs = group.Select(item => item.Run).ToList();

                return new CostRowDto(
                    group.Key,
                    Cost(runs.Sum(run => run.CostMicros)),
                    runs.Count,
                    runs.Sum(run => (long)run.Sessions.Count),
                    runs.Sum(run => run.Sessions.Sum(session => (long?)session.Calls) ?? 0),
                    runs.Sum(run => run.Sessions.Sum(session => session.Input) ?? 0),
                    runs.Sum(run => run.Sessions.Sum(session => session.Output) ?? 0),
                    runs.Sum(run => run.Sessions.Sum(session => session.CacheRead) ?? 0));
            })
            .ToList();
    }

    private static IReadOnlyList<CostRowDto> ByModel(IEnumerable<RunEntity> runs)
    {
        return runs
            .SelectMany(run => run.Sessions)
            .GroupBy(session => session.Model ?? "—", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CostRowDto(
                group.Key,
                Cost(group.Sum(session => session.CostMicros)),
                group.Select(session => session.RunId).Distinct().LongCount(),
                group.LongCount(),
                group.Sum(session => (long?)session.Calls) ?? 0,
                group.Sum(session => session.Input) ?? 0,
                group.Sum(session => session.Output) ?? 0,
                group.Sum(session => session.CacheRead) ?? 0))
            .ToList();
    }

    private static decimal? Cost(long? micros)
    {
        return micros is null ? null : micros.Value / 1_000_000m;
    }
}
