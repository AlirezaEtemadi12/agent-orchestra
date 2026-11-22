using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Events;

/// <summary>D9: <c>progress</c> events are the only ones with a need-by date — the latest line of a hop
/// that finished last month is noise — so they are deleted after seven days. Every other kind is kept:
/// the per-round detail the import cannot rebuild lives there. The import never runs this.</summary>
public sealed class EventRetentionService(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<EventRetentionService> logger) : BackgroundService
{
    public static readonly TimeSpan ProgressRetention = TimeSpan.FromDays(7);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<OrchestraDbContext>();

                await DeleteExpiredProgressAsync(database, clock.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                // A retention pass is housekeeping: it must never take the app down.
                logger.LogWarning(exception, "Deleting old progress events failed; the next pass tries again.");
            }

            try
            {
                await Task.Delay(Interval, clock, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>One pass, so a test can run it without waiting a day.</summary>
    public static Task<int> DeleteExpiredProgressAsync(
        OrchestraDbContext database,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var cutoff = nowUtc - ProgressRetention;

        return database.RunEvents
            .Where(stored => stored.Kind == EventKinds.Progress && stored.At < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
