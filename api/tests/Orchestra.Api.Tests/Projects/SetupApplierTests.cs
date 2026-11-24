using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.Setup;

namespace Orchestra.Api.Tests.Projects;

public sealed class SetupApplierTests : IDisposable
{
    private readonly SetupTestBed _bed = new();
    private readonly SqliteConnection _connection;
    private readonly OrchestraDbContext _database;
    private readonly ProjectRegistry _registry;
    private readonly ImportService _imports;

    public SetupApplierTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _database = new OrchestraDbContext(
            new DbContextOptionsBuilder<OrchestraDbContext>().UseSqlite(_connection).Options);

        _database.Database.EnsureCreated();

        var store = new ProjectStore(Path.Combine(_bed.Temp, "projects.json"));
        _registry = new ProjectRegistry(_bed.Options, store, NullLogger<ProjectRegistry>.Instance);

        _imports = new ImportService(
            _database,
            _registry,
            _bed.Options,
            TimeProvider.System,
            NullLogger<ImportService>.Instance);
    }

    [Fact]
    public async Task It_writes_the_planned_files_and_registers_the_project()
    {
        var root = _bed.Repo("shop");

        var (result, error, refusal) = await Apply(_bed.Request(root));

        error.Should().BeNull();
        refusal.Should().BeNull();
        result!.Items.Should().OnlyContain(item => item.Ok);
        File.Exists(Path.Combine(root, ".orchestra", "orchestrator.json")).Should().BeTrue();
        File.Exists(Path.Combine(root, ".orchestra", "run.ps1")).Should().BeTrue();
        File.Exists(Path.Combine(root, ".orchestra", "workflow.md")).Should().BeTrue();
        File.Exists(Path.Combine(root, "AGENTS.md")).Should().BeTrue();
        File.ReadAllText(Path.Combine(root, "AGENTS.md")).Should().StartWith("# MyShop —");
        result.Project.RunsPath.Should().Be(Path.Combine(root, ".orchestra", "runs"));
        _registry.All.Should().ContainSingle(project => project.Id == "myshop");
    }

    [Fact]
    public async Task A_second_apply_writes_nothing()
    {
        var root = _bed.Repo("shop");

        await Apply(_bed.Request(root));

        var config = Path.Combine(root, ".orchestra", "orchestrator.json");
        var local = Path.Combine(root, ".orchestra", "orchestrator.local.json");
        var before = (await File.ReadAllTextAsync(config), await File.ReadAllTextAsync(local));

        var (result, error, _) = await Apply(_bed.Request(root));

        error.Should().BeNull();
        result!.Items.Should().OnlyContain(item => item.Ok);
        (await File.ReadAllTextAsync(config)).Should().Be(before.Item1);
        (await File.ReadAllTextAsync(local)).Should().Be(before.Item2);
        _registry.All.Should().ContainSingle();
    }

    [Fact]
    public async Task A_second_apply_with_a_new_name_updates_the_one_entry()
    {
        var root = _bed.Repo("shop");

        await Apply(_bed.Request(root, "MyShop", "myshop"));
        await Apply(_bed.Request(root, "MyShop Two", "myshop"));

        _registry.All.Should().ContainSingle();
        _registry.Find("myshop")!.Name.Should().Be("MyShop Two");
    }

    [Fact]
    public async Task The_report_block_leaves_every_other_byte_identical()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));

        const string original = "{\r\n  \"profile\": \"default\",\r\n  \"repos\": { \".\": \".\" }\r\n}\r\n";

        var config = Path.Combine(root, ".orchestra", "orchestrator.json");
        await File.WriteAllTextAsync(config, original);

        var (result, error, _) = await Apply(_bed.Request(root));

        error.Should().BeNull();
        result!.Items.Should().OnlyContain(item => item.Ok);

        var text = await File.ReadAllTextAsync(config);

        text.Should().StartWith("{");
        text.Should().Contain("\"report\"");
        text.EndsWith(original[1..], StringComparison.Ordinal).Should().BeTrue();
    }

    [Fact]
    public async Task Local_json_keeps_an_unknown_key()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));

        var local = Path.Combine(root, ".orchestra", "orchestrator.local.json");
        await File.WriteAllTextAsync(local, "{\"enginePath\":\"old\",\"custom\":1}");

        var (_, error, _) = await Apply(_bed.Request(root));

        error.Should().BeNull();

        var node = JsonNode.Parse(await File.ReadAllTextAsync(local))!;

        node["custom"]!.GetValue<int>().Should().Be(1);
        node["enginePath"]!.GetValue<string>().Should().Be(_bed.Tool);
        node["reportToken"]!.GetValue<string>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task A_row_the_owner_unticked_is_not_written()
    {
        var root = _bed.Repo("shop");

        var request = _bed.Request(root) with { Skip = ["knowledge/AGENTS.md"] };

        var (result, error, _) = await Apply(request);

        error.Should().BeNull();
        result!.Items.Should().OnlyContain(item => item.Ok);
        File.Exists(Path.Combine(root, "AGENTS.md")).Should().BeFalse();
        File.Exists(Path.Combine(root, ".ai", "README.md")).Should().BeTrue();
    }

    [Fact]
    public async Task A_row_that_cannot_be_written_fails_and_the_next_row_still_runs()
    {
        var root = _bed.Repo("shop");

        // A folder where the template's file belongs: the write fails, the plan does not.
        Directory.CreateDirectory(Path.Combine(root, ".orchestra", "workflow.md"));

        var (result, error, _) = await Apply(_bed.Request(root));

        error.Should().BeNull();

        var failed = result!.Items.Single(item => item.Id == "workflow.md");

        failed.Ok.Should().BeFalse();
        failed.Error.Should().Be(SetupItemErrors.WriteFailed);
        result.Items.Single(item => item.Id == SetupPlanner.LocalJson).Ok.Should().BeTrue();
        File.Exists(Path.Combine(root, ".orchestra", "orchestrator.json")).Should().BeTrue();
    }

    [Fact]
    public async Task A_failed_git_init_stops_everything_after_it()
    {
        var root = _bed.Folder("space");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        await File.WriteAllTextAsync(Path.Combine(root, ".orchestra", ".git"), "not a gitfile");

        var (result, error, _) = await Apply(_bed.Request(root));

        error.Should().BeNull();
        result!.Items.Single(item => item.Id == SetupPlanner.GitInit).Error
            .Should().Be(SetupItemErrors.GitInitFailed);
        result.Items
            .Where(item => item.Id != SetupPlanner.GitInit)
            .Should().OnlyContain(item => item.Error == SetupItemErrors.Skipped);
        File.Exists(Path.Combine(root, ".orchestra", "orchestrator.json")).Should().BeFalse();
    }

    [Fact]
    public async Task The_two_shapes_render_the_config_of_the_plan()
    {
        var single = _bed.Repo("single");

        await Apply(_bed.Request(single, "MyShop", "myshop"));

        var one = Config(single);

        one["repos"]!.AsObject().Select(pair => pair.Key).Should().Equal("MyShop");
        one["repos"]!["MyShop"]!.GetValue<string>().Should().Be(".");
        one["plansDir"]!.GetValue<string>().Should().Be(".orchestra/plans");
        one["workflow"]!["agentsDoc"]!.GetValue<string>().Should().Be("AGENTS.md");
        one["workflow"]!["indexDoc"]!.GetValue<string>().Should().Be(".ai/README.md");
        one["workflow"]!["rolesPath"]!.GetValue<string>().Should().Be(".orchestra/roles");
        one["report"]!["url"]!.GetValue<string>().Should().Be("http://localhost:3500/api/events");
        one["report"]!["project"]!.GetValue<string>().Should().Be("myshop");

        var space = _bed.Folder("space");
        _bed.SubRepo(space, "Ui");
        _bed.SubRepo(space, "Web");

        await Apply(_bed.Request(space, "MyShop", "shop-two", "Ui", "Web"));

        var many = Config(space);

        many["repos"]!.AsObject().Select(pair => pair.Key).Should().Equal("Ui", "Web");
        many["repos"]!["Ui"]!.GetValue<string>().Should().Be("Ui");
        many["plansDir"]!.GetValue<string>().Should().Be(".orchestra/plans/{repo}");
        many["workflow"]!["agentsDoc"]!.GetValue<string>().Should().Be("{repo}/AGENTS.md");
        many["workflow"]!["indexDoc"]!.GetValue<string>().Should().Be("{repo}/.ai/README.md");
        File.Exists(Path.Combine(space, ".orchestra", "plans", "Ui", ".gitkeep")).Should().BeTrue();
        File.Exists(Path.Combine(space, "Ui", "AGENTS.md")).Should().BeTrue();
        Directory.Exists(Path.Combine(space, ".orchestra", ".git")).Should().BeTrue();
    }

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();
        _bed.Dispose();
    }

    private async Task<(SetupResultDto? Result, Error? Error, SetupRefusal? Refusal)> Apply(SetupRequest request)
    {
        return await _bed.Applier(_registry, _imports).HandleAsync(request, CancellationToken.None);
    }

    private static JsonNode Config(string root)
    {
        return JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".orchestra", "orchestrator.json")))!;
    }
}
