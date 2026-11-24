using System.Text.RegularExpressions;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Modules.Projects;

/// <summary>The body of a create or update request (plan 0003 §6.3). <c>Profile</c> stays in the shape
/// and is ignored: the engine reads the profile from the project's own file (plan 0009 D12).</summary>
public sealed record ProjectInput(
    string? Id,
    string? Name,
    string? RootPath,
    string? RunsPath,
    List<string>? Repos,
    string? Profile);

public sealed record ValidProject(
    string Id,
    string Name,
    string RootPath,
    string RunsPath,
    IReadOnlyList<string> Repos);

public sealed record ProjectSavedDto(
    string Id,
    string Name,
    string RootPath,
    string RunsPath,
    IReadOnlyList<string> Repos,
    string Profile);

/// <summary>Plan 0003 §6.1: the same rules for the store file and the request. The server's answer is one
/// code, never a per-field list (D12).</summary>
public static partial class ProjectValidation
{
    private const int NameMaxLength = 80;
    private const int ReposMax = 20;

    /// <summary>Plan 0012 D5: the path is used as typed — no map, no container, no second form. What is
    /// stored is what the owner sees.</summary>
    public static (ValidProject? Value, Error? Error) Validate(ProjectInput? input)
    {
        if (input is null)
        {
            return (null, Error.ProjectInvalid);
        }

        var id = (input.Id ?? string.Empty).Trim();
        var name = (input.Name ?? string.Empty).Trim();
        var rootPath = (input.RootPath ?? string.Empty).Trim();
        var runsPath = (input.RunsPath ?? string.Empty).Trim();
        var repos = (input.Repos ?? [])
            .Select(repo => (repo ?? string.Empty).Trim())
            .Where(repo => repo.Length > 0)
            .ToList();

        var invalid = !IdPattern().IsMatch(id)
            || name.Length is 0 or > NameMaxLength
            || !LocalPaths.LooksAbsolute(rootPath)
            || !LocalPaths.LooksAbsolute(runsPath)
            || repos.Count > ReposMax
            || repos.Any(repo => !RepoPattern().IsMatch(repo));

        if (invalid)
        {
            return (null, Error.ProjectInvalid);
        }

        return (new ValidProject(id, name, rootPath, runsPath, repos), null);
    }

    [GeneratedRegex("^[a-z0-9-]{1,40}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^[A-Za-z0-9._-]{1,60}$")]
    private static partial Regex RepoPattern();
}
