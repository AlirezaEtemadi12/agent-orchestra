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
        ["Profiles.Profile.NotFound"] = "Profile not found.",
        ["Profiles.Profile.Invalid"] = "The profile is not valid: {detail}",
        ["Profiles.Reason.Required"] = "Say why you change it.",
        ["Profiles.Step.Locked"] = "The step {step} is always on and cannot be turned off.",
        ["Profiles.Revert.Current"] = "This is already the current version.",
        ["Profiles.Project.Unreadable"] = "The project's orchestrator.json cannot be read here: {detail}",
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
        ["Profiles.Profile.NotFound"] = "پروفایل پیدا نشد.",
        ["Profiles.Profile.Invalid"] = "پروفایل معتبر نیست: {detail}",
        ["Profiles.Reason.Required"] = "دلیل تغییر را بنویسید.",
        ["Profiles.Step.Locked"] = "مرحله‌ی {step} همیشه روشن است و نمی‌شود خاموشش کرد.",
        ["Profiles.Revert.Current"] = "این همین الان نسخه‌ی فعلی است.",
        ["Profiles.Project.Unreadable"] = "فایل orchestrator.json این پروژه اینجا خوانده نمی‌شود: {detail}",
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
    public static string Get(string code, IReadOnlyList<CultureInfo> cultures, string? detail = null)
    {
        var wantsPersian = cultures.Count > 0
            && string.Equals(cultures[0].TwoLetterISOLanguageName, "fa", StringComparison.Ordinal);

        var messages = wantsPersian ? Fa : En;

        var text = messages.TryGetValue(code, out var found)
            ? found
            : En.TryGetValue(code, out var fallback) ? fallback : code;

        if (string.IsNullOrEmpty(detail))
        {
            return text;
        }

        // The message names what is wrong; the detail says which file, step or path.
        return text
            .Replace("{detail}", detail, StringComparison.Ordinal)
            .Replace("{step}", detail, StringComparison.Ordinal);
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
