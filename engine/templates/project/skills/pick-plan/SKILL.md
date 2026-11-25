---
name: pick-plan
description: List the open plans with their state, let the owner pick one, and continue that plan from where it stopped.
---

# Pick a plan

Triggers: "plans", «پلن‌ها», "pick a plan", "what's open".

1. **Read the plans.** Every `.orchestra/plans/*/plan.md` and every flat `.orchestra/plans/*.md` whose
   header Status is not `Done` and not `Dropped`. Skip `README.md` and `TEMPLATE.md`.
2. **Show one numbered table**, newest number first: number, title, Status, Repos (the header's
   `**Repos:**` line), and the run's status from `runs/<id>/task.json` when a run exists for that id.
3. **Wait for one number.** Then act by the run's state:

   | State | What to do |
   |---|---|
   | no run, plan `Draft` | `run.ps1 new -PlanFile <folder>/plan.md` |
   | `Planning` / `Reviewing` | `run.ps1 status <id>` and wait |
   | `NeedsInput` | show the question |
   | `AwaitingApproval` | show the review verdicts and say: approve, or say "implement" |
   | `Approved` / `Implemented` | `implement <id> <next phase>` - only after the owner says implement |
   | `Implemented` with every phase done | `verify <id>` |
   | `VerifyFailed` | show the failing gate |
   | `Verified` | say "close the plan" |

4. **Never approve.** This skill never approves, never implements on its own, and never pushes. Every
   command it runs and every argument is in `engine/docs/CONDUCTOR.md`.
