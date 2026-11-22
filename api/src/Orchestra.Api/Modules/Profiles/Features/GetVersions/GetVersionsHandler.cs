using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.GetVersions;

public sealed record ProfileVersionSummaryDto(int Version, string Reason, DateTime CreatedAt);

public sealed record ConfigChangeDto(string Path, string? From, string? To);

public sealed record ProfileVersionsDto(
    IReadOnlyList<ProfileVersionSummaryDto> Versions,
    IReadOnlyList<ConfigChangeDto>? Diff);

/// <summary>§6.2: the history's list, and with <c>?diff=a,b</c> the JSON-path changes between them.</summary>
public sealed class GetVersionsHandler(OrchestraDbContext database)
{
    public async Task<(ProfileVersionsDto? Versions, Error? Error, string? Detail)> HandleAsync(
        string id,
        string? diff,
        CancellationToken cancellationToken)
    {
        var profile = await database.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(stored => stored.Id == id, cancellationToken);

        if (profile is null)
        {
            return (null, Error.ProfileNotFound, null);
        }

        var versions = await database.ProfileVersions
            .AsNoTracking()
            .Where(version => version.ProfileId == id)
            .OrderByDescending(version => version.Version)
            .Select(version => new ProfileVersionSummaryDto(version.Version, version.Reason, version.CreatedAt))
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(diff))
        {
            return (new ProfileVersionsDto(versions, null), null, null);
        }

        var parts = diff.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2 || !int.TryParse(parts[0], out var left) || !int.TryParse(parts[1], out var right))
        {
            return (null, Error.ProfileInvalid, "diff must be two version numbers: ?diff=1,2");
        }

        var pair = await database.ProfileVersions
            .AsNoTracking()
            .Where(version => version.ProfileId == id && (version.Version == left || version.Version == right))
            .ToListAsync(cancellationToken);

        var before = pair.FirstOrDefault(version => version.Version == left);
        var after = pair.FirstOrDefault(version => version.Version == right);

        if (before is null || after is null)
        {
            return (null, Error.ProfileNotFound, $"one of v{left} or v{right} does not exist");
        }

        var changes = ConfigMerge.Diff(before.Json, after.Json)
            .Select(change => new ConfigChangeDto(change.Path, change.From, change.To))
            .ToList();

        return (new ProfileVersionsDto(versions, changes), null, null);
    }
}
