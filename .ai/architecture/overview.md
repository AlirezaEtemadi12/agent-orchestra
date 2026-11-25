# Architecture overview

> **AUTHORITATIVE** for how the pieces talk to each other. Decisions behind it:
> [ADR 0001](../decisions/0001-one-repo-dotnet-nextjs-sqlite.md),
> [0002](../decisions/0002-runner-reports-dashboard-never-commands.md),
> [0003](../decisions/0003-read-only-dashboard-first.md).

## The pieces

```
 Orchestra.exe — Photino.NET window (plan 0012)
┌───────────────────────────────────────────────────────────────────────────────┐
│ api/  ASP.NET Core (.NET 10) on http://localhost:3500                          │
│   /api/*  JSON                                                                 │
│   importer (phase 0) reads the project folders                                 │
│   EF Core → SQLite  (%LOCALAPPDATA%\Orchestra\orchestra.db)                     │
│   serves ui/ (the static export) at /                                          │
└───────────────────────────────────────────────────────────────────────────────┘
    ▲ POST api/events (report-only token)          ▲ phase 0: reads runs/, _ledger.md
    │                                              │
┌──────────────────────────────────────┐   ┌───────┴───────────────────────────────┐
│ <project>/.orchestra/run.ps1 shim    │   │ <project>/.orchestra/                 │
│   runs/..., plans/, knowledge        │   │   orchestrator.json, runs/, plans/    │
└──────────────────────────────────────┘   └───────────────────────────────────────┘
```

- **api/** — one ASP.NET Core app, a small **modular monolith** after Project B's Web: modules
  `Projects`, `Runs`, `Usage`, `Events`, `Profiles`, `Setup`, later `Settings`. `Setup` (order 15) owns
  `api/system/folders`, the folder browser; `Projects` owns the two setup routes that write into a project
  (plan 0012). EF Core with   SQLite; migrations in the repo.
- **ui/** — Next.js 16, **static export** (`output: 'export'`), served by the API: one process, one
  port, no Node at runtime. Stack and rules follow Project B's Ui (shadcn/ui from the official
  registry, Tailwind v4, TanStack Query, vertical slices, tokens only), in **fa (RTL) and en (LTR)**
  ([ADR 0004](../decisions/0004-persian-and-english.md)) — plan 0001 writes the trimmed rules in
  [`../ui/`](../ui/).
- **importer** (phase 0) — a hosted service in the API that scans each project's `runs/` folder and
  `_ledger.md` on start and on demand, and upserts into SQLite. Idempotent: re-importing changes nothing.
- **events** (phase 1) — `POST api/events`, a bearer token that opens only that route, and a
  `RunEvent` table the importer reads but never deletes ([ADR 0006](../decisions/0006-events-are-the-dashboards-own-record.md)).
  The token is configured or made and kept by `ReportTokenStore` in `<data>/report-token`
  ([ADR 0008](../decisions/0008-host-paths-and-own-report-token.md)). The posting side is written once,
  in `engine/lib/report.ps1`, which reads the token from the env variable or from the project's
  `orchestrator.local.json`; it is fire-and-forget — 2 s timeout, no retry, one warning line per run — so
  a run behaves the same with the dashboard down.
- **the window** (plan 0012) — `Orchestra.Desktop` builds the same host through `OrchestraHost.Create`,
  starts it, and shows `http://localhost:3500` in a Photino.NET window; the host's content root is the exe's
  folder, so `wwwroot/` and `engine/` beside it are found. Data is `%LOCALAPPDATA%\Orchestra\`. `Browse` in
  the project form asks the host for the Windows folder dialog through `window.external.sendMessage`; in a
  plain browser it falls back to the in-app folder browser.
- **setup** (plan 0012, [ADR 0009](../decisions/0009-local-app-sets-projects-up.md)) — `POST
  api/projects/setup/plan` (a dry run) and `/apply` (recomputes the plan, then writes): the project's
  `.orchestra/` files from `engine/templates/project/`, its `orchestrator.json` and git-ignored
  `orchestrator.local.json`, then the project entry and import. This is the only write into a project folder.
- **engine/** — the orchestrator itself, once for every project: `run.ps1`, `lib/` (agents, bus,
  gates, pools), `refresh-graph.ps1`, the offline suite and the conductor docs. A project keeps its
  `orchestrator.json`, its `runs/`, a git-ignored `orchestrator.local.json` with `enginePath` (and
  `profilesPath`), and a ten-line `run.ps1` shim that calls the engine with that config (plan 0005
  D10). The engine keeps no state: every setting comes
  from the project's `orchestrator.json`, and it never reads the dashboard. A project that names a
  `profile` gets the dashboard's export merged under its own file at every command (plan 0006).

## Data flow rules

1. **Files are the source of truth for runs** until phase 2. The database is a read model built
   from them; it can always be dropped and re-imported.
2. **The runner never reads from the dashboard** while a run is in flight. Settings reach it as an
   exported file (`<profilesPath>/<id>.json`), before the run starts (plan 0006).
3. **The dashboard writes into a project folder only through setup** (plan 0012), and only what §4 D7 of
   that plan allows. Everything else about a project stays the owner's, and the optional knowledge prompt
   is still an agent's job ([ADR 0005](../decisions/0005-ai-is-knowledge-orchestra-is-work.md)). The
   database indexes files; it is never their only copy.

## Hosting and security

| | Today |
|---|---|
| Access | `http://localhost:3500` only, in the app's window; `AllowedHosts` is the three loopback names |
| Sign-in | none (bound to localhost) |
| Runner → API | not needed (import reads files); the report-only bearer token comes from the project's `orchestrator.local.json` |
| Data | `%LOCALAPPDATA%\Orchestra\orchestra.db` |

Hosting — a server, HTTPS, sign-in from another machine — is an idea again
([`hosting.md`](../../.orchestra/ideas/hosting.md)) and needs a new ADR
([0009](../decisions/0009-local-app-sets-projects-up.md)).
