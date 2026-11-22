using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Features.GetRun;

namespace Orchestra.Api.Tests.Library;

public sealed class RunDetailPlanTests
{
    [Fact]
    public async Task A_run_whose_plan_is_indexed_carries_that_plan()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddRun("T-1.1", branch: "plan-0001-alpha", planNumber: "0001");

        var (run, error) = await new GetRunHandler(fixture.Database, TimeProvider.System)
            .HandleAsync(LibraryApiFixture.Demo, "T-1.1", CancellationToken.None);

        error.Should().BeNull();
        run!.Plan.Should().NotBeNull();
        run.Plan!.RelPath.Should().Be(".orchestra/plans/0001-alpha.md");
        run.Plan.Number.Should().Be("0001");
        run.Plan.Status.Should().Be("Done");
    }

    [Fact]
    public async Task A_run_without_a_plan_has_none()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddRun("T-2.1", branch: "feature/unrelated");

        var (run, _) = await new GetRunHandler(fixture.Database, TimeProvider.System)
            .HandleAsync(LibraryApiFixture.Demo, "T-2.1", CancellationToken.None);

        run!.Plan.Should().BeNull();
    }

    [Fact]
    public async Task Two_matching_plans_answer_the_same_every_time()
    {
        using var fixture = new LibraryApiFixture();

        // The fixture plan has a number and no repo, so a second copy of it matches the same run.
        fixture.AddItem(".orchestra/plans/0001-copy.md", LibraryCategory.Plan, number: "0001", status: "Done");
        fixture.AddRun("T-3.1", branch: "plan-0001-alpha", planNumber: "0001");

        var handler = new GetRunHandler(fixture.Database, TimeProvider.System);

        var first = (await handler.HandleAsync(LibraryApiFixture.Demo, "T-3.1", CancellationToken.None)).Run!;
        var second = (await handler.HandleAsync(LibraryApiFixture.Demo, "T-3.1", CancellationToken.None)).Run!;

        first.Plan.Should().NotBeNull();
        first.Plan!.RelPath.Should().Be(second.Plan!.RelPath);
        first.Plan.RelPath.Should().Be(".orchestra/plans/0001-alpha.md");
    }
}
