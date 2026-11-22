# Task: port the multi-agent orchestrator to Project A

You are working on the Project A workspace at `F:\Projects\@Source Codes\@Fartak\Project A`
(a plain container, not a git repo; it holds two git repos: `Ui/` = Next.js 16, `Web/` = .NET 10 ASP.NET Core,
`ProjectA.sln`). Read its root `AGENTS.md`, then `Ui/AGENTS.md` and `Web/AGENTS.md`, before anything else.

The source to port is a working orchestrator in another workspace — **read it, never edit it**:
`F:\Projects\@Source Codes\@Personal\Project ProjectB\orchestrator\` (its own git repo, branch `main`).
Read, in this order: `CONDUCTOR.md` (the procedure), `DESIGN.md`, `WORKING-MODEL.md`, `CLOSE-PLAN.md`,
`orchestrator.json`, `run.ps1`, `lib/*.ps1`, then skim `tests/offline/run-offline.ps1` (the mock suite).
Also read the two skill pointers in `F:\...\Project ProjectB\.claude\skills\` (`project-b-orchestrate`,
`project-b-close-plan`) to see how a skill points at a versioned file.

## Goal

The same flow in Project A: a Claude session plans; `run.ps1` drives cheap models through
review → read-back → approve → implement (all phases) → verify once → close, with the ledger
(`runs/_ledger.md`, opencode requests + tokens per session) and the `opencode stats` block after verify/close.

## Phase 0 — read and write a plan first (STOP after this phase)

Do not copy or edit anything yet. Write `orchestrator-port-plan.md` in the Project A root containing:

1. **What is copied as is**: `run.ps1`, `lib/`, `tests/offline/`, the docs.
2. **What is ProjectB-specific and must change**, found by grep, not guessed:
   - repo names (`Agent`, `Ui`, `Web` → `Ui`, `Web`), repo paths, `verifyCommands` per repo
     (Ui: `npm run typecheck`, `npm run lint`, `npm run test`, `npm run build`; Web: `dotnet build` +
     `dotnet test` on the solution — take the exact commands from each repo's AGENTS.md / `.ai/coding/ci.md`),
     task-id prefixes (`T-AGENT-`, `T-WEB-`, `T-UI-` → `T-UI-`, `T-WEB-`), every hard-coded
     "Project B" path or name, graph refresh (`graphify`) — keep it only if Project A uses graphify.
3. **The `.ai` differences** — compare Project B's `Agent/.ai/plans/` with Project A's `Ui/.ai/plans/` and
   `Web/.ai/plans/` and decide each one:
   - Project A plans are **named by slug, without a number** (`auction-countdown.md`). The orchestrator
     reads a plan number from the branch (`plan-NNNN-…`) for `close` (`Get-PlanNumber`, `Get-PlanBranch`
     in `lib/gates.ps1`, `Cmd-Close` in `run.ps1`). Propose ONE of: (a) adopt numbered plans in Project A,
     or (b) make `close` find the filed plan by slug. Recommend one, with the reason, and let the owner choose.
   - Project A has **no `.ai/plans/TEMPLATE.md`**. The conductor requires one (sections «فهم من از درخواست»,
     «جریان کار», «نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید», phases with a gate each, file manifest,
     definition of done, «تصمیم‌ها و دلیل‌ها»). Propose adding a TEMPLATE.md to both repos, built from
     Project B's `Agent/.ai/plans/TEMPLATE.md` and Project A's existing plans' shape. Copy the Persian
     section titles from Project B's file bytes — never retype Persian text.
   - Compare `plans/README.md` (index rows), `plans/done/`, `.ai/decisions/` (ADR shape) and
     `.ai/context/session-*.md` between the two workspaces; list each difference and what the orchestrator
     or CLOSE-PLAN needs to do about it.
   - Project A already has `.claude/skills/project-a-close-plan/SKILL.md`. Compare it with
     `CLOSE-PLAN.md`; merge them (keep Project A's repo-specific rules) rather than replacing it.
4. **Where the orchestrator lives**: propose `ProjectA/orchestrator/` as its own git repo (as in
   Project B), with `runs/` git-ignored.
5. Phases for the port, each with a gate. Then **stop and show the plan to the owner. Do nothing more
   until the owner approves it.**

## After approval — rules for implementing

- Copy, then change only what the plan lists. Keep the logic; do not "improve" unrelated code.
- `orchestrator.json` chains: keep the Project B ones (opencode-go/glm-5.3-flash → mimo-v2.6-flash →
  … for reviewer/verifier; opencode-go/deepseek-v4.1-flash → … for readback/implementer).
- Skills: create `.claude/skills/project-a-orchestrate/SKILL.md` and update
  `.claude/skills/project-a-close-plan/SKILL.md` as short **pointers** to the versioned files in
  `orchestrator/` (same shape as Project B's pointers).
- Adapt `tests/offline/` so it runs against Project A's repo names. Run the offline suite **once, at the
  end** (`powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1`); every check must pass.
  The suite uses mocks only — never call a real model while porting.
- Never change code in `Ui/` or `Web/` except the `.ai/` files the plan names (e.g. TEMPLATE.md).
- Never `git commit`, `push`, or run any destructive git command. The owner reviews and commits.
- Never create a `CLAUDE.md`. No secrets in any file.
- Persian text is never translated, reflowed or retyped; copy it from existing bytes.

## Report at the end

Files created/changed per repo, the owner's choice on numbered plans and how it was applied, the
offline suite result (N/N), and anything left open.
