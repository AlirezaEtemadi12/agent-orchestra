using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Profiles;

/// <summary>D3/§6.1: after every save and at start, <c>&lt;ProfilesPath&gt;/&lt;id&gt;.json</c> is the current
/// version in the wrapper the engine reads. Written atomically: temp file, then replace.</summary>
public sealed class ProfileExporter(IOptions<OrchestraOptions> options, IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Folder => options.Value.ResolveProfilesPath(environment.ContentRootPath);

    public string PathFor(string id) => Path.Combine(Folder, $"{id}.json");

    public string Write(string id, int version, string configJson)
    {
        var payload = new ProfileExport(
            id,
            version,
            DateTime.UtcNow,
            JsonSerializer.Deserialize<JsonElement>(configJson.Length == 0 ? "{}" : configJson));

        Directory.CreateDirectory(Folder);

        var path = PathFor(id);
        var temporary = path + ".tmp";

        File.WriteAllText(temporary, JsonSerializer.Serialize(payload, Json), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);

        return path;
    }

    /// <summary>The wrapper the engine reads: profile, version, exportedAt, config.</summary>
    private sealed record ProfileExport(string Profile, int Version, DateTime ExportedAt, JsonElement Config);
}
