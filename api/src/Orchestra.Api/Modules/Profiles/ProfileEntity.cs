namespace Orchestra.Api.Modules.Profiles;

/// <summary>A shared profile (plan 0006 D3): the dashboard's own data, like events. <see cref="Id"/> is a
/// slug; <see cref="CurrentVersion"/> is the version the exporter wrote last.</summary>
public sealed class ProfileEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CurrentVersion { get; set; }

    public List<ProfileVersionEntity> Versions { get; set; } = [];
}

/// <summary>One immutable version of a profile. <see cref="Json"/> is the profile's config object — the
/// D1 keys — never the export wrapper; the exporter adds profile, version and exportedAt.</summary>
public sealed class ProfileVersionEntity
{
    public Guid Id { get; set; }

    public string ProfileId { get; set; } = string.Empty;

    public int Version { get; set; }

    public string Json { get; set; } = "{}";

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ProfileEntity Profile { get; set; } = null!;
}
