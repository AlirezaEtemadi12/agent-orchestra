# 0007 — Profiles are edited in the dashboard and exported to a file

**Status:** Accepted
**Date:** 2026-11-22
**Decided by:** Alireza, 2026-11-19 (the phase-2 shape) and 2026-11-22 (the export path); written down by
plan 0006.

## Context

Every project's `orchestrator.json` repeats the same chains, tiers, adapters, pools, quotas and price
sources. The dashboard is the natural place to edit them once — but [ADR 0003](0003-read-only-dashboard-first.md)
and [ADR 0005](0005-ai-is-knowledge-orchestra-is-work.md) keep it out of project folders, and
[ADR 0002](0002-runner-reports-dashboard-never-commands.md) keeps a run working with the dashboard down.

A setting stored only in the database would make the engine depend on the dashboard. A setting written
into each project's `orchestrator.json` would make the dashboard write into a project — and would move
`orchestrator.json` out of the project's own git, where a project without Orchestra still has its settings.

## Decision

A **profile** is the dashboard's own data, versioned in `Profile`/`ProfileVersion` (the second exception
to "the database is a read model", after [ADR 0006](0006-events-are-the-dashboards-own-record.md)). On
every save the dashboard exports the current version to `<data>/profiles/<id>.json` and the engine reads
that file, merging it under the project's `orchestrator.json` at the start of every command.

Consequences that follow from it:

1. The engine never calls the dashboard and knows nothing about the database: a run needs the file, not
   the service ([ADR 0002](0002-runner-reports-dashboard-never-commands.md)).
2. The dashboard still never writes into a project: the export lives under its own data folder. A
   project opts in by naming a `profile`, and its own file keeps every override in place.
3. The export path is per machine, not per project's git: `profilesPath` sits in the project's
   git-ignored `orchestrator.local.json`, beside `enginePath`.
4. Losing the database loses the version history and reasons — the current export is still on disk and
   still drives the runs. Backups matter (plan 0008).

## Alternatives

- **Profiles in each project's `orchestrator.json` only.** Rejected: the duplication the feature removes
  is exactly the point.
- **The engine reads `GET api/profiles/{id}`.** Rejected: a dashboard that is down would stop every run
  ([ADR 0002](0002-runner-reports-dashboard-never-commands.md)).
- **The dashboard writes `orchestrator.json` into the project.** Rejected: [ADR 0003](0003-read-only-dashboard-first.md)
  and [ADR 0005](0005-ai-is-knowledge-orchestra-is-work.md); the dashboard shows the text and the owner's
  agent applies it.
