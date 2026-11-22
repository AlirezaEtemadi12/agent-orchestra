using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Data;

public sealed class DataModuleInstaller : IModuleInstaller
{
    public int Order => 10;

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrchestraDbContext>((provider, options) =>
        {
            var orchestra = provider.GetRequiredService<IOptions<OrchestraOptions>>().Value;
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            var path = orchestra.ResolveDatabasePath(environment.ContentRootPath);
            var folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            options.UseSqlite($"Data Source={path}");
        });
    }
}
