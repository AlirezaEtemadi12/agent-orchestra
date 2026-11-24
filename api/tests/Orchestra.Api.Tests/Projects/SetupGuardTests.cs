using Microsoft.AspNetCore.Http;
using Orchestra.Api.Common.Errors;
using Orchestra.Api.Modules.Projects.Features.Setup;

namespace Orchestra.Api.Tests.Projects;

public sealed class SetupGuardTests : IDisposable
{
    private readonly SetupTestBed _bed = new();

    [Theory]
    [InlineData("http://localhost:3500", true)]
    [InlineData("http://127.0.0.1:3500", true)]
    [InlineData("http://evil.example:3500", false)]
    [InlineData("http://localhost:3501", false)]
    [InlineData(null, false)]
    public void The_origin_must_be_loopback_on_this_port(string? origin, bool allowed)
    {
        var error = _bed.Guard.CheckOrigin(Request(origin, "localhost:3500"));

        if (allowed)
        {
            error.Should().BeNull();
        }
        else
        {
            error!.Code.Should().Be(Error.SetupOrigin.Code);
            error.Status.Should().Be(403);
        }
    }

    [Fact]
    public void Relative_missing_and_non_folder_paths_are_refused()
    {
        _bed.Guard.CheckRoot("relative/folder").Refusal.Should().Be(SetupRefusal.NotFolder);
        _bed.Guard.CheckRoot(Path.Combine(_bed.Work, "nope")).Refusal.Should().Be(SetupRefusal.NotFolder);
        _bed.Guard.CheckRoot(string.Empty).Refusal.Should().Be(SetupRefusal.NotFolder);
    }

    [Fact]
    public void A_drive_root_a_system_folder_and_our_own_folders_are_refused()
    {
        _bed.Guard.CheckRoot(Path.GetPathRoot(_bed.Work)!).Refusal.Should().Be(SetupRefusal.DriveRoot);

        _bed.Guard.CheckRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).Refusal
            .Should().Be(SetupRefusal.SystemFolder);

        _bed.Guard.CheckRoot(_bed.Tool).Refusal.Should().Be(SetupRefusal.OwnFolder);

        var data = Path.Combine(_bed.Tool, "data");
        Directory.CreateDirectory(data);

        _bed.Guard.CheckRoot(data).Refusal.Should().Be(SetupRefusal.OwnFolder);
    }

    [Fact]
    public void A_plain_project_folder_is_accepted()
    {
        var root = _bed.Repo("shop");

        var check = _bed.Guard.CheckRoot(root);

        check.Refusal.Should().BeNull();
        check.Root.Should().Be(root);
    }

    public void Dispose()
    {
        _bed.Dispose();
    }

    private static HttpRequest Request(string? origin, string host)
    {
        var context = new DefaultHttpContext();

        context.Request.Host = new HostString(host);

        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        return context.Request;
    }
}
