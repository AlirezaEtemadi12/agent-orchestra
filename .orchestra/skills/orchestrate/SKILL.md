---
name: orchestrate
description: Run a plan through the four gates with run.ps1 - plan, review, read-back, approval, implement, verify.
---

# Orchestrate a plan

The engine is `run.ps1`; this skill is the conductor's procedure around it. **The engine is the only thing
that changes a run** - never edit `runs/<id>/task.json`, never write a plan version by hand.

## Read first

- `engine/docs/CONDUCTOR.md` - every command, its arguments and the order it is used in.
- `engine/docs/WORKING-MODEL.md` - which colour the owner sees, and what each gate is for.
- `.orchestra/workflow.md` - the four gates of this repository.

## This repository

- **One repo, one branch.** `api/`, `ui/`, `engine/` and `scripts/` are one repository and one branch
  (`alireza/<topic>`), so a plan of this repo names no repos and carries no `**Repos:**` line
  ([ADR 0001](../../../.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md)).
- **Plan files:** plans 0013 and later are folders, `.orchestra/plans/NNNN-<topic>/plan.md`; plans 0001-0012
  are flat files and stay so ([ADR 0010](../../../.ai/decisions/0010-plan-is-a-folder.md)).
- **The engine here serves the projects.** This checkout holds `engine/run.ps1`; a project is driven through
  its own `orchestrator.json`, so every command below takes `-Config <that project's orchestrator.json>`.
  There is no `orchestrator.json` inside this repo's `.orchestra/`, and none is invented.

## The shape of a run

1. **The plan** is written in the chat with the owner (gate 1). It lives at
   `.orchestra/plans/NNNN-<slug>/plan.md`; its header names the branch, and the repos when the workspace has
   more than one.
2. **Start it:** `run.ps1 new -PlanFile <plan folder>/plan.md -Config <project>/orchestrator.json`. The plan
   folder name is the run id. Gate 2 (review, then read-back) runs and the run stops at `AwaitingApproval`.
3. **The owner answers.** Either `approve <id>` (optionally with notes), or the owner says "implement" -
   which is its own approval: `implement <id> <phase>` records `approved.by = implement`. Never approve on
   the owner's behalf, and never run `implement` on a plan the owner has not just asked to implement.
4. **Every phase, then one verify.** `implement <id> 1`, 2, 3 … to the last phase with no stop between them,
   then **one** `verify <id>` (engine/docs/CONDUCTOR.md step 7). Show each phase's summary block, not the whole
   transcript. BLOCKED, a red gate, a `Violation` or a file outside the plan stops there: fix by hand, then
   `implement <id> <phase> -Force` and go on.
5. **Close by the owner's word only.** "Close the plan" is the yes to commit, merge and delete the branch;
   `skills/close-plan/SKILL.md` does it, and it pushes nothing.

## Rules that bite

- Never commit, merge, push or delete a branch unless the owner just asked for it.
- Never answer a `manual` hop with your own words: say `run.ps1 queue` and let the human paste the answer.
- A `NeedsInput` stop is a real question for the owner; do not answer it from the plan text.
- One repo after another: a plan's repos run in the order of its `**Repos:**` line, never at once.
