---
name: close-plan
description: Close a finished plan - commit, merge and delete its branch in every repo of the plan, ask nothing.
---

# Close a plan

Read `.orchestra/orchestrator.json` for the repo names, their verify commands and the base branch. Then do
this in order. **Ask nothing**: "close the plan" is already the owner's yes to all of it.

1. **Find the plan.** By its number or slug. Without one, the only run whose status is `Verified`.
2. **Check the run.** Read `runs/<id>/task.json`. When it is not `Verified`, run `run.ps1 verify <id>`
   once; if it is still not `Verified`, stop and report - that is a report, not a question.
3. **Commit, per repo of `repoState`, in plan order.** Stage the exact paths. Where `plan.md` sits inside
   that repo (a single-repo project), stage it too, with the `Approved`/`Running` Status the engine wrote,
   so the tree is clean for the switch in step 4. Commit with: run id, the verdicts, the gate counts from
   the logs.
4. **Merge, per repo.** `git switch <baseBranch>`, then `git merge --no-ff <branch>`. On a conflict:
   `git merge --abort`, stop, and report which repos merged and which did not.
5. **Mark the plan Done.** Set the plan's Status line to
   `Done <date> - run <id>, merged as <hash per repo> (not pushed)` and update its row in the plans README.
   This happens on the base branch, after the merge, so `Done` lands on the base and not on the plan
   branch. Commit it in the repo that holds `.orchestra/`.
6. **Delete branches.** `git branch -d <branch>` per repo. Also delete any other branch `--merged` lists
   whose plan is Done. Never `-D`, never a remote branch, never the current or base branch, never a stash.
7. **Close the run.** `run.ps1 close <id>`.
8. **Report.** One table per repo: commit, merge, close hash, branches deleted, foreign files left
   unstaged. Then the ledger usage. Then one line: nothing was pushed; say "push" to push.

**Never push. Never force. Never stash.**
