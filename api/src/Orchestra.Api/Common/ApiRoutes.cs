namespace Orchestra.Api.Common;

public static class ApiRoutes
{
    public const string Base = "api";

    public const string Controller = Base + "/[controller]";

    /// <summary>Plan 0009 D17: the setup routes (<c>api/system/hosting</c>, <c>api/system/folders</c>).
    /// A literal "api" in a route is a bug.</summary>
    public const string System = Base + "/system";
}
