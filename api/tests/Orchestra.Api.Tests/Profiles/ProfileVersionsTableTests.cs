using Gridify;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Profiles;
using Orchestra.Api.Modules.Profiles.Features.GetVersions;

namespace Orchestra.Api.Tests.Profiles;

public sealed class ProfileVersionsTableTests
{
    private const string First = """
        { "roles": { "planner": { "chain": [ { "adapter": "claude", "model": "opus", "family": "claude-opus" } ] } } }
        """;

    private const string Second = """
        { "roles": { "planner": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } } }
        """;

    [Fact]
    public async Task The_default_order_is_version_desc()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", First);
        AddVersion(host, 2, Second, "second");
        AddVersion(host, 3, First, "third");

        var (page, error) = await new ProfileVersionsTableHandler(host.Database)
            .HandleAsync("default", new GridifyQuery(), CancellationToken.None);

        error.Should().BeNull();
        page!.Count.Should().Be(3);
        page.Data.Select(version => version.Version).Should().Equal(3, 2, 1);
    }

    [Fact]
    public async Task The_page_has_the_total_and_the_page_size()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", First);
        AddVersion(host, 2, Second, "second");

        var (page, _) = await new ProfileVersionsTableHandler(host.Database)
            .HandleAsync("default", new GridifyQuery { Page = 2, PageSize = 1 }, CancellationToken.None);

        page!.Count.Should().Be(2);
        page.Data.Single().Version.Should().Be(1);
    }

    [Fact]
    public async Task An_unknown_field_is_table_query_invalid()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", First);

        var (page, error) = await new ProfileVersionsTableHandler(host.Database)
            .HandleAsync("default", new GridifyQuery { OrderBy = "passwordHash" }, CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.TableQueryInvalid.Code);
    }

    [Fact]
    public async Task An_unknown_profile_is_a_not_found()
    {
        using var host = new ProfileTestHost();

        var (page, error) = await new ProfileVersionsTableHandler(host.Database)
            .HandleAsync("nope", new GridifyQuery(), CancellationToken.None);

        page.Should().BeNull();
        error!.Code.Should().Be(Error.ProfileNotFound.Code);
    }

    [Fact]
    public async Task A_missing_or_malformed_diff_is_invalid()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", First);

        var handler = new GetVersionDiffHandler(host.Database);

        var (missing, missingError, missingDetail) = await handler.HandleAsync("default", null, CancellationToken.None);
        missing.Should().BeNull();
        missingError!.Code.Should().Be(Error.ProfileInvalid.Code);
        missingDetail.Should().Be("diff must be two version numbers: ?diff=1,2");

        var (malformed, malformedError, _) = await handler.HandleAsync("default", "1", CancellationToken.None);
        malformed.Should().BeNull();
        malformedError!.Code.Should().Be(Error.ProfileInvalid.Code);
    }

    [Fact]
    public async Task A_valid_pair_returns_the_changes()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", First);
        AddVersion(host, 2, Second, "second");

        var (diff, error, _) = await new GetVersionDiffHandler(host.Database)
            .HandleAsync("default", "1,2", CancellationToken.None);

        error.Should().BeNull();
        diff!.Diff.Should().NotBeEmpty();
    }

    private static void AddVersion(ProfileTestHost host, int version, string json, string reason)
    {
        host.Database.ProfileVersions.Add(new ProfileVersionEntity
        {
            Id = Guid.NewGuid(),
            ProfileId = "default",
            Version = version,
            Json = json,
            Reason = reason,
            CreatedAt = new DateTime(2026, 1, version, 0, 0, 0, DateTimeKind.Utc),
        });

        host.Database.SaveChanges();
    }
}
