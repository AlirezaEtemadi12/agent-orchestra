namespace Orchestra.Api.Modules.Projects.Features.CheckFolder;

public sealed class CheckFolderHandler
{
    public FolderCheckDto Handle(string? rootPath)
    {
        return FolderCheck.Run(rootPath);
    }
}
