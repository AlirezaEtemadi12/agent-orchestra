using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Import;

public sealed class ImportHostedService(
    IServiceScopeFactory scopes,
    IOptions<OrchestraOptions> options,
    ILogger<ImportHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ImportOnStart)
        {
            return;
        }

        try
        {
            using var scope = scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ImportService>();

            if (!service.TryEnter())
            {
                return;
            }

            try
            {
                await service.ImportAllAsync(stoppingToken);
            }
            finally
            {
                service.Leave();
            }
        }
        catch (Exception exception)
        {
            // A failed import never takes the host down.
            logger.LogError(exception, "The import on start failed.");
        }
    }
}
