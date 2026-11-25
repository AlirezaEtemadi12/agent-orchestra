# Plan NNNN — <short title>

**Status:** Draft — one of `Draft` · `Approved` · `Running` · `Done` · `Dropped`
**Created:** <YYYY-MM-DD> · **Updated:** <YYYY-MM-DD> · **Done:** —
**Preview:** [`preview.html`](preview.html) — the page the owner approved before this plan, in this folder
**Run:** — (the orchestrator task id, filled by the conductor when it starts one)
**Repos:** <the repos this plan touches, in the order they run — one name for a single-repo project>
**Planned by:** <model>, <date>. **Executor:** another agent, ideally a cheaper model.
**Plan review:** <the reviewing model, or the owner> — a **different model family** than the planner's.
**Decided by:** <who>, <date>. Every choice below is already decided — **decide nothing new.** If reality
contradicts this plan, STOP and ask.
**Branch:** `<person>/<topic>` — named here; the implementer creates it.
**Base:** `<branch>` @ `<short hash>`
**Companion:** <a plan in another repo, or `None`>
**Steps changed:** <`None`, or what the owner added or dropped from the default workflow for this plan>

---

## My understanding of the request

<The understanding the owner confirmed in the chat (gate 1, step 1) — see
[`../../workflow.md`](../../workflow.md).>

## Flow of work

<Plain words, no class names: what the owner does, what the app does in response, what happens on
failure — then the phases in order, one line each.>

## Result, and what you check after implementation

<What is true when the plan is done, in two or three lines — then the owner's checklist in the running
app: where to go, what to do, what they should see; failure paths included.>

## 0. Ground rules for the executor

1. Follow [`../../workflow.md`](../../workflow.md): branch before the first edit; never
   commit/push/merge unless asked in that message; one phase = one uncommitted diff.
2. <anything this plan specifically needs>

### Baseline

<the commands that must be green before the first edit; record the test count — it never drops.>

## 1. Goal
## 2. Out of scope
## 3. Rules that apply
## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | <decision — one-line reason> |

## 5. File manifest

| # | File | NEW/EDIT |
|---|---|---|

## 6. Per-file specification
## 7. Tests
## 8. Stop and ask
## 9. Phases

<Per phase: steps · **Gate** (the build/test commands) · **Owner checks** (runtime, done by the owner) ·
"Stop here — show the owner the diff.">

Every phase line names its repo: `## Phase N — <repo>`, so a plan that spans repos runs them one after
another and the owner can see which repo each phase touches.

## 10. Definition of done

<…, and: `Status: Done`, the `Done:` date, the commit and the merge in the header.>
