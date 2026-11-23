# Session 2026-11-23 — plan 0007 phases B and C (the two companions)

## What was done

Phase A was already merged (`0eb7e21`). This session ran the two companions of plan 0007 §6.2 in their
workspaces (`Project B`: `Agent`, `Ui`, `Web`; `Project A`: `Ui`, `Web`): `orchestrator/` renamed to
`.orchestra/`, workflow/roles/prompts/plans/previews/ideas/research/evidence moved in, the configs
pointed at the new layout and the links fixed. **Nothing is committed in either workspace.**

The full record, with counts and the per-file Status decisions, is
kept **in each workspace**, in its git-ignored `.orchestra/evidence/0007-move/` (summary, logs,
scripts): they name the projects, so they never enter this public repo.

## Owner decisions taken here (D7)

- One `workflow.md` per workspace: the newest full copy (`Web`) plus a *Per-repo notes* section that
  keeps every rule the other copies carried. The notes sit in
  each workspace's `.orchestra/evidence/0007-move/`.
- One `TEMPLATE.md` per workspace: the newest copy (`Web`), verbatim.

## How the plans were handled (D4)

Plan files were copied byte-identically and then verified against `git show HEAD:<old path>`: only the
Status line changed. Plans whose Status says `planned`, `ready`, `implemented`, `Superseded` or Persian
were left exactly as they were — rewriting the word would be a content change — and are listed in the
logs for the owner. Four plans with no Status line at all got `**Status:** Done` as their second line.

## Checks

`run.ps1 agents` and `run.ps1 health` pass in both workspaces (logs in each workspace); `run.ps1 queue` is empty in both; neither repo has a code file dirty. Dashboard
counts were not compared: this checkout has no project settings, so D9 is the owner's step.

## Open

1. Commits in the two `.orchestra` repos and in each product repo — only on the owner's word.
2. Dashboard: `RunsPath` → `<root>/.orchestra/runs`, drop the `Library` globs, re-import, compare counts.
3. Project B's `research/` (13 107 files) is untracked in `.orchestra/` by design; decide versioning.
4. Plans filed under `done/` whose Status is not `Done` — listed in that workspace's evidence log.
