# 0010 — A plan is a folder

**Status:** Accepted
**Date:** 2026-11-24
**Decided by:** the owner (Alireza), in plan 0013.

## Context

A plan, its preview page and the screenshots an agent takes while implementing it were three things
with one identity but three paths: `.orchestra/plans/NNNN-<topic>.md`, `.orchestra/previews/NNNN-<topic>/`
and `.orchestra/evidence/NNNN-<topic>/phase-<X>/`. Finding everything about one plan meant three
searches, and the evidence folder of a long plan outlived the plan itself in a folder nobody looked at.
The dashboard's Library also had to guess which `.md` file under `plans/` was a plan and which was a
note, because `plans/*/*.md` matched `research.md` and `notes.md` alike.

## Decision

**One plan = one folder = one card on the board.**

- Every new plan lives at `.orchestra/plans/NNNN-<topic>/plan.md`, with `preview.html` beside it and
  `evidence/` beside that. The folder name is the plan's identity and the run's id.
- Nothing is copied at plan time, so the plan has one home for its whole life: no `done/` folder, no
  move at close. The header's `Status` says where it is in its life.
- The Library indexes `plans/*.md` (the old flat shape, still honoured) and `plans/*/plan.md` (the new
  one). `research.md` and `notes.md` are no longer plans by accident.
- The header names the repos the plan touches, on a `**Repos:**` line; the run records them and each
  phase names its repo.
- Old projects keep working: a flat `plans/NNNN-<topic>.md` still reads, still files, still links.

## Consequences

- Plan 0007's per-repo `plansDir` and the `done/` folder stay supported for older projects, but the
  template no longer uses either.
- The board shows one card per plan even when the run touched several repos, with a chip per repo.
- Anything that reads a plan path takes the folder form as the ordinary case and the flat file as the
  fallback; nothing may assume one or the other.
