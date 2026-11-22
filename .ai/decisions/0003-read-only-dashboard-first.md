# 0003 — Read-only first

**Status:** Accepted — 2026-11-19 (plan 0001's gate 2)
**Date:** 2026-11-19
**Decided by:** planner proposal

## Context

The orchestrator's gates are deliberate: the owner (or the conductor under the owner's standing go)
approves a plan, implementation runs on a clean tree, verify is independent. Buttons in a web page that
approve, implement or start runs would bypass the place where those decisions are made today — the
chat with the conductor — and would need the dashboard to reach the runner (ADR 0002).

## Decision

Through phase 3 the dashboard **shows, compares and edits its own settings**. It does not approve,
implement, verify, close or start runs, and it does not edit project files.

## Consequences

- The highest-value parts — model quality, cost, history — need no control at all.
- A low-risk action later (retry a failed hop, release a stale pool lock) gets its own ADR, scoped to
  that one action, and must still respect ADR 0002.
