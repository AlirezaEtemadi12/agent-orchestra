# 0001 — One repo; .NET 10 + SQLite; Next.js static export served by the API

**Status:** Accepted — 2026-11-19 (plan 0001's gate 2)
**Amended:** 2026-11-24 — one local process instead of one container ([0009](0009-local-app-sets-projects-up.md))
**Date:** 2026-11-19
**Decided by:** Alireza (stack: "the architecture we use for our projects"); repo shape proposed by the planner

## Context

The owner wants a dashboard, SQLite, and the same architecture as their other projects: a .NET modular
monolith API and a Next.js admin UI built like Project B's Ui (shadcn/ui from the official registry,
no paid template — Project B Ui ADR 0002). It is a one-person tool, hosted in Docker locally or on a
personal server.

Project A and Project B split API and UI into separate repositories because they have separate
release cadences and, for Project A, a team. Here there is one developer, one release and one
consumer.

## Decision

- **One repository** with `api/`, `ui/`, later `engine/`, and `deploy/` (deleted by [ADR 0009](0009-local-app-sets-projects-up.md); `scripts/` holds the publish). One branch covers a change
  that crosses the wire; no companion plans between repos.
- **api/**: .NET 10 ASP.NET Core, EF Core, **SQLite**. A small modular monolith (`Projects`, `Runs`,
  `Usage`, later `Settings`), with the module boundaries and conventions of Project B's Web, trimmed:
  no multi-tenant, no permissions system beyond one user.
- **ui/**: Next.js 16, **`output: 'export'`** — a static site served by the API. Same building blocks
  as Project B's Ui (shadcn/ui, Tailwind v4, TanStack Query, vertical slices, tokens, Persian RTL).
- **One local process** in production: the API serves the exported UI inside the desktop app's window
  ([ADR 0009](0009-local-app-sets-projects-up.md)).

## Consequences

- No Node process at runtime and no CORS: the UI calls `/api/*` on its own origin.
- Static export rules out Next.js server features (server actions, middleware, ISR). A read-only
  dashboard does not need them; a client-side guard is enough once sign-in arrives (phase 3).
- If a second client ever appears (e.g. a mobile app), splitting `ui/` out is a folder move.
- The honest cost: .NET + Next.js is heavier than a single-file tool would be. It was chosen because the
  owner knows it, the patterns already exist in Project B, and the tool is meant to grow.
