using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Runs.Features.Board;

namespace Orchestra.Api.Tests.Runs;

public sealed class BoardTests : IDisposable
{
    private readonly LiveFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task A_run_waiting_for_approval_is_in_needs_you()
    {
        _fixture.AddEvent(EventKinds.RunCreated, Data(("status", "Planning")), _fixture.Now.AddHours(-2));
        _fixture.AddEvent(EventKinds.Status, Data(("from", "Planning"), ("to", "AwaitingApproval")), _fixture.Now.AddMinutes(-30));

        var board = await BoardAsync();

        var card = board.Lanes.NeedsYou.Should().ContainSingle().Subject;
        card.WaitingFor.Should().Be("approval");
        card.Status.Should().Be("AwaitingApproval");
    }

    [Fact]
    public async Task A_failed_verify_is_its_own_lane()
    {
        _fixture.AddEvent(EventKinds.Status, Data(("to", "VerifyFailed")), _fixture.Now.AddMinutes(-10));

        var board = await BoardAsync();

        board.Lanes.Failed.Should().ContainSingle();
        board.Lanes.NeedsYou.Should().BeEmpty();
    }

    [Fact]
    public async Task A_verify_fail_followed_by_a_pass_leaves_needs_you()
    {
        _fixture.AddEvent(EventKinds.History, Data(("event", "verify"), ("verdict", "FAIL")), _fixture.Now.AddHours(-1));
        _fixture.AddEvent(EventKinds.History, Data(("event", "verify"), ("verdict", "PASS")), _fixture.Now.AddMinutes(-30), seq: 2);

        var board = await BoardAsync();

        board.Lanes.NeedsYou.Should().BeEmpty();
        board.Lanes.Running.Should().ContainSingle();
    }

    [Fact]
    public async Task A_run_without_an_event_for_a_day_is_in_no_lane()
    {
        _fixture.AddEvent(EventKinds.Status, Data(("to", "Implementing")), _fixture.Now.AddHours(-30));

        var board = await BoardAsync();

        board.Lanes.Running.Should().BeEmpty();
        board.Lanes.NeedsYou.Should().BeEmpty();
        board.Lanes.Done.Should().BeEmpty();
        board.Lanes.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task A_run_waiting_for_approval_longer_than_thirty_days_is_dropped()
    {
        _fixture.AddEvent(EventKinds.Status, Data(("to", "AwaitingApproval")), _fixture.Now.AddDays(-31));

        var board = await BoardAsync();

        board.Lanes.NeedsYou.Should().BeEmpty();
    }

    [Fact]
    public async Task An_unknown_status_word_runs_while_it_is_fresh_and_shows_as_written()
    {
        _fixture.AddEvent(EventKinds.Status, Data(("to", "SomethingNew")), _fixture.Now.AddMinutes(-5));

        var board = await BoardAsync();

        var card = board.Lanes.Running.Should().ContainSingle().Subject;
        card.Status.Should().Be("SomethingNew");
    }

    [Fact]
    public async Task A_closed_run_is_done()
    {
        _fixture.AddEvent(EventKinds.Status, Data(("to", "Verified")), _fixture.Now.AddHours(-3));
        _fixture.AddEvent(EventKinds.RunClosed, Data(("status", "Verified")), _fixture.Now.AddHours(-2), seq: 2);

        var board = await BoardAsync();

        board.Lanes.Done.Should().ContainSingle();
        board.Lanes.Running.Should().BeEmpty();
    }

    [Fact]
    public async Task The_card_takes_its_plan_and_key_from_the_imported_run()
    {
        _fixture.AddProject();
        _fixture.AddRun(
            "T-LIVE-001",
            "plan-0159-live",
            "Reviewing",
            "T-LIVE-001.1",
            _fixture.Now.AddHours(-2),
            planNumber: "0159",
            planTitle: "The live plan");

        _fixture.AddEvent(EventKinds.Progress, Data(("phase", "review"), ("model", "opencode:glm-5.3"), ("lastLine", "reading src")), _fixture.Now.AddMinutes(-1));

        var card = (await BoardAsync()).Lanes.Running.Should().ContainSingle().Subject;
        card.RunKey.Should().Be("T-LIVE-001.1");
        card.PlanNumber.Should().Be("0159");
        card.PlanTitle.Should().Be("The live plan");
        card.Stage.Should().Be("review");
        card.Model.Should().Be("opencode:glm-5.3");
        card.LastLine.Should().Be("reading src");
    }

    [Fact]
    public async Task A_waiting_pool_is_a_wait_not_a_question()
    {
        _fixture.AddEvent(
            EventKinds.PoolWaiting,
            Data(("pool", "agent"), ("slot", 1), ("heldBy", "T-OTHER")),
            _fixture.Now.AddMinutes(-2));

        var board = await BoardAsync();

        board.Lanes.NeedsYou.Should().BeEmpty();
        board.Lanes.Running.Should().ContainSingle().Which.WaitingFor.Should().Be("pool");
    }

    [Fact]
    public async Task The_reporter_says_whether_a_token_is_configured()
    {
        (await BoardAsync()).Reporter.Should().Be("off");

        using var configured = new LiveFixture(reportToken: "a-token");
        var (board, error) = await configured.Board.HandleAsync(null, CancellationToken.None);

        error.Should().BeNull();
        board!.Reporter.Should().Be("on");
    }

    private async Task<BoardDto> BoardAsync()
    {
        var (board, error) = await _fixture.Board.HandleAsync(null, CancellationToken.None);

        error.Should().BeNull();

        return board!;
    }

    private static Dictionary<string, object?> Data(params (string Name, object? Value)[] pairs)
    {
        return pairs.ToDictionary(pair => pair.Name, pair => pair.Value);
    }
}
