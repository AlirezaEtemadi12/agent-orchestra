using System.Security.Cryptography;
using System.Text;

namespace Orchestra.Api.Modules.Events;

/// <summary>Plan 0009 D6: the dashboard makes its own report token when none is configured, so the
/// reporter is always on and the token survives a restart. A configured value wins and nothing is
/// written. The token never reaches a log line.</summary>
public sealed class ReportTokenStore(string filePath, string? configured, ILogger<ReportTokenStore> logger)
{
    private readonly object _gate = new();

    private string? _token;

    /// <summary><c>settings</c> when the configuration supplies it, <c>generated</c> otherwise. Does not
    /// create the file.</summary>
    public string Source => string.IsNullOrWhiteSpace(configured) ? "generated" : "settings";

    public string FilePath => filePath;

    public string Get()
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        lock (_gate)
        {
            if (_token is not null)
            {
                return _token;
            }

            var stored = Read();

            if (stored is not null)
            {
                _token = stored;

                return _token;
            }

            var generated = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

            try
            {
                Write(generated);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The token still works for this process; only a restart loses it.
                logger.LogWarning("The report token could not be saved to {Path}: {Error}", filePath, exception.Message);
            }

            _token = generated;

            return _token;
        }
    }

    private string? Read()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var text = File.ReadAllText(filePath, Encoding.UTF8).Trim();

            return text.Length == 0 ? null : text;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("The saved report token could not be read from {Path}: {Error}", filePath, exception.Message);

            return null;
        }
    }

    private void Write(string token)
    {
        var folder = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var temporary = filePath + ".tmp";

        File.WriteAllText(temporary, token, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporary, filePath, overwrite: true);
    }
}
