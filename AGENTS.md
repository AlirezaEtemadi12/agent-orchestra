# Orchestra — Agent Entry Point

> Read this before touching anything. This file is a **pointer, not a rule book**: every rule lives in
> exactly one doc under `.ai/` or `.orchestra/`, and this file only links to it. If the two ever
> disagree, the linked doc wins — fix this one.
>
> Format: the vendor-neutral [agents.md](https://agents.md) convention. **Never create a `CLAUDE.md`.**

| | |
|---|---|
| **What** | A dashboard and shared engine for a gated multi-agent orchestrator (plan → review → read-back → approval → implement → verify), across projects: runs, cost, model quality, plans, settings |
| **Status** | Phases 0–1 done and merged: plans [0001](.orchestra/plans/0001-skeleton-and-import.md) (skeleton, import), [0002](.orchestra/plans/0002-library.md) (library), [0003](.orchestra/plans/0003-new-project.md) (new project) and [0004](.orchestra/plans/0004-live-events.md) (phase 1, live) and [0005](.orchestra/plans/0005-engine-move.md) (one engine; both projects call it through a shim) and [0006](.orchestra/plans/0006-profiles.md) (profiles) and [0011](.orchestra/plans/0011-fewer-git-calls.md) (one git call per repo per snapshot) and [0012](.orchestra/plans/0012-local-app-and-project-setup.md) (a Windows desktop app that sets a project up by itself) and [0007](.orchestra/plans/0007-move-projects-to-orchestra-layout.md) (both projects moved to `.orchestra/`; its last "Check setup" item is replaced by 0013's companion plans); [0013](.orchestra/plans/0013-workspace-plans/plan.md) (one plan per change, one folder per plan, one run across a workspace's repos; board by plan; closes 0007) is implemented on `alireza/workspace-plans`, not merged — phases in [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md), plan states in [`.orchestra/plans/README.md`](.orchestra/plans/README.md) |
| **Shape** | One repository: `api/` (.NET 10, SQLite), `ui/` (Next.js 16, static export served by the API, fa + en), `engine/` (the orchestrator itself, PowerShell 5.1); shipped as a desktop app — a Photino.NET window around the API and the exported UI, published by `scripts/publish.ps1` — [ADR 0001](.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md), [ADR 0009](.ai/decisions/0009-local-app-sets-projects-up.md) |
| **Folders** | `.ai/` = knowledge for any agent; `.orchestra/` = the orchestrated work (gates, skills, ideas, plans) — [ADR 0005](.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) |
| **Owner** | Alireza. Runs on their own Windows machine; the repository will be public |

## Start here

1. [`.ai/README.md`](.ai/README.md) — the index: doing X → read Y.
2. The newest [`.ai/context/session-*.md`](.ai/context/) — what the last session left open.
3. [`.orchestra/workflow.md`](.orchestra/workflow.md) — the four gates every task passes.
4. A plan handed to you: read that file in [`.orchestra/plans/`](.orchestra/plans/) first, and only the docs it lists.

## The rules that bite first

1. **Understanding, preview, plan; then ask, branch, hand over uncommitted** — [`workflow.md`](.orchestra/workflow.md).
2. **The orchestrator never depends on the dashboard.** A run works the same with the dashboard down;
   reporting to it is fire-and-forget — [ADR 0002](.ai/decisions/0002-runner-reports-dashboard-never-commands.md).
3. **Read-only first; writes only through setup.** The dashboard shows and compares; it does not approve,
   implement or start runs, and it writes into a project only through setup —
   [ADR 0003](.ai/decisions/0003-read-only-dashboard-first.md), [ADR 0005](.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md), [ADR 0009](.ai/decisions/0009-local-app-sets-projects-up.md).
4. **A rule change reviews the old rules** — merge, simplify or delete what it overlaps, in the same
   change ([`workflow.md`](.orchestra/workflow.md#rules-about-rules)).
5. **No secret and no personal path enters source.** Tokens, passwords and project paths come from
   environment variables or a git-ignored file; test fixtures are synthetic or anonymized.
6. **Persian text is never machine-translated, reflowed or retyped**; English text is written in its
   own catalogue — [ADR 0004](.ai/decisions/0004-persian-and-english.md).
