namespace Orchestra.Api.Common.Errors;

/// <summary>Codes are <c>&lt;Module&gt;.&lt;Thing&gt;.&lt;Reason&gt;</c>.</summary>
public sealed record Error(string Code, int Status)
{
    public static Error RunNotFound => new("Runs.Run.NotFound", 404);

    public static Error ProjectNotFound => new("Projects.Project.NotFound", 404);

    public static Error ProjectIdTaken => new("Projects.Project.IdTaken", 409);

    public static Error ProjectReadOnly => new("Projects.Project.ReadOnly", 409);

    public static Error ProjectInvalid => new("Projects.Project.Invalid", 400);

    public static Error ProjectStoreUnreadable => new("Projects.Store.Unreadable", 409);

    public static Error InvalidGroupBy => new("Usage.Query.InvalidGroupBy", 400);

    public static Error InvalidRange => new("Usage.Query.InvalidRange", 400);

    public static Error InvalidTimeZone => new("Usage.Query.InvalidTimeZone", 400);

    public static Error ImportAlreadyRunning => new("Import.AlreadyRunning", 409);

    public static Error EventBodyInvalid => new("Events.Body.Invalid", 400);

    public static Error EventProjectUnknown => new("Events.Project.Unknown", 422);

    public static Error EventKindUnknown => new("Events.Kind.Unknown", 422);

    public static Error LibraryItemNotFound => new("Library.Item.NotFound", 404);

    public static Error LibraryFileTooLarge => new("Library.File.TooLarge", 413);

    public static Error Unexpected => new("Api.Unexpected", 500);

    public static Error NotFound => new("Api.NotFound", 404);
}
