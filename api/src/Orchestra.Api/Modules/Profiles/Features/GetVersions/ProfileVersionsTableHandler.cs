using Gridify;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Data;

namespace Orchestra.Api.Modules.Profiles.Features.GetVersions;

/// <summary>Plan 0010 D13: the versions of one profile are a table; every save adds a row.</summary>
public sealed class ProfileVersionsTableHandler(OrchestraDbContext database)
{
    public async Task<(Paging<ProfileVersionSummaryDto>? Page, Error? Error)> HandleAsync(
        string id,
        GridifyQuery query,
        CancellationToken cancellationToken)
    {
        if (!await database.Profiles.AsNoTracking().AnyAsync(profile => profile.Id == id, cancellationToken))
        {
            return (null, Error.ProfileNotFound);
        }

        var source = database.ProfileVersions
            .AsNoTracking()
            .Where(version => version.ProfileId == id);

        var (paging, error) = TableMapper<ProfileVersionEntity, ProfileVersionSummaryDto>.Query(
            source, query, "version desc", "version");

        if (error is not null || paging is null)
        {
            return (null, error);
        }

        var rows = await paging.Query
            .Select(version => new ProfileVersionSummaryDto(version.Version, version.Reason, version.CreatedAt))
            .ToListAsync(cancellationToken);

        return (new Paging<ProfileVersionSummaryDto>(paging.Count, rows), null);
    }
}
