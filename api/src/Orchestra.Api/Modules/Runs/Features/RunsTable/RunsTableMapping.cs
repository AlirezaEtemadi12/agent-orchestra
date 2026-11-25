using Gridify;
using Orchestra.Api.Common.Paging;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Runs.Features.RunsTable;

/// <summary>Plan 0010 D6/D16: the runs table's allow-list, plus <c>costUsd</c> for ordering only.</summary>
/// <remarks>Gridify cannot make a key sort-only, so a hand-typed <c>filter=costUsd&gt;…</c> is accepted and
/// compares <c>CostMicros</c>, not dollars; the UI never offers a cost filter.</remarks>
public static class RunsTableMapping
{
    public static readonly IGridifyMapper<RunEntity> Mapper = TableMapper<RunEntity, RunListItemDto>.Build(
        mapper => mapper.AddMap("costUsd", run => run.CostMicros));
}
