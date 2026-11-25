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
- `.orchestra/workflow.md` - the four gates of this project.

## The shape of a run

1. **The plan** is written in the chat with the owner (gate 1). It lives at
   `.orchestra/plans/NNNN-<slug>/plan.md`; its header names the repos and the branch.
2. **Start it:** `run.ps1 new -PlanFile .orchestra/plans/NNNN-<slug>/plan.md`. The plan folder name is the
   run id. Gate 2 (review, then read-back) runs and the run stops at `AwaitingApproval`.
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
