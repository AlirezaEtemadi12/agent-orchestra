using System.Globalization;
using System.Text.RegularExpressions;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Library.Indexing;

/// <summary>D5: a run belongs to a plan of the same project — by number (and repo, when both name one),
/// else by the plan slug taken from the run's branch. The plan keeps the newest match; the detail lists
/// every match.</summary>
public static partial class PlanRunLinker
{
    public static void Link(IReadOnlyList<LibraryItemEntity> plans, IReadOnlyList<RunEntity> runs)
    {
        foreach (var plan in plans)
        {
            var matches = runs.Where(run => Matches(plan, run)).ToList();

            plan.RunKey = matches.Count == 0
                ? null
                : matches
                    .OrderByDescending(run => RunNumber(run.RunKey))
                    .ThenByDescending(run => run.UpdatedAt ?? run.CreatedAt)
                    .First()
                    .RunKey;
        }
    }

    /// <summary>Every run this plan owns, newest first — the detail page's list.</summary>
    public static IReadOnlyList<RunEntity> MatchesOf(LibraryItemEntity plan, IReadOnlyList<RunEntity> runs)
    {
        return runs
            .Where(run => Matches(plan, run))
            .OrderByDescending(run => RunNumber(run.RunKey))
            .ThenByDescending(run => run.UpdatedAt ?? run.CreatedAt)
            .ToList();
    }

    public static bool Matches(LibraryItemEntity plan, RunEntity run)
    {
        if (plan.Number is not null)
        {
            return string.Equals(run.PlanNumber, plan.Number, StringComparison.Ordinal)
                && RepoMatches(plan.Repo, run.Repo);
        }

        if (plan.Slug.Length == 0)
        {
            return false;
        }

        return string.Equals(SlugFromBranch(run.Branch), plan.Slug, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RepoMatches(string? planRepo, string? runRepo)
    {
        return planRepo is null || runRepo is null || string.Equals(planRepo, runRepo, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The branch's last segment, minus a leading <c>plan-</c> and then a leading
    /// <c>NNNN-</c>: <c>feature/plan-demo-slug</c> → <c>demo-slug</c>, <c>plan-0007-demo</c> → <c>demo</c>.</summary>
    public static string? SlugFromBranch(string? branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
        {
            return null;
        }

        var segments = branch.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        var name = segments[^1];

        if (name.StartsWith("plan-", StringComparison.OrdinalIgnoreCase))
        {
            name = name["plan-".Length..];
        }

        var numbered = NumberPrefix().Match(name);

        if (numbered.Success)
        {
            name = name[numbered.Length..];
        }

        return name.Length == 0 ? null : name;
    }

    /// <summary><c>RunKey</c> is <c>&lt;taskId&gt;.&lt;n&gt;</c>; <c>n</c> is compared as a number, so
    /// <c>.10</c> beats <c>.9</c>.</summary>
    public static int RunNumber(string runKey)
    {
        var dot = runKey.LastIndexOf('.');

        return dot < 0
            ? 0
            : int.TryParse(runKey[(dot + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0;
    }

    [GeneratedRegex(@"^\d{4}-", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPrefix();
}
