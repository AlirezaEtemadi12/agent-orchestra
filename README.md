# agent-orchestra

**English** | [فارسی](README.fa.md)

A gated multi-agent orchestrator for software work, and a dashboard that shows what it did.

A task never goes straight from a prompt to code. It passes four gates:

```
plan → independent review → read-back → approval → implementation (cheap model) → verify
```

- A **planner** writes a plan that decides everything; an implementer decides nothing.
- A reviewer from a **different model family** reviews the plan.
- A **read-back** makes the implementer restate the plan before it touches a file.
- **You** approve, explicitly. The result is handed over uncommitted.

## What is in this repository

| Folder | What |
|---|---|
| `engine/` | The orchestrator itself (PowerShell 5.1): chains of models per role, pools and quota, offline test suite against a mock agent. Projects call it through a ten-line shim. |
| `api/` | .NET 10 + SQLite. Imports runs and ledgers, receives live events, serves the UI. |
| `ui/` | Next.js 16 static export (shadcn/ui), Persian and English, light and dark. |
| `scripts/` | `publish.ps1`: builds the Windows app into `dist/`. |
| `.ai/` | Knowledge for any agent: architecture, decisions (ADRs), conventions. |
| `.orchestra/` | The orchestrated work: workflow, plans, previews, ideas. |

The dashboard shows runs per project, cost per day, month, model and project, model quality
(rounds to PASS, read-back value, verify FAIL rate, cache hit rate), plans, and a live board. It is
read-only: it never approves, implements or starts a run, it writes into a project only through setup
(ADR 0009), and a run works the same with the dashboard down.

<!-- Screenshots: add them under docs/images/ and link them here. -->

## Usage guide

### 1. Build the app

You need .NET 10 and Node 22 on Windows. From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish.ps1
```

It builds the UI once, publishes a single-file app, puts `wwwroot/`, `engine/` and `appsettings.json`
beside it, and zips the folder into `dist/Orchestra-win-x64.zip`. Open
`dist/Orchestra-win-x64/Orchestra.exe`: the dashboard opens in its own window on
`http://localhost:3500`, which only this computer can reach. A second launch says "Orchestra is already
open"; closing the window stops the app.

The app keeps its data in `%LOCALAPPDATA%\Orchestra\`: `orchestra.db`, `projects.json`, its report
token and the exported profiles.

### 2. Add a project

Open **Projects → New project** and type the project folder as Windows shows it, for example
`F:\Projects\MyApp`, or pick it with **Browse**, which opens the Windows folder dialog in the app
window. The form fills the id, the name, the runs folder and the repos, and you can change any of them.

**Continue** opens the setup checklist: one row per file or job the project needs, marked "will be
created", "will be added", "will be updated" or "already there". The knowledge rows (`AGENTS.md`,
`.ai/README.md`) are optional and can be unticked. Under the list are the build and test commands found
for each repo, one per line, editable.

**Set up** writes the files, registers and imports the project, and shows the result of every row. It is
safe to run again: every row reads "already there" and nothing is written. On an existing project,
**Check setup** on the projects list opens the same screen.

Two things may change in a project that already has them: `.orchestra/orchestrator.json` gets a `report`
block only when it has none, and the git-ignored `.orchestra/orchestrator.local.json` gets `enginePath`,
`profilesPath` and the report token. Nothing else is touched and nothing is committed. Reporting is
fire-and-forget: if the dashboard is down, the run still works the same way.

### 3. Run a task

Each command prints a summary that ends with a `next:` line naming the next command.

| Step | Command | What happens |
|---|---|---|
| Plan | `run.ps1 new T-APP-001 MyRepo "what to do"` | A planner writes the plan. A model from another family reviews it, and the implementer's model reads it back. |
| Approve | `run.ps1 approve T-APP-001` | You approve. The engine creates the branch. |
| Implement | `run.ps1 implement T-APP-001 1` | A cheaper model implements phase 1, which is left uncommitted. |
| Verify | `run.ps1 verify T-APP-001` | The engine runs the plan's gates and a verifier model. |
| Close | `run.ps1 close T-APP-001` | The engine files the plan. Committing and merging stay with you. |

`run.ps1 status T-APP-001` shows the state at any time. When a hop waits for a human (a manual adapter),
the engine writes the request into the run's `inbox/`. You answer it with `run.ps1 answer`. The commands,
roles and settings are described in [`engine/README.md`](engine/README.md).

### 4. Watch it

**Runs** lists each run, and **Runs board** shows them live. **Cost** splits spending by day, month,
model and project. **Model quality** shows rounds to PASS, read-back value and the verify FAIL rate.
**Getting started** in the menu shows two steps: the app is running, and the first project is set up
once its first event arrives.

### Run from source

You need .NET 10 and Node 22:

```powershell
cd ui; npm ci; npm run build; cd ..
```

```powershell
dotnet run --project api/src/Orchestra.Api
```

Open `http://localhost:3500` in a browser. In development **Browse** opens the in-app folder browser
instead of the Windows dialog. No environment variable is needed: the dashboard creates its own report
token and keeps it in `<data>/report-token` (plan 0009). A developer who edits `engine/` sets
`Orchestra:EnginePath` to that checkout in `appsettings.Local.json` beside the exe, so setup does not
repoint a project at the published copy.

### Tests

```powershell
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

The engine's offline suite runs in four parallel shards and takes about 7 minutes. The API and UI tests
are described in [`.ai/ci.md`](.ai/ci.md).

## Status

These are done: phase 0 (import, library, new project), phase 1 (live events), the engine move, profiles,
and the local desktop app that sets a project up by itself (plan 0012). Next are server-side tables
(plan 0010) and fewer `git` calls in the engine (plan 0011). The phases are in [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md) and the plan
states are in [`.orchestra/plans/`](.orchestra/plans/README.md). Agents start at [`AGENTS.md`](AGENTS.md).

This is a personal tool, built when there is time. Issues are welcome; there is no support promise.

## License

[MIT](LICENSE)
