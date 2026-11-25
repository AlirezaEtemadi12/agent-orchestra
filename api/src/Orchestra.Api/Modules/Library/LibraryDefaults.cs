using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Library;

/// <summary>The default library globs — the <c>.orchestra/</c> layout. A project overrides any category
/// in its configuration. Plan 0007 changes these and nothing else.</summary>
public static class LibraryDefaults
{
    // Plan 0013 D13: a plan is a folder with a plan.md; the flat glob stays for plans 0001-0012. The old
    // `.orchestra/plans/*/*.md` glob is gone: it would list research.md and notes.md as plans.
    // The done/ globs keep the older per-repo layout's closed plans indexed (rule 0.3); they do not bring
    // back research.md or notes.md, which are the reason `plans/*/*.md` went away.
    public static IReadOnlyList<string> Plans { get; } =
        [".orchestra/plans/*.md", ".orchestra/plans/*/plan.md", ".orchestra/plans/done/*.md", ".orchestra/plans/*/done/*.md"];

    /// <summary>Plan 0007 D4b: a run's preview is written to `runs/_drafts` first and only moved to
    /// `previews/` when the plan is filed, so the in-flight one stays indexed.</summary>
    public static IReadOnlyList<string> Previews { get; } =
    [
        ".orchestra/plans/*/preview.html",
        ".orchestra/plans/*/preview/index.html",
        ".orchestra/previews/*/index.html",
        ".orchestra/previews/*.html",
        ".orchestra/runs/_drafts/*.html",
    ];

    public static IReadOnlyList<string> Ideas { get; } = [".orchestra/ideas/*.md"];

    public static IReadOnlyList<string> Research { get; } =
        [".orchestra/plans/*/research.md", ".orchestra/research/**/*.md"];

    public static IReadOnlyList<string> Evidence { get; } =
        [".orchestra/plans/*/evidence/**/*", ".orchestra/evidence/**/*"];

    public static IReadOnlyList<string> For(LibraryCategory category, LibraryOptions options)
    {
        var configured = category switch
        {
            LibraryCategory.Plan => options.Plans,
            LibraryCategory.Preview => options.Previews,
            LibraryCategory.Idea => options.Ideas,
            LibraryCategory.Research => options.Research,
            LibraryCategory.Evidence => options.Evidence,
            _ => null,
        };

        return configured is { Count: > 0 } ? configured : Default(category);
    }

    public static IReadOnlyList<string> Default(LibraryCategory category)
    {
        return category switch
        {
            LibraryCategory.Plan => Plans,
            LibraryCategory.Preview => Previews,
            LibraryCategory.Idea => Ideas,
            LibraryCategory.Research => Research,
            LibraryCategory.Evidence => Evidence,
            _ => [],
        };
    }
}
