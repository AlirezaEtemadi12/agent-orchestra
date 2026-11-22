using Orchestra.Api.Common;
using Orchestra.Api.Modules.Usage.Features.CostSummary;
using Orchestra.Api.Modules.Usage.Features.ModelQuality;

namespace Orchestra.Api.Modules.Usage;

public sealed class UsageModuleInstaller : IModuleInstaller
{
    public int Order => 50;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CostSummaryHandler>();
        services.AddScoped<ModelQualityHandler>();
    }
}
