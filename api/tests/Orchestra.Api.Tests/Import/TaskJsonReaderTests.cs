using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Import.Tasks;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class TaskJsonReaderTests
{
    private static readonly SourceClock Clock = SourceClock.From("Asia/Tehran");

    private static TaskSnapshot Read(string relative)
    {
        var path = Path.Combine(FixtureFiles.RunsRoot, relative);

        return TaskJsonReader.Read(File.ReadAllText(path), relative, Clock).Snapshot!;
    }

    [Fact]
    public void The_open_run_keeps_its_fields_and_plan()
    {
        var task = Read("T-DEMO-002/task.json");

        task.TaskId.Should().Be("T-DEMO-002");
        task.Status.Should().Be("Implementing");
        task.Branch.Should().Be("plan-0102-open");
        task.PlanNumber.Should().Be("0102");
        task.PlanTitle.Should().Be("The open run");
        task.Round.Should().Be(2);
        task.CreatedAt.Should().Be(new DateTime(2026, 11, 16, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void The_history_carries_both_rounds_of_each_stage()
    {
        var task = Read("T-DEMO-002/task.json");

        task.History.Should().HaveCount(4);
        task.History[0].Stage.Should().Be(StageKind.Review);
        task.History[0].Verdict.Should().Be("FINDINGS");
        task.History[1].Round.Should().Be(2);
        task.History[2].Stage.Should().Be(StageKind.ReadBack);
    }

    [Fact]
    public void Phases_and_verify_are_kept()
    {
        var task = Read("T-DEMO-002/task.json");

        task.Phases.Should().HaveCount(2);
        task.Phases[1].Phase.Should().Be(2);
        task.Phases[1].OutsidePlan.Should().Be(1);

        task.Verify.Should().NotBeNull();
        task.Verify!.Verdict.Should().Be("FAIL");
        task.Verify.GatesGreen.Should().BeTrue();
        task.Verify.ModelVerdict.Should().Be("FAIL");
    }

    [Fact]
    public void A_planning_run_reads_nulls_and_an_empty_branch()
    {
        var task = Read("T-DEMO-003/task.json");

        task.Status.Should().Be("Planning");
        task.Branch.Should().BeEmpty();
        task.Review.Should().BeNull();
        task.Implement.Should().BeNull();
        task.Verify.Should().BeNull();
        task.Phases.Should().BeEmpty();
        task.History.Should().BeEmpty();
        task.PlanTitle.Should().Be("Nothing yet");
    }

    [Fact]
    public void Bad_json_is_one_error_naming_the_file()
    {
        var result = TaskJsonReader.Read("{ not json", "T-DEMO-404/task.json", Clock);

        result.Snapshot.Should().BeNull();
        result.Error.Should().StartWith("T-DEMO-404/task.json");
    }
}
