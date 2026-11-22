using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Library.Features.GetLibraryItem;
using Orchestra.Api.Modules.Library.Features.ReadLibraryFile;

namespace Orchestra.Api.Tests.Library;

public sealed class ReadLibraryFileTests
{
    private const string Plan = ".orchestra/plans/0001-alpha.md";
    private const string Preview = ".orchestra/previews/0001-alpha/index.html";
    private const string Image = ".orchestra/evidence/0001-demo/phase-A/shot.png";
    private const string Report = ".orchestra/evidence/0001-demo/phase-A/report.md";

    [Fact]
    public async Task Only_an_indexed_item_of_that_category_is_served()
    {
        using var fixture = new LibraryApiFixture();

        (await Read(fixture, LibraryCategory.Plan, "not/indexed.md")).Error!.Code
            .Should().Be(Error.LibraryItemNotFound.Code);

        (await Read(fixture, LibraryCategory.Plan, "../today/ideas.md")).Error!.Code
            .Should().Be(Error.LibraryItemNotFound.Code);

        // The index.html is a preview, not a plan.
        (await Read(fixture, LibraryCategory.Plan, Preview)).Error!.Code
            .Should().Be(Error.LibraryItemNotFound.Code);
    }

    [Fact]
    public async Task A_path_indexed_under_two_categories_is_served_by_each()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddItem(Preview, LibraryCategory.Research);

        (await Read(fixture, LibraryCategory.Preview, Preview)).File.Should().NotBeNull();
        (await Read(fixture, LibraryCategory.Research, Preview)).File.Should().NotBeNull();
    }

    [Fact]
    public async Task Html_carries_the_exact_sandbox_policy_and_no_network()
    {
        using var fixture = new LibraryApiFixture();

        var file = (await Read(fixture, LibraryCategory.Preview, Preview)).File!;

        file.ContentType.Should().Be("text/html; charset=utf-8");
        file.ContentSecurityPolicy.Should().Be(
            "sandbox allow-scripts; default-src 'none'; "
            + "script-src 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://unpkg.com; "
            + "style-src 'unsafe-inline' https://fonts.googleapis.com; "
            + "font-src https://fonts.gstatic.com; "
            + "img-src data: blob:; media-src data: blob:; "
            + "connect-src 'none'; form-action 'none'; base-uri 'none'; frame-src 'none'");
        file.ContentSecurityPolicy.Should().Contain("connect-src 'none'");
        file.ContentDisposition.Should().BeNull();
    }

    [Fact]
    public async Task Markdown_and_images_are_served_with_no_network_and_other_types_as_attachments()
    {
        using var fixture = new LibraryApiFixture();

        var markdown = (await Read(fixture, LibraryCategory.Plan, Plan)).File!;
        markdown.ContentType.Should().Be("text/markdown; charset=utf-8");
        markdown.ContentSecurityPolicy.Should().Be(ReadLibraryFileHandler.NonePolicy);

        var image = (await Read(fixture, LibraryCategory.Evidence, Image)).File!;
        image.ContentType.Should().Be("image/png");
        image.ContentSecurityPolicy.Should().Be(ReadLibraryFileHandler.NonePolicy);

        var report = (await Read(fixture, LibraryCategory.Evidence, Report)).File!;
        report.ContentType.Should().Be("text/markdown; charset=utf-8");

        LibraryFixture.Write(fixture.Root, ".orchestra/research/notes/dump.txt", "text");
        fixture.AddItem(".orchestra/research/notes/dump.txt", LibraryCategory.Research);

        var other = (await Read(fixture, LibraryCategory.Research, ".orchestra/research/notes/dump.txt")).File!;
        other.ContentType.Should().Be("application/octet-stream");
        other.ContentDisposition.Should().Be("attachment");
    }

    [Fact]
    public async Task Unknown_categories_and_a_project_without_a_root_are_not_found()
    {
        using var fixture = new LibraryApiFixture();

        var handler = new ReadLibraryFileHandler(fixture.Database, fixture.Registry);

        (await handler.HandleAsync(LibraryApiFixture.Demo, "Notes", Plan, CancellationToken.None)).Error!.Code
            .Should().Be(Error.LibraryItemNotFound.Code);

        (await handler.HandleAsync("nope", "plan", Plan, CancellationToken.None)).Error!.Code
            .Should().Be(Error.LibraryItemNotFound.Code);
    }

    [Fact]
    public async Task An_oversized_file_is_a_413()
    {
        using var fixture = new LibraryApiFixture();

        LibraryFixture.Write(fixture.Root, ".orchestra/research/big.bin", "x");
        using (var stream = new FileStream(
            Path.Combine(fixture.Root, ".orchestra", "research", "big.bin"),
            FileMode.Open,
            FileAccess.Write))
        {
            stream.SetLength(ReadLibraryFileHandler.MaxBytes + 1);
        }

        fixture.AddItem(".orchestra/research/big.bin", LibraryCategory.Research);

        (await Read(fixture, LibraryCategory.Research, ".orchestra/research/big.bin")).Error!.Code
            .Should().Be(Error.LibraryFileTooLarge.Code);
    }

    [Fact]
    public async Task A_file_replaced_by_a_link_after_indexing_is_not_served()
    {
        using var fixture = new LibraryApiFixture();

        var outside = LibraryFixture.TempRoot();
        LibraryFixture.Write(outside, "goal.md", "not the note");

        var note = Path.Combine(fixture.Root, ".orchestra", "research", "notes", "one-note.md");

        try
        {
            File.Delete(note);
            File.CreateSymbolicLink(note, Path.Combine(outside, "goal.md"));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            // Creating a link needs a privilege this account may not have; there is nothing to test then.
            LibraryFixture.DeleteTree(outside);

            return;
        }

        try
        {
            (await Read(fixture, LibraryCategory.Research, ".orchestra/research/notes/one-note.md")).Error!.Code
                .Should().Be(Error.LibraryItemNotFound.Code);
        }
        finally
        {
            LibraryFixture.DeleteTree(outside);
        }
    }

    [Fact]
    public async Task The_detail_lists_the_plans_runs_and_its_evidence()
    {
        using var fixture = new LibraryApiFixture();
        fixture.AddRun("T-1.1", branch: "plan-0001-alpha", planNumber: "0001");

        var (detail, error) = await new GetLibraryItemHandler(fixture.Database)
            .HandleAsync(LibraryApiFixture.Demo, "plan", Plan, CancellationToken.None);

        error.Should().BeNull();
        detail!.Item.Number.Should().Be("0001");
        detail.PreviewUrl.Should().Be(
            "library/demo/preview/file?path=.orchestra%2Fpreviews%2F0001-alpha%2Findex.html");
        detail.Runs.Should().ContainSingle().Which.RunKey.Should().Be("T-1.1");
        detail.Evidence.Select(item => item.RelPath).Should().BeEquivalentTo([Image, Report]);

        (await new GetLibraryItemHandler(fixture.Database)
            .HandleAsync(LibraryApiFixture.Demo, "plan", ".orchestra/plans/0002-beta.md", CancellationToken.None))
            .Item!.PreviewUrl.Should().BeNull();
    }

    private static async Task<(LibraryFile? File, Error? Error)> Read(
        LibraryApiFixture fixture,
        LibraryCategory category,
        string path)
    {
        return await new ReadLibraryFileHandler(fixture.Database, fixture.Registry)
            .HandleAsync(LibraryApiFixture.Demo, category.ToString(), path, CancellationToken.None);
    }
}
