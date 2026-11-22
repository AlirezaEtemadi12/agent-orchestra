using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Modules.Events.Features.ReceiveEvent;

/// <summary>Validates one envelope and stores it (plan 0004 §6.2). The answer is always 202 with no
/// body once the event is accepted <em>or</em> already stored: the engine never reads a reply (D4), and
/// a duplicate is a normal outcome of a resume (D11).</summary>
public sealed class ReceiveEventHandler(
    OrchestraDbContext database,
    ProjectRegistry registry,
    TimeProvider clock)
{
    public const int MaxBodyBytes = 64 * 1024;

    public async Task<Error?> HandleAsync(EventEnvelope? envelope, CancellationToken cancellationToken)
    {
        if (envelope is null
            || envelope.Schema != 1
            || envelope.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(envelope.TaskId))
        {
            return Error.EventBodyInvalid;
        }

        if (string.IsNullOrWhiteSpace(envelope.Project) || registry.Find(envelope.Project) is null)
        {
            return Error.EventProjectUnknown;
        }

        if (!EventKinds.IsKnown(envelope.Kind))
        {
            return Error.EventKindUnknown;
        }

        var exists = await database.RunEvents
            .AsNoTracking()
            .AnyAsync(stored => stored.Id == envelope.Id, cancellationToken);

        if (exists)
        {
            return null;
        }

        database.RunEvents.Add(new RunEventEntity
        {
            Id = envelope.Id,
            ProjectId = envelope.Project,
            TaskId = envelope.TaskId,
            Branch = envelope.Branch,
            At = envelope.At.UtcDateTime,
            Seq = envelope.Seq,
            Kind = envelope.Kind!,
            DataJson = DataOrEmpty(envelope),
            ReceivedAt = clock.GetUtcNow().UtcDateTime,
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request stored the same id between the check and the write: the event exists once,
            // which is what the engine asked for.
            database.ChangeTracker.Clear();
        }

        return null;
    }

    private static string DataOrEmpty(EventEnvelope envelope)
    {
        return envelope.Data.ValueKind is System.Text.Json.JsonValueKind.Undefined
            ? "{}"
            : envelope.Data.GetRawText();
    }
}
