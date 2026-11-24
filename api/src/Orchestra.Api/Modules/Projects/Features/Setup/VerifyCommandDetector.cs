using System.Text.Json;

namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Plan 0012 D11: what the form is prefilled with. A solution means the .NET commands; a
/// package.json means one npm script per command it actually has; both means .NET first.</summary>
public static class VerifyCommandDetector
{
    private static readonly string[] NpmScripts = ["typecheck", "lint", "test", "build"];

    public static IReadOnlyList<string> Detect(string repoFolder)
    {
        var commands = new List<string>();

        if (HasSolution(repoFolder))
        {
            commands.Add("dotnet build");
            commands.Add("dotnet test");
        }

        commands.AddRange(NpmCommands(repoFolder));

        return commands;
    }

    private static bool HasSolution(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return false;
        }

        return Directory.EnumerateFiles(folder, "*.slnx", SearchOption.TopDirectoryOnly).Any()
            || Directory.EnumerateFiles(folder, "*.sln", SearchOption.TopDirectoryOnly).Any();
    }

    private static IEnumerable<string> NpmCommands(string folder)
    {
        var package = Path.Combine(folder, "package.json");

        if (!File.Exists(package))
        {
            return [];
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllText(package));
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("scripts", out var scripts)
                || scripts.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return
            [
                .. NpmScripts
                    .Where(name => scripts.TryGetProperty(name, out _))
                    .Select(name => $"npm run {name}"),
            ];
        }
    }
}
