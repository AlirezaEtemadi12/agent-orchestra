using System.Text.Json;
using Orchestra.Api.Modules.Runs.Entities;
using static Orchestra.Api.Modules.Import.Tasks.JsonFields;

namespace Orchestra.Api.Modules.Import.Tasks;

public static class TaskJsonReader
{
    public static TaskSnapshotResult Read(string json, string file, SourceClock clock)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException exception)
        {
            return new TaskSnapshotResult(null, $"{file}: {exception.Message}");
        }

        using (document)
        {
            return new TaskSnapshotResult(Map(document.RootElement, file, clock), null);
        }
    }

    internal static TaskSnapshot Map(JsonElement root, string file, SourceClock clock)
    {
        var snapshot = new TaskSnapshot
        {
            File = file,
            TaskId = Text(root, "id"),
            Repo = Text(root, "repo"),
            Task = Text(root, "task"),
            Status = Text(root, "status"),
            Branch = Text(root, "branch"),
            Round = Number(Property(root, "round")),
        };

        snapshot.CreatedAt = Convert(clock, Text(root, "createdAt"));
        snapshot.UpdatedAt = Convert(clock, Text(root, "updatedAt"));

        var plan = Property(root, "plan");

        if (plan is { ValueKind: JsonValueKind.String })
        {
            snapshot.PlanTitle = plan.Value.GetString();
        }
        else if (plan is { ValueKind: JsonValueKind.Object })
        {
            snapshot.PlanVersion = Number(Property(plan, "version"));
            snapshot.PlanNumber = Text(plan, "number");
            snapshot.PlanTitle = Text(plan, "title");
        }

        snapshot.Review = Stage(Property(root, "review"), StageKind.Review, null, clock);
        snapshot.ReadBack = Stage(Property(root, "readback") ?? Property(root, "readBack"), StageKind.ReadBack, null, clock);
        snapshot.Implement = Stage(Property(root, "implement"), StageKind.Implement, null, clock);
        snapshot.Verify = Stage(Property(root, "verify"), StageKind.Verify, null, clock);

        if (Property(root, "phases") is { ValueKind: JsonValueKind.Array } phases)
        {
            foreach (var phase in phases.EnumerateArray())
            {
                var stage = Stage(phase, StageKind.Implement, Number(Property(phase, "phase")), clock);

                if (stage is not null)
                {
                    snapshot.Phases.Add(stage);
                }
            }
        }

        if (Property(root, "history") is { ValueKind: JsonValueKind.Array } history)
        {
            foreach (var entry in history.EnumerateArray())
            {
                var name = Text(entry, "stage") ?? Text(entry, "type");
                var stage = Stage(entry, JsonFields.Stage(name), Number(Property(entry, "phase")), clock);

                if (stage is not null)
                {
                    snapshot.History.Add(stage);
                }
            }
        }

        return snapshot;
    }

    private static DateTime? Convert(SourceClock clock, string? text)
    {
        return Utc(text, clock);
    }

    private static StageSnapshot? Stage(JsonElement? element, StageKind kind, int? phase, SourceClock clock)
    {
        if (element is not { ValueKind: JsonValueKind.Object })
        {
            return null;
        }

        var (adapter, model) = ModelName.Split(Text(element, "model"));
        var at = Utc(Property(element, "at"), clock);

        return new StageSnapshot
        {
            Stage = kind,
            Round = Number(Property(element, "round")),
            Phase = phase,
            Adapter = adapter,
            Model = model,
            Verdict = Text(element, "verdict"),
            Blockers = Number(Property(element, "blockers") ?? Property(element, "blocker")),
            Should = Number(Property(element, "should")),
            Nits = Number(Property(element, "nits") ?? Property(element, "nit")),
            Changed = Number(Property(element, "changed")),
            OutsidePlan = Number(Property(element, "outsidePlan") ?? Property(element, "notInPlan")),
            Violations = Number(Property(element, "violations")),
            GatesGreen = Flag(Property(element, "gatesGreen")),
            GatesNote = Text(element, "gatesNote"),
            ModelVerdict = Text(element, "modelVerdict"),
            At = at,
        };
    }
}
