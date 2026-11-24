using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Tests.Projects;

public sealed class ProjectRegistryTests : IDisposable
{
    private readonly ProjectsFixture _fixture = new();

    [Fact]
    public void Config_and_store_are_merged_with_their_origins()
    {
        _fixture.Store.Write([_fixture.Entry("shop")]);

        var registry = Registry();

        registry.All.Select(project => project.Id).Should().BeEquivalentTo([ProjectsFixture.ConfigProject, "shop"]);
        registry.OriginOf(ProjectsFixture.ConfigProject).Should().Be(ProjectOrigin.Config);
        registry.OriginOf("shop").Should().Be(ProjectOrigin.Dashboard);
        registry.OriginOf("nope").Should().BeNull();
    }

    [Fact]
    public void A_shared_id_is_the_config_one()
    {
        _fixture.Store.Write([_fixture.Entry(ProjectsFixture.ConfigProject)]);

        var registry = Registry();

        registry.Find(ProjectsFixture.ConfigProject)!.Name.Should().Be("Config project");
        registry.OriginOf(ProjectsFixture.ConfigProject).Should().Be(ProjectOrigin.Config);
    }

    [Fact]
    public void A_created_project_is_listed_without_a_restart()
    {
        var registry = Registry();
        var entry = _fixture.Entry("shop");

        registry.Create(entry);

        registry.Find("shop").Should().NotBeNull();
        registry.OriginOf("shop").Should().Be(ProjectOrigin.Dashboard);
        new ProjectStore(_fixture.StorePath).Read().Projects.Should().ContainSingle();
    }

    [Fact]
    public void A_failed_write_leaves_the_snapshot_unchanged()
    {
        var registry = Registry();

        Directory.CreateDirectory(Path.GetDirectoryName(_fixture.StorePath)!);
        File.WriteAllText(_fixture.StorePath, "{ broken");

        var create = () => registry.Create(_fixture.Entry("shop"));

        create.Should().Throw<ProjectStoreUnreadableException>();
        registry.Find("shop").Should().BeNull();
        registry.All.Should().ContainSingle();
    }

    [Fact]
    public void Remove_and_update_only_touch_the_store_entries()
    {
        var registry = Registry();

        registry.Create(_fixture.Entry("shop"));
        registry.Update("shop", entry => entry with { Name = "Renamed" }).Should().BeTrue();
        registry.Find("shop")!.Name.Should().Be("Renamed");

        registry.Remove(ProjectsFixture.ConfigProject).Should().BeFalse();
        registry.Find(ProjectsFixture.ConfigProject).Should().NotBeNull();

        registry.Remove("shop").Should().BeTrue();
        registry.Find("shop").Should().BeNull();
    }

    private ProjectRegistry Registry()
    {
        return new ProjectRegistry(_fixture.Settings, _fixture.Store, NullLogger<ProjectRegistry>.Instance);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
