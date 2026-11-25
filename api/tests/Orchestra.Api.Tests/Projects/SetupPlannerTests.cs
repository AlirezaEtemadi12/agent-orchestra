using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Projects.Features.Setup;

namespace Orchestra.Api.Tests.Projects;

/// <summary>A temp workspace with a synthetic engine and template folder, so the setup tests write only
/// their own files. The workspace sits under the test's own folder, never under <c>%LOCALAPPDATA%</c>,
/// which setup refuses (plan 0012 D13).</summary>
internal sealed class SetupTestBed : IDisposable
{
    public SetupTestBed()
    {
        Temp = Path.Combine(AppContext.BaseDirectory, $"setup-{Guid.NewGuid():n}");
        Work = Path.Combine(Temp, "work");
        Tool = Path.Combine(Temp, "tool");
        TemplatesFolder = Path.Combine(Tool, "engine", "templates", "project");

        Directory.CreateDirectory(Work);
        Directory.CreateDirectory(Path.Combine(Tool, "engine"));
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "plans"));
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "knowledge", ".ai"));

        File.WriteAllText(Path.Combine(Tool, "engine", "run.ps1"), "# shim");
        File.WriteAllText(Path.Combine(TemplatesFolder, "workflow.md"), "# Workflow\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "run.ps1"), "# shim\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, ".gitignore"), "runs/\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "plans", "README.md"), "# Plans\n");
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "prompts"));
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "roles"));
        File.WriteAllText(Path.Combine(TemplatesFolder, "prompts", "plan.md"), "# Plan prompt\n");
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "skills", "close-plan"));
        Directory.CreateDirectory(Path.Combine(TemplatesFolder, "claude-skills", "close-plan"));
        File.WriteAllText(Path.Combine(TemplatesFolder, "skills", "close-plan", "SKILL.md"), "# Close a plan\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "claude-skills", "close-plan", "SKILL.md"), "# pointer\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "roles", "README.md"), "# Roles\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "knowledge", "AGENTS.md"), "# <Name> — Agent Entry Point\n");
        File.WriteAllText(Path.Combine(TemplatesFolder, "knowledge", ".ai", "README.md"), "# Knowledge index\n");

        HostEnvironment = new FakeWebHostEnvironment(Tool);
        Options = Microsoft.Extensions.Options.Options.Create(new OrchestraOptions
        {
            DatabasePath = "data/orchestra.db",
            ProfilesPath = "profiles",
        });

        Locator = new EngineLocator(Options, HostEnvironment);
        Templates = new TemplateSet(TemplatesFolder);
        Tokens = new ReportTokenStore(
            Path.Combine(Tool, "data", "report-token"),
            null,
            NullLogger<ReportTokenStore>.Instance);

        Planner = new SetupPlanner(Options, HostEnvironment, Locator, Templates, Tokens);
        Guard = new SetupGuard(Options, HostEnvironment, Locator);
    }

    public string Temp { get; }

    public string Work { get; }

    public string Tool { get; }

    public string TemplatesFolder { get; }

    public IWebHostEnvironment HostEnvironment { get; }

    public IOptions<OrchestraOptions> Options { get; }

    public EngineLocator Locator { get; }

    public TemplateSet Templates { get; }

    public ReportTokenStore Tokens { get; }

    public SetupPlanner Planner { get; }

    public SetupGuard Guard { get; }

    public SetupApplier Applier(ProjectRegistry registry, ImportService imports)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Urls"] = "http://localhost:3500" })
            .Build();

        return new SetupApplier(
            Guard,
            Planner,
            Templates,
            Locator,
            configuration,
            registry,
            imports,
            TimeProvider.System);
    }

    public SetupRequest Request(string root, string name = "MyShop", string id = "myshop", params string[] repos)
    {
        return new SetupRequest(root, name, id, repos.Length == 0 ? ["."] : repos, null, null);
    }

    public string Folder(string name)
    {
        var path = Path.Combine(Work, name);
        Directory.CreateDirectory(path);

        return path;
    }

    public string Repo(string name)
    {
        var path = Folder(name);
        Directory.CreateDirectory(Path.Combine(path, ".git"));

        return path;
    }

    public string SubRepo(string parent, string name)
    {
        var path = Path.Combine(parent, name);
        Directory.CreateDirectory(path);
        Directory.CreateDirectory(Path.Combine(path, ".git"));

        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Temp))
        {
            Directory.Delete(Temp, recursive: true);
        }
    }
}

public sealed class SetupPlannerTests : IDisposable
{
    private readonly SetupTestBed _bed = new();

    [Fact]
    public void An_empty_repo_folder_plans_every_row_as_create()
    {
        var root = _bed.Repo("shop");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.EngineFound.Should().BeTrue();
        plan.IsSingleRepo.Should().BeTrue();
        plan.Items.Should().NotContain(item => item.Id == SetupPlanner.GitInit);
        plan.Items.Where(item => item.Action == SetupActions.Exists).Should().BeEmpty();
        plan.Items.Should().Contain(item =>
            item.Id == "workflow.md"
            && item.Path == ".orchestra/workflow.md"
            && item.Action == SetupActions.Create);
        plan.Items.Should().Contain(item =>
            item.Id == "knowledge/AGENTS.md" && item.Optional && item.Action == SetupActions.Create);
        plan.VerifyCommands.Should().ContainKey("MyShop");
    }

