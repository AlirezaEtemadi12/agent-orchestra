using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Common.Hosting;
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
        page.Folders[0].Path.Should().Be("/projects");
        page.Folders[0].Name.Should().Be("projects");
    }

    [Fact]
    public void A_root_lists_folder_names_with_their_badges_and_no_files()
    {
        var (page, error) = Handler().Handle("/projects");

        error.Should().BeNull();
        page!.Path.Should().Be("/projects");
        page.Parent.Should().BeNull();
        page.Folders.Select(folder => folder.Name).Should().Equal("ready", "repo");

        var ready = page.Folders.Single(folder => folder.Name == "ready");
        ready.IsReady.Should().BeTrue();
        ready.IsRepo.Should().BeFalse();
        ready.Path.Should().Be("/projects/ready");

        page.Folders.Single(folder => folder.Name == "repo").IsRepo.Should().BeTrue();
    }

    [Fact]
    public void A_subfolder_lists_with_a_parent()
    {
        var (page, error) = Handler().Handle("/projects/ready");

        error.Should().BeNull();
        page!.Parent.Should().Be("/projects");
        page.Folders.Should().BeEmpty();
    }

    [Fact]
    public void Outside_the_roots_and_a_missing_folder_are_two_codes()
    {
        Handler().Handle("/elsewhere").Error!.Code.Should().Be(Error.FolderOutsideRoots.Code);
        Handler().Handle("/projects/nope").Error!.Code.Should().Be(Error.FolderMissing.Code);
    }

    [Fact]
    public void Dot_dot_cannot_leave_the_roots()
    {
        Handler().Handle("/projects/ready/../..").Error!.Code.Should().Be(Error.FolderOutsideRoots.Code);
    }

    [Fact]
    public void A_direct_install_walks_down_its_native_paths()
    {
        var options = Options.Create(new OrchestraOptions { BrowseRoots = [_root] });
        var hosting = new HostingInfo(options, new FakeReader(container: false), NullLogger<HostingInfo>.Instance);
        var handler = new FoldersHandler(hosting);

        var (page, error) = handler.Handle(_root);

        error.Should().BeNull();
        page!.Folders.Select(folder => folder.Name).Should().Equal("ready", "repo");

        var (child, childError) = handler.Handle(page.Folders[0].Path);

        childError.Should().BeNull();
        child!.Parent.Should().NotBeNull();

        handler.Handle(Path.Combine(_root, "..")).Error!.Code.Should().Be(Error.FolderOutsideRoots.Code);
    }

    [Theory]
    [InlineData("C:/Users", "C:/", true)]
    [InlineData("C:/", "C:/", true)]
    [InlineData("F:/Projects/App", "F:/Projects", true)]
    [InlineData("F:/ProjectsX", "F:/Projects", false)]
    [InlineData("/host/app", "/host", true)]
    public void Covers_whole_segments_and_drive_roots(string value, string root, bool expected)
    {
        PathMapper.Covers(value, root).Should().Be(expected);
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
        var options = Options.Create(new OrchestraOptions
        {
            PathMap = [new PathMapEntry { Host = "/projects", Server = _root }],
        });

        var hosting = new HostingInfo(options, new FakeReader(container: true), NullLogger<HostingInfo>.Instance);

        return new FoldersHandler(hosting);
    }

    private sealed class FakeReader(bool container) : IHostingReader
    {
        public bool RunningInContainer { get; } = container;
    }
}
