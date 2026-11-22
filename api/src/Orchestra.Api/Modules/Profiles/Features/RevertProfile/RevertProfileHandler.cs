using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Profiles.Features.SaveProfile;

namespace Orchestra.Api.Modules.Profiles.Features.RevertProfile;

public sealed record RevertProfileInput(int? Version);

/// <summary>D4: a revert is a new version with the reason <c>revert to v&lt;n&gt;</c>; a revert to the
/// current version is refused.</summary>
public sealed class RevertProfileHandler(
    OrchestraDbContext database,
    ProfileExporter exporter,
    TimeProvider clock)
{
    public async Task<(ProfileSavedDto? Saved, Error? Error, string? Detail)> HandleAsync(
        string id,
        RevertProfileInput? input,
        CancellationToken cancellationToken)
    {
        var profile = await database.Profiles
            .FirstOrDefaultAsync(stored => stored.Id == id, cancellationToken);

        if (profile is null)
        {
            return (null, Error.ProfileNotFound, null);
        }

        var wanted = input?.Version ?? 0;

        if (wanted == profile.CurrentVersion)
        {
            return (null, Error.ProfileRevertCurrent, null);
        }

        var source = await database.ProfileVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                version => version.ProfileId == id && version.Version == wanted,
                cancellationToken);

        if (source is null)
        {
            return (null, Error.ProfileNotFound, $"version {wanted}");
        }

        var version = profile.CurrentVersion + 1;
        var reason = $"revert to v{wanted}";

        profile.CurrentVersion = version;
        database.ProfileVersions.Add(new ProfileVersionEntity
        {
            Id = Guid.NewGuid(),
            ProfileId = id,
            Version = version,
            Json = source.Json,
            Reason = reason,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });

        await database.SaveChangesAsync(cancellationToken);

        try
        {
            var exported = exporter.Write(id, version, source.Json);

            return (new ProfileSavedDto(id, version, reason, exported), null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, Error.Unexpected, $"the version was saved but could not be exported to '{exporter.PathFor(id)}': {exception.Message}");
        }
    }
}