    [Fact]
    public void A_workspace_with_two_repos_plans_git_init_and_a_plans_folder_per_repo()
    {
        var root = _bed.Folder("space");
        _bed.SubRepo(root, "Ui");
        _bed.SubRepo(root, "Web");

        var plan = _bed.Planner.Plan(_bed.Request(root, "MyShop", "myshop", "Ui", "Web"), root);

        plan.IsSingleRepo.Should().BeFalse();
        plan.Items.Should().Contain(item => item.Id == SetupPlanner.GitInit && item.Action == SetupActions.Create);
        plan.Items.Should().Contain(item => item.Id == "plans/Ui/.gitkeep" && item.Path == ".orchestra/plans/Ui/.gitkeep");
        plan.Items.Should().Contain(item => item.Id == "plans/Web/.gitkeep");
        plan.Items.Should().Contain(item => item.Id == "knowledge/Ui/AGENTS.md" && item.Path == "Ui/AGENTS.md");
        plan.Items.Should().Contain(item => item.Id == "knowledge/Web/.ai/README.md" && item.Path == "Web/.ai/README.md");
        plan.VerifyCommands.Keys.Should().Equal("Ui", "Web");
    }

    [Fact]
    public void A_saved_project_that_lists_orchestra_as_a_repo_plans_without_it()
    {
        var root = _bed.Folder("listed");
        _bed.SubRepo(root, "Ui");

        var plan = _bed.Planner.Plan(_bed.Request(root, "MyShop", "myshop", ".orchestra", "Ui"), root);

        plan.Items.Should().NotContain(item => item.Path.StartsWith(".orchestra/plans/.orchestra", StringComparison.Ordinal));
        plan.Items.Should().NotContain(item => item.Path == ".orchestra/AGENTS.md");
        plan.VerifyCommands.Keys.Should().Equal("Ui");
    }

    [Fact]
    public void An_existing_config_without_report_gets_one_add_row()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        File.WriteAllText(
            Path.Combine(root, ".orchestra", "orchestrator.json"),
            "{\"profile\":\"default\"}");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.Items.Should().Contain(item =>
            item.Id == SetupPlanner.OrchestratorJson && item.Action == SetupActions.Exists);
        plan.Items.Should().Contain(item =>
            item.Id == SetupPlanner.ReportBlock && item.Action == SetupActions.Add);
    }

    [Fact]
    public void An_existing_config_with_report_gets_no_extra_row()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        File.WriteAllText(
            Path.Combine(root, ".orchestra", "orchestrator.json"),
            "{\"profile\":\"default\",\"report\":{\"enabled\":true}}");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.Items.Should().NotContain(item => item.Id == SetupPlanner.ReportBlock);
    }

    [Fact]
    public void A_local_json_with_other_paths_is_an_update_whose_detail_names_no_token()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        File.WriteAllText(
            Path.Combine(root, ".orchestra", "orchestrator.local.json"),
            "{\"enginePath\":\"old\",\"profilesPath\":\"old\",\"reportToken\":\"abc123\",\"custom\":1}");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);
        var row = plan.Items.Single(item => item.Id == SetupPlanner.LocalJson);

        row.Action.Should().Be(SetupActions.Update);
        row.Detail.Should().Contain("enginePath").And.Contain("profilesPath").And.Contain("reportToken");
        row.Detail.Should().NotContain("abc123");
    }

    [Fact]
    public void A_claude_skill_pointer_lands_in_dot_claude_while_its_id_stays_the_template_path()
    {
        var root = _bed.Repo("shop");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.Items.Should().Contain(item =>
            item.Id == "claude-skills/close-plan/SKILL.md"
            && item.Path == ".claude/skills/close-plan/SKILL.md");
        plan.Items.Should().Contain(item =>
            item.Id == "skills/close-plan/SKILL.md"
            && item.Path == ".orchestra/skills/close-plan/SKILL.md");
    }

    [Fact]
    public void A_config_with_its_own_prompt_and_role_folders_plans_no_prompt_or_role_rows()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        File.WriteAllText(
            Path.Combine(root, ".orchestra", "orchestrator.json"),
            "{\"workflow\":{\"promptsPath\":\".orchestra/prompts/{repo}\",\"rolesPath\":\".orchestra/roles/{repo}\"}}");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.Items.Should().NotContain(item => item.Id.StartsWith("prompts/", StringComparison.Ordinal));
        plan.Items.Should().NotContain(item => item.Id.StartsWith("roles/", StringComparison.Ordinal));
        plan.Items.Should().Contain(item => item.Id == "workflow.md");
    }

    [Fact]
    public void A_config_with_the_standard_folders_still_plans_the_prompt_and_role_rows()
    {
        var root = _bed.Repo("shop");
        Directory.CreateDirectory(Path.Combine(root, ".orchestra"));
        File.WriteAllText(
            Path.Combine(root, ".orchestra", "orchestrator.json"),
            "{\"workflow\":{\"promptsPath\":\".orchestra/prompts/\",\"rolesPath\":\".orchestra/roles\"}}");

        var plan = _bed.Planner.Plan(_bed.Request(root), root);

        plan.Items.Should().Contain(item => item.Id == "prompts/plan.md" && item.Path == ".orchestra/prompts/plan.md");
        plan.Items.Should().Contain(item => item.Id == "roles/README.md" && item.Path == ".orchestra/roles/README.md");
    }

    public void Dispose()
    {
        _bed.Dispose();
    }
}

internal sealed class FakeWebHostEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Orchestra.Tests";

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string WebRootPath { get; set; } = contentRoot;

    public string ContentRootPath { get; set; } = contentRoot;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public string EnvironmentName { get; set; } = "Development";
}
