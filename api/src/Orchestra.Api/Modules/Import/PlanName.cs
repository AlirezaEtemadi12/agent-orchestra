using System.Text.RegularExpressions;

namespace Orchestra.Api.Modules.Import;

public static partial class PlanName
{
    /// <summary>From the task text, else from the branch, else from the task id (plan 0013 D4/D12:
    /// a run is named after its plan folder, `0013-tenant-filter`, and the slug is its fallback title).
    /// The task id is optional so every older call site keeps compiling.</summary>
    public static (string? Number, string? Title) From(string? task, string? branch, string? taskId = null)
    {
        if (task is not null)
        {
            var match = PlanInTask().Match(task);

            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value.Trim());
            }
        }

        if (branch is not null)
        {
            var match = PlanInBranch().Match(branch);

            if (match.Success)
            {
                return (match.Groups[1].Value, null);
            }
        }

        if (taskId is not null)
        {
            var match = PlanInId().Match(taskId);

            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }
        }

        return (null, null);
    }

    [GeneratedRegex(@"^Plan (\d{4}):\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanInTask();

    [GeneratedRegex(@"^plan-(\d{4})-", RegexOptions.CultureInvariant)]
    private static partial Regex PlanInBranch();

    [GeneratedRegex(@"^(\d{4})-(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanInId();
}
