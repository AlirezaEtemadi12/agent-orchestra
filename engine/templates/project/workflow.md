# Workflow — the four gates

> **AUTHORITATIVE** for how a session moves from a request to a reviewable diff in this repo. The owner
> may add or drop a step for one request by saying so (the plan header records it).

| # | Gate | In one line |
|---|---|---|
| 1 | **Plan** | Understanding → preview → plan, before touching a file |
| 2 | **Approval** | Ask, explicitly, whether to implement — and wait |
| 3 | **Branch** | New branch first, *then* edit |
| 4 | **Hand over** | Leave it uncommitted for the owner to review |

## Gate 1 — Plan first, always

| The work | What comes first |
|---|---|
| One file, obvious shape | 2–6 lines in the chat: what changes, where, what stays untouched. No preview |
| Anything larger, or spanning sessions | the three steps below, then a plan file |

1. **Understanding.** In the chat, in the planner's own words — never a copy of the owner's message.
   Name every sentence that could mean two things and the reading chosen, and what the owner may have
   meant that this work will **not** do. The owner corrects it; repeat until it is right.
2. **Preview.** An HTML page in [`previews/NNNN-<topic>/`](previews/): the decisions as a schematic,
   the flow of work, and mock-ups of any screen — whichever the work has. The plan number is reserved
   here (the next free one in [`plans/`](plans/)). Open questions are asked here, one at a time.
3. **Plan.** Only after the owner approves the preview: copy [`plans/TEMPLATE.md`](plans/TEMPLATE.md)
   to `plans/NNNN-<topic>.md`. Its understanding section is the confirmed text from step 1.

A plan decides everything; an implementer decides nothing. The header names its author on a
**`Planned by:`** line (the orchestrator reads the author's model family from it).

## Gate 2 — Ask before implementing

The plan is reviewed by a **different model family** than the planner's, or by the owner. Then ask,
**in so many words**, whether to implement it — and stop.

- A plan is not permission. An approval covers one plan, or one phase of it.
- "do it", "go ahead", «بزن بریم», «پیاده‌سازی کن» are approval. A question or a comment is not.
- Read-only work (reading, answering, reviewing, writing an understanding, a preview or a plan file)
  needs no approval.

## Gate 3 — Branch before you edit

```bash
git switch -c <person>/<topic>
```

From the current local HEAD. Lowercase; no branch may be a prefix of another. Look first —
`git worktree list`, `git status`: changes you did not make mean another session is probably live;
say so and offer a worktree (worktrees only on request). Never `git stash`.
**Dirty means code.** Only uncommitted changes to code, config and tests make a tree dirty. Uncommitted
Markdown (plans, knowledge docs, notes) does not: the owner often writes the next plan while another is
being implemented. Leave it untouched and never copy it from the working tree.

## Gate 4 — Hand it over uncommitted

The deliverable is a **dirty working tree on the branch**, gates green: the repo's build and test
commands, as listed in `orchestrator.json` → `verifyCommands`. Screenshots and reports from an agent's
own checks go to `.orchestra/evidence/NNNN-<topic>/phase-<X>/` (git-ignored). No `commit`, `push`,
`merge`, `rebase` or `branch -d` unless the owner asks for that action in that message. When a commit
is authorized, stage the exact paths — never `git add -A` / `git add .`. The close is three separate
questions: commit? merge? delete the branch?

## Rules about rules

- **Every rule lives in one doc.** Others link to it; nothing is restated in two places.
- **A rule change reviews the old rules.** Adding or changing a rule means re-reading the rules around
  it and merging, simplifying or deleting what it overlaps or makes obsolete, in the same change. The
  hand-over says what was removed. One simple rule beats three special cases.
- **Tell the owner when a request looks wrong.** Over-engineered, unclear or risky: say so plainly with
  the reason and a better option before doing it — then follow the owner's decision.

## Always, at the end of a session

Write `.ai/context/session-YYYY-MM-DD-<gituser>.md` (one session log per session, newest read first)
and promote any newly settled rule out of it into the doc that owns it.
