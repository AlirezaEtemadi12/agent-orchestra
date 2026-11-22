using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.SaveProfile;

public sealed record SaveProfileInput(string? Json, string? Reason);

public sealed record ProfileSavedDto(string Id, int Version, string Reason, string? ExportedTo);

/// <summary>§6.2/D4: validate → new version → export. A save always makes a new version; a profile is
/// never rewritten in place, so git-like history and a revert stay honest.</summary>
public sealed class SaveProfileHandler(
    OrchestraDbContext database,
    ProfileExporter exporter,
    TimeProvider clock)
{
    public async Task<(ProfileSavedDto? Saved, Error? Error, string? Detail)> HandleAsync(
        string id,
        SaveProfileInput? input,
        CancellationToken cancellationToken)
    {
        var reason = input?.Reason?.Trim() ?? string.Empty;

        if (reason.Length is < 1 or > 300)
        {
            return (null, Error.ProfileReasonRequired, null);
        }

        var json = input?.Json ?? string.Empty;
        var check = ProfileSchema.Validate(json);

        if (check.Code == "Profiles.Step.Locked")
        {
            return (null, Error.ProfileStepLocked, check.Detail);
        }

        if (check.Code is not null)
        {
            return (null, Error.ProfileInvalid, check.Detail);
        }

        var profile = await database.Profiles
            .FirstOrDefaultAsync(stored => stored.Id == id, cancellationToken);

        if (profile is null)
        {
            return (null, Error.ProfileNotFound, null);
        }

        var canonical = ConfigMerge.ProfileOnly(ConfigMerge.ParseObject(json)!);
        var version = profile.CurrentVersion + 1;

        profile.CurrentVersion = version;
        database.ProfileVersions.Add(new ProfileVersionEntity
        {
            Id = Guid.NewGuid(),
            ProfileId = id,
            Version = version,
            Json = canonical,
            Reason = reason,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });

        await database.SaveChangesAsync(cancellationToken);

        string? exported = null;

        try
        {
            exported = exporter.Write(id, version, canonical);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The version is stored, but the engine reads the file: a save whose export failed is
            // reported, not hidden. D5 says the folder must be writable by the image's uid 10001.
            return (null, Error.Unexpected, $"the version was saved but could not be exported to '{exporter.PathFor(id)}': {exception.Message}");
        }

        return (new ProfileSavedDto(id, version, reason, exported), null, null);
    }
}
