# Roadmap — phases

> No deadlines: this is the owner's own tool, built when there is time (often while waiting on a
> client). Each phase is useful on its own and leaves the orchestrator working exactly as before.
> Ideas without a phase: [`../../.orchestra/ideas/`](../../.orchestra/ideas/README.md).

| Phase | What | Changes the orchestrator? | Plans |
|---|---|---|---|
| **0** | Skeleton, **import**, library, new-project flow — the dashboard reads files the orchestrators already write | No | 0001 · 0002 · 0003 |
| **1** | **Live**: the runner reports events to the API as they happen | Yes — a fire-and-forget reporter | 0004 |
| **2** | **One engine**: the orchestrator moves into `engine/`; profiles and per-project overrides | Yes — the engine relocates | 0005 · 0006 · 0007 |

**Order (owner, 2026-11-20):** 0005 (engine move) runs **before** 0004 (live), so the reporter is
written once, in `engine/`; then 0004, 0006, 0007. Then 0009
(easy onboarding), [0010](../../.orchestra/plans/0010-gridify-tables/plan.md) (server-side Gridify tables),
0011 (one git call per repo per snapshot) and
0012 (a local app that sets a project up by
itself).

**No server (owner, 2026-11-24):** the dashboard runs on the same Windows machine as the engine, in the
desktop app of plan 0012. 0008 is `Dropped`; hosting is an idea
again ([hosting.md](../../.orchestra/ideas/hosting.md)) and would need a new ADR.

## Phase 0 — read what exists

Value from day one with **no risk to running work**. Runs locally (`dotnet run`, or the desktop app of
plan 0012).

| Plan | What |
|---|---|
| 0001 | Skeleton (`api/`, `ui/`, `deploy/`), fa + en, light + dark. Import of `_ledger.md` and the open runs' `task.json` from today's `orchestrator/runs`. Screens: runs per project (sessions, calls, cost), run detail, cost (day, month, model, project), model quality (reviewer rounds to PASS and final counts, read-back value, verify FAIL rate, violations and files outside the plan per implementer, time per stage, cache hit rate) |
| 0002 | **Library**: plans with status and dates and a "copy prompt for the conductor" button; previews, ideas, research and evidence of every registered project |
| 0003 | **New project**: a form (name, path, repos, profile), a check of the folder (`.ai/`, `.orchestra/`, `AGENTS.md`, `.claude/`, `.cursor/`) and a prompt for the owner's agent to create what is missing ([ADR 0005](../decisions/0005-ai-is-knowledge-orchestra-is-work.md)) |

Since 2026-11-19 both orchestrators' `close` writes `runs/_archive/<id>.json` (the run's `task.json`,
the review / read-back / verify texts and the session usage) before deleting `runs/<id>/`; the
importer reads it (plan 0001). Runs closed before that date keep only their ledger block. The archive
is temporary: once phase 1 stores the events, it can be turned off (`archive.enabled`).

## Phase 1 — live

The runner posts each event (stage start/end, verdict, pool wait, usage) to `POST /api/events` with a
**report-only token**. **Fire-and-forget**: a short timeout, no retry loop, and a failure never
changes the run; events missed while the dashboard is down are lost, and the ledger remains the
back-fill. Screens: a runs board with lanes *Running / Needs you / Done / Failed*, pools and quota
(who holds each slot, which pool is exhausted until when), the hop's latest line on its card.

## Phase 2 — one engine

- The orchestrator (`run.ps1`, `lib/`, tests, prompts) moves into `engine/`; the offline suite moves
  with it.
- **Profiles** in Orchestra hold the shared defaults (chains, tiers, pools, reviewer, read-back,
  workflow steps, model catalogue with live prices); each project overrides them in its own
  `orchestrator.json` (key `profile`; plan 0006). Profile changes are versioned with who, when and why, and exported to the
  file the engine reads — the engine never needs the network ([ADR 0002](../decisions/0002-runner-reports-dashboard-never-commands.md)).
- Project A and Project B move from `orchestrator/` to `.ai/` + `.orchestra/` — a **companion plan
  in each of those repos** ([ADR 0005](../decisions/0005-ai-is-knowledge-orchestra-is-work.md)).
- A third project is the first real test of profiles.

## Later — Linux and macOS

Owner, 2026-11-24: Windows first; multi-platform comes after plan 0012. Plan 0012's app adds nothing
Windows-only (Photino.NET runs on all three), so the work is in the engine and the publish script: the
engine on PowerShell 7 (`pwsh`), a shim that picks `pwsh` or `powershell`, the offline suite on Linux,
then `linux-x64` and `osx-arm64` builds and project setup on those systems. Details:
[`engine-cross-platform.md`](../../.orchestra/ideas/engine-cross-platform.md).

## Later — Project B on plan folders

Owner, 2026-11-25: after Orchestra and Project A run on plan folders, Project B moves the same way —
`NNNN-<topic>/` folders numbered per workspace, the three skills, `plansDir` without `{repo}`. Its numbers
are per repo today and collide under one numbering, so its open plans are renumbered then. Both
preconditions held on 2026-11-25; the move is The89Group's plan 0164 (`.orchestra/plans/0164-orchestra-layout/`
there, Draft): Agent's numbers stay, Ui 0017 and Web 0033 become 0166 and 0165.
