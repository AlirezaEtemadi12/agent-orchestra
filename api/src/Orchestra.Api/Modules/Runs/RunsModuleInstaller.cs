using Orchestra.Api.Common;
using Orchestra.Api.Modules.Runs.Features.Board;
using Orchestra.Api.Modules.Runs.Features.GetRun;
using Orchestra.Api.Modules.Runs.Features.ListRuns;
using Orchestra.Api.Modules.Runs.Features.Pools;

namespace Orchestra.Api.Modules.Runs;

public sealed class RunsModuleInstaller : IModuleInstaller
{
    public int Order => 40;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ListRunsHandler>();
        services.AddScoped<GetRunHandler>();
        services.AddScoped<BoardHandler>();
        services.AddScoped<PoolsHandler>();
    }
}
