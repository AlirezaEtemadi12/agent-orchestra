using Orchestra.Api.Common;
using Orchestra.Api.Modules.Setup.Features.Folders;

namespace Orchestra.Api.Modules.Setup;

public sealed class SetupModuleInstaller : IModuleInstaller
{
    public int Order => 15;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<FoldersHandler>();
    }
}
