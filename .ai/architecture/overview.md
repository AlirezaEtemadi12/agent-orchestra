# Architecture overview

> **AUTHORITATIVE** for how the pieces talk to each other. Decisions behind it:
> [ADR 0001](../decisions/0001-one-repo-dotnet-nextjs-sqlite.md),
> [0002](../decisions/0002-runner-reports-dashboard-never-commands.md),
> [0003](../decisions/0003-read-only-dashboard-first.md).

## The pieces

```
 Windows machine (where the repos are)                 Docker — local, or the personal server
┌──────────────────────────────────────┐              ┌───────────────────────────────────────┐
│ orchestrator/run.ps1 shim per project│  phase 1:    │ api/  ASP.NET Core (.NET 10)          │
│   writes runs/<id>/task.json,        │  POST events │   /api/*  JSON                        │
│   progress.json, _ledger.md, _pool   │ ───────────► │   importer (phase 0, reads files)     │
│                                      │  report-only │   EF Core → SQLite  (/data/orch.db)   │
│                                      │  token       │   serves ui/ static files at /        │
└──────────────────────────────────────┘              └───────────────────────────────────────┘
        ▲ phase 0: the API reads these folders (mounted read-only)       ▲
        └────────────────────────────────────────────────────────────────┘ browser
```

- **api/** — one ASP.NET Core app, a small **modular monolith** after Project B's Web: modules
  `Projects`, `Runs`, `Usage`, later `Settings`. EF Core with SQLite; migrations in the repo.
- **ui/** — Next.js 16, **static export** (`output: 'export'`), served by the API: one container, one
  port, no Node at runtime. Stack and rules follow Project B's Ui (shadcn/ui from the official
  registry, Tailwind v4, TanStack Query, vertical slices, tokens only), in **fa (RTL) and en (LTR)**
  ([ADR 0004](../decisions/0004-persian-and-english.md)) — plan 0001 writes the trimmed rules in
  [`../ui/`](../ui/).
- **importer** (phase 0) — a hosted service in the API that scans each project's `runs/` folder and
  `_ledger.md` on start and on demand, and upserts into SQLite. Idempotent: re-importing changes nothing.
- **events** (phase 1) — `POST api/events`, a bearer token that opens only that route, and a
  `RunEvent` table the importer reads but never deletes ([ADR 0006](../decisions/0006-events-are-the-dashboards-own-record.md)).
  The posting side is written once, in `engine/lib/report.ps1`; it is fire-and-forget — 2 s timeout, no
  retry, one warning line per run — so a run behaves the same with the dashboard down.
- **engine/** — the orchestrator itself, once for every project: `run.ps1`, `lib/` (agents, bus,
  gates, pools), `refresh-graph.ps1`, the offline suite and the conductor docs. A project keeps its
  `orchestrator.json`, its `runs/`, a git-ignored `orchestrator.local.json` with `enginePath`, and a
  ten-line `run.ps1` shim that calls the engine with that config (plan 0005
  [D10](../../.orchestra/plans/0005-engine-move.md)). The engine keeps no state: every setting comes
  from the project's `orchestrator.json`, and it never reads the dashboard.

## Data flow rules

1. **Files are the source of truth for runs** until phase 2. The database is a read model built
   from them; it can always be dropped and re-imported.
2. **The runner never reads from the dashboard** while a run is in flight. In phase 2 settings reach
   it as an exported file, before the run starts.
3. **The dashboard never writes to a project folder**; mounts are read-only. What a project lacks is
   created by the owner's agent from a prompt the dashboard shows
   ([ADR 0005](../decisions/0005-ai-is-knowledge-orchestra-is-work.md)). The database indexes files;
   it is never their only copy.

## Hosting and security

| | Local (phases 0–2) | Personal server (phase 3) |
|---|---|---|
| Access | `http://localhost:<port>` only | HTTPS behind the server's reverse proxy |
| Sign-in | none (bound to localhost) | one user, password or passkey |
| Runner → API | not needed (import reads files) | report-only bearer token, from an env var |
| Data | `data/orch.db` (git-ignored) | a Docker volume, backed up |

What leaves the Windows machine in phase 3 (metadata only, or plan and review text too) is decided in
that phase's plan.
