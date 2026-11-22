using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.GetProfile;

public sealed record ProfileVersionDto(string Id, int Version, string Json, DateTime CreatedAt);

/// <summary>§6.2: one version's JSON, current by default.</summary>
public sealed class GetProfileHandler(OrchestraDbContext database)
{
    public async Task<(ProfileVersionDto? Profile, Error? Error)> HandleAsync(
        string id,
        int? version,
        CancellationToken cancellationToken)
    {
        var profile = await database.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(stored => stored.Id == id, cancellationToken);

        if (profile is null)
        {
            return (null, Error.ProfileNotFound);
        }

        var wanted = version ?? profile.CurrentVersion;
        var storedVersion = await database.ProfileVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.ProfileId == id && item.Version == wanted,
                cancellationToken);

        return storedVersion is null
            ? (null, Error.ProfileNotFound)
            : (new ProfileVersionDto(id, storedVersion.Version, storedVersion.Json, storedVersion.CreatedAt), null);
    }
}
