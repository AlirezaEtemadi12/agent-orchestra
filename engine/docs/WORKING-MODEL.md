# Working model — who does what, and why

Decisions from the orchestrator review sessions, ported to ProjectB. The procedure lives in
[`CONDUCTOR.md`](CONDUCTOR.md); the gates in each repo's `.ai/agents/workflow.md`
(`Agent/`, `Ui/`, `Web/`); the commands in `run.ps1 help`.

## The flow

```
user <-> conductor (chat): discuss, settle every product decision
  -> conductor writes the plan                                   (gate 1)
  -> run.ps1 new -PlanFile: independent review, other family     (gate 2, review session R)
  -> only after a review PASS, read-back: "where would I guess"  (gate 2.5, implement session I)
  -> conductor revises on real findings (revise -PlanFile), max ~3 rounds
  -> user approves in chat -> conductor runs approve [notes]     (plan.approved.md frozen, sha256)
  -> orchestrator creates the branch; implementer continues session I, one phase   (gate 3)
  -> orchestrator runs the gate commands; verifier continues session R, reads diff (gate 4)
  -> user closes: commit / merge / delete, one at a time
```

Gate 3 needs a clean tree, but uncommitted records under `.ai/plans/` and `.ai/context/` (plans and
session logs, not code) are ignored and never block it.

Without a conductor (`run.ps1 new` with no `-PlanFile`), the planner chain writes the plan and the
loop revises automatically on review or read-back findings (`revise.maxAutoRounds`).

## Who does what

| Work | Judgement | Volume | Who |
|---|---|---|---|
| Discussion, product decisions | very high | low–medium | conductor (strong model) + user |
| Writing the plan | high | medium; the conductor already has the context | conductor |
| Plan review | medium | medium | cheap model, **different family** from the plan's author |
| Read-back | low–medium | medium | the implementer's own (cheap) model |
| Implementation | low if the plan is exact | **high** | cheap model |
| Tests, build, git checks | none | long output | the script itself, no model |
| Supervision | medium | low (summaries only) | conductor |

Cost facts behind this:
- In a chat session every reply reprocesses the whole conversation (mostly cached, not free). Long
  sessions make every message dearer; start a fresh session per feature.
- The strongest model draws most on the subscription quota; `claude -p` hops in the orchestrator use
  the same subscription. Free opencode models do not touch it.
- Even a tiny task has a fixed context cost: the prompt carries the repo's workflow doc, the role and
  the stage prompt (the Ui planner prompt is ~7.8 KB, Web's ~16 KB, Agent's ~19 KB — the .NET repos
  carry the longest workflow doc).

## Decisions

1. **Strong model plans; a different-family model reviews.** Plan mistakes multiply through the
   implementation; checking is easier than designing, so a medium reviewer catches real issues. Family
   matters more than strength. A very weak reviewer produces false findings and churns the revise
   loop. Big/risky plans (migrations, contracts) may add a strong reviewer.
2. **A cheap model can implement — when the plan is exact** (signatures, names, strings, manifest).
   Weak models fail at debugging red tests, multi-file refactors, framework quirks and plan/code
   mismatches. Planned, not built: escalate a phase to a stronger model after two verify FAILs.
3. **Questions are settled up front, not answered by a bot.** Answers in the code/docs are the
   conductor's job; product decisions are the user's only — a model "answering" them buries a guess
   in the code. The discussion with the conductor *is* the question phase.
4. **Read-back (gate 2.5).** The implementer's model reads the plan before approval and lists where
   it would have to guess. If the cheap model sees ambiguity, it is real. Its findings count like
   review findings at approval.
5. **More review rounds have diminishing returns.** One serious review + up to three revisions; after
   that, disagreements go to the user.
6. **Approval is per message.** The conductor runs `approve` / `implement` only when the user says so
   in that message; nothing is ever committed by the orchestrator.

## Decisions specific to this workspace

- **Workers run with the target repo as cwd** (`defaults.agentCwd: "repo"`), not the workspace root.
  Each repo owns its own `AGENTS.md` and `.ai/`; the root `AGENTS.md` is only a router between three
  repositories, and the root also holds unrelated files. Sibling repos stay readable as `../Web`,
  `../Agent`, `../Ui`; the fingerprint guard still catches any write outside the target repo.
- **Gate commands come from each repo's own `ci.md`**: Ui runs its four npm gates (including
  `npm run build`), Web and Agent run `dotnet build` + `dotnet test`. Ui's build is the slow one
  (~1 min) and is included because Ui's own gate doc defines done as all four.
- **The code graph is refreshed for the target repo only**, before a plan. A refresh writes only the
  gitignored `graphify-out/`, never a source file, and never runs a full `graphify extract` (that
  would drop the graphs' LLM-extracted concept nodes; `refresh-graph.ps1` backs a graph up first and
  keeps only the newest two backups). A successful refresh also records HEAD in
  `graphify-out/.orch-head`, and that record — not the graph's mtime — is what tells
  `run.ps1 graph -Check` that a commit has landed: `graphify update` leaves `graph.json` untouched
  when no code file changed, so its timestamp can lag behind the commit it already covers. An
  uncommitted edit newer than the graph still counts as stale.
  `run.ps1 graph -Check` is the free way to see whether the graph the conductor is about to query
  still matches the code — worth doing *before* writing the plan, because a read-only worker cannot
  run graphify and the `new`-time refresh comes too late for a conductor-written plan.

## Deferred (agreed, not built)

- **Phase 0 UI prototype:** for UI features, phase 0 builds the screens with mock data only; the user
  sees the real UI in the browser before any logic is written. The plan also carries a user-test
  checklist.
- **Tester persona:** an agent that brings the app up and walks the checklist (interactive with the
  user, or Playwright e2e as a verifyCommand). Limits found: headless runs cannot be interactive;
  the WPF Agent is not browser-testable; Ui needs the Web API running; screenshots are token-heavy.
- **Escalation** of a failing phase to a stronger implementer.

## Using another harness as the conductor

The orchestrator is a plain PowerShell CLI; any harness with a shell can conduct (Codex CLI,
opencode, Gemini CLI, Cursor, WorkBuddy, or you by hand). Point it at
[`CONDUCTOR.md`](CONDUCTOR.md) explicitly and pass `-PlanFamily <your model family>` so gate 2 picks a
reviewer from a different family. The caveats (no conductor rule in a root `AGENTS.md`, check each
tool's terms) are in that file.
