# 0008 — The owner types a host path; the dashboard translates it and keeps its own report token

**Status:** Accepted
**Date:** 2026-11-23
**Decided by:** the owner, in the chat of 2026-11-23 (plan [0009](../../.orchestra/plans/0009-easy-onboarding.md))

## Context

Adding a project meant knowing what the container calls the folder (`/projects/root1`), writing the runs
folder by hand, and creating a report token somewhere and pasting it in three places. A container has one
mount per project, so every new project needs a `deploy/.env` line and a compose edit. The report token was
optional, and when it was unset the events route answered 404 — a second "feature off" state to explain.

## Decision

1. **The owner types the path they see in Windows.** `Orchestra:PathMap` is a list of `{ Host, Server }`
   pairs; `PathMapper.ToServer` translates on the way to disk and `ToHost` translates back for every
   response and every prompt. The path is stored exactly as typed in `projects.json`; only the options the
   importer and the indexer use are mapped. An empty map — IIS, `dotnet run` — is the identity.
2. **Docker mounts one parent folder.** `${ORCHESTRA_HOST_ROOT}:/host:ro` replaces the per-project mounts,
   with `Orchestra__PathMap__0__Host=${ORCHESTRA_HOST_ROOT}` and `Server=/host` beside it. A new project is
   then a form entry, not a compose change.
3. **Hosting is detected, never asked.** `DOTNET_RUNNING_IN_CONTAINER` decides between `container` and
   `direct`; `GET api/system/hosting` reports the mode, the browse roots and where the token comes from.
4. **The dashboard makes its own report token.** `Orchestra:ReportToken` still wins when set; otherwise the
   store reads `<data>/report-token`, and when that is missing it writes 32 random bytes there (atomic
   temp-and-move) and uses them. The "no token configured → 404" branch is deleted: a wrong or missing
   bearer still answers 401 with an empty body, and the token never reaches a log line.
5. **The engine reads the token from the file too.** `report.ps1` takes the environment variable named by
   `tokenEnv` first, then `reportToken` from the project's git-ignored `orchestrator.local.json` beside the
   config. A single machine therefore needs no Windows environment variable.
6. **Connecting is two pastes.** `GET api/projects/{id}/connect` returns the `report` block, the
   `orchestrator.local.json` keys (with `profilesPath` only when the map moves it) and an agent prompt that
   never contains the token. `Orchestra:PublicUrl` names the origin when the browser's is behind IIS or a
   proxy.

## Consequences

- `README` and `README.fa.md` describe one `ORCHESTRA_HOST_ROOT` line and the Getting-started page; the
  IIS paragraph says Hosting Bundle 10, app-pool read access, and `localhost` only.
- A path that maps to nothing rooted is a named 400 (`Projects.Path.NotVisible`,
  `Projects.RunsPath.NotVisible`) from create/update, while `POST api/projects/check` always answers 200
  with `visible: false` so the form can show its own text.
- Until plan 0008, the connect endpoint hands out a live token and `api/system/folders` lists folders with
  no sign-in: the compose port stays bound to `127.0.0.1`, `dotnet run` keeps `Urls=http://localhost:3500`,
  and an IIS site must bind to `localhost` only. Plan 0008 gates `api/system/*` and
  `api/projects/{id}/connect`, and must require `Orchestra:BrowseRoots` instead of the fixed-drive default.
- One more module (`Setup`, order 15) owns the hosting and folder routes; routes come from `ApiRoutes.System`.
