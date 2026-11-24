using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Extensions;
using Xunit;

namespace Orchestra.Api.Tests;

/// <summary>Plan 0012 §6.7: the desktop app builds the same host <c>Program.cs</c> runs.</summary>
public sealed class OrchestraHostTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"orchestra-host-{Guid.NewGuid():n}");

    public OrchestraHostTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Create_builds_the_host_on_the_given_content_root_without_running_it()
    {
        var database = Path.Combine(_root, "orchestra.db");

        using var app = OrchestraHost.Create(
            [$"--Orchestra:DatabasePath={database}", "--Urls=http://127.0.0.1:3501"],
            _root);

        app.Environment.ContentRootPath.Should().Be(_root);

        var options = app.Services.GetRequiredService<IOptions<OrchestraOptions>>().Value;

        options.ResolveDatabasePath(_root).Should().Be(database);
        File.Exists(database).Should().BeTrue("the host migrates before it is returned");
    }

    public void Dispose()
    {
        // The host migrated the database, so its pool still holds the file until it is cleared.
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp folder must not fail a green test.
        }
    }
}
