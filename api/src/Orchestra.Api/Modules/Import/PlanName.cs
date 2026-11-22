using System.Text.RegularExpressions;

namespace Orchestra.Api.Modules.Import;

public static partial class PlanName
{
    /// <summary>From the task text, else from the branch, else nothing.</summary>
    public static (string? Number, string? Title) From(string? task, string? branch)
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

        return (null, null);
    }

    [GeneratedRegex(@"^Plan (\d{4}):\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanInTask();

    [GeneratedRegex(@"^plan-(\d{4})-", RegexOptions.CultureInvariant)]
    private static partial Regex PlanInBranch();
}
