using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;
using Orchestra.Api.Tests.Fixtures;

namespace Orchestra.Api.Tests.Library;

/// <summary>An in-memory database over a temp copy of <c>Fixtures/library/orchestra</c>, indexed with
/// the default globs — the shape the API tests ask questions about.</summary>
internal sealed class LibraryApiFixture : IDisposable
{
    public const string Demo = "demo";

    private readonly SqliteConnection _connection;

    public LibraryApiFixture(bool index = true)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        Database.Database.EnsureCreated();

        Root = Path.Combine(Path.GetTempPath(), $"orchestra-api-{Guid.NewGuid():n}");
        LibraryFixture.Copy(LibraryFixture.OrchestraRoot, Root);

        var options = Options.Create(new OrchestraOptions
        {
            Projects =
            [
                new ProjectOptions
                {
                    Id = Demo,
                    Name = "Demo",
                    RunsPath = Path.Combine(Root, "orchestrator", "runs"),
                    RootPath = Root,
                },
            ],
        });

        Registry = TestRegistries.ConfigOnly(options);

        Database.Projects.Add(new ProjectEntity
        {
            Id = Demo,
            Name = "Demo",
            RunsPath = Path.Combine(Root, "orchestrator", "runs"),
        });

        Database.SaveChanges();

        if (index)
        {
            var result = LibraryIndexer.Build(Registry.Find(Demo)!, []);

            Database.LibraryItems.AddRange(result.Items);
            Database.SaveChanges();
        }
    }

    public OrchestraDbContext Database { get; }

    public ProjectRegistry Registry { get; }

    public string Root { get; }

    public LibraryItemEntity Item(string relPath, LibraryCategory category = LibraryCategory.Plan)
    {
        return Database.LibraryItems.AsNoTracking()
            .Single(item => item.RelPath == relPath && item.Category == category);
    }

    public void AddRun(string runKey, string status = "Verified", string? branch = null, string? planNumber = null)
    {
        Database.Runs.Add(new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = Demo,
            RunKey = runKey,
            TaskId = runKey.Split('.')[0],
            Status = status,
            Branch = branch,
            PlanNumber = planNumber,
            CreatedAt = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
        });

        Database.SaveChanges();
    }

    public void AddItem(
        string relPath,
        LibraryCategory category,
        string? number = null,
        string? group = null,
        string? status = null,
        DateTime? createdOn = null,
        DateTime? updatedOn = null,
        string? repo = null)
    {
        Database.LibraryItems.Add(new LibraryItemEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = Demo,
            Category = category,
            RelPath = relPath,
            Title = relPath,
            Slug = Path.GetFileNameWithoutExtension(relPath),
            Number = number,
            Group = group,
            Status = status,
            CreatedOn = createdOn,
            UpdatedOn = updatedOn,
            Repo = repo,
            ModifiedAt = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
        });

        Database.SaveChanges();
    }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();
        LibraryFixture.DeleteTree(Root);
    }
}
