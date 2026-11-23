using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.Connect;
using Orchestra.Api.Modules.Projects.Features.CreateProject;
using Orchestra.Api.Modules.Projects.Features.DeleteProject;
using Orchestra.Api.Modules.Projects.Features.ListProjects;
using Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;
using Orchestra.Api.Modules.Projects.Features.StoreState;
using Orchestra.Api.Modules.Projects.Features.UpdateProject;

namespace Orchestra.Api.Modules.Projects;

public sealed class ProjectsModuleInstaller : IModuleInstaller
{
    public int Order => 20;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // The store lives beside the database: both are the dashboard's own data (plan 0003 D2).
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OrchestraOptions>>().Value;
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            var databasePath = options.ResolveDatabasePath(environment.ContentRootPath);
            var folder = Path.GetDirectoryName(databasePath) ?? environment.ContentRootPath;

            return new ProjectStore(Path.Combine(folder, "projects.json"));
        });

        services.AddSingleton<ProjectRegistry>();

        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<StoreStateHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<DeleteProjectHandler>();
        services.AddScoped<CheckFolderHandler>();
        services.AddScoped<ConnectHandler>();
        services.AddScoped<ScaffoldPromptHandler>();
    }
}
