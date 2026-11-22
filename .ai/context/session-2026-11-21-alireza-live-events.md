# Session 2026-11-21 — plan 0004 Live, phases A–D

**Repository state:** branch `alireza/live-events`. Phase A `d0bbd39`, phase B `e06c141`, phase C
`ed99ef2`, phase D `fc40d6d`; merged as `fb217bc` into `alireza/skeleton-and-import`.

## What changed

Plan: [`../plans/0004-live-events.md`](../../.orchestra/plans/0004-live-events.md).

- **A — endpoint and store.** `OrchestraOptions.ReportToken`, `Modules/Events/{RunEventEntity,EventKind,EventEnvelope}`,
  `ReportTokenFilter`, `Features/ReceiveEvent/*`, `EventsController` (Order 70), `EventRetentionService`,
  `RunEventConfiguration` + migration, `RunSource.Events`, ADR 0006, `.env.example` and
  `appsettings.Local.example.json`.
- **B — merge, board and pools API.** `RunAssembler` reads events into non-final `StageResult` rows and
  builds event-only runs; `Features/Board/{BoardDtos,BoardHandler}`, `Features/Pools/{PoolDtos,PoolsHandler}`,
  `GET api/runs/board` on `RunsController`, `PoolsController` for `GET api/pools` (D12).
- **C — screens.** `ui/src/features/board/**`, `ui/src/features/pools/**`, pages `/{locale}/board` and
  `/{locale}/pools`, `FEATURE_KEYS`/registry/messages, and the live block of the run detail page.
- **D — reporter in the engine.** New `engine/lib/report.ps1`; hooks in `engine/lib/{bus,agents,pool}.ps1`
  and `engine/run.ps1` (dot-source, `$script:ReportCtx`, `run.created`, six `Add-History -Ctx` calls,
  `ledger.written`, `run.closed`); offline-suite cases `T-RP1`, `T-RP-003`, `T-RP2`; `engine/VERSION`
  1.1.0; the `report` block in `engine/examples/orchestrator.json`; `engine/README.md` "Reporting to the
  dashboard".

## Verified

- `dotnet build Orchestra.slnx` 0 warnings / 0 errors; `dotnet test Orchestra.slnx` **217/217**
  (`-p:ArtifactsPath=<repo>/artifacts/agent`, the owner's `dotnet run` was never stopped).
- `ui`: `npm run typecheck`, `npm run lint`, `npm run test` **56/56**, `npm run build` static export with
  the board and pools routes.
- `powershell -NoProfile -File engine/tests/offline/run-offline.ps1` — **124/124 with
  `number.orchestrator.json` and 124/124 with `slug.orchestrator.json`**, exit 0. Log:
  [`evidence/0004-live-events/suite-phase-d-run3.log`](../../evidence/0004-live-events/).
- Every edited PowerShell file parses (Parser API); every new JSON file parses.
- A manual pool check (`.orchestra/evidence/0004-live-events/pool-check.ps1`): a holder script with only
  `lib/pool.ps1` dot-sourced still acquires, and a dead dashboard costs one warning line and ~1 s.

## Not verified

- The owner's checks in «نتیجه» of the plan: a real run reporting into the dashboard, the lanes moving,
  pools with two runs, and the dashboard-down run. Phase D's `report` block still has to be pasted into
  each project's `orchestrator.json`, with the token in `ORCHESTRA_REPORT_TOKEN`.
- The board and pools screens were built and unit-tested with props, not opened in a browser.
- Plan 0005 phase C (the shim in the two projects) is still open; D11 needs the owner to confirm that no
  `run.ps1` of that project is executing before the swap.

## Deviations

- **Circuit breaker in the reporter.** §6.6 says one warning line per run; the first delivery of that
  took ~12 s across the suite's runs. The process now stops trying after the first failure, so a run
  pays at most one 2 s timeout and the warning is still printed once.
- **`RunAssembler.Build` takes the events** and hands `AddEventStage` a `HashSet<StageResultEntity>` of
  the rows that came from `task.json`, so an event replaces the matching `task.json` history row without
  touching the ledger's final rows.
- **`BoardHandler` builds cards from event groups**, not from the `Run` table, so an event-only run
  shows up before any import. `RunKey` is nullable for that reason.
- **`pools` carries `unknown: bool` per slot** instead of a new table (D10): a slot whose last event is
  older than 6 h is shown as unknown, never as held.
- **The engine hooks are guarded** with `if (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue)`.
  The offline suite and the pool holder scripts dot-source `lib/{bus,agents,pool}.ps1` on their own; without
  the guard those files fail on a function that was never loaded. This was the cause of the first run's
  11 FAILs (`T-RP2` plus `P-2`–`P-12`).
- **`ui/next-env.d.ts` is left out of the commits** — `next dev` writes it; it stays dirty in the working
  tree. The owner's `ui/ui.code-workspace` was committed, and `.vs/` went into `.gitignore` instead
  (a 968 K binary cache).
