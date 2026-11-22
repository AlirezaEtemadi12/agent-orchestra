using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Tests.Import;

public sealed class SourceClockTests
{
    [Fact]
    public void Tehran_local_time_becomes_utc()
    {
        var clock = SourceClock.From("Asia/Tehran");

        var utc = clock.ToUtc(new DateTime(2026, 11, 19, 9, 37, 0, DateTimeKind.Unspecified));

        utc.Kind.Should().Be(DateTimeKind.Utc);
        utc.Should().Be(new DateTime(2026, 11, 19, 6, 7, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void An_already_utc_value_is_left_alone()
    {
        var clock = SourceClock.From("Asia/Tehran");
        var value = new DateTime(2026, 11, 19, 6, 7, 0, DateTimeKind.Utc);

        clock.ToUtc(value).Should().Be(value);
    }

    [Fact]
    public void An_unknown_zone_fails_loudly()
    {
        var act = () => SourceClock.From("Mars/Olympus");

        act.Should().Throw<TimeZoneNotFoundException>();
    }
}
