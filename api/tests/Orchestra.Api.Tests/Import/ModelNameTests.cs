using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Tests.Import;

public sealed class ModelNameTests
{
    [Fact]
    public void The_adapter_is_the_part_before_the_first_colon()
    {
        var (adapter, model) = ModelName.Split("opencode:opencode-go/glm-5.3-flash");

        adapter.Should().Be("opencode");
        model.Should().Be("opencode-go/glm-5.3-flash");
    }

    [Fact]
    public void A_name_without_a_colon_has_no_adapter()
    {
        var (adapter, model) = ModelName.Split("opencode-go/deepseek-v4.1-flash");

        adapter.Should().BeNull();
        model.Should().Be("opencode-go/deepseek-v4.1-flash");
    }

    [Fact]
    public void An_empty_name_is_an_empty_model()
    {
        ModelName.Split(null).Should().Be((null, string.Empty));
        ModelName.Split("  ").Should().Be((null, string.Empty));
    }
}
