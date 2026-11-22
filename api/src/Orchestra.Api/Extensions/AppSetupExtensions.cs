using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Ui;
using Orchestra.Api.Data;

namespace Orchestra.Api.Extensions;

internal static class AppSetupExtensions
{
    public static void ConfigureApp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<OrchestraOptions>>().Value;

        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("ui-dev");
        }

        app.UseUi(options);

        app.MapControllers();
        app.MapUiFallback(options);

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<OrchestraDbContext>().Database.Migrate();
    }
}
