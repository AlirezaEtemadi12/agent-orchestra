using System.Globalization;

namespace Orchestra.Api.Common.Errors;

public static class ErrorMessages
{
    public static IReadOnlyDictionary<string, string> En { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Runs.Run.NotFound"] = "Run not found.",
        ["Projects.Project.NotFound"] = "Project not found.",
        ["Projects.Project.IdTaken"] = "A project with this id already exists.",
        ["Projects.Project.ReadOnly"] = "This project comes from the configuration file; change it there.",
        ["Projects.Project.Invalid"] = "Some fields are not valid.",
        ["Projects.Store.Unreadable"] = "projects.json cannot be read; fix or remove it first.",
        ["Usage.Query.InvalidGroupBy"] = "groupBy must be day, month, model or project.",
        ["Usage.Query.InvalidRange"] = "The date range is not valid.",
        ["Usage.Query.InvalidTimeZone"] = "Unknown time zone.",
        ["Import.AlreadyRunning"] = "An import is already running.",
        ["Events.Body.Invalid"] = "The body is not a schema 1 event.",
        ["Events.Project.Unknown"] = "Unknown project id in the event.",
        ["Events.Kind.Unknown"] = "Unknown event kind.",
        ["Library.Item.NotFound"] = "This item is not in the library.",
        ["Library.File.TooLarge"] = "The file is too large to show.",
        ["Api.Unexpected"] = "Something went wrong on the server.",
        ["Api.NotFound"] = "No such API route.",
    };

    public static IReadOnlyDictionary<string, string> Fa { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Runs.Run.NotFound"] = "run پیدا نشد.",
        ["Projects.Project.NotFound"] = "پروژه پیدا نشد.",
        ["Projects.Project.IdTaken"] = "پروژه‌ای با این شناسه وجود دارد.",
        ["Projects.Project.ReadOnly"] = "این پروژه از فایل تنظیمات می‌آید. آن را همان‌جا تغییر دهید.",
        ["Projects.Project.Invalid"] = "بعضی فیلدها معتبر نیستند.",
        ["Projects.Store.Unreadable"] = "فایل projects.json خوانده نمی‌شود. اول آن را درست یا حذف کنید.",
        ["Usage.Query.InvalidGroupBy"] = "groupBy باید یکی از day، month، model یا project باشد.",
        ["Usage.Query.InvalidRange"] = "بازه‌ی تاریخ معتبر نیست.",
        ["Usage.Query.InvalidTimeZone"] = "منطقه‌ی زمانی ناشناخته است.",
        ["Import.AlreadyRunning"] = "یک خواندن در حال انجام است.",
        ["Events.Body.Invalid"] = "بدنه یک رویداد schema 1 نیست.",
        ["Events.Project.Unknown"] = "شناسه‌ی پروژه در رویداد ناشناخته است.",
        ["Events.Kind.Unknown"] = "نوع رویداد ناشناخته است.",
        ["Library.Item.NotFound"] = "این مورد در کتابخانه نیست.",
        ["Library.File.TooLarge"] = "فایل برای نمایش خیلی بزرگ است.",
        ["Api.Unexpected"] = "خطایی در سرور رخ داد.",
        ["Api.NotFound"] = "چنین مسیری در API نیست.",
    };

    /// <summary>Persian only when the request prefers it; English in every other case.</summary>
    public static string Get(string code, IReadOnlyList<CultureInfo> cultures)
    {
        var wantsPersian = cultures.Count > 0
            && string.Equals(cultures[0].TwoLetterISOLanguageName, "fa", StringComparison.Ordinal);

        var messages = wantsPersian ? Fa : En;

        if (messages.TryGetValue(code, out var text))
        {
            return text;
        }

        return En.TryGetValue(code, out var fallback) ? fallback : code;
    }

    public static string Get(string code)
    {
        return Get(code, []);
    }

    /// <summary>Accept-Language, in header order; unknown tags are dropped.</summary>
    public static IReadOnlyList<CultureInfo> CulturesFrom(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return [];
        }

        var cultures = new List<CultureInfo>();

        foreach (var part in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tag = part.Split(';')[0].Trim();

            if (tag.Length == 0)
            {
                continue;
            }

            try
            {
                cultures.Add(CultureInfo.GetCultureInfo(tag));
            }
            catch (CultureNotFoundException)
            {
                // An unknown tag is ignored; the next one decides.
            }
        }

        return cultures;
    }
}
