# Orchestrator — review brief

## Purpose

A PowerShell orchestrator that runs several separate headless coding agents through a fixed 4-gate
workflow (plan -> independent plan review -> implement on a branch -> verify), with a human approving
at gate 2 and closing at the end. Goals: low token cost (cheap/free models do most work), no
vendor-ToS violations, never commit automatically.

Workspace root = the folder above this one. It is **not** a git repo; `Agent` (.NET 10 WPF), `Ui`
(Next.js 16) and `Web` (.NET 10 API) are three separate git repos under it.

## Files (orchestrator/)

- `orchestrator.json` — role -> fallback chain of `{adapter, model, family}`; adapters; rules;
  `verifyCommands` per repo; timeouts; `agentCwd: repo`; the `workflow` block of per-repo doc paths
  (`{repo}` is replaced with the target repo's folder name).
- `run.ps1` — commands: `health | agents | graph | new | status | approve | implement | verify |
  revise | readback`. `graph [-Check] [repo]` is the free graph-age report (`-Check` writes nothing);
  `new` first runs `refresh-graph.ps1` for the target repo.
- `lib/agents.ps1` — config loader, `Invoke-External` (process runner, stdin prompt, timeout, env
  removal), adapters (`opencode`, `claude`, `codebuddy`, `manual`, generic `exec`),
  `Invoke-Role` (walks the chain, first exit-0 wins), `New-RolePrompt` (embeds the repo's workflow doc
  + role + stage prompt), `Test-AgentHealth` (PONG ping), `Test-ModelNames` (free opencode check).
- `lib/bus.ps1` — file bus `runs/<task>/{inbox,outbox,artifacts,transcript}` + `task.json` state;
  sentinels `@@NEED_INPUT|@@BLOCKED|@@PASS`; `VERDICT`/`FINDING` parsing.
- `lib/gates.ps1` — git helpers, `Get-AgentCwd`, `Get-RepoGateDoc`, `Test-Gate2ModelDiffer`,
  `Get-GraphStatus` (is a graph still current?), `Get-SemanticIds`, `Format-Age`.
- `refresh-graph.ps1` — AST-only `graphify update` for the repos named on the command line (never a
  full rebuild), backs up a graph that holds LLM-extracted nodes (only the newest two backups are
  kept), records HEAD in `graphify-out/.orch-head` when that directory is git-ignored, optional
  `-MergeRoot`. Freshness is computed by `Get-GraphStatus` in `lib/gates.ps1` for
  `run.ps1 graph -Check`.
- `CONDUCTOR.md` — the conductor procedure (vendor-neutral; the `.claude` skill is only a pointer).
- `WORKING-MODEL.md` — who does what and why. `DESIGN.md` — the original design, kept as history.
- `tests/offline/` — mock agent + harness: the whole flow and every guard, no model calls.

## Flow

1. `new <id> <repo> "<task>"`: refresh graph -> planner (gate 1, read-only) -> plan-reviewer (gate 2,
   read-only, different model family) -> status `AwaitingApproval`.
2. Human reads both, `approve <id>` (refused if reviewer family == planner family, or if findings are
   open and no notes/`-Force` given).
3. `implement <id> [phase]`: the orchestrator checks the tree is clean, creates the branch itself and
   records the base commit; the implementer (auto, write access) edits; nothing is committed.
4. `verify <id>`: the orchestrator runs `verifyCommands[repo]` and gives the output + `git diff` to
   the verifier (read-only). Human decides commit/merge/delete.

## Current role chains (first = preferred)

- planner: claude/opus -> opencode deepseek-v4.1-flash-free -> opencode big-pickle
- plan-reviewer: opencode glm-5.3-flash-free -> opencode big-pickle -> claude/sonnet
- implementer (auto, write): codebuddy deepseek-v4.1-flash -> opencode deepseek-v4.1-flash-free -> opencode big-pickle
- verifier: codebuddy hy3 -> opencode glm-5.3-flash-free -> opencode big-pickle

## Adapter specifics

- claude: `claude -p --output-format text --model X`; read-only roles add
  `--allowedTools Read,Grep,Glob,LS,WebFetch,WebSearch --disallowedTools Edit,Write,MultiEdit,NotebookEdit,Bash`;
  `ANTHROPIC_*` env vars are stripped from the child (a dead relay used to hang it). Uses the user's
  own OAuth login.
- codebuddy (WorkBuddy CLI): `-p --output-format text --model X`; auto roles
  `--dangerously-skip-permissions`; read-only `--allowedTools Read,Grep,Glob`.
- opencode: `run -m <model> --format default [--agent plan] [--auto]`, prompt via stdin. Read-only
  roles use `--agent plan` (verified in the source workspace: it refuses writes headless).
- Excluded on purpose: freebuff (its ToS forbids scripts/orchestrators/other agents controlling it;
  used manually only), codex/qwen/MiniMax removed.

## Tested

- **This port, offline only:** `tests/offline/run-offline.ps1` — **53/53 checks** on 2026-10-20, in a
  temp workspace with three throwaway git repos and the mock agent. Beyond the run flow it covers the
  reliability work: a numbered `1. FINDING [blocker]` line counting as a blocker, a reply with no
  `VERDICT:` recorded as `NO_VERDICT` (and refused by `approve`), the plan's own `git switch -c` naming
  the branch, an uncommitted `.md` not staling the graph (an uncommitted `.cs` still does), a streaming
  hop outliving the idle timeout, a silent hop killed as `idle` or (with `idleTimeoutSec 0`) as `total`,
  a sibling repo changing being a note while the target repo still throws, `progress.json` valid at
  every read and ending `done`, the estimate being the median of past runs, `watch`, and the manual
  bridge (defer/answer/stale key/moved repo/`answer` guards/config refusal). Covers conductor mode
  (`new -PlanFile`), review FINDINGS stopping, `revise -PlanFile` v2, read-back, approve refused with
  open findings, approve freezing `plan.approved.md`, implement creating the branch, verify PASS,
  phase 2 after verify, dirty-tree refusal, a read-only role that writes, a write role failing after
  an edit (no fallback), sibling-repo change, a worker commit, tampered approval, red gate, and the
  free `Test-ModelNames` (12 rows, 8 opencode hops all `ok`). The graph checks also cover a missing
  graph, an uncommitted edit and an untracked file (but not a file under `graphify-out/`), `.orch-head`
  recorded by a refresh, a checkout of an older commit, a no-op update after an empty commit (the
  sidecar must beat the graph's mtime, and dropping the sidecar must make it stale), a graphify
  warning on stderr (printed, does not abort, next repo still processed), the `.orch-head` git-ignore
  precondition, the two-backup cap, and the id-based root-merge refusal.
- **Not yet run here:** any real model call, `health`, and a real Ui/Web/Agent task. The source
  workspace's end-to-end record (task T-UI-001 on a Next.js repo: all four roles answered, different
  planner/reviewer models, two real findings, implementation passed its four gates, verifier passed)
  is history for the code path, not a test of this workspace's paths and gate commands. `Ui` and `Web`
  were clean at port time; `Agent` had an unrelated plan in progress.

## Known weaknesses / please review

1. Reviewer findings are fed back to the planner automatically only with `-AutoRevise`, or by the
   conductor's `revise -PlanFile`; the orchestrator itself never edits the plan.
2. `approve` notes become an "APPROVED AMENDMENTS" block; there is no per-line diff of what changed.
3. Verdict parsing is regex-based (`PASS` / `@@PASS`); reply sentinels are produced by the model and
   may be missing or malformed (a missing verdict is treated as not-passed).
4. Fallback is silent when a model name is wrong (hop just fails and the next is tried); only the
   `attempts` list in the error/transcript shows it.
5. Gate-2 independence is checked by model family string only; two names can be the same underlying
   model.
6. `agentCwd: repo` keeps agents in the right repo, but write isolation still rests on the prompt plus
   the fingerprint check — not on a sandbox. Auto roles use skip-permissions flags.
7. `verifyCommands` run via `powershell -Command` with the repo's full privileges; output tail is
   limited to 6000 chars. Ui's `npm run build` makes each verify slower.
8. No concurrency/locking across tasks (only per-run), no resume after a crash mid-hop, no cost/token
   accounting. Timeouts are a per-hop cap (`defaults.timeoutSec`, 3600 s) plus an idle timeout
   (`idleTimeoutSec`, 300 s, per-adapter override), so a quiet hop is still killed by inactivity.
9. The `manual` hop works, but only where a human answer cannot decide an edit: `plan-reviewer`,
   `readback` and `verifier`. It never waits - the prompt goes to `runs/<id>/inbox/<key>.md`, the command exits 3
   (`DEFERRED`), and `run.ps1 answer` stores the reply in `answers/<key>.md`. The key is what the answer
   is valid for (`plan-reviewer.v<round>`, `readback.v<round>`, `verifier.<hash of gates+diff>`); an
   answer for an older key is ignored, and one whose repo moved since the prompt is a violation. What
   consumes it: `review` (gate 2 for the version on disk), `readback`, `verify` - **`revise` cannot**,
   because it writes v+1 and would ask for the new key. `queue` lists what is waiting (free) and
   `watch <id> -ThenResume` waits for the answer and runs the consumer. A config with `manual` in
   `planner` or `implementer` refuses to load. Freebuff stays manual-only: it is the `readback` role's
   one hop (0.5.4), answered by the owner by hand.
10. Tool sessions are not recorded in the tools' own histories (headless runs); only
    `runs/<id>/transcript/*.json` exists.
11. A full `graphify extract` would drop a graph's LLM-extracted concept nodes (Agent 75, Ui 15,
    Web 48 at port time). `refresh-graph.ps1` never runs one and backs the graph up before `update`;
    the repos' own graphify docs still describe the rebuild fallback without that warning.
