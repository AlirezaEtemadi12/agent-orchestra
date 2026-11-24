using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Orchestra.Api.Common;

internal sealed partial class OrchestraOptionsValidator : IValidateOptions<OrchestraOptions>
{
    private const int NameMaxLength = 80;

    public ValidateOptionsResult Validate(string? name, OrchestraOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.UiPath))
        {
            failures.Add("Orchestra:UiPath must not be empty.");
        }

        if (options.SourceTimeZone is { Length: > 0 } timeZone)
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }
            catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                failures.Add($"Orchestra:SourceTimeZone '{timeZone}' is not a known time zone.");
            }
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < options.Projects.Count; index++)
        {
            var project = options.Projects[index];
            var key = $"Orchestra:Projects:{index}";

            if (!IdPattern().IsMatch(project.Id))
            {
                failures.Add($"{key}:Id must match ^[a-z0-9-]{{1,40}}$ (was '{project.Id}').");
            }
            else if (!ids.Add(project.Id))
            {
                failures.Add($"{key}:Id '{project.Id}' is used by another project.");
            }

            if (string.IsNullOrWhiteSpace(project.Name) || project.Name.Length > NameMaxLength)
            {
                failures.Add($"{key}:Name must be 1 to {NameMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(project.RunsPath))
            {
                failures.Add($"{key}:RunsPath must not be empty.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    [GeneratedRegex("^[a-z0-9-]{1,40}$")]
    private static partial Regex IdPattern();
}
