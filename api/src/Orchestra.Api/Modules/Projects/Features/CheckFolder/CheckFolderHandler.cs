using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Modules.Projects.Features.CheckFolder;

public sealed class CheckFolderHandler(HostingInfo hosting)
{
    public FolderCheckDto Handle(string? rootPath)
    {
        return FolderCheck.Run(rootPath, hosting.Mapper);
    }
}