12. Graph freshness is inferred, not verified: `graph -Check` knows that HEAD moved and that files
    changed, not that `graphify update` actually ingested them. With no `.orch-head` (any graph
    refreshed before 0.5.2) it falls back to mtime, which a no-op update can leave misleading; the
    next refresh writes the sidecar.

## Changes in 0.3.0 (after the first review)

- Timeout kills the whole process tree (`taskkill /T`); pipe reads are bounded, so a stuck child can no
  longer hang the orchestrator.
- Every reply must end with `VERDICT: ...` (role-specific values); parsed by `Get-Verdict`, missing =
  not passed. Reviewer findings are `FINDING [blocker|should|nit] ...`. Orchestrator-mode rules are
  appended to every prompt (no approval questions, verifier never offers to commit).
- Gate 2 compares model **families** (`family` per hop) and skips same-family reviewer hops at run time.
- Revise loop: `new` auto-revises while the verdict is FINDINGS (`revise.maxAutoRounds`, default 2);
  `revise <id> "notes"` adds a round by hand and answers planner NEED_INPUT.
- `approve <id> "notes" [-Branch] [-Force]` freezes `plan.approved.md` (sha256-checked later); refuses
  a non-PASS review without notes or `-Force`.
- Gate 3 in code: the orchestrator checks the tree is clean, creates the branch itself and records the
  base commit. After each phase: sibling repos unchanged, branch unchanged, no commits since base,
  files not mentioned in the plan (warning). Hard breaks -> status `Violation`.
