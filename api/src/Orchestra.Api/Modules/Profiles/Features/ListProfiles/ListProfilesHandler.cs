using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.ListProfiles;

public sealed record ProfileSummaryDto(string Id, string Name, int CurrentVersion, DateTime? UpdatedAt);

/// <summary>§6.2: the list the editor's first tab reads.</summary>
public sealed class ListProfilesHandler(OrchestraDbContext database)
{
    public async Task<IReadOnlyList<ProfileSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var profiles = await database.Profiles
            .AsNoTracking()
            .Select(profile => new
            {
                profile.Id,
                profile.Name,
                profile.CurrentVersion,
                UpdatedAt = profile.Versions.Max(version => (DateTime?)version.CreatedAt),
            })
            .OrderBy(profile => profile.Id)
            .ToListAsync(cancellationToken);

        return [.. profiles.Select(profile => new ProfileSummaryDto(
            profile.Id,
            profile.Name,
            profile.CurrentVersion,
            profile.UpdatedAt))];
    }
}
