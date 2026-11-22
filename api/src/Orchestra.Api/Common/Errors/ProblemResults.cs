using Microsoft.AspNetCore.Mvc;

namespace Orchestra.Api.Common.Errors;

public static class ProblemResults
{
    public static ProblemDetails From(Error error, HttpContext context)
    {
        var cultures = ErrorMessages.CulturesFrom(context.Request.Headers.AcceptLanguage.ToString());

        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = ErrorMessages.Get(error.Code, cultures),
        };

        problem.Extensions["code"] = error.Code;

        return problem;
    }
}
