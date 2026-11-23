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
`-p:ArtifactsPath=<repo>/artifacts/agent` instead of stopping their process — never take the runtime.

The browser checks (both languages, both themes, mobile width) are the **owner's**, not a gate the
implementer runs.

## Engine (Windows only, owner-run)

Outside the green table: it needs Windows PowerShell 5.1, no model and no network, but it takes 15+
minutes. Run it in the background with its output in a log and wait for the tally; never stop it on a
timeout.

| Where | Command | Must be |
|---|---|---|
| `engine/` | `powershell -NoProfile -File engine/tests/offline/run-offline.ps1` | all pass, count never drops |

The suite runs its whole set once per config in [`../engine/tests/offline/configs/`](../engine/tests/offline/)
(`number` and `slug` plan filing) and prints a total.

| When | Tests |
|---|---|
| after plan 0007 phase A — `close` by `plansDir` and `Status: Done` (2026-11-22) | 139 per config, 278 across `number` and `slug`; ~19 min per config |
| after plan 0006 — profiles merge in the engine (2026-11-22) | 133 per config, 266 across `number` and `slug`; ~19 min per config |
| after the Project A gaps in the engine — `**Branch:**` line, `contractDoc`, planner phase name (2026-11-22) | 127 per config, 254 across `number` and `slug` |
| after plan 0004 phase D — the reporter in the engine (2026-11-21) | 124 per config, 248 across `number` and `slug`; ~18 min per config |
| after plan 0005 phase A+B — engine move, both filing styles (2026-11-20) | 121 per config, 242 across `number` and `slug`; ~18 min per config |

## Baseline after plan 0007 phase A — the engine and the dashboard read `.orchestra/` (2026-11-22)

| Side | Tests |
|---|---|
| `api` | 248 |
| `ui` | 64 |
| `engine` (offline suite, Windows only — its own section above) | 139 per config |

### Earlier

| When | `api` | `ui` |
|---|---|---|
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
