# Session 2026-11-22 — plan 0005 phase C and close

**Repository state:** `alireza/skeleton-and-import` at the merge `f3e8f58` (gaps branch
`alireza/engine-project-a-gaps`, commit `fe8a77f`, kept). Project repos: Project B orchestrator `d7da51c`,
Project A orchestrator `58d1806`. Nothing pushed.

## What changed

- **Phase C in both projects** (a session in each project, from
  [`engine-shim-companion.md`](../../.orchestra/prompts/engine-shim-companion.md)): `run.ps1` is the shim,
  `orchestrator.local.json` (git-ignored) holds `enginePath`, `orchestrator.json` has `description`,
  `planFiling` (and `contractDoc` in Project A); `lib/`, `tests/`, `refresh-graph.ps1` and the moved docs
  are deleted; the orchestrate skills point at `engine/docs/`.
- **Engine gaps** found before Project A's delete: `Get-PlanBranch` reads the `**Branch:**` line; config
  `contractDoc`; planner phase `"plan$(…)"`. Tests `T-CG1`–`T-CG3`. `ARCHIVE-ON-CLOSE-PLAN.md` moved to
  `engine/docs/history/`.

## Verified

- Offline suite 127/127 on `number` and `slug` (`.orchestra/evidence/0005-engine-move/gaps-suite.log`).
- In each project: `agents` (with the `engine:` line), `new … -DryRun`, and `health` (owner, after a
  `claude` re-login) — all roles PONG.

## Open

- Check 4 of the companion (a `close` that finds the filed plan) is still the owner's, on the next real close.
- Lesson: phase A took the shared code from one project; a diff of **both** projects against the engine
  before deleting caught what was lost. Keep that step in any future move.
- Next: plan 0006 (profiles) — gate-2 review, then implementation.
