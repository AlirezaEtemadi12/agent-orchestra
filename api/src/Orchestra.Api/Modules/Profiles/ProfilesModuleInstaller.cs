using Orchestra.Api.Common;
using Orchestra.Api.Modules.Profiles.Features.GetProfile;
using Orchestra.Api.Modules.Profiles.Features.GetVersions;
using Orchestra.Api.Modules.Profiles.Features.ListProfiles;
using Orchestra.Api.Modules.Profiles.Features.RevertProfile;
using Orchestra.Api.Modules.Profiles.Features.SaveProfile;
using Orchestra.Api.Modules.Projects.Features.EffectiveConfig;

namespace Orchestra.Api.Modules.Profiles;

/// <summary>Plan 0006 D3: profiles are the dashboard's own data. Order 80 runs last — the seed needs the
/// migrated database, and the exporter needs the resolved options.</summary>
public sealed class ProfilesModuleInstaller : IModuleInstaller
{
    public int Order => 80;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ProfileExporter>();
        services.AddHostedService<ProfileSeedService>();

        services.AddScoped<ListProfilesHandler>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<SaveProfileHandler>();
        services.AddScoped<RevertProfileHandler>();
        services.AddScoped<GetVersionsHandler>();
        services.AddScoped<EffectiveConfigHandler>();
    }
}
