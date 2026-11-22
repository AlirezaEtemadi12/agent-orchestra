# 0002 — The runner reports; the dashboard never commands

**Status:** Accepted — 2026-11-19 (plan 0001's gate 2)
**Date:** 2026-11-19
**Decided by:** planner proposal, from the owner's hosting plan (Docker locally or a personal server)

## Context

The orchestrator runs on the owner's Windows machine, next to the repos, and drives agents that edit
code there. The dashboard may run on a different machine. A link from the server back to the Windows
machine would let anything that compromises the dashboard run agents, git and shell commands on the
owner's code.

## Decision

- Information flows **one way**: the runner (and, in phase 0, the importer reading files) → the dashboard.
- The runner's reporting is **fire-and-forget**: short timeout, no retry loop, and a failure is at most
  a one-line note. A run behaves identically with the dashboard down.
- The runner authenticates with a **report-only token**: it can post events and nothing else.
- In phase 2, settings reach the runner as an **exported file read before a run starts**, never as a
  live call during a run.

## Consequences

- The dashboard can be offline, wiped or re-imported without touching any run.
- Live views lag by at most one event, and nothing is lost: import back-fills from the files.
- Any future "do something" button needs its own ADR (see 0003).
