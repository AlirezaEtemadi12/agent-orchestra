using Orchestra.Api.Common;
using Orchestra.Api.Modules.Library.Features.GetLibraryItem;
using Orchestra.Api.Modules.Library.Features.ListLibrary;
using Orchestra.Api.Modules.Library.Features.PlanPrompt;
using Orchestra.Api.Modules.Library.Features.ReadLibraryFile;

namespace Orchestra.Api.Modules.Library;

public sealed class LibraryModuleInstaller : IModuleInstaller
{
    public int Order => 60;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<LibraryTableHandler>();
        services.AddScoped<GetLibraryItemHandler>();
        services.AddScoped<ReadLibraryFileHandler>();
        services.AddScoped<PlanPromptHandler>();
    }
}
