using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Modules.Profiles;
using Orchestra.Api.Modules.Profiles.Features.RevertProfile;
using Orchestra.Api.Modules.Profiles.Features.SaveProfile;

namespace Orchestra.Api.Tests.Profiles;

public sealed class SaveRevertTests
{
    private const string Valid = """
        {
          "roles": {
            "planner": { "chain": [ { "adapter": "claude", "model": "opus", "family": "claude-opus" } ] }
          },
          "adapters": { "claude": { "enabled": true, "transport": "cli" } },
          "tiers": { "paid": { "maxConcurrent": 1 } }
        }
        """;

    [Fact]
    public async Task A_save_needs_a_reason()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Valid);

        var handler = new SaveProfileHandler(host.Database, host.Exporter, TimeProvider.System);

        var (saved, error, _) = await handler.HandleAsync(
            "default",
            new SaveProfileInput(Valid, "  "),
            CancellationToken.None);

        saved.Should().BeNull();
        error!.Code.Should().Be("Profiles.Reason.Required");
        (await host.Database.ProfileVersions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task A_save_increments_the_version_and_exports_it()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Valid);

        var handler = new SaveProfileHandler(host.Database, host.Exporter, TimeProvider.System);
        var (saved, error, _) = await handler.HandleAsync(
            "default",
            new SaveProfileInput(Valid.Replace("maxConcurrent\": 1", "maxConcurrent\": 2"), "wider pool"),
            CancellationToken.None);

        error.Should().BeNull();
        saved!.Version.Should().Be(2);

        var profile = await host.Database.Profiles.SingleAsync();
        var current = await host.Database.ProfileVersions
            .SingleAsync(version => version.Version == profile.CurrentVersion);

        profile.CurrentVersion.Should().Be(2);
        current.Reason.Should().Be("wider pool");

        var exported = await File.ReadAllTextAsync(Path.Combine(host.ProfilesFolder, "default.json"));

        exported.Should().Contain("\"version\": 2");
        exported.Should().Contain("maxConcurrent");
    }

    [Fact]
    public async Task A_save_drops_keys_a_profile_does_not_own()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Valid);

        var handler = new SaveProfileHandler(host.Database, host.Exporter, TimeProvider.System);
        var (saved, _, _) = await handler.HandleAsync(
            "default",
            new SaveProfileInput("""{ "roles": {}, "report": { "enabled": true } }""", "tidy"),
            CancellationToken.None);

        saved.Should().NotBeNull();

        var current = await host.Database.ProfileVersions
            .SingleAsync(version => version.Version == 2);

        current.Json.Should().NotContain("report");
    }

    [Fact]
    public async Task An_invalid_profile_is_refused_with_a_detail()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Valid);

        var handler = new SaveProfileHandler(host.Database, host.Exporter, TimeProvider.System);
        var (_, error, detail) = await handler.HandleAsync(
            "default",
            new SaveProfileInput("""{ "adapters": {} }""", "no roles"),
            CancellationToken.None);

        error!.Code.Should().Be("Profiles.Profile.Invalid");
        detail.Should().Contain("roles");
    }

    [Fact]
    public async Task A_revert_makes_a_new_equal_version_and_refuses_the_current_one()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Valid);

        var save = new SaveProfileHandler(host.Database, host.Exporter, TimeProvider.System);
        await save.HandleAsync("default", new SaveProfileInput("""{ "roles": { "planner": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } }, "adapters": { "claude": {} } }""", "sonnet"), CancellationToken.None);

        var revert = new RevertProfileHandler(host.Database, host.Exporter, TimeProvider.System);

        var (refused, refusedError, _) = await revert.HandleAsync(
            "default",
            new RevertProfileInput(2),
            CancellationToken.None);

        refused.Should().BeNull();
        refusedError!.Code.Should().Be("Profiles.Revert.Current");

        var (saved, error, _) = await revert.HandleAsync(
            "default",
            new RevertProfileInput(1),
            CancellationToken.None);

        error.Should().BeNull();
        saved!.Version.Should().Be(3);
        saved.Reason.Should().Be("revert to v1");

        var v1 = await host.Database.ProfileVersions.SingleAsync(version => version.Version == 1);
        var v3 = await host.Database.ProfileVersions.SingleAsync(version => version.Version == 3);

        v3.Json.Should().Be(v1.Json);
    }
}
