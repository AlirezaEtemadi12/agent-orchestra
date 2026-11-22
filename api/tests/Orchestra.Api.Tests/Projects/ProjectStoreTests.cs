using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Tests.Projects;

public sealed class ProjectStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"orchestra-store-{Guid.NewGuid():n}");

    private string FilePath => Path.Combine(_folder, "projects.json");

    [Fact]
    public void A_missing_file_is_an_empty_list_without_an_error()
    {
        var read = new ProjectStore(FilePath).Read();

        read.Projects.Should().BeEmpty();
        read.Error.Should().BeNull();
    }

    [Fact]
    public void An_entry_round_trips()
    {
        var store = new ProjectStore(FilePath);
        var entry = Entry();

        store.Write([entry]);

        var read = store.Read();

        read.Error.Should().BeNull();
        read.Projects.Should().ContainSingle();
        read.Projects[0].Should().BeEquivalentTo(entry);
        File.Exists(FilePath).Should().BeTrue();
        File.Exists(FilePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public void A_write_replaces_the_previous_list()
    {
        var store = new ProjectStore(FilePath);

        store.Write([Entry("first"), Entry("second")]);
        store.Write([Entry("second")]);

        store.Read().Projects.Select(project => project.Id).Should().Equal("second");
    }

    [Fact]
    public void A_file_that_does_not_parse_is_reported_and_never_overwritten()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{ \"schema\": 1, ");

        var store = new ProjectStore(FilePath);
        var read = store.Read();

        read.Error.Should().NotBeNull();
        read.Projects.Should().BeEmpty();

        var write = () => store.Write([Entry()]);

        write.Should().Throw<ProjectStoreUnreadableException>();
        File.ReadAllText(FilePath).Should().Be("{ \"schema\": 1, ");
    }

    [Fact]
    public void An_unknown_schema_or_invalid_entry_is_unreadable()
    {
        Directory.CreateDirectory(_folder);

        File.WriteAllText(FilePath, "{ \"schema\": 2, \"projects\": [] }");
        new ProjectStore(FilePath).Read().Error.Should().NotBeNull();

        File.WriteAllText(FilePath, "{ \"schema\": 1, \"projects\": [ { \"id\": \"x\" } ] }");
        new ProjectStore(FilePath).Read().Error.Should().NotBeNull();
    }

    private static StoredProject Entry(string id = "shop")
    {
        return new StoredProject
        {
            Id = id,
            Name = "MyShop",
            RootPath = @"D:\work\MyShop",
            RunsPath = @"D:\work\MyShop\.orchestra\runs",
            Repos = ["api", "web"],
            CreatedAt = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc),
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
