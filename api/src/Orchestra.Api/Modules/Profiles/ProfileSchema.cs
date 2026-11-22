using System.Text.Json.Nodes;

namespace Orchestra.Api.Modules.Profiles;

/// <summary>Plan 0006 §6.3: the engine's own three checks by transport, plus the dashboard's early checks.
/// The engine re-validates the effective config (D8) and remains the guarantee; this exists so a broken
/// profile is refused at save time with a readable detail instead of at the next run.</summary>
public static class ProfileSchema
{
    /// <summary>D6: always on; a profile may not turn one off.</summary>
    public static IReadOnlyList<string> LockedSteps { get; } =
        ["understanding", "plan", "review", "approve", "implement", "verify"];

    public static IReadOnlyList<string> AllSteps { get; } =
        ["understanding", "preview", "plan", "review", "approve", "implement", "verify"];

    public static IReadOnlyList<string> KnownTiers { get; } = ["free", "paid"];

    /// <summary>The result of a check: no error, or a code plus the detail the message shows.</summary>
    public sealed record Result(string? Code, string? Detail)
    {
        public static Result Ok { get; } = new(null, null);

        public static Result Invalid(string detail) => new("Profiles.Profile.Invalid", detail);

        public static Result Locked(string step) => new("Profiles.Step.Locked", step);
    }

    public static Result Validate(string json)
    {
        JsonObject? config;

        try
        {
            config = ConfigMerge.ParseObject(json);
        }
        catch (System.Text.Json.JsonException exception)
        {
            return Result.Invalid(exception.Message);
        }

        return config is null ? Result.Invalid("the profile is not a JSON object.") : Validate(config);
    }

    public static Result Validate(JsonObject config)
    {
        var adapters = config["adapters"] as JsonObject ?? new JsonObject();

        if (config["roles"] is not JsonObject roles)
        {
            return Result.Invalid("roles is missing.");
        }

        foreach (var (role, value) in roles)
        {
            if (ConfigMerge.IsNote(role))
            {
                continue;
            }

            if (value is not JsonObject spec || spec["chain"] is not JsonArray chain || chain.Count == 0)
            {
                return Result.Invalid($"role '{role}' has no chain.");
            }

            foreach (var hop in chain)
            {
                if (hop is not JsonObject item)
                {
                    return Result.Invalid($"role '{role}' has a hop that is not an object.");
                }

                var adapter = Text(item["adapter"]);

                if (adapter.Length == 0)
                {
                    return Result.Invalid($"a hop in '{role}' has no adapter.");
                }

                if (Text(item["model"]).Length == 0)
                {
                    return Result.Invalid($"a hop in '{role}' has no model.");
                }

                if (Text(item["family"]).Length == 0)
                {
                    return Result.Invalid($"a hop in '{role}' has no family.");
                }

                if (adapters[adapter] is not JsonObject)
                {
                    return Result.Invalid($"role '{role}' uses adapter '{adapter}', which adapters does not define.");
                }

                var transport = Transport(adapter, adapters[adapter] as JsonObject);

                if (transport is not ("cli" or "inbox"))
                {
                    return Result.Invalid($"adapter '{adapter}' has transport '{transport}'; use 'cli' or 'inbox'.");
                }

                // The engine's own rule, by transport as the engine does it: the planner's plan must be
                // written by a model, so a manual (inbox) hop is forbidden there and nowhere else.
                if (role == "planner" && transport == "inbox")
                {
                    return Result.Invalid("the planner may not have a manual (inbox) hop; its plan must be written by a model.");
                }
            }
        }

        foreach (var (name, value) in adapters)
        {
            if (ConfigMerge.IsNote(name))
            {
                continue;
            }

            var transport = Transport(name, value as JsonObject);

            if (transport is not ("cli" or "inbox"))
            {
                return Result.Invalid($"adapter '{name}' has transport '{transport}'; use 'cli' or 'inbox'.");
            }
        }

        if (config["tiers"] is JsonObject tiers)
        {
            foreach (var (name, _) in tiers)
            {
                if (ConfigMerge.IsNote(name) || KnownTiers.Contains(name, StringComparer.Ordinal))
                {
                    continue;
                }

                return Result.Invalid($"tier '{name}' is not known; use free or paid.");
            }
        }

        if (config["workflow"] is JsonObject workflow && workflow["steps"] is JsonObject steps)
        {
            foreach (var step in LockedSteps)
            {
                if (steps[step] is JsonObject spec && spec["enabled"] is JsonValue value
                    && value.TryGetValue<bool>(out var enabled) && !enabled)
                {
                    return Result.Locked(step);
                }
            }
        }

        return Result.Ok;
    }

    /// <summary>The engine's default: an adapter's own `transport`, else `manual` is the inbox and every
    /// other adapter is a CLI.</summary>
    public static string Transport(string adapter, JsonObject? spec)
    {
        if (spec?["transport"] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
        {
            return text.ToLowerInvariant();
        }

        return adapter == "manual" ? "inbox" : "cli";
    }

    private static string Text(JsonNode? node)
    {
        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;
    }
}
