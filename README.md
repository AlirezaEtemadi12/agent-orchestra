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
| `deploy/` | Dockerfile and `docker-compose.yml`. |
| `.ai/` | Knowledge for any agent: architecture, decisions (ADRs), conventions. |
| `.orchestra/` | The orchestrated work: workflow, plans, previews, ideas. |

The dashboard shows runs per project, cost per day, month, model and project, model quality
(rounds to PASS, read-back value, verify FAIL rate, cache hit rate), plans, and a live board. It is
read-only: it never approves, implements, or writes into a project folder, and a run works the same
with the dashboard down.

<!-- Screenshots: add them under docs/images/ and link them here. -->

## Usage guide

### 1. Start the dashboard (Docker)

You need Docker Desktop. Do this once: copy `deploy/.env.example` to `deploy/.env` and fill in two folders.

| Key | What |
|---|---|
| `ORCHESTRA_HOST_ROOT` | The folder that holds your projects, such as `F:/Projects`. It is mounted read-only at `/host`. |
| `ORCHESTRA_PROFILES_DIR` | Where the dashboard exports profiles. The default, `./profiles`, is next to the compose file. |

Then, from the repository root:

```powershell
docker compose -f deploy/docker-compose.yml up --build -d
```

Open `http://localhost:3500`. The port is bound to `127.0.0.1` only. Other commands you will need:

```powershell
docker compose -f deploy/docker-compose.yml logs -f
```

```powershell
docker compose -f deploy/docker-compose.yml down
```

To update after a `git pull`, run the same `up --build -d` again. The database lives in the
`orchestra-data` volume, so it survives both `down` and a rebuild.

### 2. Add a project

In the menu, open **Projects → New project** and type the project's folder exactly as Windows shows it, for example
`F:\Projects\MyApp`. You can also pick it with **Browse**. The form fills the id, the name, the runs
folder and the repos, and you can change any of them before you save. The dashboard translates the Windows
path to `/host/...` itself. It only reads the project folder and never writes into it.

### 3. Connect the project's engine

A project calls the engine through `.orchestra/run.ps1`, a ten-line shim. Next to it is
`.orchestra/orchestrator.local.json`, which is git-ignored and specific to each machine:

```json
{ "enginePath": "F:/Projects/Orchestra", "reportToken": "<from the Connect page>" }
```

Open the project's **Connect** page. Copy the `report` block from it into the project's
`orchestrator.json`, and copy the `reportToken` from it into the local file above. Check the setup:

```powershell
powershell -File .orchestra/run.ps1 agents
```

When the first event reaches the dashboard, the Connect page shows **Connected**. Reporting is
fire-and-forget: if the dashboard is down, the run still works the same way.

### 4. Run a task

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

### 5. Watch it

**Runs** lists each run, and **Runs board** shows them live. **Cost** splits spending by day, month,
model and project. **Model quality** shows rounds to PASS, read-back value and the verify FAIL rate. **Getting started** in the
menu ticks each step above from what the dashboard really sees.

### Without Docker

You need .NET 10 and Node 22:

```powershell
cd ui; npm ci; npm run build; cd ..
```

```powershell
dotnet run --project api/src/Orchestra.Api
```

No environment variable is needed. The dashboard creates its own report token and keeps it in
`<data>/report-token` (plan 0009). Under IIS, install the ASP.NET Core 10 Hosting Bundle, give the app pool
read access to your projects folder, and bind the site to `localhost` only until plan 0008 adds sign-in.

### Tests

```powershell
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

The engine's offline suite runs in four parallel shards and takes about 7 minutes. The API and UI tests
are described in [`.ai/ci.md`](.ai/ci.md).

## Status

These are done: phase 0 (import, library, new project), phase 1 (live events), the engine move, profiles,
and adding a project by its Windows path. Next are server-side tables (plan 0010) and fewer `git` calls
in the engine (plan 0011). The phases are in [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md) and the plan
states are in [`.orchestra/plans/`](.orchestra/plans/README.md). Agents start at [`AGENTS.md`](AGENTS.md).

This is a personal tool, built when there is time. Issues are welcome; there is no support promise.

## License

[MIT](LICENSE)
