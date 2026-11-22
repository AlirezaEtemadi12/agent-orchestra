using Orchestra.Api.Modules.Import.Ledger;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class LedgerParserTests
{
    private static readonly LedgerParseResult Parsed = LedgerParser.Parse(FixtureFiles.LedgerShapes, "ledger.shapes.md");

    [Fact]
    public void Three_blocks_are_read_and_nothing_is_dropped()
    {
        Parsed.Errors.Should().BeEmpty();
        Parsed.Blocks.Select(block => block.Kind).Should().Equal("close", "verify", "close");
        Parsed.Blocks.Should().OnlyContain(block => block.TaskId == "T-DEMO-101");
    }

    [Fact]
    public void The_repo_line_gives_status_plan_and_branch()
    {
        var block = Parsed.Blocks[2];

        block.Repo.Should().Be("Agent");
        block.Status.Should().Be("Verified");
        block.PlanVersion.Should().Be(3);
        block.Branch.Should().Be("plan-0101-shapes");
        block.LocalTime.Should().Be(new DateTime(2026, 11, 18, 11, 30, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public void Review_read_back_phase_and_verify_lines_are_kept()
    {
        var block = Parsed.Blocks[0];

        block.Stages.Should().HaveCount(4);

        var review = block.Stages[0];
        review.Stage.Should().Be(StageKind.Review);
        review.Verdict.Should().Be("FINDINGS");
        review.Blockers.Should().Be(1);
        review.Should.Should().Be(2);
        review.Nits.Should().Be(3);
        review.Round.Should().Be(1);
        review.Adapter.Should().Be("opencode");
        review.Model.Should().Be("opencode-go/glm-5.3-flash");

        var readBack = block.Stages[1];
        readBack.Stage.Should().Be(StageKind.ReadBack);
        readBack.Adapter.Should().BeNull();
        readBack.Model.Should().Be("opencode-go/deepseek-v4.1-flash");

        var implement = block.Stages[2];
        implement.Stage.Should().Be(StageKind.Implement);
        implement.Phase.Should().Be(1);
        implement.Verdict.Should().Be("NEED_INPUT");
        implement.OutsidePlan.Should().Be(1);
    }

    [Fact]
    public void A_red_gate_keeps_its_note_and_the_model_verdict()
    {
        var verify = Parsed.Blocks[0].Stages[3];

        verify.Stage.Should().Be(StageKind.Verify);
        verify.Verdict.Should().Be("FAIL");
        verify.GatesGreen.Should().BeFalse();
        verify.GatesNote.Should().Be("dotnet test");
        verify.ModelVerdict.Should().Be("PASS");
    }

    [Fact]
    public void The_oldest_session_table_has_no_requests_and_no_cost()
    {
        var session = Parsed.Blocks[0].Sessions[0];

        session.SessionId.Should().Be("s-1");
        session.FirstRole.Should().Be("implementer");
        session.Calls.Should().Be(12);
        session.Requests.Should().BeNull();
        session.Input.Should().Be(529_900);
        session.Output.Should().Be(2_200_000);
        session.Reasoning.Should().Be(15_400);
        session.CacheRead.Should().Be(100_200);
        session.CostMicros.Should().BeNull();
    }

    [Fact]
    public void The_requests_only_shape_reads_requests()
    {
        Parsed.Blocks[1].Sessions.Should().ContainSingle();
        Parsed.Blocks[1].Sessions[0].Requests.Should().Be(9);
    }

    [Fact]
    public void The_newest_shape_reads_the_stage_table_and_the_total()
    {
        var block = Parsed.Blocks[2];

        block.Sessions.Should().HaveCount(2);
        block.Sessions[1].CostMicros.Should().Be(58_000);
        block.TotalCostMicros.Should().Be(170_000);

        block.StageUsage.Should().HaveCount(2);
        block.StageUsage[0].Stage.Should().Be("review");
        block.StageUsage[0].Seconds.Should().Be(1_325);
        block.StageUsage[1].Seconds.Should().Be(9_481);
        block.StageUsage[1].CostMicros.Should().Be(112_000);
    }

    [Fact]
    public void The_code_fence_is_ignored_and_footnotes_are_ignored()
    {
        Parsed.Blocks[2].Stages.Should().HaveCount(5);
        Parsed.Blocks[2].Sessions.Should().HaveCount(2);
    }

    [Fact]
    public void The_bom_and_the_crlf_line_ends_read_the_same_as_lf()
    {
        var bytes = File.ReadAllBytes(Path.Combine(FixtureFiles.Root, "ledger.shapes.md"));
        var text = FixtureFiles.LedgerShapes;

        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });
        text.Should().Contain("\r\n");

        var plain = text.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal);
        var fromLf = LedgerParser.Parse(plain, "ledger.shapes.md");

        fromLf.Blocks.Should().HaveCount(Parsed.Blocks.Count);
        fromLf.Blocks[2].TotalCostMicros.Should().Be(170_000);
        fromLf.Errors.Should().BeEmpty();
    }
}
