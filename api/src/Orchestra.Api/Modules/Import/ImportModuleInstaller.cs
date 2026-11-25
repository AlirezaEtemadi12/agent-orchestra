using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Import;

public sealed class ImportModuleInstaller : IModuleInstaller
{
    public int Order => 30;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ImportService>();
        services.AddScoped<ImportLogsTableHandler>();
        services.AddHostedService<ImportHostedService>();
    }
}
