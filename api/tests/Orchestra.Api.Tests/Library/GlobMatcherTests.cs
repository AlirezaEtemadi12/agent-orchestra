using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Tests.Library;

public sealed class GlobMatcherTests
{
    [Fact]
    public void A_single_star_matches_one_segment_only()
    {
        GlobMatcher.Matches("*/.ai/plans/*.md", "Web/.ai/plans/0001-x.md").Should().BeTrue();
        GlobMatcher.Matches("*/.ai/plans/*.md", "Web/.ai/plans/done/0001-x.md").Should().BeFalse();
    }

    [Fact]
    public void A_double_star_matches_any_depth()
    {
        GlobMatcher.Matches(".orchestra/research/**/*.md", ".orchestra/research/note.md").Should().BeTrue();
        GlobMatcher.Matches(".orchestra/research/**/*.md", ".orchestra/research/a/b/note.md").Should().BeTrue();
        GlobMatcher.Matches(".orchestra/research/**/*.md", ".orchestra/plans/note.md").Should().BeFalse();
    }

    [Fact]
    public void Readme_and_template_are_never_items()
    {
        GlobMatcher.Matches(".orchestra/plans/*.md", ".orchestra/plans/README.md").Should().BeFalse();
        GlobMatcher.Matches(".orchestra/plans/*.md", ".orchestra/plans/TEMPLATE.md").Should().BeFalse();
        GlobMatcher.Matches(".orchestra/plans/*.md", ".orchestra/plans/0001-x.md").Should().BeTrue();
    }

    [Fact]
    public void The_fixed_prefix_is_where_the_walk_starts()
    {
        // Plan 0013 D13: the plan-folder globs are under the same prefix as the flat ones, so the walk of
        // plans/ is still one walk.
        LibraryDefaults.Plans.Select(GlobMatcher.FixedPrefix).Distinct()
            .Should().Equal(".orchestra/plans", ".orchestra/plans/done");
        LibraryDefaults.Plans.Should().Contain(
            ".orchestra/plans/*/plan.md", "the plan is a folder with a plan.md");
        LibraryDefaults.Previews.Select(GlobMatcher.FixedPrefix).Distinct()
            .Should().Equal(".orchestra/plans", ".orchestra/previews", ".orchestra/runs/_drafts");
        LibraryDefaults.Ideas.Select(GlobMatcher.FixedPrefix).Should().Equal(".orchestra/ideas");
        LibraryDefaults.Research.Select(GlobMatcher.FixedPrefix).Distinct()
            .Should().Equal(".orchestra/plans", ".orchestra/research");
        LibraryDefaults.Evidence.Select(GlobMatcher.FixedPrefix).Distinct()
            .Should().Equal(".orchestra/plans", ".orchestra/evidence");

        GlobMatcher.FixedPrefix("*/.ai/plans/*.md").Should().BeEmpty();
        GlobMatcher.FixedPrefix("*.html").Should().BeEmpty();
    }

    [Fact]
    public void The_repo_is_the_first_whole_star_segment()
    {
        GlobMatcher.RepoOf("*/.ai/plans/*.md", "Web/.ai/plans/0001-x.md").Should().Be("Web");
        GlobMatcher.RepoOf(".orchestra/plans/*/*.md", ".orchestra/plans/web/0007-x.md").Should().Be("web");

        // The star is in the file name, not a folder: there is no repo.
        GlobMatcher.RepoOf(".orchestra/plans/*.md", ".orchestra/plans/0001-x.md").Should().BeNull();
    }

    [Fact]
    public void A_matched_done_folder_is_never_a_repo()
    {
        GlobMatcher.RepoOf(".orchestra/plans/*/*.md", ".orchestra/plans/done/0003-x.md").Should().BeNull();
        GlobMatcher.RepoOf(".orchestra/plans/*/*.md", ".orchestra/plans/DONE/0003-x.md").Should().BeNull();
    }

    [Fact]
    public void A_non_matching_path_has_no_repo()
    {
        GlobMatcher.RepoOf("*/.ai/plans/*.md", "Web/ideas.md").Should().BeNull();
    }
}
