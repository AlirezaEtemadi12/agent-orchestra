using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects.Features.CheckFolder;
using Orchestra.Api.Modules.Projects.Features.ScaffoldPrompt;

namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>One row that could not be done, with the code the screen translates and the system's own text.</summary>
internal sealed class SetupItemException(string code, string detail) : Exception(detail)
{
    public string Code { get; } = code;
}

/// <summary>Plan 0012 §6.3.3: recomputes the plan server-side, then executes it in order. A file is
/// created only when it is missing; only <c>orchestrator.json</c>'s report block and the three keys of
/// <c>orchestrator.local.json</c> may touch an existing file. Nothing is deleted or committed.</summary>
public sealed class SetupApplier(
    SetupGuard guard,
    SetupPlanner planner,
    TemplateSet templates,
    EngineLocator engine,
    IConfiguration configuration,
    ProjectRegistry registry,
    ImportService imports,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions LocalJson = new() { WriteIndented = true };

    public async Task<(SetupResultDto? Result, Error? Error, SetupRefusal? Refusal)> HandleAsync(
        SetupRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return (null, Error.ProjectInvalid, null);
        }

        var check = guard.CheckRoot(request.RootPath);

        if (check.Refusal is { } refusal)
        {
            return (null, Error.SetupPathRefused, refusal);
        }

        var root = check.Root!;

        if (engine.Find() is null || !templates.IsAvailable)
        {
            return (null, Error.SetupEngineNotFound, null);
        }

        var repos = SetupPlanner.NormalizeRepos(request.Repos);
        var runsPath = Path.Combine(root, ".orchestra", "runs");

        var (valid, invalid) = ProjectValidation.Validate(
            new ProjectInput(request.Id, request.Name, root, runsPath, [.. repos], null));

        if (valid is null)
        {
            return (null, invalid, null);
        }

        if (registry.OriginOf(valid.Id) == ProjectOrigin.Config)
        {
            return (null, Error.ProjectReadOnly, null);
        }

        var applied = request with { RootPath = root, Id = valid.Id, Name = valid.Name, Repos = repos };
        var plan = planner.Plan(applied, root);
        var results = Run(applied, plan, root);

        StoredProject? entry;
        var existing = registry.Stored(valid.Id);

        try
        {
            if (existing is null)
            {
                entry = new StoredProject
                {
                    Id = valid.Id,
                    Name = valid.Name,
                    RootPath = root,
                    RunsPath = runsPath,
                    Repos = [.. repos],

                    // The store's one profile value: the engine reads the real one from the project's
                    // orchestrator.json (plan 0012 D10).
                    Profile = "default",
                    CreatedAt = clock.GetUtcNow().UtcDateTime,
                };

                registry.Create(entry);
            }
            else
            {
                registry.Update(valid.Id, current => current with
                {
                    Name = valid.Name,
                    RootPath = root,
                    RunsPath = runsPath,
                    Repos = [.. repos],
                });

                entry = registry.Stored(valid.Id);
            }
        }
        catch (ProjectStoreUnreadableException)
        {
            return (null, Error.ProjectStoreUnreadable, null);
        }

        if (entry is null)
        {
            return (null, Error.ProjectStoreUnreadable, null);
        }

        await imports.ImportOneAsync(entry.ToOptions(), cancellationToken);

        var prompt = ScaffoldPromptBuilder.Build(valid.Name, root, repos, FolderCheck.Run(root));
        var saved = new ProjectSavedDto(entry.Id, entry.Name, entry.RootPath, entry.RunsPath, entry.Repos, entry.Profile);

        return (new SetupResultDto(results, saved, prompt), null, null);
    }

    private List<SetupResultItemDto> Run(SetupRequest request, SetupPlanDto plan, string root)
    {
        var skip = new HashSet<string>(request.Skip ?? [], StringComparer.Ordinal);
        var results = new List<SetupResultItemDto>();
        var stop = false;
        var folderError = TryCreateFolder(Path.Combine(root, ".orchestra"));
        var folderReported = false;

        foreach (var item in plan.Items)
        {
            if (item.Action == SetupActions.Exists || skip.Contains(item.Id))
            {
                results.Add(new SetupResultItemDto(item.Id, true, null, null));

                continue;
            }

            if (stop)
            {
                results.Add(new SetupResultItemDto(item.Id, false, SetupItemErrors.Skipped, null));

                continue;
            }

            // A .orchestra/ that cannot be created stops everything after it (plan 0012 §6.3.3 step 2).
            if (folderError is not null && !folderReported)
            {
                folderReported = true;
                stop = true;
                results.Add(new SetupResultItemDto(item.Id, false, SetupItemErrors.WriteFailed, folderError));

                continue;
            }

            try
            {
                var detail = Execute(item, request, plan, root);

                results.Add(new SetupResultItemDto(item.Id, true, null, detail));
            }
            catch (SetupItemException exception)
            {
                stop |= item.Id == SetupPlanner.GitInit;

                results.Add(new SetupResultItemDto(item.Id, false, exception.Code, exception.Message));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                stop |= item.Id == SetupPlanner.GitInit;

                results.Add(new SetupResultItemDto(item.Id, false, SetupItemErrors.WriteFailed, exception.Message));
            }
        }

        return results;
    }

    private string? Execute(SetupItemDto item, SetupRequest request, SetupPlanDto plan, string root)
    {
        var path = Path.Combine(root, item.Path.Replace('/', Path.DirectorySeparatorChar));

        if (item.Id == SetupPlanner.GitInit)
        {
            return GitInit(root);
        }

        if (item.Id == SetupPlanner.ReportBlock)
        {
            return AddReportBlock(root, request);
        }

        if (item.Id == SetupPlanner.LocalJson)
        {
            return WriteLocalJson(path);
        }

        if (item.Id == SetupPlanner.OrchestratorJson)
        {
            WriteText(path, RenderConfig(request, plan));

            return null;
        }

        if (item.Id.StartsWith("knowledge/", StringComparison.Ordinal))
        {
            return WriteKnowledge(item, path, request);
        }

        if (item.Id.StartsWith("plans/", StringComparison.Ordinal))
        {
            WriteText(path, string.Empty);

            return null;
        }

        WriteBytes(path, templates.Read(item.Id));

        return null;
    }

    private static string? GitInit(string root)
    {
        var orchestra = Path.Combine(root, ".orchestra");

        Directory.CreateDirectory(orchestra);

        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(orchestra);
        start.ArgumentList.Add("init");

        Process process;

        try
        {
            process = Process.Start(start)
                ?? throw new SetupItemException(SetupItemErrors.GitInitFailed, "git could not be started.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new SetupItemException(SetupItemErrors.GitInitFailed, exception.Message);
        }

        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();

        if (!process.WaitForExit(30_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
            {
                // The process is already gone.
            }

            throw new SetupItemException(SetupItemErrors.GitInitFailed, "git init did not finish within 30 seconds.");
        }

        _ = output.GetAwaiter().GetResult();
        var text = errors.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            throw new SetupItemException(
                SetupItemErrors.GitInitFailed,
                FirstLine(text) ?? $"git exited with code {process.ExitCode}.");
        }

        return null;
    }

    private string? AddReportBlock(string root, SetupRequest request)
    {
        var path = Path.Combine(root, ".orchestra", "orchestrator.json");
        var bytes = File.ReadAllBytes(path);
        var index = Array.IndexOf(bytes, (byte)'{');

        if (index < 0)
        {
            throw new SetupItemException(SetupItemErrors.InvalidJson, "The file has no JSON object.");
        }

        var insertion = Encoding.UTF8.GetBytes($"\n    \"report\": {ReportBlock(request)},");

        using var output = new MemoryStream(bytes.Length + insertion.Length);

        output.Write(bytes, 0, index + 1);
        output.Write(insertion);
        output.Write(bytes, index + 1, bytes.Length - index - 1);

        WriteBytes(path, output.ToArray());

        return null;
    }

    private string? WriteLocalJson(string path)
    {
        var (existing, error) = SetupPlanner.ReadLocalJson(path);

        if (existing is null)
        {
            throw new SetupItemException(SetupItemErrors.InvalidJson, error ?? "The file is not a JSON object.");
        }

        foreach (var pair in planner.DesiredLocalJson())
        {
            existing[pair.Key] = pair.Value;
        }

        WriteText(path, existing.ToJsonString(LocalJson));

        return null;
    }

    private string? WriteKnowledge(SetupItemDto item, string path, SetupRequest request)
    {
        var isAgents = item.Id.EndsWith("AGENTS.md", StringComparison.Ordinal);
        var template = isAgents ? "knowledge/AGENTS.md" : "knowledge/.ai/README.md";
        var name = isAgents ? KnowledgeName(item.Id, request.Name) : request.Name;

        WriteText(path, Templates(template).Replace("<Name>", name, StringComparison.Ordinal));

        return null;
    }

    private string Templates(string relative)
    {
        return templates.ReadText(relative);
    }

    private static string KnowledgeName(string id, string projectName)
    {
        var parts = id.Split('/');

        return parts.Length >= 3 ? parts[1] : projectName;
    }

    private string RenderConfig(SetupRequest request, SetupPlanDto plan)
    {
        var repos = SetupPlanner.NormalizeRepos(request.Repos);
        var single = plan.IsSingleRepo;

        var repoObject = new JsonObject();

        foreach (var repo in repos)
        {
            repoObject[single ? request.Name : repo] = single ? "." : repo;
        }

        var commands = new JsonObject();

        foreach (var pair in plan.VerifyCommands)
        {
            var list = new JsonArray();

            foreach (var command in pair.Value)
            {
                list.Add(command);
            }

            commands[pair.Key] = list;
        }

        var config = new JsonObject
        {
            ["profile"] = "default",
            ["root"] = "..",
            ["description"] = new JsonObject
            {
                ["title"] = request.Name,
                ["repos"] = single ? $"  - Repo: {repos[0]}" : $"  - Repos: {string.Join(", ", repos)}",
            },
            ["planFiling"] = "number",
            ["repos"] = repoObject,
            ["plansDir"] = single ? ".orchestra/plans" : ".orchestra/plans/{repo}",
            ["workflow"] = new JsonObject
            {
                ["gatesPath"] = ".orchestra/workflow.md",
                ["rolesPath"] = ".orchestra/roles",
                ["rolesIndex"] = ".orchestra/roles/README.md",
                ["promptsPath"] = ".orchestra/prompts",
                ["agentsDoc"] = single ? "AGENTS.md" : "{repo}/AGENTS.md",
                ["indexDoc"] = single ? ".ai/README.md" : "{repo}/.ai/README.md",
                ["planTemplate"] = ".orchestra/plans/TEMPLATE.md",
            },
            ["verifyCommands"] = commands,
            ["report"] = ReportBlockObject(request),
        };

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentSize = 4 });
    }

    private JsonObject ReportBlockObject(SetupRequest request)
    {
        return new JsonObject
        {
            ["enabled"] = true,
            ["url"] = ReportUrl(),
            ["project"] = request.Id,
            ["tokenEnv"] = "ORCHESTRA_REPORT_TOKEN",
            ["timeoutSec"] = 2,
            ["progressEverySec"] = 10,
        };
    }

    private string ReportBlock(SetupRequest request)
    {
        return ReportBlockObject(request).ToJsonString();
    }

    /// <summary>The first loopback address in <c>Urls</c>, plus <c>/api/events</c> (plan 0012 D12).</summary>
    private string ReportUrl()
    {
        var urls = configuration["Urls"] ?? string.Empty;

        foreach (var candidate in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (candidate.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("127.0.0.1", StringComparison.Ordinal)
                || candidate.Contains("[::1]", StringComparison.Ordinal))
            {
                return candidate.TrimEnd('/') + "/api/events";
            }
        }

        return "http://localhost:3500/api/events";
    }

    private static string? TryCreateFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }

    private static string? FirstLine(string? text)
    {
        return text?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    /// <summary>Temp file then move, so a half-written file never sits at a real path (plan 0012 D7).</summary>
    private static void WriteText(string path, string text)
    {
        WriteBytes(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text));
    }

    private static void WriteBytes(string path, byte[] bytes)
    {
        var folder = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var temporary = path + ".tmp";

        File.WriteAllBytes(temporary, bytes);
        File.Move(temporary, path, overwrite: true);
    }
}
