# Plans

A plan is a spec an executor (often a cheaper model, possibly through the orchestrator) can follow
without deciding anything. The process is [`../workflow.md`](../workflow.md); the shape is
[`TEMPLATE.md`](TEMPLATE.md).

- One plan is one folder: `NNNN-<topic>/plan.md`, with its page at `NNNN-<topic>/preview.html` and its
  evidence at `NNNN-<topic>/evidence/` (git-ignored). The number is the plan's identity and is reserved
  when the folder is made. Old flat plans (`NNNN-<topic>.md`) keep working; new ones are folders.
- A plan **stays where it is** for its whole life — there is no `done/` folder. Its header carries
  `Status` (`Draft` · `Approved` · `Running` · `Done` · `Dropped`) and the dates `Created`, `Updated`,
  `Done`; lists and the dashboard sort and filter on those. A `Done` plan also records the commit and
  merge in its header.
- The header names the author on a **`Planned by:`** line: the orchestrator reads the author's model
  family from it and keeps the review on another family.
- The header names the repos on a **`Repos:`** line, in the order their phases run. One name for a
  single-repo project; a plan that names none runs in the repo its run was started for.

| # | Plan | Status | Created | Updated |
|---|---|---|---|---|
