using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Runs;

namespace Orchestra.Api.Tests.Events;

/// <summary>D1: the report token opens one route. This is a reflection test on purpose — the token must
/// be attached per action, so a filter added to a base class or a controller-wide attribute would fail
/// it before a request was ever made.</summary>
public sealed class TokenScopeTests
{
    [Fact]
    public void Only_post_api_events_carries_the_report_token_filter()
    {
        var guarded = typeof(EventsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<ReportTokenFilterAttribute>(inherit: true).Any())
            .Select(method => method.Name)
            .ToList();

        guarded.Should().BeEquivalentTo(["Post"]);
    }

    [Fact]
    public void No_other_controller_action_carries_it()
    {
        var others = typeof(RunsController).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && type != typeof(EventsController))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<ReportTokenFilterAttribute>(inherit: true).Any())
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}")
            .ToList();

        others.Should().BeEmpty();
    }
}
