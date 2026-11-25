using Gridify;
using Orchestra.Api.Common.Paging;

namespace Orchestra.Api.Modules.Library.Features.ListLibrary;

/// <summary>Plan 0010 D12/D16: the library allow-list; the project name is the one extra key.</summary>
public static class LibraryTableMapping
{
    public static readonly IGridifyMapper<LibraryItemEntity> Mapper =
        TableMapper<LibraryItemEntity, LibraryItemDto>.Build(
            mapper => mapper.AddMap("projectName", item => item.Project.Name));
}
