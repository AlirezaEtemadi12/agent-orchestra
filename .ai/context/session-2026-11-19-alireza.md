# Session 2026-11-19 — alireza — project founded

Started from a session in `Project A` (orchestrator work), moved here by the owner's request.

> “No code yet” below is this session's state: plan 0001 (skeleton, import) and plan 0002 (library)
> were implemented and merged later the same day — see
> [`session-2026-11-19-alireza-library.md`](session-2026-11-19-alireza-library.md).

## Repository state

- `git init -b main` done; **no commit**, no branch, no remote. Everything below is untracked.
- No code yet: docs only.

## What changed

- Created the repo skeleton: `AGENTS.md`, `README.md`, `.gitignore`, `.ai/` (index, workflow, vision,
  architecture, decisions 0001–0003, plans README + TEMPLATE + plan 0001 outline, context).
- Rules adapted from Project B (`Ui/.ai`, `Web/.ai`), trimmed for a one-person tool. Two owner rules
  added in `agents/workflow.md` → *Rules about rules*: a rule change reviews the old rules; tell the
  owner when a request looks wrong.

## Background the next session needs

- Open-source tools compared before starting (kandev, OrchestratorInc/agent-orchestrator, cline/kanban,
  AI-Agents-Orchestrator, quartet): none has a gated plan → review → read-back → verify pipeline,
  quota pools with fallback chains, or cost per stage — recorded in `vision/ideas.md`.
- The orchestrators' `close` deletes `runs/<id>/`; only `_ledger.md` survives (Project A's had 30
  blocks, Project B's 17 on this date; Project B still has ~30 unclosed run folders).
- Latest orchestrator state: commits `a8a63a1` (Project A) and `b8f8ffa` (Project B) — review on
  deepseek, read-back on glm, plan family from the `Planned by:` line, live prices from
  opencode.ai/data cached in `runs/_model-rates.json`.

## Not verified

- Nothing built or run — there is no code.

## Open

- Plan 0001 §8: five questions for the owner (name, port, theme, companion `close` plans, git remote).
- Plan 0001 §5–§7 to be written after those answers.
- ADRs 0001–0003 are `Proposed`; confirmed at plan 0001's gate 2.
