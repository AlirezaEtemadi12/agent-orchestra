namespace Orchestra.Api.Modules.Projects.Features.StoreState;

public sealed record ProjectStoreStateDto(bool Readable, string? Error);

public sealed class StoreStateHandler(ProjectStore store)
{
    public ProjectStoreStateDto Handle()
    {
        var read = store.Read();

        return new ProjectStoreStateDto(read.Error is null, read.Error);
    }
}
