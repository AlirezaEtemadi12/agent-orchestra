# CI and the phase gates

> **AUTHORITATIVE** for what "green" means. The plan's phases each end on this gate; the counts below
> are the baseline the next phase must not drop under.

## Gates

| Where | Command | Must be |
|---|---|---|
| `api/` | `dotnet build Orchestra.slnx` | 0 warnings, 0 errors |
| `api/` | `dotnet test Orchestra.slnx` | all pass, count never drops |
| `ui/` | `npm run typecheck` | clean (`next typegen && tsc --noEmit`) |
| `ui/` | `npm run lint` | clean |
| `ui/` | `npm run test` | all pass, count never drops |
| `ui/` | `npm run build` | static export succeeds, every route prerendered |

If the owner's `dotnet run` holds `bin/Debug`, build and test with
`-p:ArtifactsPath=$env:TEMP/orchestra-build` (outside the repo) instead of stopping their process — never take the runtime.

The browser checks (both languages, both themes, mobile width) are the **owner's**, not a gate the
implementer runs.

## Engine (Windows only, owner-run)

Outside the green table: it needs Windows PowerShell 5.1, no model and no network, and takes about
7 minutes. Run it in the background with its output in a log and wait for the tally; never stop it on a
timeout.

| Where | Command | Must be |
|---|---|---|
| `engine/` | `powershell -NoProfile -File engine/tests/offline/run-offline.ps1` | all pass, count never drops |

The suite runs once per config in [`../engine/tests/offline/configs/`](../engine/tests/offline/) — one
config since 2026-11-23 (`number`; the close checks set both filing styles themselves, so a `slug` pass
re-ran everything to test nothing new). Its scenario sections run in four child processes at once
(`-Shards`, contiguous blocks); a section that fails runs once more alone, and the report names any check
that passed only on that rerun (timing under load). It prints a total and the wall time.

| When | Tests |
|---|---|
| after plan 0012 — project templates, no hosting (2026-11-24) | 146; 289 s |
| after plan 0011 — one git call per repo per snapshot (2026-11-24) | 142; 25 git calls per `new` (was 61); 320 s |
| suite sped up — one config, four shards, workspaces copied from one template (2026-11-23) | 140; ~7 min (was ~37 for two configs) |
| after plan 0009 — the report token from `orchestrator.local.json` (2026-11-23) | 140 per config |
| after plan 0007 phase A — `close` by `plansDir` and `Status: Done` (2026-11-22) | 139 per config, 278 across `number` and `slug`; ~19 min per config |
| after plan 0006 — profiles merge in the engine (2026-11-22) | 133 per config, 266 across `number` and `slug`; ~19 min per config |
| after the Project A gaps in the engine — `**Branch:**` line, `contractDoc`, planner phase name (2026-11-22) | 127 per config, 254 across `number` and `slug` |
| after plan 0004 phase D — the reporter in the engine (2026-11-21) | 124 per config, 248 across `number` and `slug`; ~18 min per config |
| after plan 0005 phase A+B — engine move, both filing styles (2026-11-20) | 121 per config, 242 across `number` and `slug`; ~18 min per config |

## Baseline after plan 0010 phase D — the rule (2026-11-25)

| Side | Tests |
|---|---|
| `api` | 345 |
| `ui` | 106 |
| `engine` (offline suite, Windows only — its own section above) | 146 (not touched) |

## Baseline after plan 0010 phase C — the other tables (2026-11-25)

| Side | Tests |
|---|---|
| `api` | 345 |
| `ui` | 106 |
| `engine` (offline suite, Windows only — its own section above) | 146 (not touched) |

## Baseline after plan 0010 phase B — the Gridify UI base and Runs (2026-11-25)

| Side | Tests |
|---|---|
| `api` | 327 |
| `ui` | 103 |
| `engine` (offline suite, Windows only — its own section above) | 146 (not touched) |

## Baseline after plan 0010 phase A — the Gridify API base and Runs (2026-11-25)

| Side | Tests |
|---|---|
| `api` | 327 |
| `ui` | 85 |
| `engine` (offline suite, Windows only — its own section above) | 146 (not touched) |

## Baseline after plan 0014 — one plan shape, two board fixes, a header that stays (2026-11-25)

| Side | Tests |
|---|---|
| `api` | 313 |
| `ui` | 85 |
| `engine` (offline suite, Windows only — its own section above) | 146 (not touched) |

## Baseline after plan 0012 — the local app sets projects up (2026-11-24)

| Side | Tests |
|---|---|
| `api` | 300 |
| `ui` | 83 |
| `engine` (offline suite, Windows only — its own section above) | 146 |

## Baseline after plan 0009 — easy onboarding (2026-11-23)

| Side | Tests |
|---|---|
| `api` | 269 |
| `ui` | 77 |
| `engine` (offline suite, Windows only — its own section above) | 140 |

## Baseline after plan 0007 phase A — the engine and the dashboard read `.orchestra/` (2026-11-22)

| Side | Tests |
|---|---|
| `api` | 248 |
| `ui` | 64 |
| `engine` (offline suite, Windows only — its own section above) | 139 per config |

### Earlier

| When | `api` | `ui` |
|---|---|---|
| after plan 0012 — the local app sets projects up (2026-11-24) | 300 | 83 |
| after plan 0009 — easy onboarding (2026-11-23) | 269 | 77 |
| after plan 0006 — profiles (2026-11-22) | 246 | 64 |
| after plan 0004 phase D — the reporter in the engine (2026-11-21) | 217 | 56 |
| after plan 0004 phase C — the board and pools screens (2026-11-21) | 217 | 56 |
| after plan 0004 phase A — events: the endpoint and the store (2026-11-20) | 217 | 56 |
| after plan 0004 phase B — board and pools API (2026-11-21) | 217 | 49 |
| after plan 0005 — engine move, no api/ui change (2026-11-20) | 176 | 49 |
| after plan 0003 phase C — projects: the screens (2026-11-20) | 176 | 49 |
| after plan 0002 phase C — library: the screens (2026-11-19) | 147 | 35 |
| after plan 0002 phase B — library: the API (2026-11-19) | 147 | 22 |
| after plan 0002 phase A — library: config and indexing (2026-11-19) | 123 | 22 |
| after plan 0001 phase E — skeleton, import and the four screens (2026-11-19) | 68 | 22 |

Update this table at the end of every phase with the real numbers from the gate output.
