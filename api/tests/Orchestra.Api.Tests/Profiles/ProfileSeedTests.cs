using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestra.Api.Modules.Profiles;

namespace Orchestra.Api.Tests.Profiles;

public sealed class ProfileSeedTests
{
    [Fact]
    public async Task Seeds_default_v1_once_and_exports_it()
    {
        using var host = new ProfileTestHost();
        var seed = new ProfileSeedService(host.Scopes, host.Exporter, NullLogger<ProfileSeedService>.Instance);

        await seed.StartAsync(CancellationToken.None);
        await seed.StartAsync(CancellationToken.None);

        (await host.Database.Profiles.CountAsync()).Should().Be(1);
        (await host.Database.ProfileVersions.CountAsync()).Should().Be(1);

        var profile = await host.Database.Profiles.SingleAsync();

        profile.Id.Should().Be("default");
        profile.CurrentVersion.Should().Be(1);

        var file = Path.Combine(host.ProfilesFolder, "default.json");

        File.Exists(file).Should().BeTrue();

        var exported = JsonNode.Parse(await File.ReadAllTextAsync(file))!.AsObject();

        exported["profile"]!.GetValue<string>().Should().Be("default");
        exported["version"]!.GetValue<int>().Should().Be(1);
        exported["config"].Should().NotBeNull();
    }

    [Fact]
    public void The_seed_holds_exactly_the_D1_keys()
    {
        var config = JsonNode.Parse(ProfileSeedService.ReadEmbedded())!.AsObject();

        config.Select(pair => pair.Key).Should().BeEquivalentTo(ConfigMerge.ProfileKeys);
    }

    [Fact]
    public void The_seed_roles_and_tiers_equal_the_engine_example()
    {
        var examplePath = Path.Combine(RepoRoot(), "engine", "examples", "orchestrator.json");
        var example = JsonNode.Parse(File.ReadAllText(examplePath))!.AsObject();
        var seed = JsonNode.Parse(ProfileSeedService.ReadEmbedded())!.AsObject();

        JsonNode.DeepEquals(WithoutNotes(seed["roles"]), WithoutNotes(example["roles"])).Should().BeTrue();
        JsonNode.DeepEquals(WithoutNotes(seed["tiers"]), WithoutNotes(example["tiers"])).Should().BeTrue();
    }

    [Fact]
    public void The_seed_turns_every_workflow_step_on()
    {
        var seed = JsonNode.Parse(ProfileSeedService.ReadEmbedded())!.AsObject();
        var steps = seed["workflow"]!["steps"]!.AsObject();

        steps.Select(pair => pair.Key).Should().BeEquivalentTo(ProfileSchema.AllSteps);

        foreach (var (_, value) in steps)
        {
            value!["enabled"]!.GetValue<bool>().Should().BeTrue();
        }
    }

    /// <summary>D11: note/description keys are documentation and differ between the two files.</summary>
    private static JsonNode? WithoutNotes(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var copy = new JsonObject();

            foreach (var (name, value) in obj)
            {
                if (!ConfigMerge.IsNote(name))
                {
                    copy[name] = WithoutNotes(value);
                }
            }

            return copy;
        }

        if (node is JsonArray array)
        {
            var copy = new JsonArray();

            foreach (var item in array)
            {
                copy.Add(WithoutNotes(item));
            }

            return copy;
        }

        return node?.DeepClone();
    }

    /// <summary>Walks up from the test binary to the repository that holds engine/ — drift fails here,
    /// never in Docker (which has no engine folder) because this test is not run there.</summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "engine", "examples", "orchestrator.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("engine/examples/orchestrator.json was not found above the test binary.");
    }
}
