using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Import.Archive;
using Orchestra.Api.Modules.Import.Ledger;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class RunAssemblerTests
{
    private static readonly SourceClock Clock = SourceClock.From("Asia/Tehran");

    private static IReadOnlyList<RunEntity> Build()
    {
        var blocks = LedgerParser.Parse(FixtureFiles.LedgerLifecycle, "_ledger.md").Blocks;

        var tasks = new[] { "T-DEMO-002/task.json", "T-DEMO-003/task.json" }
            .Select(relative => TaskJsonReader.Read(
                File.ReadAllText(Path.Combine(FixtureFiles.RunsRoot, relative)),
                relative,
                Clock).Snapshot!)
            .ToList();

        var archives = new[] { "T-DEMO-001.json", "T-DEMO-009.json" }
            .Select(name => ArchiveReader.Read(
                File.ReadAllText(Path.Combine(FixtureFiles.RunsRoot, "_archive", name)),
                $"_archive/{name}",
                Clock).Snapshot!)
            .ToList();

        return RunAssembler.Build("demo", blocks, tasks, archives, [], Clock);
    }

    [Fact]
    public void A_reused_task_id_on_a_new_branch_is_a_new_run()
    {
        var runs = Build();

        runs.Select(run => run.RunKey).Should().Contain(["T-DEMO-001.1", "T-DEMO-001.2"]);
        runs.Single(run => run.RunKey == "T-DEMO-001.1").Branch.Should().Be("plan-0100-lifecycle");
        runs.Single(run => run.RunKey == "T-DEMO-001.2").Branch.Should().Be("plan-0101-reused");
    }

    [Fact]
    public void A_later_block_replaces_the_usage_instead_of_adding_to_it()
    {
        var run = Build().Single(item => item.RunKey == "T-DEMO-001.1");

        run.Sessions.Should().ContainSingle();
        run.CostMicros.Should().Be(30_000);
    }

    [Fact]
    public void A_second_close_on_the_same_branch_joins_the_run()
    {
        var runs = Build();
        var run = runs.Single(item => item.RunKey == "T-DEMO-001.2");

        runs.Count(item => item.TaskId == "T-DEMO-001").Should().Be(2);
        run.CostMicros.Should().Be(16_000);
        run.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void An_open_task_merges_with_its_ledger_run_and_the_live_status_wins()
    {
        var run = Build().Single(item => item.RunKey == "T-DEMO-002.1");

        run.Source.Should().Be(RunSource.Both);
        run.Status.Should().Be("Implementing");
        run.Sessions.Should().HaveCount(1);
        run.Stages.Should().Contain(stage => stage.Stage == StageKind.Verify);
    }

    [Fact]
    public void A_task_without_a_ledger_run_is_a_live_run()
    {
        var run = Build().Single(item => item.RunKey == "T-DEMO-003.1");

        run.Source.Should().Be(RunSource.Live);
        run.Status.Should().Be("Planning");
    }

    [Fact]
    public void History_rows_are_not_final_except_the_last_of_each_stage()
    {
        var run = Build().Single(item => item.RunKey == "T-DEMO-003.1");

        run.Stages.Should().BeEmpty();
    }

    [Fact]
    public void The_archive_joins_the_run_with_its_branch_not_the_first_one()
    {
        var runs = Build();
        var second = runs.Single(item => item.RunKey == "T-DEMO-001.2");
        var first = runs.Single(item => item.RunKey == "T-DEMO-001.1");

        second.Texts.Should().HaveCount(4);
        first.Texts.Should().BeEmpty();
        second.Source.Should().Be(RunSource.Both);
    }

    [Fact]
    public void An_archive_without_a_ledger_block_is_a_run_of_its_own()
    {
        var run = Build().Single(item => item.RunKey == "T-DEMO-009.1");

        run.Source.Should().Be(RunSource.Archive);
        run.Texts.Should().HaveCount(2);
        run.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Run_keys_are_numbered_per_task_id_in_order()
    {
        var runs = Build();

        runs.Select(run => run.RunKey).Should().Equal(
            "T-DEMO-001.1",
            "T-DEMO-001.2",
            "T-DEMO-002.1",
            "T-DEMO-003.1",
            "T-DEMO-009.1");
    }
}
