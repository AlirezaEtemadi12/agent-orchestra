using Microsoft.Extensions.Logging.Abstractions;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Tests.Events;

public sealed class ReportTokenStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"orchestra-token-{Guid.NewGuid():n}");

    [Fact]
    public void The_first_call_writes_a_generated_token_and_a_second_store_reads_it()
    {
        var file = Path.Combine(_folder, "report-token");

        var first = Store(file, configured: null);
        var token = first.Get();

        token.Should().HaveLength(64);
        token.Should().MatchRegex("^[0-9a-f]{64}$");
        first.Source.Should().Be("generated");
        File.Exists(file).Should().BeTrue();
        File.ReadAllText(file).Should().Be(token);

        Store(file, configured: null).Get().Should().Be(token);
        first.Get().Should().Be(token);
    }

    [Fact]
    public void A_configured_token_wins_and_no_file_is_written()
    {
        var file = Path.Combine(_folder, "report-token");
        var store = Store(file, configured: "pinned-token");

        store.Get().Should().Be("pinned-token");
        store.Source.Should().Be("settings");
        File.Exists(file).Should().BeFalse();
    }

    [Fact]
    public void An_empty_saved_file_is_replaced()
    {
        Directory.CreateDirectory(_folder);

        var file = Path.Combine(_folder, "report-token");
        File.WriteAllText(file, "  ");

        var token = Store(file, configured: null).Get();

        token.Should().HaveLength(64);
        File.ReadAllText(file).Should().Be(token);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static ReportTokenStore Store(string file, string? configured)
    {
        return new ReportTokenStore(file, configured, NullLogger<ReportTokenStore>.Instance);
    }
}
