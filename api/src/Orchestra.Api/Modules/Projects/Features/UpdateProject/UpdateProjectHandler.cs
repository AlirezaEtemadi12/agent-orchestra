using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Modules.Projects.Features.UpdateProject;

public sealed class UpdateProjectHandler(ProjectRegistry registry, ImportService imports, PathMapper mapper)
{
    public async Task<(ProjectSavedDto? Saved, Error? Error)> HandleAsync(
        string id,
        ProjectInput input,
        CancellationToken cancellationToken)
    {
        var origin = registry.OriginOf(id);

        if (origin is null)
        {
            return (null, Error.ProjectNotFound);
        }

        if (origin == ProjectOrigin.Config)
        {
            return (null, Error.ProjectReadOnly);
        }

        var (valid, error) = ProjectValidation.Validate(input with { Id = id }, mapper);

        if (valid is null)
        {
            return (null, error);
        }

        StoredProject? saved = null;

        try
        {
            var updated = registry.Update(id, entry =>
            {
                saved = entry with
                {
                    Name = valid.Name,
                    RootPath = valid.RootPath,
                    RunsPath = valid.RunsPath,
                    Repos = [.. valid.Repos],
                    Profile = "default",
                };

                return saved;
            });

            if (!updated || saved is null)
            {
                return (null, Error.ProjectNotFound);
            }
        }
        catch (ProjectStoreUnreadableException)
        {
            return (null, Error.ProjectStoreUnreadable);
        }

        await imports.ImportOneAsync(saved.ToOptions(mapper), cancellationToken);

        return (new ProjectSavedDto(saved.Id, saved.Name, saved.RootPath, saved.RunsPath, saved.Repos, saved.Profile), null);
    }
}
