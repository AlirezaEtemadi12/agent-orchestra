namespace Orchestra.Api.Modules.Projects;

/// <summary>Where a project comes from: the configuration file (read-only in the UI) or the
/// dashboard's own <c>projects.json</c> (plan 0003 D1, D8).</summary>
public enum ProjectOrigin
{
    Config,
    Dashboard,
}