- Workspace fingerprint before/after every hop: a read-only role that changes any repo is stopped; a
  write role that fails after editing gets no fallback.
- opencode read-only roles run with `--agent plan`.
- Failure classes (`timeout`, `rate-limit` (one retry), `bad-model`, `auth`, `empty-output`); each
  failed hop is printed. `health` first checks opencode model names for free.
- Verify: red verifyCommands or a violation = FAIL whatever the model says; diff is against the base
  commit and capped. Phases can repeat (`Implemented -> Verified/VerifyFailed -> implement next phase`).
- Per-run lock file; `phase` defaults to 1; `-Config` for alternative configs; generic `exec` adapter
  (used by the offline mock test).

## Changes in 0.4.0 (conductor mode)

- `new ... -PlanFile <plan.md> [-PlanFamily claude] [-AutoRevise]`: a plan written by the chat
  conductor enters at gate 2. Findings stop for the conductor; `revise <id> -PlanFile <v2>` submits the
  next version.
- Gate 2.5 read-back: after a PASS review the implementer chain runs read-only (`--agent plan` /
  allowed-tools) and lists where it would have to guess; its findings count at approval and feed
  auto-revise. `readback <id>` runs it by hand; `-NoReadBack` / `readback.enabled` turn it off.
- Every command ends with a compact `=== SUMMARY ===` block (status, verdicts, stale markers, next
  command) so the conductor reads little.
- Worker prompts state they are workers and must never run the orchestrator. Procedure:
  `CONDUCTOR.md`; decisions: `WORKING-MODEL.md`.

