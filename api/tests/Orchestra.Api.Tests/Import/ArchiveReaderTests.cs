using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Import.Archive;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Import;

public sealed class ArchiveReaderTests
{
    private static readonly SourceClock Clock = SourceClock.From("Asia/Tehran");

    private static ArchiveSnapshotResult Read(string name)
    {
        var relative = $"_archive/{name}";

        return ArchiveReader.Read(File.ReadAllText(Path.Combine(FixtureFiles.RunsRoot, relative)), relative, Clock);
    }

    [Fact]
    public void Schema_one_reads_with_the_archived_task_without_its_usage()
    {
        var result = Read("T-DEMO-001.json");

        result.Error.Should().BeNull();

        var archive = result.Snapshot!;
        archive.Schema.Should().Be(1);
        archive.TaskId.Should().Be("T-DEMO-001");
        archive.Task.Status.Should().Be("Verified");
        archive.Task.Branch.Should().Be("plan-0101-reused");
        archive.Task.History.Should().HaveCount(2);
    }

    [Fact]
    public void The_archived_at_offset_becomes_utc()
    {
        var archive = Read("T-DEMO-001.json").Snapshot!;

        archive.ArchivedAt.Should().Be(new DateTime(2026, 11, 16, 9, 35, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Every_review_read_back_and_verify_text_keeps_its_round()
    {
        var archive = Read("T-DEMO-001.json").Snapshot!;

        archive.Texts.Should().HaveCount(4);
        archive.Texts.Count(text => text.Stage == "Review").Should().Be(2);
        archive.Texts.Single(text => text.Stage == "ReadBack").Round.Should().Be(1);
        archive.Texts.Single(text => text.Stage == "Verify").Round.Should().BeNull();
    }

    [Fact]
    public void An_unsupported_schema_is_one_error()
    {
        var result = Read("T-DEMO-010.json");

        result.Snapshot.Should().BeNull();
        result.Error.Should().Contain("schema 2");
    }

    [Fact]
    public void A_unique_file_name_still_gives_the_task_id()
    {
        ArchiveReader.TaskIdOf("_archive/T-X.20261119.json").Should().Be("T-X");
        ArchiveReader.TaskIdOf("_archive/T-DEMO-001.json").Should().Be("T-DEMO-001");
    }
}
