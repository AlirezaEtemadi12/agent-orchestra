# Plans

A plan is a spec an executor (often a cheaper model, possibly through the orchestrator) can follow
without deciding anything. The process is [`../workflow.md`](../workflow.md); the shape is
[`TEMPLATE.md`](TEMPLATE.md).

- One plan is one folder: `NNNN-<topic>/plan.md`, with its page at `NNNN-<topic>/preview.html` and its
  evidence at `NNNN-<topic>/evidence/` (git-ignored). The number is reserved when the folder is made
  ([ADR 0010](../../.ai/decisions/0010-plan-is-a-folder.md)). The flat plans 0001–0012 were deleted on
  2026-11-25 (0010 moved into its folder); git keeps them: `git show 361658b:.orchestra/plans/<file>`.
- A plan **stays where it is** for its whole life — there is no `done/` folder. Its header carries
  `Status` (`Draft` · `Approved` · `Running` · `Done` · `Dropped`) and the dates `Created`, `Updated`,
  `Done`; lists and the dashboard sort and filter on those. A `Done` plan also records the commit and
  merge in its header.
- The header names the author on a **`Planned by:`** line (`Claude Opus 5.5, 2026-11-19`): the
  orchestrator reads the author's model family from it and keeps the review on another family.
- One plan may change `api/`, `ui/`, `engine/` and `scripts/` together — one repo, one branch ([ADR 0001](../../.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md)).
- A change the plan needs in another repository (an orchestrator in `Project A` or
  `Project B`) is a **companion plan in that repo**, never an edit from here.

| # | Plan | Status | Created | Updated |
|---|---|---|---|---|
| [0010](0010-gridify-tables/plan.md) | Every growing table (runs, import log, library, profile versions) is a server-side Gridify table | Done | 2026-11-23 | 2026-11-25 |
| [0013](0013-workspace-plans/plan.md) | One plan per change, one folder per plan, one run across a workspace's repos; board by plan; close-plan and pick-plan skills; back and reload | Done | 2026-11-24 | 2026-11-25 |
| [0014](0014-tidy-plans/plan.md) | One plan shape in Orchestra (old flat plans deleted, 0010 moved); a closed card reads closed; card titles from the plan; sticky header | Done | 2026-11-25 | 2026-11-25 |
| [0015](0015-agent-files/plan.md) | Agent-specific files stay out of git (`.claude/skills/` excepted); the New project check asks only for what Orchestra needs; import without a runs folder; single-repo key without spaces; verify commands one folder down; no health card or row; card title before the import; import after a run's events; an unknown cost shows "—"; `branch -` is no branch; a run starts at its earliest time | Draft | 2026-11-25 | 2026-11-25 |
