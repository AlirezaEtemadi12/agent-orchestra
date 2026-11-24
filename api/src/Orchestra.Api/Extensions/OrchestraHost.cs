namespace Orchestra.Api.Extensions;

/// <summary>Plan 0012 §6.7: the same web host, built and configured but not run. <c>Program.cs</c> runs it;
/// the desktop app starts it and stops it when its window closes.</summary>
public static class OrchestraHost
{
    public static WebApplication Create(string[] args, string? contentRoot = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = contentRoot,
        });

        builder.ConfigureServices();

        var app = builder.Build();

        app.ConfigureApp();

        return app;
    }
}
