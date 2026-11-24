using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Tests.Common;

public sealed class LocalPathsTests
{
    [Theory]
    [InlineData(@"F:\Projects\App", true)]
    [InlineData("F:/Projects/App", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData("relative/path", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void LooksAbsolute_accepts_what_the_owner_types(string path, bool expected)
    {
        LocalPaths.LooksAbsolute(path).Should().Be(expected);
    }

    [Theory]
    [InlineData(@"F:\Projects\App\", "F:/Projects/App")]
    [InlineData("F:/Projects/App///", "F:/Projects/App")]
    [InlineData("F:/", "F:/")]
    [InlineData("   ", "")]
    public void Normalize_keeps_a_drive_root_slash(string path, string expected)
    {
        LocalPaths.Normalize(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("C:/Users", "C:/", true)]
    [InlineData("C:/", "C:/", true)]
    [InlineData("F:/Projects/App", "F:/Projects", true)]
    [InlineData("F:/ProjectsX", "F:/Projects", false)]
    [InlineData("/host/app", "/host", true)]
    public void Covers_whole_segments_and_drive_roots(string value, string root, bool expected)
    {
        LocalPaths.Covers(value, root).Should().Be(expected);
    }
}
