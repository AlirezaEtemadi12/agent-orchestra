using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Projects;

public sealed class ProjectEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RunsPath { get; set; } = string.Empty;

    public List<RunEntity> Runs { get; set; } = [];
}
