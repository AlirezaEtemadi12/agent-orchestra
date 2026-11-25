using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.GetVersions;

public sealed record ProfileVersionSummaryDto(int Version, string Reason, DateTime CreatedAt);

public sealed record ConfigChangeDto(string Path, string? From, string? To);

public sealed record ProfileDiffDto(IReadOnlyList<ConfigChangeDto> Diff);

/// <summary>Plan 0010 D5/D13: the <c>?diff=a,b</c> half of the history; the list moved to
/// <c>/versions/table</c>. A missing or malformed pair is 400 with the same detail.</summary>
public sealed class GetVersionDiffHandler(OrchestraDbContext database)
{
    private const string DiffDetail = "diff must be two version numbers: ?diff=1,2";

    public async Task<(ProfileDiffDto? Diff, Error? Error, string? Detail)> HandleAsync(
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

        if (string.IsNullOrWhiteSpace(diff))
        {
            return (null, Error.ProfileInvalid, DiffDetail);
        }

        var parts = diff.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2 || !int.TryParse(parts[0], out var left) || !int.TryParse(parts[1], out var right))
        {
            return (null, Error.ProfileInvalid, DiffDetail);
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

        return (new ProfileDiffDto(changes), null, null);
    }
}
