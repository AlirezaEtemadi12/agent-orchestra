using Orchestra.Api.Modules.Profiles;

namespace Orchestra.Api.Tests.Profiles;

public sealed class ProfileSchemaTests
{
    private const string Base = """
        {
          "roles": {
            "planner": { "chain": [ { "adapter": "claude", "model": "opus", "family": "claude-opus" } ] },
            "plan-reviewer": { "chain": [ { "adapter": "manual", "model": "manual/any-agent", "family": "manual" } ] }
          },
          "adapters": {
            "claude": { "enabled": true, "transport": "cli" },
            "manual": { "enabled": true, "transport": "inbox" }
          },
          "tiers": { "free": { "maxConcurrent": 1 }, "paid": { "maxConcurrent": 1 } },
          "workflow": { "steps": { "preview": { "enabled": true }, "plan": { "enabled": true } } }
        }
        """;

    [Fact]
    public void The_base_profile_is_valid()
    {
        ProfileSchema.Validate(Base).Code.Should().BeNull();
    }

    [Fact]
    public void A_planner_with_a_manual_hop_is_refused()
    {
        var json = Base.Replace(
            "\"planner\": { \"chain\": [ { \"adapter\": \"claude\", \"model\": \"opus\", \"family\": \"claude-opus\" } ] }",
            "\"planner\": { \"chain\": [ { \"adapter\": \"manual\", \"model\": \"x\", \"family\": \"manual\" } ] }");

        var result = ProfileSchema.Validate(json);

        result.Code.Should().Be("Profiles.Profile.Invalid");
        result.Detail.Should().Contain("planner");
    }

    [Fact]
    public void A_hop_without_a_family_is_refused()
    {
        var json = Base.Replace("\"model\": \"opus\", \"family\": \"claude-opus\"", "\"model\": \"opus\"");

        ProfileSchema.Validate(json).Detail.Should().Contain("family");
    }

    [Fact]
    public void A_hop_naming_an_unknown_adapter_is_refused()
    {
        var json = Base.Replace("\"adapter\": \"claude\", \"model\": \"opus\"", "\"adapter\": \"ghost\", \"model\": \"opus\"");

        ProfileSchema.Validate(json).Detail.Should().Contain("ghost");
    }

    [Fact]
    public void An_unknown_transport_is_refused()
    {
        var json = Base.Replace("\"transport\": \"cli\"", "\"transport\": \"carrier-pigeon\"");

        ProfileSchema.Validate(json).Detail.Should().Contain("carrier-pigeon");
    }

    [Fact]
    public void An_unknown_tier_is_refused()
    {
        var json = Base.Replace("\"free\":", "\"cheap\":");

        ProfileSchema.Validate(json).Detail.Should().Contain("cheap");
    }

    [Fact]
    public void A_locked_step_turned_off_is_reported_as_locked()
    {
        var json = Base.Replace("\"plan\": { \"enabled\": true }", "\"plan\": { \"enabled\": false }");

        var result = ProfileSchema.Validate(json);

        result.Code.Should().Be("Profiles.Step.Locked");
        result.Detail.Should().Be("plan");
    }

    [Fact]
    public void Preview_may_be_turned_off()
    {
        var json = Base.Replace("\"preview\": { \"enabled\": true }", "\"preview\": { \"enabled\": false }");

        ProfileSchema.Validate(json).Code.Should().BeNull();
    }

    [Fact]
    public void Note_keys_are_skipped()
    {
        var json = Base.Replace(
            "\"tiers\": {",
            "\"tiers\": { \"note\": \"what free and paid mean\",")
            .Replace("\"claude\": {", "\"note\": \"adapters\", \"claude\": {");

        ProfileSchema.Validate(json).Code.Should().BeNull();
    }

    [Fact]
    public void A_role_with_an_empty_chain_is_refused()
    {
        var json = Base.Replace(
            "\"plan-reviewer\": { \"chain\": [ { \"adapter\": \"manual\", \"model\": \"manual/any-agent\", \"family\": \"manual\" } ] }",
            "\"plan-reviewer\": { \"chain\": [] }");

        ProfileSchema.Validate(json).Detail.Should().Contain("plan-reviewer");
    }
}
