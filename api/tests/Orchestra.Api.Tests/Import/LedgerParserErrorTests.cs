using Orchestra.Api.Modules.Import.Ledger;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class LedgerParserErrorTests
{
    [Fact]
    public void A_row_with_a_missing_cell_becomes_one_error_and_the_other_blocks_parse()
    {
        var parsed = LedgerParser.Parse(FixtureFiles.LedgerCorrupt, "ledger.corrupt.md");

        parsed.Errors.Should().ContainSingle();
        parsed.Errors[0].Heading.Should().Contain("T-DEMO-021");
        parsed.Errors[0].Message.Should().Contain("cells");

        parsed.Blocks.Select(block => block.TaskId).Should().Equal("T-DEMO-020", "T-DEMO-022");
    }

    [Fact]
    public void A_block_without_a_repo_line_is_dropped_with_an_error()
    {
        var text = "## 2026-11-17 11:00 - T-DEMO-030 - close\n\n- verify: PASS (gates green, model said PASS) by opencode-go/glm-5.3-flash\n";

        var parsed = LedgerParser.Parse(text, "inline.md");

        parsed.Blocks.Should().BeEmpty();
        parsed.Errors.Should().ContainSingle().Which.Message.Should().Contain("repo");
    }
}
