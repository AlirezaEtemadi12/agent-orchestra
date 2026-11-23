# `engine/` — the orchestrator, once

The multi-agent, four-gate driver (plan → review → read-back → approval → implement → verify) that used
to live twice, in `Project ProjectB/orchestrator` and `Project ProjectA/orchestrator`. It lives here
now, once; each project keeps only its own settings.

## The engine has no state of its own

Everything a run needs comes from the project's `orchestrator.json`:

- `root` — the project folder whose repos the agents work in;
- `runsDir` — where `runs/<taskId>/` lives (relative to the config file's folder);
- `repos`, `verifyCommands`, workflow paths, roles, adapters, pools, `planFiling`, `description`;
- `plansDir` — where the project's plans live, workspace-relative with the `{repo}` token
  (`".orchestra/plans/{repo}"`), optional: when it is set a plan is closed by its `**Status:** Done`
  line instead of by moving it into a `done/` folder (plan 0007 D3).

The engine reads its own files from `$PSScriptRoot` (`lib/`, `refresh-graph.ps1`) and the project's from
the folder `-Config` points at. That separation is why it can be shared: nothing in a project's folder
is found by looking next to `run.ps1`.

## How a project calls it

Each project keeps:

```
.orchestra/                    ← plan 0007's name for the old orchestrator/ folder
├── orchestrator.json          ← the project's settings (this one is versioned)
├── orchestrator.local.json    ← { "enginePath": …, "profilesPath": … } (git-ignored)
├── run.ps1                    ← a 10-line shim
├── workflow.md                ← the gates, one file for the whole workspace (plan 0007 D7)
├── roles/<Repo>/              ← that repo's role files and its README index (D2)
├── prompts/<Repo>/            ← that repo's stage prompts (D2)
├── plans/<Repo>/              ← the repo's plans and plans/README.md; plans/TEMPLATE.md at the root
├── previews/  ideas/  research/  evidence/
├── CLOSE-PLAN.md              ← the project's close-plan checklist (its skill points at it)
└── runs/                      ← run state (git-ignored)
```

A workspace that has not run the plan 0007 companion still calls the folder `orchestrator/` and keeps
its plans, roles and prompts inside the repos' `.ai/`; nothing in the engine depends on either name —
the project's own `orchestrator.json` names every path (see `workflow` and `plansDir`).

and the same commands as before:

```powershell
powershell -File .orchestra/run.ps1 agents
powershell -File .orchestra/run.ps1 new T-AGENT-032 Agent "…"
```

The shim reads `enginePath` from `orchestrator.local.json` and calls
`<enginePath>\engine\run.ps1 -Config <this project>\.orchestra\orchestrator.json` (the shim passes its own
folder's config, whatever that folder is called). It refuses a `-Config` of its
own: a run always uses its project's config. Missing file or a path without `engine\run.ps1` → one red
line saying what to write, exit 1.

`orchestrator.local.json` is per machine and per project, and is never committed — a machine path never
enters a project's git. One function, `Get-EnginePath` in the shim, is the only place that knows how the
path is found (plan 0005 D10). It also holds `profilesPath`, the dashboard's profiles folder; the engine
reads it only when the project's `orchestrator.json` names a `profile` (plan 0006 D5).

## Plans and their folder (plan 0007)

With `plansDir` set, a plan is written once into `<plansDir>/NNNN-topic.md` (slug filing:
`<slug>.md`) and stays there; `close` looks for it there, by number or by slug, and accepts it only
when its header's `Status:` line says `Done` (`**Status:** Done`, `Status: **Done**` and an unbolded
`Status: Done` all count). Without `plansDir` nothing changes: the plan is filed into the repo's
`plansDoneDir` (`.ai/plans/done` by default) and that file is what `close` checks.

## Profiles (plan 0006)

A project opts in by adding `"profile": "default"` to its own `orchestrator.json`. At every command
`Import-OrchConfig` reads `<profilesPath>/<profile>.json` (the dashboard's export), merges the profile's
`config` under the project's whole file — key by key and recursively, arrays replace, null removes — and
validates the result. A missing `profilesPath`, or a missing export, is one line naming what to add or
copy. The effective config is recorded once per run in `runs/<id>/config.effective.json` and travels to
the dashboard in the close archive; a profile that moved on since gets one line, and the run goes on with
the current version. Only `workflow.steps.preview` is adjustable; the other six steps are always on.

## Updating the engine

`git pull` in this repository. Every project picks the change up at its next command; nothing is copied.

A project can pin an engine version later — plan 0006 gives `engine/VERSION` a reader. Today the file is
a marker only.

## What is here

| Path | |
|---|---|
| `run.ps1` | the command dispatcher and every command (`help`, `agents`, `new`, `approve`, `implement`, `verify`, `close`, …) |
| `lib/agents.ps1` | config, adapters, chains, sessions, `progress.json` |
| `lib/bus.ps1` | artifacts, transcripts, verdicts, findings, `task.json` |
| `lib/gates.ps1` | git checks, plan branch / number / slug, graph status |
| `lib/pool.ps1` | quota pools: slots, waiting, cooldown, exhaustion |
| `lib/report.ps1` | the dashboard's event reporter: fire-and-forget, off unless configured |
| `refresh-graph.ps1` | the free graph refresh `run.ps1 graph` calls |
| `tests/offline/` | the offline suite — a whole run against a mock agent, in temp folders |
| `docs/` | the conductor procedure (`CONDUCTOR.md`), review brief, working model |
| `docs/history/` | design notes and finished plans, kept as history — not as rules |
| `examples/` | a project's `orchestrator.json` and `orchestrator.local.json` |

## Reporting to the dashboard

The orchestrator tells [Project Orchestra](../../README.md) what it is doing — a run starting, every
review round's counts, a status change, the latest line of a hop, a pool slot taken or released, a ledger
block, a close. It is **fire-and-forget**: a two-second timeout, no retry, one `[report] dashboard not
reachable` line per run, and a run behaves exactly the same with the dashboard down
([ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md)). The dashboard never
sends anything back, so a run works the same either way.

Off unless a project asks for it. The block goes in that project's `orchestrator.json` (the owner pastes
it; no engine file names a project):

```json
"report": {
  "enabled": true,
  "url": "http://localhost:3500/api/events",
  "project": "<the dashboard's project id>",
  "tokenEnv": "ORCHESTRA_REPORT_TOKEN",
  "timeoutSec": 2,
  "progressEverySec": 10
}
```

The token is read from the named environment variable, never from a file. `enabled: false` or no block
means no request is made at all. `progressEverySec` is the throttle for the latest-line event (D5).

## Tests

```powershell
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

Windows PowerShell 5.1, no model calls, 15+ minutes: run it in the background with a log and wait for the
tally. It runs the whole suite once per config in `tests/offline/configs/` (`number` and `slug` plan
filing). See [`.ai/ci.md`](../.ai/ci.md) for its count, and
[`.ai/engine/conventions.md`](../.ai/engine/conventions.md) before changing an engine file.
