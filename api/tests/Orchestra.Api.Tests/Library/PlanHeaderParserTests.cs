using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Tests.Library;

public sealed class PlanHeaderParserTests
{
    private const string OrchestraStyle = """
        # Plan 0001 — Skeleton, fa/en, and phase-0 import

        **Status:** Done — phases A–E implemented and reviewed (2026-11-19)
        **Created:** 2026-11-19 · **Updated:** 2026-11-20 · **Done:** 2026-11-21
        **Preview:** [`../previews/0001-skeleton-and-import/`](../previews/0001-skeleton-and-import/index.html) — approved
        **Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent.

        ## 1. Goal
        """;

    [Fact]
    public void The_orchestra_style_reads_every_field()
    {
        var header = PlanHeaderParser.Parse(OrchestraStyle, ".orchestra/plans/0001-skeleton-and-import.md");

        header.Number.Should().Be("0001");
        header.Title.Should().Be("Skeleton, fa/en, and phase-0 import");
        header.Status.Should().Be("Done");
        header.CreatedOn.Should().Be(new DateTime(2026, 11, 19));
        header.UpdatedOn.Should().Be(new DateTime(2026, 11, 20));
        header.DoneOn.Should().Be(new DateTime(2026, 11, 21));
        header.PlannedBy.Should().Be("Claude Opus 5.5");
        header.PreviewRelPath.Should().Be(".orchestra/previews/0001-skeleton-and-import/index.html");
    }

    [Fact]
    public void A_legacy_done_line_is_a_done_plan_with_its_date()
    {
        const string text = """
            # Plan 0032 — Group refactor

            **done** 2026-11-18 — merged
            **Author:** Claude Opus 5.6, 2026-11-18

            Body
            """;

        var header = PlanHeaderParser.Parse(text, "Web/.ai/plans/0032-group-refactor.md");

        header.Status.Should().Be("Done");
        header.DoneOn.Should().Be(new DateTime(2026, 11, 18));
        header.PlannedBy.Should().Be("Claude Opus 5.6");
    }

    [Fact]
    public void A_done_status_line_with_a_slug_file_name_has_no_number()
    {
        const string text = """
            # Plan — rename the customer record

            **Status:** Done — closed

            Body
            """;

        var header = PlanHeaderParser.Parse(text, "Web/.ai/plans/rename-customer-record.md");

        header.Number.Should().BeNull();
        header.Title.Should().Be("rename the customer record");
        header.Status.Should().Be("Done");
    }

    [Fact]
    public void The_number_comes_from_the_file_name_when_the_title_has_none()
    {
        const string text = """
            # Plan — no number in the title

            **Status:** Draft
            """;

        PlanHeaderParser.Parse(text, "Web/.ai/plans/0042-no-number.md").Number.Should().Be("0042");
    }

    [Fact]
    public void A_dash_date_is_null()
    {
        const string text = """
            # Plan 0005 — empty dates

            **Status:** Draft
            **Created:** 2026-11-01 · **Updated:** — · **Done:** —
            """;

        var header = PlanHeaderParser.Parse(text, ".orchestra/plans/0005-x.md");

        header.CreatedOn.Should().Be(new DateTime(2026, 11, 1));
        header.UpdatedOn.Should().BeNull();
        header.DoneOn.Should().BeNull();
    }

    [Fact]
    public void A_legacy_done_folder_is_a_done_plan()
    {
        const string text = "# Plan — closed in the old layout\n\nBody\n";

        PlanHeaderParser.Parse(text, "Web/.ai/plans/done/0003-legacy.md").Status.Should().Be("Done");
    }

    [Fact]
    public void No_status_line_is_unknown()
    {
        PlanHeaderParser.Parse("# Plan 0009 — no header\n\nBody\n", ".orchestra/plans/0009-x.md")
            .Status.Should().Be("Unknown");
    }

    [Fact]
    public void An_unknown_status_word_is_unknown()
    {
        PlanHeaderParser.Parse("# Plan 0009 — odd\n\n**Status:** Waiting\n", ".orchestra/plans/0009-x.md")
            .Status.Should().Be("Unknown");
    }

    [Theory]
    [InlineData("**Preview:** [x](../previews/0001-x/index.html)")]
    [InlineData("**Preview:** `../previews/0001-x/`")]
    [InlineData("**Preview:** ../previews/0001-x/")]
    public void A_preview_target_resolves_to_a_root_relative_file(string previewLine)
    {
        var text = $"# Plan 0001 — x\n\n**Status:** Draft\n{previewLine}\n";

        PlanHeaderParser.Parse(text, ".orchestra/plans/0001-x.md")
            .PreviewRelPath.Should().Be(".orchestra/previews/0001-x/index.html");
    }

    [Fact]
    public void A_preview_outside_the_root_is_not_a_preview()
    {
        const string text = "# Plan 0001 — x\n\n**Status:** Draft\n**Preview:** ../../../elsewhere/index.html\n";

        PlanHeaderParser.Parse(text, ".orchestra/plans/0001-x.md")
            .PreviewRelPath.Should().BeNull();
    }
}
