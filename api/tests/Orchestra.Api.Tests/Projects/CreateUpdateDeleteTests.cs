using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.CreateProject;
using Orchestra.Api.Modules.Projects.Features.DeleteProject;
using Orchestra.Api.Modules.Projects.Features.UpdateProject;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Tests.Projects;

public sealed class CreateUpdateDeleteTests : IDisposable
{
    private readonly ProjectsFixture _fixture = new();

    [Fact]
    public async Task Create_saves_the_entry_and_imports_the_project()
    {
        var root = _fixture.NewRoot("shop");

        var (saved, error) = await Create.HandleAsync(Input("shop", root), CancellationToken.None);

        error.Should().BeNull();
        saved!.Id.Should().Be("shop");

        _fixture.Registry.Find("shop").Should().NotBeNull();
        _fixture.Registry.OriginOf("shop").Should().Be(ProjectOrigin.Dashboard);
        (await _fixture.Database.Projects.AnyAsync(project => project.Id == "shop")).Should().BeTrue();
        (await _fixture.Database.ImportLogs.CountAsync(log => log.ProjectId == "shop")).Should().Be(1);
        File.Exists(_fixture.StorePath).Should().BeTrue();
    }

    [Fact]
    public async Task A_taken_id_and_invalid_fields_are_refused()
    {
        var taken = await Create.HandleAsync(
            Input(ProjectsFixture.ConfigProject, _fixture.NewRoot("taken")),
            CancellationToken.None);

        taken.Error!.Code.Should().Be(Error.ProjectIdTaken.Code);

        // A relative path is invalid, not "not visible": that code (and its Docker text) is kept for an
        // absolute path a container cannot reach, which only a Linux host can produce.
        var relative = Input("shop", "C:/nope") with { RootPath = ".orchestra/runs" };

        (await Create.HandleAsync(relative, CancellationToken.None)).Error!
            .Code.Should().Be(Error.ProjectInvalid.Code);

        var badId = Input("SHOP", _fixture.NewRoot("bad-id"));

        (await Create.HandleAsync(badId, CancellationToken.None)).Error!
            .Code.Should().Be(Error.ProjectInvalid.Code);

        _fixture.Registry.Find("shop").Should().BeNull();
    }

    [Fact]
    public async Task Update_refuses_a_config_project_and_an_unknown_id()
    {
        var handler = new UpdateProjectHandler(_fixture.Registry, _fixture.Imports, _fixture.Mapper);

        var config = await handler.HandleAsync(
            ProjectsFixture.ConfigProject,
            Input(ProjectsFixture.ConfigProject, _fixture.NewRoot("config")),
            CancellationToken.None);

        config.Error!.Code.Should().Be(Error.ProjectReadOnly.Code);

        var unknown = await handler.HandleAsync("nope", Input("nope", _fixture.NewRoot("nope")), CancellationToken.None);

        unknown.Error!.Code.Should().Be(Error.ProjectNotFound.Code);
    }

    [Fact]
    public async Task Update_renames_a_dashboard_project_and_re_imports_it()
    {
        var root = _fixture.NewRoot("shop");
        await Create.HandleAsync(Input("shop", root), CancellationToken.None);

        var renamed = Input("shop", root) with { Name = "Renamed" };

        var (saved, error) = await new UpdateProjectHandler(_fixture.Registry, _fixture.Imports, _fixture.Mapper)
            .HandleAsync("shop", renamed, CancellationToken.None);

        error.Should().BeNull();
        saved!.Name.Should().Be("Renamed");
        _fixture.Registry.Find("shop")!.Name.Should().Be("Renamed");
        (await _fixture.Database.ImportLogs.CountAsync(log => log.ProjectId == "shop")).Should().Be(2);
    }

    [Fact]
    public async Task Delete_removes_the_rows_its_logs_and_their_errors()
    {
        var root = _fixture.NewRoot("shop");
        await Create.HandleAsync(Input("shop", root), CancellationToken.None);

        var log = await _fixture.Database.ImportLogs.SingleAsync(item => item.ProjectId == "shop");

        _fixture.Database.ImportErrors.Add(new ImportErrorEntity
        {
            Id = Guid.NewGuid(),
            ImportLogId = log.Id,
            File = "task.json",
            Message = "boom",
        });

        _fixture.Database.Runs.Add(new RunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "shop",
            RunKey = "T-1.1",
            TaskId = "T-1",
            Status = "Verified",
            CreatedAt = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
        });

        _fixture.Database.LibraryItems.Add(new LibraryItemEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = "shop",
            Category = LibraryCategory.Plan,
            RelPath = ".orchestra/plans/0001-a.md",
            Title = "A",
            Slug = "a",
            ModifiedAt = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
        });

        await _fixture.Database.SaveChangesAsync();

        var marker = Path.Combine(root, "keep.txt");
        await File.WriteAllTextAsync(marker, "untouched");

        var error = await Handler().HandleAsync("shop", CancellationToken.None);

        error.Should().BeNull();
        _fixture.Registry.Find("shop").Should().BeNull();
        _fixture.Registry.All.Should().ContainSingle();

        (await _fixture.Database.Projects.CountAsync(project => project.Id == "shop")).Should().Be(0);
        (await _fixture.Database.Runs.CountAsync(run => run.ProjectId == "shop")).Should().Be(0);
        (await _fixture.Database.LibraryItems.CountAsync(item => item.ProjectId == "shop")).Should().Be(0);
        (await _fixture.Database.ImportLogs.CountAsync(item => item.ProjectId == "shop")).Should().Be(0);
        (await _fixture.Database.ImportErrors.CountAsync()).Should().Be(0);

        File.ReadAllText(marker).Should().Be("untouched");
        new ProjectStore(_fixture.StorePath).Read().Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_works_for_a_project_that_was_never_imported()
    {
        _fixture.Registry.Create(_fixture.Entry("shop"));

        var error = await Handler().HandleAsync("shop", CancellationToken.None);

        error.Should().BeNull();
        _fixture.Registry.Find("shop").Should().BeNull();
        new ProjectStore(_fixture.StorePath).Read().Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_refuses_a_config_project_and_an_unknown_id()
    {
        (await Handler().HandleAsync(ProjectsFixture.ConfigProject, CancellationToken.None))!
            .Code.Should().Be(Error.ProjectReadOnly.Code);

        (await Handler().HandleAsync("nope", CancellationToken.None))!
            .Code.Should().Be(Error.ProjectNotFound.Code);
    }

    private CreateProjectHandler Create => new(_fixture.Registry, _fixture.Imports, _fixture.Mapper, TimeProvider.System);

    private DeleteProjectHandler Handler()
    {
        return new DeleteProjectHandler(_fixture.Database, _fixture.Registry, _fixture.Imports);
    }

    private static ProjectInput Input(string id, string root)
    {
        return new ProjectInput(
            id,
            "MyShop",
            root,
            Path.Combine(root, ".orchestra", "runs"),
            [],
            "default");
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
