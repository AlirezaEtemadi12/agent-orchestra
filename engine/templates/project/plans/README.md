# Plans

A plan is a spec an executor (often a cheaper model, possibly through the orchestrator) can follow
without deciding anything. The process is [`../workflow.md`](../workflow.md); the shape is
[`TEMPLATE.md`](TEMPLATE.md).

- File name: `NNNN-<topic>.md`. The number is reserved when the preview is made, so the preview
  (`../previews/NNNN-<topic>/`) and the plan share it.
- A plan **stays where it is** for its whole life — there is no `done/` folder. Its header carries
  `Status` (`Draft` · `Approved` · `Running` · `Done` · `Dropped`) and the dates `Created`, `Updated`,
  `Done`; lists and the dashboard sort and filter on those. A `Done` plan also records the commit and
  merge in its header.
- The header names the author on a **`Planned by:`** line: the orchestrator reads the author's model
  family from it and keeps the review on another family.

| # | Plan | Status | Created | Updated |
|---|---|---|---|---|
