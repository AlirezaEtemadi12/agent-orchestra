using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Orchestra.Api.Modules.Events;

/// <summary>D1/D2: the report token opens <c>POST api/events</c> and nothing else. The token comes from
/// <see cref="ReportTokenStore"/> — configured, saved, or generated on first use (plan 0009 D6), so there
/// is no "feature off" branch any more. A missing or wrong token answers 401 with an empty body (nothing
/// to probe). The comparison is constant time over SHA-256 hashes, and the token never reaches a log line.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReportTokenFilterAttribute : ServiceFilterAttribute
{
    public ReportTokenFilterAttribute()
        : base(typeof(ReportTokenFilter))
    {
    }
}

public sealed class ReportTokenFilter : IAsyncActionFilter
{
    private const string Prefix = "Bearer ";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tokens = context.HttpContext.RequestServices.GetRequiredService<ReportTokenStore>();
        var expected = tokens.Get();
        var header = context.HttpContext.Request.Headers.Authorization.ToString();

        if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || !Matches(header[Prefix.Length..].Trim(), expected))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    private static bool Matches(string given, string expected)
        => CryptographicOperations.FixedTimeEquals(Hash(given), Hash(expected));

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
