using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Tests.Import;

public sealed class PlanNameTests
{
    [Fact]
    public void The_task_text_wins()
    {
        PlanName.From("Plan 0155: Android icon and splash", "plan-0154-other")
            .Should().Be(("0155", "Android icon and splash"));
    }

    [Fact]
    public void The_branch_gives_the_number_when_the_task_does_not()
    {
        PlanName.From("Fix the header", "plan-0155-android-icon").Should().Be(("0155", null));
    }

    [Fact]
    public void Nothing_matches_nothing()
    {
        PlanName.From("Fix the header", "main").Should().Be((null, null));
        PlanName.From(null, null).Should().Be((null, null));
    }
}
