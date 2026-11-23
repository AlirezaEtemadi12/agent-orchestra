using Orchestra.Api.Common;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Setup.Features.Folders;
using Orchestra.Api.Modules.Setup.Features.Hosting;

namespace Orchestra.Api.Modules.Setup;

public sealed class SetupModuleInstaller : IModuleInstaller
{
    public int Order => 15;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IHostingReader, EnvironmentHostingReader>();
        services.AddSingleton<HostingInfo>();

        // One mapper for the whole app: projects validate and store through it, the check and the
        // folder browser answer in host form through it (plan 0009 D2).
        services.AddSingleton<PathMapper>(provider => provider.GetRequiredService<HostingInfo>().Mapper);

        services.AddScoped<HostingHandler>();
        services.AddScoped<FoldersHandler>();
    }
}
