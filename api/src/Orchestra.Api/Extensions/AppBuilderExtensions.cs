using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Extensions;

internal static class AppBuilderExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services
            .AddOptions<OrchestraOptions>()
            .Bind(builder.Configuration.GetSection(OrchestraOptions.SectionName))
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<OrchestraOptions>, OrchestraOptionsValidator>();

        // Every clock in the API comes from here; DateTime.Now never appears outside the import clock.
        builder.Services.AddSingleton(TimeProvider.System);

        // Plan 0012 §6.7: the published app is one file, and there MVC's dependency-context discovery
        // finds no application part, so every controller 404s. Name the assembly it lives in.
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(AppBuilderExtensions).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddProblemDetails();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options => options.AddPolicy(
                "ui-dev",
                policy => policy
                    .WithOrigins("http://localhost:3501")
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
        }

        foreach (var installer in DiscoverInstallers())
        {
            installer.Install(builder.Services, builder.Configuration);
        }

        return builder;
    }

    private static IEnumerable<IModuleInstaller> DiscoverInstallers()
    {
        return typeof(Program).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(IModuleInstaller).IsAssignableFrom(type))
            .Where(type => type.GetConstructor(Type.EmptyTypes) is not null
                || type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
            .Select(type => (IModuleInstaller)Activator.CreateInstance(type)!)
            .OrderBy(installer => installer.Order)
            .ToArray();
    }
}
