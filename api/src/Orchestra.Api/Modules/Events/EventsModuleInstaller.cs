using Orchestra.Api.Common;
using Orchestra.Api.Modules.Events.Features.ReceiveEvent;

namespace Orchestra.Api.Modules.Events;

public sealed class EventsModuleInstaller : IModuleInstaller
{
    public int Order => 70;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ReceiveEventHandler>();
        services.AddScoped<ReportTokenFilter>();
        services.AddHostedService<EventRetentionService>();
    }
}
