# Plans

A plan is a spec an executor (often a cheaper model, possibly through the orchestrator) can follow
without deciding anything. The process is [`../workflow.md`](../workflow.md); the shape is
[`TEMPLATE.md`](TEMPLATE.md).

- One plan is one folder: `NNNN-<topic>/plan.md`, with its page at `NNNN-<topic>/preview.html` and its
  evidence at `NNNN-<topic>/evidence/` (git-ignored). The number is reserved when the folder is made
  ([ADR 0010](../../.ai/decisions/0010-plan-is-a-folder.md)). Plans 0001–0012 keep their flat
  `NNNN-<topic>.md` name and their `../previews/` folder; every reader accepts both shapes.
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
| [0001](0001-skeleton-and-import.md) | Skeleton (api + ui + deploy), fa/en, and phase-0 import of both projects' runs and ledgers | Done | 2026-11-19 | 2026-11-19 |
| [0002](0002-library.md) | Library: plans (status, dates, copy prompt), previews, ideas, research, evidence | Done | 2026-11-19 | 2026-11-19 |
| [0003](0003-new-project.md) | New project: form, folder check, scaffold prompt for the owner's agent | Done | 2026-11-19 | 2026-11-20 |
| [0004](0004-live-events.md) | Live events from the runner; runs board, pools, latest line (§8 answered) | Done | 2026-11-19 | 2026-11-21 |
| [0005](0005-engine-move.md) | One engine in `engine/`; projects call it through a shim (§8 answered) | Done | 2026-11-19 | 2026-11-22 |
| [0006](0006-profiles.md) | Profiles and per-project overrides, exported to a file (§8 answered) | Done | 2026-11-19 | 2026-11-22 |
| [0007](0007-move-projects-to-orchestra-layout.md) | Move Project A and Project B to `.ai/` + `.orchestra/` (§8 answered; revised after 0005/0006); closed by 0013's companion plans | Done | 2026-11-19 | 2026-11-25 |
| [0008](0008-hosting.md) | Server mode: sign-in, HTTPS, sync, profile pull, backups — **Dropped**: the owner wants no server (2026-11-24) | Dropped | 2026-11-19 | 2026-11-24 |
| [0009](0009-easy-onboarding.md) | Adding a project by its Windows path: path map, folder browser, auto-fill, own report token, connect page, getting started | Done | 2026-11-23 | 2026-11-23 |
| [0010](0010-gridify-tables.md) | Every growing table (runs, import log, library, profile versions) is a server-side Gridify table | Draft | 2026-11-23 | 2026-11-23 |
| [0011](0011-fewer-git-calls.md) | One git call per repo per snapshot instead of four; the before/after check stays | Done | 2026-11-23 | 2026-11-24 |
| [0012](0012-local-app-and-project-setup.md) | A Windows desktop app (Photino.NET exe, no Docker) that sets a project up by itself: setup screen, templates, folder dialog | Done | 2026-11-24 | 2026-11-24 |
| [0013](0013-workspace-plans/plan.md) | One plan per change, one folder per plan, one run across a workspace's repos; board by plan; close-plan and pick-plan skills; back and reload | Done | 2026-11-24 | 2026-11-25 |
