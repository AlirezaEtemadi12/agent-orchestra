using System.Globalization;
using Orchestra.Api.Modules.Import.Ledger;

namespace Orchestra.Api.Tests.Import;

public sealed class LedgerNumbersTests
{
    [Theory]
    [InlineData("529.9k", 529_900)]
    [InlineData("2.2m", 2_200_000)]
    [InlineData("2.2M", 2_200_000)]
    [InlineData("512K", 512_000)]
    [InlineData("0", 0)]
    public void Tokens_are_read_in_both_case_and_suffix(string raw, long expected)
    {
        LedgerNumbers.Tokens(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("—")]
    [InlineData("-")]
    public void A_missing_token_column_stays_null(string raw)
    {
        LedgerNumbers.Tokens(raw).Should().BeNull();
    }

    [Fact]
    public void Durations_are_seconds()
    {
        LedgerNumbers.Duration("22:05").Should().Be(1_325);
        LedgerNumbers.Duration("2:38:01").Should().Be(9_481);
    }

    [Fact]
    public void Money_is_micro_dollars()
    {
        LedgerNumbers.Money("$0.170").Should().Be(170_000);
        LedgerNumbers.Money("$1.5").Should().Be(1_500_000);
    }

    [Fact]
    public void The_numbers_do_not_depend_on_the_current_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fa-IR");

        try
        {
            LedgerNumbers.Money("$0.058").Should().Be(58_000);
            LedgerNumbers.Tokens("88.1k").Should().Be(88_100);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
