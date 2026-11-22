using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Features.PlanPrompt;

namespace Orchestra.Api.Tests.Library;

public sealed class PlanPromptTests
{
    [Fact]
    public async Task The_prompt_is_the_mock_template_filled_from_the_plan()
    {
        using var fixture = new LibraryApiFixture();

        var (text, error) = await new PlanPromptHandler(fixture.Database)
            .HandleAsync(LibraryApiFixture.Demo, ".orchestra/plans/0001-alpha.md", CancellationToken.None);

        error.Should().BeNull();
        text.Should().Be(
            "Run plan .orchestra/plans/0001-alpha.md\n"
            + "through the orchestrator (repo: Demo).\n"
            + "Status: Done. Preview: .orchestra/previews/0001-alpha/\n"
            + "Read the plan header first; decide nothing new.\n");
    }

    [Fact]
    public async Task A_missing_preview_is_none_and_a_repo_folder_is_used_as_it_is()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddItem(".orchestra/plans/0009-repo.md", LibraryCategory.Plan, number: "0009", status: "Draft", repo: "Web");

        var (text, _) = await new PlanPromptHandler(fixture.Database)
            .HandleAsync(LibraryApiFixture.Demo, ".orchestra/plans/0009-repo.md", CancellationToken.None);

        text.Should().Contain("through the orchestrator (repo: Web).");
        text.Should().Contain("Status: Draft. Preview: none");
    }

    [Fact]
    public async Task An_unknown_path_is_a_404()
    {
        using var fixture = new LibraryApiFixture();

        var (text, error) = await new PlanPromptHandler(fixture.Database)
            .HandleAsync(LibraryApiFixture.Demo, ".orchestra/plans/9999-nope.md", CancellationToken.None);

        text.Should().BeNull();
        error!.Code.Should().Be(Error.LibraryItemNotFound.Code);
    }

    [Theory]
    [InlineData(".orchestra/previews/0001-x/index.html", ".orchestra/previews/0001-x/")]
    [InlineData("preview.html", "preview.html")]
    [InlineData(null, "none")]
    public void A_preview_folder_keeps_its_trailing_slash(string? preview, string expected)
    {
        PlanPromptHandler.PreviewFolder(preview).Should().Be(expected);
    }
}
