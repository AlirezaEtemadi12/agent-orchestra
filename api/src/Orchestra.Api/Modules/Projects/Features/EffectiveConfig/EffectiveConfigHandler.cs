using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Profiles;

namespace Orchestra.Api.Modules.Projects.Features.EffectiveConfig;

public sealed record ConfigSourceDto(string Path, string From);

public sealed record EffectiveConfigDto(
    JsonElement Effective,
    IReadOnlyList<ConfigSourceDto> Sources,
    string SuggestedOrchestratorJson,
    string? Profile,
    int? ProfileVersion);

/// <summary>§6.2: the project's effective values, where each came from, and the orchestrator.json text an
/// agent would paste. Read-only: the dashboard never writes into a project (ADR 0005).</summary>
public sealed class EffectiveConfigHandler(ProjectRegistry registry, OrchestraDbContext database)
{
    public async Task<(EffectiveConfigDto? Config, Error? Error, string? Detail)> HandleAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var project = registry.Find(id);

        if (project is null)
        {
            return (null, Error.ProjectNotFound, null);
        }

        var root = project.ResolveRootPath();

        if (string.IsNullOrWhiteSpace(root))
        {
            return (null, Error.ProfileProjectUnreadable, "the project has no RootPath.");
        }

        var path = Path.Combine(root, "orchestrator", "orchestrator.json");

        if (!File.Exists(path))
        {
            return (null, Error.ProfileProjectUnreadable, path);
        }

        JsonObject projectConfig;

        try
        {
            projectConfig = ConfigMerge.ParseObject(await File.ReadAllTextAsync(path, cancellationToken))
                ?? throw new JsonException("the file is not a JSON object.");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return (null, Error.ProfileProjectUnreadable, $"{path}: {exception.Message}");
        }

        var profileId = projectConfig["profile"] is JsonValue value && value.TryGetValue<string>(out var named)
            ? named
            : null;

        string? profileJson = null;
        int? profileVersion = null;

        if (!string.IsNullOrWhiteSpace(profileId))
        {
            var profile = await database.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(stored => stored.Id == profileId, cancellationToken);

            if (profile is null)
            {
                return (null, Error.ProfileNotFound, profileId);
            }

            var version = await database.ProfileVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    stored => stored.ProfileId == profileId && stored.Version == profile.CurrentVersion,
                    cancellationToken);

            if (version is null)
            {
                return (null, Error.ProfileNotFound, profileId);
            }

            profileJson = version.Json;
            profileVersion = version.Version;
        }

        var effective = ConfigMerge.MergeObject(profileJson, projectConfig);
        var sources = ConfigMerge.Sources(profileJson, projectConfig)
            .Select(source => new ConfigSourceDto(source.Path, source.From))
            .ToList();

        var dto = new EffectiveConfigDto(
            JsonSerializer.SerializeToElement(effective),
            sources,
            ConfigMerge.Suggested(projectConfig, profileJson),
            profileId,
            profileVersion);

        return (dto, null, null);
    }
}
