using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles;

/// <summary>D11/§6.1: at start, when no profile exists, <c>default</c> v1 comes from the embedded
/// <c>default-profile.json</c> — never from <c>engine/</c>, which is not in the Docker image. Then every
/// profile is exported, so the file always equals the current version.</summary>
public sealed class ProfileSeedService(
    IServiceScopeFactory scopes,
    ProfileExporter exporter,
    ILogger<ProfileSeedService> logger) : IHostedService
{
    public const string DefaultId = "default";

    private const string Resource = "Orchestra.Api.Modules.Profiles.default-profile.json";
    private const string SeedReason = "initial profile";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<OrchestraDbContext>();

        if (!await database.Profiles.AnyAsync(cancellationToken))
        {
            var json = ReadEmbedded();

            database.Profiles.Add(new ProfileEntity
            {
                Id = DefaultId,
                Name = "Default",
                CurrentVersion = 1,
                Versions =
                [
                    new ProfileVersionEntity
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = DefaultId,
                        Version = 1,
                        Json = json,
                        Reason = SeedReason,
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            });

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeded the default profile from {Resource}.", Resource);
        }

        foreach (var profile in await database.Profiles.AsNoTracking().ToListAsync(cancellationToken))
        {
            try
            {
                var version = await database.ProfileVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        stored => stored.ProfileId == profile.Id && stored.Version == profile.CurrentVersion,
                        cancellationToken);

                if (version is not null)
                {
                    exporter.Write(profile.Id, version.Version, version.Json);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A profile folder that cannot be written must not stop the dashboard: the save path
                // reports the failure then, with the request that asked for it.
                logger.LogWarning("Could not export profile {Id} at start: {Error}", profile.Id, exception.Message);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static string ReadEmbedded()
    {
        var assembly = typeof(ProfileSeedService).Assembly;

        using var stream = assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"The embedded resource {Resource} is missing.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
