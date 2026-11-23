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

## Run the dashboard

You need .NET 10 and Node 22, or Docker. Open `http://localhost:3500` afterwards and start from
**Getting started** in the menu: it walks from "run the dashboard" to "first plan" and ticks each step
from what the dashboard really sees.

With Docker, copy `deploy/.env.example` to `deploy/.env` and set `ORCHESTRA_HOST_ROOT` to the folder that
holds your projects (e.g. `F:/Projects`). It is mounted read-only at `/host`, and the dashboard translates
the Windows paths you type into `/host/...` itself:

```
docker compose -f deploy/docker-compose.yml up --build
```

Without Docker — `dotnet run`, or IIS:

```
cd ui && npm ci && npm run build && cd ..
dotnet run --project api/src/Orchestra.Api
```

Then add a project by the path you see in Windows (`F:\Projects\MyApp`): the form fills the id, the runs
folder and the repos, and the Connect page hands you the `report` block and the `reportToken` key. No
environment variable is needed — the dashboard makes its own token and keeps it in `<data>/report-token`
(plan 0009). Under IIS, install the ASP.NET Core 10 Hosting Bundle, give the app pool read access to your
projects folder, and bind the site to `localhost` only until plan 0008 adds sign-in.

## Use the engine

A project keeps a small `run.ps1` shim and a git-ignored `orchestrator.local.json` with `enginePath`
pointing at this repository's `engine/`. Commands, roles and config are in
[`engine/README.md`](engine/README.md). The offline suite:

```
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

## Status

Phase 0 (import, library, new project), phase 1 (live events) and the engine move are done. Profiles
with per-project overrides are next. The phases are in [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md);
the plan states in [`.orchestra/plans/`](.orchestra/plans/README.md). Agents start at [`AGENTS.md`](AGENTS.md).

This is a personal tool, built when there is time. Issues are welcome; there is no support promise.

## License

[MIT](LICENSE)
