# 0009 — The local app sets a project up

**Status:** Accepted — 2026-11-24 (plan 0012's gate 2)
**Date:** 2026-11-24
**Decided by:** Alireza, in the chat of 2026-11-24 ("no server", "simple for me first", "adding a project
must do every step itself")

## Context

Orchestra ran as a container on the same Windows machine, with the owner's folders mounted read-only and a
path map translating host paths to container paths (ADR 0008). Every new project then cost a compose edit, a
`deploy/.env` line, a runs folder typed by hand, a token pasted in three places, and a scaffold prompt run
through another agent before the dashboard could see the project. The owner wanted no server at all: the app
should run where the engine and the projects already are.

## Decision

- **Local only.** Docker, `deploy/`, the path map (`Orchestra:PathMap`, `Orchestra:PublicUrl`), the container
  detection and the connect page are gone. Paths are used as typed; the dashboard and the engine share one
  machine. Orchestra is a Photino.NET window around the same API and the exported UI, published by
  `scripts/publish.ps1`; a second copy answers "Orchestra is already open" and exits.
- **Its data is local too.** The database, `projects.json`, `report-token` and `profiles/` live in
  `%LOCALAPPDATA%\Orchestra\` by default (an empty `DatabasePath` resolves there); `dotnet run` in
  development keeps `data/orchestra.db`.
- **Setup is one feature.** `POST api/projects/setup/plan` (dry run, never writes) and
  `POST api/projects/setup/apply` (recomputes the plan server-side, then executes it) turn a folder into a
  registered, connected project: `.orchestra/` from the templates in `engine/templates/project/`, a
  `git init` in an empty `.orchestra/` when the root is not a repo, `orchestrator.json` with a `report` block
  and the detected build and test commands, `orchestrator.local.json` with `enginePath`, `profilesPath` and
  the report token, and the project entry plus its import.
- **The write policy is narrow.** A file is created only when it is missing. Exactly two existing files may
  change, each shown as its own row: `orchestrator.json` gets a `report` block only when it has none (the
  rest of the file byte-identical), and `orchestrator.local.json` gets those three keys. Nothing is deleted,
  nothing is committed, and no other handler gains a write into a project folder.
- **The engine folder is found, not asked for.** `Orchestra:EnginePath` wins; otherwise the app walks up from
  its content root for `engine/run.ps1`. The report URL is the first loopback address in `Urls` plus
  `/api/events`.
- **Setup is reachable only from Orchestra's own page.** `AllowedHosts` is loopback; both setup routes
  require an `Origin` header whose host is `localhost`, `127.0.0.1` or `[::1]` on the request's port, and
  refuse a root that is not a folder, a drive root, a system or data folder, or Orchestra's own folder.

## Consequences

- The token is written by the server into the project's git-ignored `orchestrator.local.json`; no endpoint
  returns it, and the connect endpoint is deleted.
- [Plan 0008](../../.orchestra/plans/0008-hosting.md) (hosting, sign-in, HTTPS) is `Dropped`. Hosting is an
  idea again ([`hosting.md`](../../.orchestra/ideas/hosting.md)) and needs a new ADR: this one assumes
  loopback, so a non-loopback `Urls` makes setup refuse every request.
- The scaffold prompt shrinks to the knowledge part (`.ai/`, `AGENTS.md`); the config is written by the app.
- `.ai/`, `AGENTS.md` and the workflow a new project starts with are the templates' text, not an agent's
  reading of the code; the optional prompt fills them afterwards.
- Workspace `.orchestra/` is its own repository when the root is not one, so runs and secrets stay out of
  every project repo; the root `.gitignore` is never touched.
