# CONDUCTOR — run a task through the orchestrator

The conductor procedure for this workspace. Any agent harness with a shell (Claude Code, Codex CLI,
opencode, WorkBuddy, Gemini CLI …) can conduct by reading this file; nothing here depends on a
tool-specific directory.

**First check:** if your prompt begins with `# ORCHESTRATOR CALL`, you are a **worker** launched by the
orchestrator, not the conductor. Ignore this file entirely and do the role in your prompt.

The working model and the reasons behind it are in [`WORKING-MODEL.md`](WORKING-MODEL.md). The gates
themselves are owned per repo, in:

| Repo | Gates (authoritative) | Roles | Gate commands |
|---|---|---|---|
| `Agent/` (.NET 10 WPF) | `Agent/.ai/agents/workflow.md` | `Agent/.ai/agents/{planner,plan-reviewer,implementer,verifier}.md` | `Agent/.ai/coding/ci.md` |
| `Ui/` (Next.js 16) | `Ui/.ai/agents/workflow.md` | `Ui/.ai/agents/README.md` sections | `Ui/.ai/coding/ci.md` |
| `Web/` (.NET 10 API) | `Web/.ai/agents/workflow.md` | `Web/.ai/agents/{planner,plan-reviewer,implementer,verifier}.md` | `Web/.ai/coding/ci.md` |

This file is only the procedure. It restates no repo rule.

## Your job, and its limits

You do the expensive-judgement, low-volume work: **discuss, write the plan, supervise.** Cheap agents
do the high-volume work (review, read-back, implementation, verification); the orchestrator script
runs tests and git checks itself. Every message in this session costs the user's quota, so:

- read the `=== SUMMARY ===` block, not full outputs; open an artifact only when the summary shows a
  problem
- run long commands in the background and read only their tail
- use `graphify query` before reading source files (per repo: `Agent/.ai/tools/graphify.md`,
  `Web/.ai/tools/graphify.md`, `Ui/.ai/coding/graphify.md`)
- never implement the plan yourself unless the user says so in that message

## Two sessions

The expensive judgement and the cheap loop run in **two different sessions**, so neither pays for the
other's context:

- **Session A — the conductor (a strong model, in the desktop app).** Discusses with the owner, builds the
  preview (step 1b) and writes the plan. The plan carries a «تصمیم‌ها و دلیل‌ها» ("decisions and why")
  section recording every product decision and its reason, so nothing lives only in this session's chat.
  Then it hands off; it does not run the loop itself.
- **Session B — the executor (Sonnet, a fresh session).** Runs `run.ps1 new -PlanFile …` and the whole
  review → read-back → revise → approve → implement → verify loop. It reads only the `=== SUMMARY ===`
  block, never full outputs. On findings it triages: a **mechanical** fix it applies to the plan itself;
  one that **touches a decision** it stops on and gives the owner a short note to take back to Session A;
  one that is **wrong** it rejects in one line. A version it wrote itself carries its own
  `**Planned by:**` line (e.g. `Claude Sonnet 5.5`), so gate 2 still picks a different-family reviewer. It approves
  and implements under the owner's standing go (step 6 below): no second approval, no go per phase.

**Quota.** The Go pools (GLM, MiMo, DeepSeek) share one account-wide cap. When a Go hop reports the cap is
spent, the whole pool is marked exhausted and every other Go hop of that chain is skipped without a call;
the chain falls to Sonnet or codebuddy, and finally to the **manual inbox** — a prompt any agent of the
owner's choice can answer. The owner either answers it with any agent, or waits for the cap to reset and
reruns the same command with `-Retry`. A pending manual request is answered, not overtaken: rerunning
without `-Retry` skips the automatic hops until the answer is stored.

## Procedure

1. **Discuss.** Understand the goal; read only what you need. Settle every product decision with the
   user *now* (behaviour, edge cases, UI text, data rules). Questions whose answer is in the code or
   docs are yours to answer, not the user's. Batch the remaining questions into one message.

