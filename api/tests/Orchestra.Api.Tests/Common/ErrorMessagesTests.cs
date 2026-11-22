using Orchestra.Api.Common.Errors;

namespace Orchestra.Api.Tests.Common;

public sealed class ErrorMessagesTests
{
    [Fact]
    public void Every_code_has_both_a_persian_and_an_english_text()
    {
        ErrorMessages.En.Keys.Should().BeEquivalentTo(ErrorMessages.Fa.Keys);
    }

    [Fact]
    public void No_text_is_empty()
    {
        ErrorMessages.En.Values.Should().NotContain(text => string.IsNullOrWhiteSpace(text));
        ErrorMessages.Fa.Values.Should().NotContain(text => string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void Persian_is_used_when_the_request_prefers_it_and_english_otherwise()
    {
        ErrorMessages
            .Get("Runs.Run.NotFound", ErrorMessages.CulturesFrom("fa-IR,fa;q=0.9"))
            .Should().Be("run پیدا نشد.");

        ErrorMessages
            .Get("Runs.Run.NotFound", ErrorMessages.CulturesFrom("de"))
            .Should().Be("Run not found.");
    }
}
