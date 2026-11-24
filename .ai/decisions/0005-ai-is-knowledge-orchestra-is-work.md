# 0005 — `.ai/` is the knowledge, `.orchestra/` is the work; the dashboard never writes into a project

**Status:** Accepted
**Amended:** 2026-11-24 — writing into a project is allowed through setup ([0009](0009-local-app-sets-projects-up.md))
**Date:** 2026-11-19
**Decided by:** Alireza (the split, the folder name, the agent-written scaffold), in the plan 0001 preview

## Context

In Project A and Project B, `.ai/` mixes two things: knowledge about the project (architecture,
decisions, conventions) and the orchestrated way of working (gates, roles, plans). Any agent — Claude,
Cursor, Codex — should be able to use the knowledge without the orchestrator, and the orchestrator's
products (ideas, previews, plans, test evidence) need one categorized place the dashboard can index.
Both must stay in the project's own git.

The dashboard may run in Docker or on a server, where project folders are mounted read-only
([ADR 0003](0003-read-only-dashboard-first.md)).

## Decision

- Every project has **`.ai/`** — the agent-neutral knowledge tree: an index, architecture,
  decisions, per-area conventions, session logs.
- Every project has **`.orchestra/`** — the orchestrator's folder: `workflow.md`, `ideas/`,
  `research/`, `previews/`, `plans/`, `prompts/`, and from phase 2 `project.json` (overrides of the
  shared profile), `roles/` and `runs/`. `evidence/` and `runs/` are git-ignored.
- **Files are the source of truth.** The dashboard's database only indexes them (and keeps its own
  logs); it can be dropped and rebuilt. A file the owner deletes disappears from the dashboard.
- **The dashboard never writes into a project folder**, except through setup
  ([ADR 0009](0009-local-app-sets-projects-up.md)). When a project lacks `.ai/` or `.orchestra/`, the
  dashboard shows a prompt — with the file contents it needs, such as `project.json` — for the owner's own
  agent to run. Outside setup it writes only its own data (its project list, its database).
- Orchestra's own repository follows the same layout.

## Consequences

- An agent that knows nothing about the orchestrator still finds the knowledge in `.ai/`.
- The scaffold is written by an agent that reads the code, not stamped from an empty template.
- Project A and Project B keep their current layout until a phase-2 companion plan in each repo moves
  them; the importer reads each project's paths from configuration, so the move changes no code.