1b. **Preview — required when the change is visible on screen.** Before writing the plan, build a static
   HTML preview of the finished screen (layout, names, buttons, dialogs, charts with sample data; light
   interaction such as opening a dialog is fine) and send it to the user. It shows **what the screen will
   look like**, not how it is built — no plan detail goes in it. The same page has two more sections, in
   the user's language:
   - **Flow** — what happens, step by step, in plain words: what the user does, what the app does in
     response (which reads, when, what is saved, what is shown), and what happens on failure. A short
     numbered list or a simple step diagram; no class or method names.
   - **What you test** — a checklist the user follows in the running app after implementation: where to go
     (which window, menu, tab, link), what to click or type, and what they should see. One line per check,
     concrete («تب «دیده‌بان» → دکمهٔ «جزئیات» کنار شکیمیا → پنجره باز می‌شود و دو نمودار دارد»). Include
     the unhappy paths that matter (no session, market closed, a failed read).
   The plan's owner-check line (its phase list's last paragraph) copies this checklist, and the close
   report repeats it as the items to confirm. Save it as
   `orchestrator/runs/_drafts/<taskId>.preview.html` (the task id the run will use) and keep it as the
   reference the plan follows. **Do not write the plan until the user approves the preview**; every change
   they ask for is made in the preview first. When the plan is written, its "Decided by" line names the
   approved preview file. Skip this step only for a change with nothing on screen (a service, a session
   fix, docs) — say so in one line.

2. **Check the graph is current — free, before you read source.**
   `powershell -NoProfile -File orchestrator/run.ps1 graph -Check` writes nothing and calls no model:
   it compares the HEAD recorded at the last refresh (`graphify-out/.orch-head`), the graph's mtime
   and any uncommitted edit, and prints one `Reason` per stale repo. If the target repo is stale, run
   the same command without `-Check` (AST only). A plan written against a stale graph is the most
   common source of review and read-back findings, and the read-only worker roles cannot refresh it
   themselves (they have no shell).

3. **Write the plan** in the target repo's shape (`.ai/plans/TEMPLATE.md`, or the short chat form for
   one obvious file): branch named as `` `<person>/<topic>` `` (or `plan-NNNN-<topic>`) in backticks, a
   complete file manifest, per-file exact shapes, a gate per phase, definition of done, no open
   decision. The plan opens with «فهم من از درخواست» — **your own understanding** of the request, not
   the user's words copied: requests are often worded loosely, so say which reading you took where one
   could mean two things — then «جریان کار» and «نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید» (the
   result and the owner's post-implementation checklist; the repo's `TEMPLATE.md` says what each carries). Write it to
   `orchestrator/runs/_drafts/<taskId>.plan.md` — never inside a repo you are not implementing.

3b. **Show, then review.** Show the user those three sections and a short outline, not the whole file
   unless they ask. Start the run (step 4) only after they confirm the understanding; a correction goes
   into the plan first.

