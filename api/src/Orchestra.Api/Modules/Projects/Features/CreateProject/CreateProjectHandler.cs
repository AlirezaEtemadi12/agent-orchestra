using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Modules.Projects.Features.CreateProject;

public sealed class CreateProjectHandler(
    ProjectRegistry registry,
    ImportService imports,
    TimeProvider clock)
{
    public async Task<(ProjectSavedDto? Saved, Error? Error)> HandleAsync(
        ProjectInput input,
        CancellationToken cancellationToken)
    {
        var (valid, error) = ProjectValidation.Validate(input);

        if (valid is null)
        {
            return (null, error);
        }

        if (registry.Find(valid.Id) is not null)
        {
            return (null, Error.ProjectIdTaken);
        }

        var entry = new StoredProject
        {
            Id = valid.Id,
            Name = valid.Name,
            RootPath = valid.RootPath,
            RunsPath = valid.RunsPath,
            Repos = [.. valid.Repos],
            Profile = "default",
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };

        try
        {
            registry.Create(entry);
        }
        catch (ProjectStoreUnreadableException)
        {
            return (null, Error.ProjectStoreUnreadable);
        }

        // Saved first, imported second: the dashboard lists it even when its folder has nothing yet.
        await imports.ImportOneAsync(entry.ToOptions(), cancellationToken);

        return (new ProjectSavedDto(entry.Id, entry.Name, entry.RootPath, entry.RunsPath, entry.Repos, entry.Profile), null);
    }
}
