using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Modules.Projects.Features.DeleteProject;

/// <summary>Plan 0003 D6, D11: the rows go (runs, library, its import logs) and then the entry; nothing on
/// disk outside the dashboard's own data is touched.</summary>
public sealed class DeleteProjectHandler(OrchestraDbContext database, ProjectRegistry registry, ImportService imports)
{
    public async Task<Error?> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var origin = registry.OriginOf(id);

        if (origin is null)
        {
            return Error.ProjectNotFound;
        }

        if (origin == ProjectOrigin.Config)
        {
            return Error.ProjectReadOnly;
        }

        await imports.EnterAsync(cancellationToken);

        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

            // The import log has no foreign key to Project, so it is deleted here; its errors go by
            // the existing cascade. The run and library rows go by the Project cascade.
            await database.ImportLogs
                .Where(log => log.ProjectId == id)
                .ExecuteDeleteAsync(cancellationToken);

            await database.Projects
                .Where(project => project.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Still inside the gate: a full import that starts after Leave must no longer see this
            // project, or it would rebuild the rows just deleted and leave them orphaned.
            registry.Remove(id);
        }
        catch (ProjectStoreUnreadableException)
        {
            // The rows are gone and the store still hides them; the next import rebuilds what it can.
            return Error.ProjectStoreUnreadable;
        }
        finally
        {
            imports.Leave();
        }

        return null;
    }
}
