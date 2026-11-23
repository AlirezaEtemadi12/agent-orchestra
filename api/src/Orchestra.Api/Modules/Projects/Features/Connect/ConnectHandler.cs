using System.Text.Json;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Modules.Projects.Features.Connect;

/// <summary>Plan 0009 D8/D9: the three texts the owner pastes. Only the second holds the token; the
/// prompt meant for an agent never does.</summary>
public sealed record ConnectDto(string ReportBlock, string LocalKeys, string AgentPrompt, bool HasProfilesPath);

public sealed class ConnectHandler(
    ProjectRegistry registry,
    ReportTokenStore tokens,
    HostingInfo hosting,
    IOptions<OrchestraOptions> options,
    IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public (ConnectDto? Connect, Error? Error) Handle(string id, string requestOrigin)
    {
        var project = registry.Find(id);

        if (project is null)
        {
            return (null, Error.ProjectNotFound);
        }

        var hostRoot = hosting.Mapper.ToHost(project.ResolveRootPath() ?? project.RunsPath);

        var block = JsonSerializer.Serialize(
            new
            {
                report = new
                {
                    enabled = true,
                    url = EventUrl(requestOrigin),
                    project = project.Id,
                    tokenEnv = "ORCHESTRA_REPORT_TOKEN",
                    timeoutSec = 2,
                    progressEverySec = 10,
                },
            },
            Json);

        var keys = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["reportToken"] = tokens.Get(),
        };

        var profiles = options.Value.ResolveProfilesPath(environment.ContentRootPath);
        var hostProfiles = hosting.Mapper.ToHost(profiles);

        if (!string.Equals(hostProfiles, profiles, StringComparison.Ordinal))
        {
            keys["profilesPath"] = hostProfiles;
        }

        var prompt = $"""
            Add this "report" block at the top level of .orchestra/orchestrator.json in {project.Name} ({hostRoot}). Change nothing else. Do not commit.

            {block}
            """;

        return (new ConnectDto(block, JsonSerializer.Serialize(keys, Json), prompt, keys.ContainsKey("profilesPath")), null);
    }

    private string EventUrl(string requestOrigin)
    {
        var origin = string.IsNullOrWhiteSpace(options.Value.PublicUrl)
            ? requestOrigin
            : options.Value.PublicUrl!;

        return origin.TrimEnd('/') + "/api/events";
    }
}
