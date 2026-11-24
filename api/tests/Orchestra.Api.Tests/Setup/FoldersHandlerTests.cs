using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Setup.Features.Folders;

namespace Orchestra.Api.Tests.Setup;

public sealed class FoldersHandlerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-folders-{Guid.NewGuid():n}");

    public FoldersHandlerTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "ready", ".orchestra"));
        File.WriteAllText(Path.Combine(_root, "ready", ".orchestra", "orchestrator.json"), "{}");

        Directory.CreateDirectory(Path.Combine(_root, "repo", ".git"));
        Directory.CreateDirectory(Path.Combine(_root, ".hidden"));
        File.WriteAllText(Path.Combine(_root, "file.txt"), "x");
    }

    [Fact]
    public void An_empty_path_lists_the_roots()
    {
        var (page, error) = Handler().Handle(null);

        error.Should().BeNull();
        page!.Path.Should().BeNull();
        page.Parent.Should().BeNull();
        page.Folders.Should().ContainSingle();
        page.Folders[0].Path.Should().Be(_root);
        page.Folders[0].Name.Should().Be(Path.GetFileName(_root));
    }

    [Fact]
    public void A_root_lists_folder_names_with_their_badges_and_no_files()
    {
        var (page, error) = Handler().Handle(_root);

        error.Should().BeNull();
        page!.Path.Should().Be(_root);
        page.Parent.Should().BeNull();
        page.Folders.Select(folder => folder.Name).Should().Equal("ready", "repo");

        var ready = page.Folders.Single(folder => folder.Name == "ready");
        ready.IsReady.Should().BeTrue();
        ready.IsRepo.Should().BeFalse();
        ready.Path.Should().Be(Path.Combine(_root, "ready"));

        page.Folders.Single(folder => folder.Name == "repo").IsRepo.Should().BeTrue();
    }

    [Fact]
    public void A_subfolder_lists_with_a_parent()
    {
        var (page, error) = Handler().Handle(Path.Combine(_root, "ready"));

        error.Should().BeNull();
        page!.Parent.Should().Be(_root);
        page.Folders.Should().BeEmpty();
    }

    [Fact]
    public void Outside_the_roots_and_a_missing_folder_are_two_codes()
    {
        Handler().Handle(Path.Combine(Path.GetTempPath(), "elsewhere")).Error!.Code
            .Should().Be(Error.FolderOutsideRoots.Code);
        Handler().Handle(Path.Combine(_root, "nope")).Error!.Code.Should().Be(Error.FolderMissing.Code);
    }

    [Fact]
    public void Dot_dot_cannot_leave_the_roots()
    {
        Handler().Handle(Path.Combine(_root, "ready", "..", "..")).Error!.Code
            .Should().Be(Error.FolderOutsideRoots.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private FoldersHandler Handler()
    {
        return new FoldersHandler(Options.Create(new OrchestraOptions { BrowseRoots = [_root] }));
    }
}
