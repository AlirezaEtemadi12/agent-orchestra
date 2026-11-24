using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Photino.NET;
using Orchestra.Api.Extensions;

namespace Orchestra.Desktop;

/// <summary>Plan 0012 §6.7: the API and the exported UI in one window, no console.</summary>
internal static class Program
{
    private const string Url = "http://localhost:3500/";
    private const string HealthUrl = "http://localhost:3500/api/health";

    [STAThread]
    private static int Main(string[] args)
    {
        switch (Check())
        {
            case Health.Running:
                ShowMessage("Orchestra is already open.");
                return 0;
            case Health.Taken:
                ShowMessage("Port 3500 is used by another program.");
                return 1;
        }

        WebApplication app;

        try
        {
            app = OrchestraHost.Create(args, AppContext.BaseDirectory);
            app.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            ShowMessage(FirstLine(exception));
            return 1;
        }

        var window = new PhotinoWindow()
            .SetTitle("Orchestra")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 860)
            .Center()
            .SetResizable(true)
            .RegisterWebMessageReceivedHandler(OnMessage);

        var icon = Path.Combine(AppContext.BaseDirectory, "wwwroot", "favicon.ico");

        if (File.Exists(icon))
        {
            window.SetIconFile(icon);
        }

        window.Load(Url);
        window.WaitForClose();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        app.StopAsync(timeout.Token).GetAwaiter().GetResult();

        return 0;
    }

    /// <summary>Plan 0012 D2a: one instance. A health answer means this app already runs; a taken port does not.</summary>
    private static Health Check()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        try
        {
            using var response = client.GetAsync(HealthUrl).GetAwaiter().GetResult();

            return Health.Running;
        }
        catch (Exception)
        {
            return PortInUse() ? Health.Taken : Health.Free;
        }
    }

    private static bool PortInUse()
    {
        try
        {
            using var client = new TcpClient();

            client.Connect("127.0.0.1", 3500);

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    /// <summary>Plan 0012 D3b: the folder dialog the exported UI cannot open by itself.</summary>
    private static void OnMessage(object? sender, string message)
    {
        var window = (PhotinoWindow)sender!;

        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var type) || type.GetString() != "pickFolder")
            {
                return;
            }

            var start = root.TryGetProperty("start", out var value) ? value.GetString() ?? string.Empty : string.Empty;
            var picked = window.ShowOpenFolder("Choose the project folder", start);

            window.SendWebMessage(JsonSerializer.Serialize(new
            {
                type = "pickedFolder",
                path = picked.FirstOrDefault() ?? string.Empty,
            }));
        }
        catch (JsonException)
        {
            // Not one of ours; the UI sent something else and gets no answer.
        }
    }

    private static void ShowMessage(string message)
    {
        new PhotinoWindow().ShowMessage("Orchestra", message);
    }

    private static string FirstLine(Exception exception)
    {
        var text = exception.GetBaseException().Message;

        return text.Split('\n')[0].Trim();
    }

    private enum Health
    {
        Free,
        Running,
        Taken,
    }
}
