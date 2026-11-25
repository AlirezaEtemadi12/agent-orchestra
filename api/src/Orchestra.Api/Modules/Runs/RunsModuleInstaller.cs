using Orchestra.Api.Common;
using Orchestra.Api.Modules.Runs.Features.Board;
using Orchestra.Api.Modules.Runs.Features.GetRun;
using Orchestra.Api.Modules.Runs.Features.Pools;
using Orchestra.Api.Modules.Runs.Features.RunsTable;

namespace Orchestra.Api.Modules.Runs;

public sealed class RunsModuleInstaller : IModuleInstaller
{
    public int Order => 40;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RunsTableHandler>();
        services.AddScoped<RunsSummaryHandler>();
        services.AddScoped<GetRunHandler>();
        services.AddScoped<BoardHandler>();
        services.AddScoped<PoolsHandler>();
    }
}
