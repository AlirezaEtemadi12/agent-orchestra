using Orchestra.Api.Modules.Library.Indexing;

namespace Orchestra.Api.Tests.Library;

public sealed class SafePathTests : IDisposable
{
    private readonly string _root;

    public SafePathTests()
    {
        _root = LibraryFixture.TempRoot();
    }

    public void Dispose()
    {
        LibraryFixture.DeleteTree(_root);
    }

    [Fact]
    public void A_file_under_the_root_is_inside()
    {
        var file = Path.Combine(_root, "a", "b.md");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "x");

        SafePath.IsInside(_root, file).Should().BeTrue();
    }

    [Fact]
    public void A_sibling_sharing_the_name_prefix_is_outside()
    {
        var sibling = _root + "-sibling";
        Directory.CreateDirectory(sibling);
        var file = Path.Combine(sibling, "b.md");
        File.WriteAllText(file, "x");

        try
        {
            SafePath.IsInside(_root, file).Should().BeFalse();
        }
        finally
        {
            LibraryFixture.DeleteTree(sibling);
        }
    }

    [Fact]
    public void Dot_dot_out_of_the_root_is_outside()
    {
        var file = Path.Combine(_root, "..", "outside.md");

        SafePath.IsInside(_root, file).Should().BeFalse();
    }

    [Fact]
    public void Case_differences_follow_the_platform()
    {
        var file = Path.Combine(_root, "Case", "b.md");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "x");

        var differentCase = Path.Combine(_root, "case", "b.md");

        SafePath.IsInside(_root, differentCase).Should().Be(OperatingSystem.IsWindows());
    }

    [Fact]
    public void A_folder_link_below_the_root_is_outside()
    {
        var outside = LibraryFixture.TempRoot();
        var target = Path.Combine(outside, "hidden");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "b.md"), "x");

        var link = Path.Combine(_root, "link");

        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Windows without the privilege cannot create a link; the other checks still cover the rule.
            LibraryFixture.DeleteTree(outside);

            return;
        }

        try
        {
            SafePath.IsInside(_root, Path.Combine(link, "b.md")).Should().BeFalse();
        }
        finally
        {
            LibraryFixture.DeleteTree(outside);
        }
    }
}
