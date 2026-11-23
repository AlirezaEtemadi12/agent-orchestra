using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Events.Features.ReceiveEvent;

namespace Orchestra.Api.Modules.Events;

public sealed class EventsModuleInstaller : IModuleInstaller
{
    public int Order => 70;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // The token file sits beside the database, the dashboard's own data folder (plan 0009 D6).
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OrchestraOptions>>().Value;
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            var databasePath = options.ResolveDatabasePath(environment.ContentRootPath);
            var folder = Path.GetDirectoryName(databasePath) ?? environment.ContentRootPath;

            return new ReportTokenStore(
                Path.Combine(folder, "report-token"),
                options.ReportToken,
                provider.GetRequiredService<ILogger<ReportTokenStore>>());
        });

        services.AddScoped<ReceiveEventHandler>();
        services.AddScoped<ReportTokenFilter>();
        services.AddHostedService<EventRetentionService>();
    }
}