## Changes in 0.5.0 (port to Project B)

- Repos are `Agent`, `Ui`, `Web`; `verifyCommands` are read from each repo's `.ai/coding/ci.md`
  (Ui: typecheck, lint, test, build; Web: `dotnet build ProjectB.Web.sln` /
  `dotnet test ProjectB.Web.sln`; Agent: `dotnet build` / `dotnet test`).
- `workflow` paths carry `{repo}`; `New-RolePrompt` resolves them per target repo and falls back to the
  `## <role>` section of `.ai/agents/README.md` where a repo has no per-role files (Ui). The prompt
  also lists the repo's `AGENTS.md`, `.ai/README.md`, plan template and — for Agent/Ui — Web's
  `api-surface-v3.md`.
- `defaults.agentCwd` is `repo`, not `root`: workers get the repo's own rules instead of the root
  router, and read siblings as `../Web` etc.
- `refresh-graph.ps1` reads the repo list from `orchestrator.json`, refreshes only the repos it is
  given (run.ps1 passes the target), backs up a graph that holds LLM-extracted nodes, never runs a full
  rebuild, and builds a merged root graph only with `-MergeRoot`.
- `CONDUCTOR.md` replaces the source's tool-specific skill; `.claude/skills/project-b-orchestrate/`
  contains a pointer with no rules.
- Free tests: `run.ps1 help`, `agents`, three `new -DryRun` prompts (Ui/Web/Agent) with the target
  repo's workflow doc and the orchestrator-mode section, and the 19-check offline harness.

## Changes in 0.5.1

- `run.ps1 graph [-Check] [repo]`: a **free** report of each `graphify-out/graph.json` against HEAD
  (state, HEAD, commit/graph times, how far behind, whether the graph holds LLM-extracted nodes).
  `-Check` writes nothing and calls no model; without it the named repo (or all) is refreshed.
- `CONDUCTOR.md` now checks this graph before writing a plan. The read-only worker roles have no
  shell, so they cannot refresh it themselves — and for conductor mode the `new`-time refresh happens
  after the plan is already written.
- `Graphify check-update` was deliberately **not** used: it took over 90 s on the Ui graph; age vs HEAD
  is deterministic and instant.

## Changes in 0.5.2

Four defects in the 0.5.1 graph check, ported from the DrivingScore workspace:

- **A graphify warning no longer aborts the refresh, and is no longer swallowed.** graphify writes
  warnings to stderr and still exits 0; PS 5.1 turns a native command's stderr into a terminating
  error under `$ErrorActionPreference = 'Stop'`. The refresh had already stopped aborting, but it
  only echoed `community set changed` notes, so a real warning (`tree_sitter_sql not installed`) was
  invisible. Now the first line matching `warning` is printed and the exit code alone decides.
- **Uncommitted work marks the graph stale.** `Get-GraphStatus` scans `git status --porcelain
  --untracked-files=all` (skipping `graphify-out/`) and flags any file modified after `graph.json`,
  so an in-progress phase no longer leaves the graph looking fresh.
- **HEAD is recorded, not guessed.** A successful refresh writes the repo's HEAD to
  `graphify-out/.orch-head`, and when that file exists it wins over the mtime/commit-time check —
  `graphify update` does not rewrite `graph.json` when no code file changed, so its timestamp can be
  older than a commit the refresh already covered. A checkout of an older commit is caught too. The
  sidecar is written only after `git check-ignore` confirms `graphify-out/` is ignored, so it can
  never dirty a checkout.
- **Backups are capped.** Only the newest 2 `graph.json.bak-*` per repo are kept (5–25 MB each; the
  Agent repo had already collected two).
- The opt-in `-MergeRoot` now refuses only when the existing root graph holds a semantic **node id**
  that no repo graph has, instead of refusing whenever the root holds any semantic node.
- `run.ps1 graph -Check` prints a `Reason` column (`stale: Agent - refresh with: run.ps1 graph`).

Read-only check on this workspace after the change: `Agent` stale (one plan-0081 file newer than its
10:25 graph), `Ui` and `Web` fresh; `graphify-out/` ignores confirmed in all three.

## Changes in 0.5.3