4. **Start the run** (background; it takes minutes):

   ```
   powershell -NoProfile -File orchestrator/run.ps1 new <taskId> <Agent|Ui|Web> "<one-line task>" `
       -PlanFile orchestrator/runs/_drafts/<taskId>.plan.md -NoReadBack
   ```

   Task ids: `T-<REPO>-NNN`, next free number under `orchestrator/runs/`.

   **Who wrote the plan comes from the plan, not from you.** The orchestrator reads the family from the
   plan's `**Planned by:**` header line (`Claude Opus 5.5` -> `claude-opus`) and gate 2 excludes that
   family from the review. The conductor can be any model; what it runs on does not matter, and it does
   **not** pass `-PlanFamily` with its own model. The orchestrator handles the rest itself:
   - a `-PlanFamily` that disagrees with the header is a small slip: the header wins, with a note;
   - a revision with no `**Planned by:**` line keeps the previous version's family, with a note;
   - a plan whose header names no known model (missing, a placeholder, a bare `Claude`) **stops** `new`
     before the run exists. Do not guess a family: tell the owner and ask the planner to add
     `**Planned by:** <model>, <date>` to the plan, then rerun. Pass `-PlanFamily <family>` only when
     the owner says the author is not a model (e.g. `-PlanFamily human` for a plan the owner wrote).

5. **Read the summary.** On review FINDINGS: read the artifact, decide which findings are real, fix the
   plan file (`<taskId>.plan.v2.md`), and run `run.ps1 revise <taskId> -PlanFile <v2> -NoReadBack`.
   Findings you reject: say why to the user in one line each. Three revision rounds is normally the
   limit; after that, bring the disagreement to the user.

5b. **Read-back only after the review passes (owner's rule, 2026-11-16).** A fresh read-back explores the
   repo and takes ~25 min; a review takes 1-5 min. So review rounds run with `-NoReadBack`, and only once
   the review is PASS does `run.ps1 readback <taskId>` run.
   - PASS, or nits only: approve (step 6); the nits go into the approve notes, not into a new version.
   - A blocker or should: fix the plan and `run.ps1 revise <taskId> -PlanFile <vN>` **without**
     `-NoReadBack`. Every later review, read-back, implement and verify continues its own session from
     the cache with only the plan diff since the version that session last saw, however many versions
     lie between (up to `sessions.maxFollowUps` turns per session).
   - At most 10 read-backs per run; past that, bring the disagreement to the owner. (The owner may lower
     it to 2 if long loops start to hurt.)
   - Who does what (owner's choice, 2026-11-17): the **review** runs on the implementer's own model
     (DeepSeek), so it catches what would block it at implement time; the **read-back** runs on another
     family (GLM) as a second pair of eyes. `implement` then continues the DeepSeek review session and
     `verify` the GLM read-back session, so each keeps its cache; when the role changes in a session it
     gets that role's full instructions, not a short follow-up.

6. **Approve - the owner's standing go (owner's rule, 2026-11-15).** The owner's confirmation in step 3b
   is the approval. When the latest review is PASS and the read-back has no blocker and no should
   (nits are fine; after read-back 2, step 5b's notes rule applies), run `run.ps1 approve <taskId> ["notes"]` yourself, without asking again; put any
   amendments the owner stated into the notes verbatim. Ask instead only when a finding touches a
   decision, or you overruled a blocker/should — then wait for the owner's word.

7. **Implement every phase, then verify once.** `run.ps1 implement <taskId> 1` (background), then 2, 3 …
   to the last phase, without asking between phases; then **one** `run.ps1 verify <taskId>` at the end.
   After each phase read its summary line and go on only if it is DONE/PASS with 0 violations and no
   file outside the plan. **Stop and report** on: an implementer question (NEED_INPUT) or a manual DEFER,
   BLOCKED, a violation, a file outside the plan, a crash, or a verify FAIL. Report at the end from the
   summary: per phase verdict and changed files; verify verdict and gates; the usage block. While a long
   hop runs, give the user `run.ps1 watch <taskId>` instead of asking them to wait silently - it is free
   and shows what the hop is reading right now.

7b. **A small edit after implementation** (no new decision). A verified run cannot be re-approved
   (`approve` needs AwaitingApproval), so:
   - **Tiny** (one or two obvious files: a string, a colour, one condition): the conductor makes it
     itself on the plan branch, runs the repo's gates, and shows the diff. No orchestrator run.
   - **Small but wider** (a few files): a new short run: a short-form plan (the phase list and the exact
     change), `run.ps1 new <newId> <repo> "<task>" -PlanFile <file> -NoReadBack`, approve per step 6,
     `implement <newId> 1`, `verify <newId>`. Its first phase needs a clean tree, so commit or close the
     previous run first.
   - Anything that needs a product decision or touches many files is a normal new plan.

8. **Close.** The user commits, merges and deletes the branch — or tells you to, one action per
   message. The orchestrator never does it. Last, once the plan is filed in the repo's
   `.ai/plans/done/` and the executor's report is checked, **`run.ps1 close <taskId>`** deletes the run's
   working files (`runs/<id>/` and `runs/_drafts/<id>.*`) — no need to ask; `runs/` is scratch, the filed
   plan is the record. Keep a run only while something still reads it (e.g. a test report a retest needs).
   Before deleting, `close` appends the run's block to **`runs/_ledger.md`** (verdicts, models, calls, opencode requests and
   opencode's token totals per session); `verify` appends one too. Both then print
   `opencode stats --days 1 --models --cost` - **put that usage block in your report to the user.**

## Running plans in parallel

- **Two repos (`Web/` + `Agent/` or `Ui/`) may run at the same time**, as two tasks (`T-WEB-…`,
  `T-AGENT-…`), only under a **shared contract**: before either plan is written, settle with the user one
  contract — route, verb, request and response JSON, error codes, permissions — and put it, word for word,
  in a "Contract" section of both plans, each naming the other plan. Neither plan may change it; a change
  stops both and goes back to the user. One preview (step 1b) covers both.
- **The client is tested against the contract, not the server**, until the server is live: unit tests and a
  fake returning the contract's shapes. The user's in-app check of the client waits for the deploy.
- **Merge order: the server first.** `Web/` is merged and deployed, then the client is merged. A client
  merged first calls a route that does not exist yet.
- **Two plans in one repo run in parallel only when their file manifests share no file**, and each in its
  own worktree (the orchestrator refuses a dirty tree, so two runs in one checkout stop each other).
  Otherwise they run one after the other. That clean-tree check (gate 3) ignores uncommitted records
  under `.ai/plans/` and `.ai/context/` (plans and session logs, not code), so they never block it.
- **Phases of one plan never run in parallel**: each builds on the one before.
- **Start parallel runs freely; the orchestrator queues them.** A `free` pool runs one hop at a time
  across every run, and a waiting run prints whom it is waiting for. Do not hand-stagger runs, and do not
  hand a run to a manual agent because another one holds the pool. Check with `run.ps1 pools`.
- **A 429 storm means a pool's limits are wrong**, not that runs must be started by hand: raise
  `minGapSec` / `rateLimitBackoffSec` for that adapter in `orchestrator.json`.
- Each parallel run still costs its own read-back.

## When something goes wrong

- `Violation` / `VerifyFailed`: report what the summary says, show the relevant diff lines, propose the
  fix; do not fix silently.
- A hop failed and the chain fell back: mention which model actually served the role.
- **A hop is slow: do not poll it yourself.** Start `run.ps1 watch <taskId>` (bare `watch` shows every
  running task) and point the user at it. It prints elapsed time, the median of the previous runs of
  that role and model (`over by m:ss` past it) and the last output line, every 90 s by default. It
  reads `runs/<id>/progress.json`, calls no model and changes nothing.
- **Two timeouts, two meanings.** `idle` = the hop printed nothing for `idleTimeoutSec` (300 s;
  `claude` and `codebuddy` have it off in `orchestrator.json` because they print nothing until they
  finish) and was killed for being silent. `total` = the hard cap (`defaults.timeoutSec`, 3600 s). The
  attempt record carries `timeoutKind`. A killed hop produced no answer - rerun the same command.
- **`NO_VERDICT` in the summary:** the reply stopped without its `VERDICT:` line. Nothing was reviewed,
  so `approve` refuses that version. Rerun the review (`revise`, `readback` or `verify`); it is not a
  finding round and its 0/0/0 count means nothing.
- **The read-back is the owner's, in a manual agent.** With the `readback` role configured, `new` / `revise`
  exit 3 after the review with `readback.v<n>` waiting. That is expected, not a failure: report the
  review, then tell the user, in one line they can paste into the agent of their choice, to read
  `orchestrator/runs/<id>/inbox/readback.v<n>.md` and do what it says (it writes
  `answers/readback.v<n>.md` itself). When the user says it is done, run `run.ps1 readback <id>` (or let
  `watch <id> -ThenResume` do it) and treat its findings like the reviewer's.
- **How each agent is reached** is the adapter's `transport` in `orchestrator.json` (described by its
  `adapterTransport` key): `cli` runs the adapter's command and reads stdout; `inbox` writes a prompt for a
  human to hand to the agent. Any adapter name can be `inbox` (a desktop app, a tool whose terms forbid
  scripting it); `manual` is the inbox by default. Inbox hops are allowed only in plan-reviewer, readback and
  verifier.
- **`exit 3` / `DEFERRED`:** an `inbox` hop (a desktop app or an interactive tool) wrote its prompt to
  `runs/<id>/inbox/<key>.md` and is waiting for a human. Nothing failed and `task.json` is untouched.
  `run.ps1 queue` lists every such request (free); `run.ps1 answer <taskId> <key> <file>` stores the
  reply (it refuses an empty file or one with no `VERDICT:` line). Then let `review` / `readback` /
  `verify` consume it - or start `watch <taskId> -ThenResume` first and it runs the right one as soon as
  the answer lands; while waiting it changes nothing, so Ctrl+C is safe. `revise` cannot consume a
  review answer: it writes v+1 and asks for a new key. The key says what the answer is valid for - a new
  plan version or a new diff needs a new answer, and one written for a tree that has since changed comes
  back as a violation instead of a pass.
- `run.ps1 health` costs model calls (one ping per role); run it only when a run fails for agent
  reasons, not before every task. `Test-ModelNames` (inside `health`) is the free half.
- The code graph is stale or missing: `run.ps1 graph` reports it (free) and the same command without
  `-Check` refreshes it (free, AST only). Do not run `graphify extract` on these repos — it drops the
  LLM-extracted concept nodes; `refresh-graph.ps1` backs a graph up before touching it.

## Who can conduct

The orchestrator is a plain PowerShell CLI; any harness with a shell can drive it. Point it at this
file explicitly:

> Read `orchestrator/CONDUCTOR.md` and `orchestrator/WORKING-MODEL.md` and act as the conductor for:
> <task>.

Caveats:

- **Do not set `-PlanFamily` to your own model.** The plan's `**Planned by:**` line names its author and
  gate 2 excludes that family (step 4). A conductor on DeepSeek that labels an Opus plan `deepseek`
  makes gate 2 skip the DeepSeek reviewer for no reason.
- **Do not add a root `AGENTS.md` rule that makes agents conductors.** Every worker runs with a repo
  (or the workspace root) as cwd and would read it; each worker prompt says it is a worker, but do not
  invite the confusion.
- Check each tool's terms before letting it drive other agents (some forbid being scripted).
- Doing it by hand also works: `run.ps1 new` without `-PlanFile` uses the planner chain, and you read
  the summaries yourself.
