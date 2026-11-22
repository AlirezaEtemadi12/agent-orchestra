using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orchestra.Api.Common;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Tests.Events;

public sealed class ReportTokenFilterTests
{
    private const string Token = "s3cret-report-token";

    [Fact]
    public async Task A_missing_header_is_401_with_an_empty_body()
    {
        var (filter, context, ran) = Arrange(header: null, configured: Token);

        await filter.OnActionExecutionAsync(context, Next(ran));

        context.Result.Should().BeOfType<UnauthorizedResult>();
        ran.Value.Should().BeFalse();
    }

    [Fact]
    public async Task A_wrong_token_is_401()
    {
        var (filter, context, ran) = Arrange("Bearer not-the-token", Token);

        await filter.OnActionExecutionAsync(context, Next(ran));

        context.Result.Should().BeOfType<UnauthorizedResult>();
        ran.Value.Should().BeFalse();
    }

    [Fact]
    public async Task A_header_without_the_bearer_scheme_is_401()
    {
        var (filter, context, ran) = Arrange(Token, Token);

        await filter.OnActionExecutionAsync(context, Next(ran));

        context.Result.Should().BeOfType<UnauthorizedResult>();
        ran.Value.Should().BeFalse();
    }

    [Fact]
    public async Task The_right_token_passes_and_the_action_runs()
    {
        var (filter, context, ran) = Arrange($"Bearer {Token}", Token);

        await filter.OnActionExecutionAsync(context, Next(ran));

        context.Result.Should().BeNull();
        ran.Value.Should().BeTrue();
    }

    [Fact]
    public async Task No_token_configured_means_the_feature_is_off_and_the_route_is_404()
    {
        var (filter, context, ran) = Arrange($"Bearer {Token}", configured: null);

        await filter.OnActionExecutionAsync(context, Next(ran));

        context.Result.Should().BeOfType<NotFoundResult>();
        ran.Value.Should().BeFalse();
    }

    private static (ReportTokenFilter Filter, ActionExecutingContext Context, StrongBox<bool> Ran) Arrange(
        string? header,
        string? configured)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<OrchestraOptions>>(
            Options.Create(new OrchestraOptions { ReportToken = configured }));

        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        if (header is not null)
        {
            http.Request.Headers.Authorization = header;
        }

        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(
            action,
            [],
            new Dictionary<string, object?>(StringComparer.Ordinal),
            controller: new object());

        return (new ReportTokenFilter(), context, new StrongBox<bool>());
    }

    private static ActionExecutionDelegate Next(StrongBox<bool> ran)
    {
        return () =>
        {
            ran.Value = true;

            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                controller: new object()));
        };
    }
}
