using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;

namespace Orchestra.Api.Modules.Events;

/// <summary>D1/D2: the report token opens <c>POST api/events</c> and nothing else. A missing or wrong
/// token answers 401 with an empty body (nothing to probe); a server with no token configured answers
/// 404, as if the feature did not exist. The comparison is constant time over SHA-256 hashes, and the
/// token never reaches a log line.</summary>
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
        var options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<OrchestraOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(options.ReportToken))
        {
            context.Result = new NotFoundResult();
            return;
        }

        var header = context.HttpContext.Request.Headers.Authorization.ToString();

        if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || !Matches(header[Prefix.Length..].Trim(), options.ReportToken))
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
