using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Tests.Library;

public sealed class IdeaHeaderParserTests
{
    [Fact]
    public void The_one_line_header_is_read_field_by_field()
    {
        const string text = """
            # Dark mode everywhere

            **Status:** raw · **Added:** 2026-11-19 · **Plan:** —

            The body.
            """;

        var header = IdeaHeaderParser.Parse(text, ".orchestra/ideas/dark-mode.md");

        header.Title.Should().Be("Dark mode everywhere");
        header.Status.Should().Be("raw");
        header.CreatedOn.Should().Be(new DateTime(2026, 11, 19));
        header.LinkedPlanNumber.Should().BeNull();
    }

    [Fact]
    public void A_linked_plan_is_the_four_digit_number()
    {
        const string text = "# Ideas\n\n**Status:** planned · **Added:** 2026-11-01 · **Plan:** [0002](../plans/0002-x.md)\n";

        IdeaHeaderParser.Parse(text, "ideas.md").LinkedPlanNumber.Should().Be("0002");
    }

    [Fact]
    public void An_unknown_status_word_is_no_status()
    {
        const string text = "# Idea\n\n**Status:** maybe · **Added:** 2026-11-01\n";

        IdeaHeaderParser.Parse(text, "idea.md").Status.Should().BeNull();
    }

    [Fact]
    public void Created_is_used_when_added_is_absent()
    {
        const string text = "# Idea\n\n**Status:** ready · **Created:** 2026-11-02\n";

        IdeaHeaderParser.Parse(text, "idea.md").CreatedOn.Should().Be(new DateTime(2026, 11, 2));
    }

    [Fact]
    public void A_file_with_no_heading_takes_its_name()
    {
        IdeaHeaderParser.Parse("**Status:** dropped\n", ".orchestra/ideas/plain-note.md")
            .Title.Should().Be("plain-note.md");
    }
}
