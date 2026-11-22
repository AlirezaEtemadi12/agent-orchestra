using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orchestra.Api.Modules.Profiles;

/// <summary>Plan 0006 D2 and D1, in the dashboard: the profile's config is the base, the project's whole
/// orchestrator.json goes on top, key by key and recursively; arrays replace; null removes. The engine
/// implements the same rule in <c>Merge-ConfigObject</c> (agents.ps1) — the two must stay in step.</summary>
public static class ConfigMerge
{
    /// <summary>D1: the keys a profile owns. Every other key stays in the project's own file, untouched.</summary>
    public static IReadOnlyList<string> ProfileKeys { get; } =
    [
        "roles",
        "tiers",
        "adapters",
        "modelRatesSource",
        "modelRates",
        "quota",
        "sessions",
        "revise",
        "readback",
        "preflight",
        "defaults",
        "rules",
        "workflow",
    ];

    /// <summary>Keys the engine never reads: documentation only. A schema check that walks a section
    /// must skip them (they sit inside tiers, modelRates, modelRatesSource and each adapter).</summary>
    public static bool IsNote(string name)
    {
        return string.Equals(name, "note", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "description", StringComparison.OrdinalIgnoreCase);
    }

    public static JsonObject? ParseObject(string json)
    {
        return JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        }) as JsonObject;
    }

    /// <summary>A profile holds exactly the D1 keys: anything else a save carries is dropped here, so a
    /// project key can never leak into the base of a merge.</summary>
    public static string ProfileOnly(JsonObject config)
    {
        var kept = new JsonObject();

        foreach (var key in ProfileKeys)
        {
            if (config.TryGetPropertyValue(key, out var value) && value is not null)
            {
                kept[key] = value.DeepClone();
            }
        }

        return kept.ToJsonString(Options);
    }

    /// <summary>The effective config as JSON: profile first, project on top.</summary>
    public static string Merge(string? profileJson, JsonObject project)
    {
        var result = ParseObject(profileJson ?? "{}") ?? new JsonObject();

        Overlay(result, project);

        return result.ToJsonString(Options);
    }

    public static JsonObject MergeObject(string? profileJson, JsonObject project)
    {
        var result = ParseObject(profileJson ?? "{}") ?? new JsonObject();

        Overlay(result, project);

        return result;
    }

    private static void Overlay(JsonObject target, JsonObject over)
    {
        foreach (var (name, value) in over)
        {
            if (value is null)
            {
                target.Remove(name);
                continue;
            }

            // Arrays are values, not containers: D2 replaces them whole. Only two objects merge.
            if (value is JsonObject overObject && target[name] is JsonObject targetObject)
            {
                Overlay(targetObject, overObject);
                continue;
            }

            target[name] = value.DeepClone();
        }
    }

    /// <summary>Where each merged value came from: the project when it carries that path, else the profile.</summary>
    public static IReadOnlyList<ConfigSource> Sources(string? profileJson, JsonObject project)
    {
        var profile = ParseObject(profileJson ?? "{}") ?? new JsonObject();
        var effective = MergeObject(profileJson, project);

        var fromProject = new HashSet<string>(StringComparer.Ordinal);
        var fromProfile = new HashSet<string>(StringComparer.Ordinal);

        Flatten(project, string.Empty, fromProject);
        Flatten(profile, string.Empty, fromProfile);

        var sources = new List<ConfigSource>();

        foreach (var path in fromProject.Union(fromProfile, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (!Exists(effective, path))
            {
                continue;
            }

            sources.Add(new ConfigSource(path, fromProject.Contains(path) ? "project" : "profile"));
        }

        return sources;
    }

    /// <summary>D1/§6.5: what the project's file would look like with this profile — the non-D1 keys plus
    /// only the D1 values that differ from the profile.</summary>
    public static string Suggested(JsonObject project, string? profileJson)
    {
        var profile = ParseObject(profileJson ?? "{}") ?? new JsonObject();
        var suggested = new JsonObject();

        foreach (var (name, value) in project)
        {
            if (value is null)
            {
                continue;
            }

            if (ProfileKeys.Contains(name, StringComparer.Ordinal))
            {
                // D1 says `workflow` is merged, so only its non-profile parts (the paths) are the project's
                // to keep; the profile's steps never go into the suggested file.
                if (name == "workflow" && value is JsonObject workflow)
                {
                    var kept = new JsonObject();

                    foreach (var (child, childValue) in workflow)
                    {
                        if (child != "steps" && childValue is not null)
                        {
                            kept[child] = childValue.DeepClone();
                        }
                    }

                    if (kept.Count > 0)
                    {
                        suggested[name] = kept;
                    }

                    continue;
                }

                if (profile[name] is { } profileValue && JsonNode.DeepEquals(profileValue, value))
                {
                    continue;
                }
            }

            suggested[name] = value.DeepClone();
        }

        return suggested.ToJsonString(Options);
    }

    /// <summary>A JSON-path list of changes between two versions, for the history's diff.</summary>
    public static IReadOnlyList<ConfigChange> Diff(string before, string after)
    {
        var left = new Dictionary<string, string?>(StringComparer.Ordinal);
        var right = new Dictionary<string, string?>(StringComparer.Ordinal);

        FlattenValues(ParseObject(before) ?? new JsonObject(), string.Empty, left);
        FlattenValues(ParseObject(after) ?? new JsonObject(), string.Empty, right);

        var changes = new List<ConfigChange>();

        foreach (var path in left.Keys.Union(right.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            left.TryGetValue(path, out var from);
            right.TryGetValue(path, out var to);

            if (from != to)
            {
                changes.Add(new ConfigChange(path, from, to));
            }
        }

        return changes;
    }

    private static void FlattenValues(JsonObject node, string prefix, Dictionary<string, string?> into)
    {
        foreach (var (name, value) in node)
        {
            var path = prefix.Length == 0 ? name : $"{prefix}.{name}";

            if (value is JsonObject child)
            {
                FlattenValues(child, path, into);
            }
            else
            {
                into[path] = value?.ToJsonString();
            }
        }
    }

    private static void Flatten(JsonObject node, string prefix, HashSet<string> into)
    {
        foreach (var (name, value) in node)
        {
            var path = prefix.Length == 0 ? name : $"{prefix}.{name}";

            if (value is JsonObject child)
            {
                Flatten(child, path, into);
            }
            else if (value is not null)
            {
                into.Add(path);
            }
        }
    }

    private static bool Exists(JsonObject effective, string path)
    {
        JsonNode? current = effective;

        foreach (var part in path.Split('.'))
        {
            if (current is not JsonObject node || !node.TryGetPropertyValue(part, out current))
            {
                return false;
            }
        }

        return current is not null;
    }

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

public sealed record ConfigSource(string Path, string From);

public sealed record ConfigChange(string Path, string? From, string? To);
