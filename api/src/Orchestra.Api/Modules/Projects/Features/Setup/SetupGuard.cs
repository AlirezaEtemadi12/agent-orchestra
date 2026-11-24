using System.Globalization;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Why a root was refused, so the message can be translated (plan 0012 §6.6).</summary>
public enum SetupRefusal
{
    NotFolder,
    DriveRoot,
    SystemFolder,
    OwnFolder,
}

public static class SetupRefusalText
{
    public static string For(SetupRefusal refusal, IReadOnlyList<CultureInfo> cultures)
    {
        var persian = cultures.Count > 0
            && string.Equals(cultures[0].TwoLetterISOLanguageName, "fa", StringComparison.Ordinal);

        return (refusal, persian) switch
        {
            (SetupRefusal.NotFolder, true) => "پوشه نیست",
            (SetupRefusal.DriveRoot, true) => "ریشه‌ی درایو است",
            (SetupRefusal.SystemFolder, true) => "پوشه‌ی سیستمی است",
            (SetupRefusal.OwnFolder, true) => "پوشه‌ی خود Orchestra است",
            (SetupRefusal.NotFolder, false) => "not a folder",
            (SetupRefusal.DriveRoot, false) => "a drive root",
            (SetupRefusal.SystemFolder, false) => "a system folder",
            _ => "Orchestra's own folder",
        };
    }
}

/// <summary>The resolved root, or the one reason it was refused.</summary>
public sealed record RootCheck(string? Root, SetupRefusal? Refusal);

/// <summary>Plan 0012 D13: who may call setup, and which folders it may write into. Loopback only —
/// setup assumes the dashboard and the engine share a machine (ADR 0009).</summary>
public sealed class SetupGuard(
    IOptions<OrchestraOptions> options,
    IWebHostEnvironment environment,
    EngineLocator engine)
{
    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "[::1]", "::1"];

    /// <summary>Null when the request may set up; the 403 error otherwise. A missing Origin counts as a
    /// refusal: a page that is not Orchestra's own sends none.</summary>
    public Error? CheckOrigin(HttpRequest request)
    {
        var header = request.Headers.Origin.ToString();

        if (string.IsNullOrWhiteSpace(header) || !Uri.TryCreate(header, UriKind.Absolute, out var origin))
        {
            return Error.SetupOrigin;
        }

        if (!LoopbackHosts.Contains(origin.Host.ToLowerInvariant(), StringComparer.Ordinal))
        {
            return Error.SetupOrigin;
        }

        var expected = request.Host.Port ?? (request.IsHttps ? 443 : 80);
        var port = origin.IsDefaultPort ? (origin.Scheme == "https" ? 443 : 80) : origin.Port;

        return port == expected ? null : Error.SetupOrigin;
    }

    /// <summary>Not rooted, missing, a drive root, a system folder, Orchestra's own folder or its data
    /// folder: refused before anything is planned.</summary>
    public RootCheck CheckRoot(string? rootPath)
    {
        var typed = (rootPath ?? string.Empty).Trim();

        if (typed.Length == 0 || !LocalPaths.LooksAbsolute(typed))
        {
            return new RootCheck(null, SetupRefusal.NotFolder);
        }

        string full;

        try
        {
            full = Path.GetFullPath(typed);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new RootCheck(null, SetupRefusal.NotFolder);
        }

        if (!Directory.Exists(full))
        {
            return new RootCheck(null, SetupRefusal.NotFolder);
        }

        var normalized = LocalPaths.Normalize(full);
        var pathRoot = Path.GetPathRoot(full);

        if (!string.IsNullOrEmpty(pathRoot)
            && string.Equals(LocalPaths.Normalize(pathRoot), normalized, StringComparison.OrdinalIgnoreCase))
        {
            return new RootCheck(null, SetupRefusal.DriveRoot);
        }

        foreach (var folder in SystemFolders())
        {
            if (LocalPaths.Covers(normalized, LocalPaths.Normalize(folder)))
            {
                return new RootCheck(null, SetupRefusal.SystemFolder);
            }
        }

        if (engine.Find() is { } engineRoot
            && LocalPaths.Covers(normalized, LocalPaths.Normalize(engineRoot)))
        {
            return new RootCheck(null, SetupRefusal.OwnFolder);
        }

        var database = options.Value.ResolveDatabasePath(environment.ContentRootPath);
        var dataFolder = Path.GetDirectoryName(database);

        if (!string.IsNullOrEmpty(dataFolder)
            && LocalPaths.Covers(normalized, LocalPaths.Normalize(dataFolder)))
        {
            return new RootCheck(null, SetupRefusal.OwnFolder);
        }

        return new RootCheck(full, null);
    }

    private static IEnumerable<string> SystemFolders()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