The D7 manual hop could not be finished from the outside: nothing consumed a `plan-reviewer.v<round>`
answer. Every path into the review wrote a new version first (`new` runs the planner, `revise` adds
v+1), so the command asked for `v<round+1>` and the answer stayed orphaned. Three additions close that;
the hop itself stays dormant - no chain in `orchestrator.json` uses `manual`.

- **`run.ps1 review <taskId>`.** Gate 2 again for the plan already on disk: no new version, no planner
  call. Refused once the plan is approved or implemented. `-NoReadBack` applies as elsewhere.
- **`run.ps1 queue [taskId]` (free).** Every inbox request without a non-empty answer, with task, key,
  repo, recorded state, age, both paths, and the two next commands. Filesystem only: no model, no lock,
  nothing written.
- **`run.ps1 watch <id> -ThenResume [-PollSec 5]`.** Waits for the one pending answer, then runs the
  consumer (`review` / `readback` / `verify`, by key prefix). While it waits it is read-only, so Ctrl+C
  changes nothing; it resumes only on a file that already carries a `VERDICT:` line, so a half-saved
  file cannot be consumed. Two pending requests, or a key no command consumes, is refused, not guessed.
- Known sharp edge, documented rather than hidden: the verifier key hashes the gate output **and** the
  diff, so a run whose gate output is not byte-identical asks for a new key. The earlier answer is then
  simply unused and `verify` defers again; `queue` shows the new request.
- Offline harness: `T-Q1` (queue lists a request, then drops it once answered), `T-R1` (a deferred
  review is consumed by `review` with the round unchanged), `T-W1` (a real `watch -ThenResume` process
  waits without taking the lock, then consumes the answer). 59/59 checks pass.

## Changes in 0.5.4

The implementer read-back is done by hand in Freebuff (the owner's subscription; its terms forbid
scripting it), while implementation stays automatic on opencode.

- **A `readback` role in `orchestrator.json`**, one `manual` hop (`freebuff/deepseek-v4.1-flash`, family
  `deepseek`). `Invoke-ReadBack` uses it when present, else the implementer chain read-only, as before.
  The D7 validator allows `manual` in `readback`; `planner` and `implementer` still refuse it.
- **Requested before the review.** With a manual read-back, `new` / `revise` / `review` write
  `inbox/readback.v<round>.md` first, run the review as usual and record it, then exit 3. The owner works
  in Freebuff while the reviewer works; an automatic read-back still runs only after a PASS.
- **The request says where its answer goes.** Every inbox file now starts with a short header: the
  absolute `answers/<key>.md` path to write, the VERDICT rule, "do not edit anything else", and the repo
  root to read relative paths against (Freebuff runs at the workspace root, not in the repo).
- **Stale requests are history, not pending.** `queue` and `watch -ThenResume` skip a
  `plan-reviewer`/`readback` key for an older round or an approved run, and a `verifier` key that is not
  the newest while a verify is due. Before, an old `readback.v1` next to a new `readback.v2` made
  `-ThenResume` refuse with "two requests waiting".
- The DEFERRED exit names the consuming command (`readback` / `review` / `verify`) instead of "rerun the
  same command", which was wrong after `new` or `revise`. `health` reports a manual hop as "not pinged"
  instead of throwing for want of a key.
- Offline harness: `T-RB1` (request before review, review recorded, exit 3, header present), `T-RB2`
  (an answer the tool wrote itself is consumed by `readback`), `T-RB3` (v2 asks for `readback.v2`; queue
  shows only it), `T-RB4` (of two verifier requests only the newest is pending); `T-M6`'s message
  updated; `S15` counts 13 hops (the readback hop has no model list to check). **63/63 checks pass.**
- **`verify` is told the phase scope.** Verify runs after every phase, but the verifier saw only the whole
  plan and judged phase 1 of `T-AGENT-005` (plan 0084) against the full definition of done: gates green,
  the phase's own +5 test target met, and still FAIL for phase 2's absence. The prompt now lists the phases
  implemented so far and says to verify only those, against their own phase gates. The rerun passed.

## Review questions

- Is the gate enforcement strong enough, or should it be in code rather than prompts (e.g. refuse
  implement if not on a fresh branch, refuse verify if diff touches files outside the plan manifest)?
- Better fallback/retry design (classify errors, backoff, detect wrong model names at preflight)?
- Safer permission model for auto roles?
- Should a `Ui` verify really run `npm run build` every phase, or only before approval?
