using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Profiles;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.EffectiveConfig;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Profiles;

public sealed class EffectiveConfigTests
{
    private const string Profile = """
        {
          "roles": {
            "planner": { "chain": [ { "adapter": "claude", "model": "opus", "family": "claude-opus" } ] },
            "plan-reviewer": { "chain": [ { "adapter": "opencode", "model": "deepseek", "family": "deepseek" } ] }
          },
          "adapters": {
            "claude": { "transport": "cli" },
            "opencode": { "transport": "cli" }
          },
          "tiers": { "free": { "maxConcurrent": 1 } },
          "workflow": { "steps": { "preview": { "enabled": true } } }
        }
        """;

    [Fact]
    public void Objects_merge_and_arrays_replace()
    {
        var project = ConfigMerge.ParseObject("""
            {
              "roles": { "plan-reviewer": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } },
              "workflow": { "gatesPath": "{repo}/workflow.md" }
            }
            """)!;

        var merged = ConfigMerge.ParseObject(ConfigMerge.Merge(Profile, project))!;

        merged["roles"]!["planner"]!["chain"]!.AsArray().Count.Should().Be(1);
        merged["roles"]!["plan-reviewer"]!["chain"]!.AsArray().Count.Should().Be(1);
        merged["roles"]!["plan-reviewer"]!["chain"]![0]!["model"]!.GetValue<string>().Should().Be("sonnet");

        // D2: workflow merges too - the project's path and the profile's step both survive.
        merged["workflow"]!["gatesPath"]!.GetValue<string>().Should().Be("{repo}/workflow.md");
        merged["workflow"]!["steps"]!["preview"]!["enabled"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void Null_removes_a_key()
    {
        var project = ConfigMerge.ParseObject("""{ "tiers": null }""")!;
        var merged = ConfigMerge.ParseObject(ConfigMerge.Merge(Profile, project))!;

        merged.ContainsKey("tiers").Should().BeFalse();
    }

    [Fact]
    public void Sources_name_the_file_each_value_came_from()
    {
        var project = ConfigMerge.ParseObject("""
            {
              "roles": { "plan-reviewer": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } },
              "workflow": { "gatesPath": "x" }
            }
            """)!;

        var sources = ConfigMerge.Sources(Profile, project);

        sources.Single(source => source.Path == "roles.planner.chain").From.Should().Be("profile");
        sources.Single(source => source.Path == "roles.plan-reviewer.chain").From.Should().Be("project");
        sources.Single(source => source.Path == "workflow.gatesPath").From.Should().Be("project");
    }

    [Fact]
    public void The_suggested_file_keeps_non_D1_keys_and_only_different_D1_values()
    {
        var project = ConfigMerge.ParseObject("""
            {
              "version": 1,
              "root": ".",
              "profile": "default",
              "tiers": { "free": { "maxConcurrent": 1 } },
              "roles": { "plan-reviewer": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } },
              "workflow": { "gatesPath": "x", "steps": { "preview": { "enabled": false } } }
            }
            """)!;

        var suggested = ConfigMerge.ParseObject(ConfigMerge.Suggested(project, Profile))!;

        suggested["root"]!.GetValue<string>().Should().Be(".");
        suggested["profile"]!.GetValue<string>().Should().Be("default");
        suggested.ContainsKey("tiers").Should().BeFalse();
        suggested["roles"]!["plan-reviewer"].Should().NotBeNull();
        suggested["workflow"]!["gatesPath"]!.GetValue<string>().Should().Be("x");
        suggested["workflow"]!.AsObject().ContainsKey("steps").Should().BeFalse();
    }

    [Fact]
    public async Task The_endpoint_reports_an_unreadable_project_file_as_422()
    {
        using var host = new ProfileTestHost();
        var root = Path.Combine(host.Folder, "project");
        Directory.CreateDirectory(root);

        var options = Options.Create(new OrchestraOptions
        {
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RootPath = root, RunsPath = Path.Combine(root, "runs") }],
        });

        var handler = new EffectiveConfigHandler(
            TestRegistries.ConfigOnly(options),
            host.Database);

        var (config, error, detail) = await handler.HandleAsync("demo", CancellationToken.None);

        config.Should().BeNull();
        error!.Code.Should().Be("Profiles.Project.Unreadable");
        detail.Should().Contain(".orchestra");
        detail.Should().Contain("orchestrator.json");
    }

    [Fact]
    public async Task The_endpoint_merges_the_projects_profile_and_names_sources()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Profile);

        var root = Path.Combine(host.Folder, "project");
        Directory.CreateDirectory(Path.Combine(root, "orchestrator"));

        await File.WriteAllTextAsync(
            Path.Combine(root, "orchestrator", "orchestrator.json"),
            """
            {
              "version": 1,
              "root": ".",
              "profile": "default",
              "roles": { "plan-reviewer": { "chain": [ { "adapter": "claude", "model": "sonnet", "family": "claude-sonnet" } ] } }
            }
            """);

        var options = Options.Create(new OrchestraOptions
        {
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RootPath = root, RunsPath = Path.Combine(root, "runs") }],
        });

        var handler = new EffectiveConfigHandler(TestRegistries.ConfigOnly(options), host.Database);

        var (config, error, _) = await handler.HandleAsync("demo", CancellationToken.None);

        error.Should().BeNull();
        config!.Profile.Should().Be("default");
        config.ProfileVersion.Should().Be(1);

        JsonNode.Parse(config.Effective.GetRawText())!["roles"]!["plan-reviewer"]!["chain"]![0]!["model"]!
            .GetValue<string>().Should().Be("sonnet");

        config.Sources.Single(source => source.Path == "tiers.free.maxConcurrent").From.Should().Be("profile");
        config.Sources.Single(source => source.Path == "roles.plan-reviewer.chain").From.Should().Be("project");
        config.SuggestedOrchestratorJson.Should().Contain("\"profile\": \"default\"");
        config.SuggestedOrchestratorJson.Should().NotContain("\"steps\"");
        config.ConfigPath.Should().Be("orchestrator/orchestrator.json");
    }

    [Fact]
    public async Task The_endpoint_reads_the_new_orchestra_layout_first()
    {
        using var host = new ProfileTestHost();
        host.AddProfile("default", Profile);

        var root = Path.Combine(host.Folder, "project");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        Directory.CreateDirectory(Path.Combine(root, "orchestrator"));

        // Both layouts exist: the moved one wins, and the page is told which file it read.
        await File.WriteAllTextAsync(
            Path.Combine(root, ".orchestra", "orchestrator.json"),
            """
            { "version": 2, "root": "..", "profile": "default", "description": { "title": "moved" } }
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "orchestrator", "orchestrator.json"),
            """
            { "version": 1, "root": ".", "description": { "title": "old copy" } }
            """);

        var options = Options.Create(new OrchestraOptions
        {
            Projects = [new ProjectOptions { Id = "demo", Name = "Demo", RootPath = root, RunsPath = Path.Combine(root, ".orchestra", "runs") }],
        });

        var handler = new EffectiveConfigHandler(TestRegistries.ConfigOnly(options), host.Database);

        var (config, error, _) = await handler.HandleAsync("demo", CancellationToken.None);

        error.Should().BeNull();
        config!.ConfigPath.Should().Be(".orchestra/orchestrator.json");
        config.Profile.Should().Be("default");
        JsonNode.Parse(config.Effective.GetRawText())!["description"]!["title"]!.GetValue<string>().Should().Be("moved");
    }
}
