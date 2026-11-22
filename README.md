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

You need .NET 10 and Node 22, or Docker.

Copy `api/src/Orchestra.Api/appsettings.Local.example.json` to `appsettings.Local.json` (git-ignored)
and fill in your projects' runs folders.

```
cd ui && npm ci && npm run build && cd ..
dotnet run --project api/src/Orchestra.Api
```

Or with Docker (copy `deploy/.env.example` to `deploy/.env` and fill it in first):

```
docker compose -f deploy/docker-compose.yml up --build
```

Open `http://localhost:3500`. The projects' folders are mounted read-only.

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
