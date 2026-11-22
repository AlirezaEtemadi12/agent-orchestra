# Run reliability — activity timeout, scoped guard, honest parsing, and a non-blocking manual hop

Status: **Implemented (2026-10-20).** Written by the conductor session after plans 0081–0083 exposed each problem
below on a real run. Executor: one agent session working **only inside `orchestrator/`**.
Offline suite: **53/53** (baseline 36 + the 17 tests this plan asks for). Implementation notes are in §6.

## 0. Before the first edit

- `orchestrator/` is **not a git repository**. Before editing, copy the folder to
  `orchestrator.bak-<yyyyMMdd-HHmm>` beside it (excluding `runs/`) so the owner can diff and roll back.
- **Do not edit while a run is in flight.** Check that no `powershell … run.ps1` process is running
  (`Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? CommandLine -match 'run\.ps1'`) and that no
  `runs/*/.lock` exists. If one does, STOP and say so.
- Baseline: `powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1` — record the real count
  (36/36 at the time of writing). A red baseline is a STOP.
- **Never call a real model, never touch `Agent/`, `Web/`, `Ui/`** or anything under `runs/`. Every test goes
  through `tests/offline/mock-agent.ps1`. No `run.ps1 health`.
- Nothing is committed anywhere (there is no repo). Hand over with a list of changed files.

## 1. What went wrong (evidence)

| # | Seen on | Problem | Where |
|---|---|---|---|
| P1 | T-AGENT-002 v9 | Both reviewers were killed at 900 s while visibly working (stderr shows them reading files). The timeout is on **total** wall time, not inactivity. | `lib/agents.ps1` `Invoke-External` ≈ L106 (`WaitForExit($TimeoutSec*1000)`) |
| P2 | T-AGENT-002 read-back | A commit in **`Ui/`** by another session cancelled an **`Agent/`** read-back after 9 minutes: the read-only guard fingerprints every repo. | `Invoke-Role` ≈ L242–L253 (`Get-WorkspaceFingerprint $Ctx`) |
| P3 | T-AGENT-002 v6 read-back | The summary said «blocker 0, should 0» while the reply listed 3 blockers and 10 shoulds, because the lines were `1. \`FINDING [blocker] …`: `Get-Findings` does not allow a list number before `FINDING`. | `lib/bus.ps1` `Get-Findings` ≈ L113 (and whatever `Get-Counts` uses) |
| P4 | T-AGENT-002 v8 | A review that stopped mid-reply (exit 0, no `VERDICT:` line) was recorded as `FINDINGS (0/0/0)`, indistinguishable from a real finding round. | `run.ps1` `Invoke-ReviewStep` ≈ L228 |
| P5 | T-AGENT-003 approve | The branch was named `alireza/t-agent-003` although the plan says `git switch -c plan-0081-mofid-quantity-after-freeze` in its §0 code block. | `Get-PlanBranch` (called from `Cmd-Approve`) |
| P6 | `graph -Check` | Four uncommitted **`.md`** files made `Agent` «stale»; `graphify update` never rewrites `graph.json` for docs, so a refresh cannot clear it. | `lib/gates.ps1` `Get-GraphStatus` ≈ L232–L241 |
| P7 | design | The `manual` adapter is a stub (`Invoke-Manual` ≈ L170, always `ExitCode -2` → `exit -2` → next hop). | `lib/agents.ps1` |

## 2. Decisions (decide nothing new; STOP if the code contradicts one)

**D1 — Activity timeout (P1).** `Invoke-External` stops waiting on total time alone. It reads stdout/stderr through
`OutputDataReceived` / `ErrorDataReceived` (appending to `StringBuilder`s, recording `lastActivity = now` on every
line) instead of `ReadToEndAsync`, and loops `WaitForExit(1000)`:
- kill when `now − lastActivity > IdleTimeoutSec` **(only when `IdleTimeoutSec > 0`)**, or when
  `now − start > TimeoutSec` (the absolute cap);
- every 60 s of a running hop print one host line: `  [<exe>] working, last output <n> s ago`;
- the result keeps `ExitCode`, `Stdout`, `Stderr`, `TimedOut`, and adds `TimeoutKind` (`'idle'`, `'total'` or
  `$null`). `Get-FailureClass` returns `'timeout'` for both (unchanged), and the attempt record carries the kind.
- Config: `defaults.timeoutSec` becomes the cap, **3600**; new `defaults.idleTimeoutSec` **300**; a per-adapter
  `"idleTimeoutSec"` overrides it. **`claude` gets `"idleTimeoutSec": 0`** (`-p --output-format json` prints
  nothing until it ends). `opencode` uses the default (it streams progress on stderr — the T-AGENT-002 stderr tails
  prove it). For `codebuddy`, look at its stderr/stdout in an existing `runs/*/transcript/*.json`: if it shows
  progress lines, use the default; if not, set `0` and say so in the hand-over.
- The BOM-less stdin handling (≈ L91–L105) and `Stop-ProcessTree` stay exactly as they are.

**D2 — Guard the target repo only (P2).** `Invoke-Role` gets a `[string]$Repo` parameter (every caller in
`run.ps1` passes `$State.repo` / `$Repo`). Fingerprints are taken for that repo only; a change detected in **another**
configured repo is printed as one yellow line (`  [<role>] note: <repo> changed meanwhile (not this run)`) and
never throws. The read-only/write rules for the target repo are unchanged. `Get-WorkspaceFingerprint` gains an
optional `-Repos` filter rather than a second function.

**D3 — Findings parse list items (P3).** The `FINDING` regex (and every counter built on it) accepts an optional
list marker before the decoration: `^\s*(?:\d+[.)]\s+|[-*+]\s+)?[\s>*_`#-]*FINDING\s*\[(blocker|should|nit)\]`.
It also accepts the level **without brackets** (`FINDING should §9 …`, seen in T-AGENT-004's v2 read-back, which the
summary then counted as 0/0/0): `FINDING\s*(?:\[(blocker|should|nit)\]|(blocker|should|nit)\b)`, taking whichever
group matched. Add a fact for it (T-F3, **+1**). `Get-Verdict` already tolerates decoration; leave it.

**D4 — No verdict is its own state (P4).** When a review, read-back or verify reply has no `VERDICT:` line, the
recorded verdict is **`NO_VERDICT`** (not `FINDINGS`), and the summary line reads
`review:   NO_VERDICT (reply ended without a verdict — rerun) by …`. `approve` treats `NO_VERDICT` exactly as an
unreviewed version (refused without `-Force`). Everything that today checks `-eq 'FINDINGS'` / `-ne 'PASS'` keeps
treating it as not-passed — grep every use and list them in the hand-over.

**D5 — Branch from the plan's own command (P5).** `Get-PlanBranch` first looks, anywhere in the plan (code blocks
included), for `git switch -c <name>` or `git checkout -b <name>` and takes the first `<name>`; only then the
existing backtick rule; only then the `<gituser>/<taskid>` fallback. The name is validated with
`git check-ref-format --branch` semantics (or the regex already used, if one exists).

**D6 — Docs never stale the graph (P6).** In `Get-GraphStatus`, an uncommitted file counts only when its extension
is **not** `.md` (case-insensitive). Nothing else changes; the `.orch-head` sidecar logic stays.

**D7 — The manual hop is non-blocking and keyed by what it answers (P7).** The design reviewed on 2026-10-20 is
adopted **with these changes**: no waiting loop, no `attempt` counter, no `outbox/` (that folder already holds
`Save-Outbox` copies).
- **Key** = what the answer is valid for: `plan-reviewer.v<round>`, `readback.v<round>`,
  `verifier.<first 12 hex of SHA-256 over the gate output + git diff the verifier is given>`. Callers pass it to
  `Invoke-Role -ManualKey`.
- `Invoke-Manual` (needs `$Ctx`, task id, key, prompt, target repo path):
  1. If `runs/<id>/answers/<key>.md` exists and is non-empty → check the target repo's fingerprint against
     `runs/<id>/inbox/<key>.fingerprint`; equal → return `ExitCode 0`, `Stdout` = the file; different → return
     `ExitCode -3`, `Stderr = 'manual: the repo changed after the prompt was written'` (a violation, D7 last bullet).
  2. Else, if `inbox/<key>.md` is missing → write it and `inbox/<key>.fingerprint` atomically (temp file in the
     same folder + `Move-Item -Force`).
  3. Return `ExitCode -2`, `Stderr = "manual: deferred — answer expected at <path>"`. **Returns at once; never waits.**
- `Get-FailureClass`: `-2` with `manual: deferred` → `'deferred'`; `-3` → `'manual-violation'`.
- `Invoke-Role`: `'deferred'` throws `DEFERRED: <stderr>` **without trying the next hop** (a human may still answer);
  `'manual-violation'` throws like today's read-only violation. `run.ps1`'s top-level `catch` prints a `DEFERRED`
  error in cyan, leaves `task.json` untouched and exits **3** (other errors stay exit 1).
- New command `run.ps1 answer <taskId> <key> <file>`: refuses an empty file and a file with no `VERDICT:` line,
  then copies it atomically to `answers/<key>.md`. Rerunning the original command (`revise`, `readback`, `verify`)
  consumes it. A key already answered is refused unless `-Force`.
- **Where manual may appear:** config load fails (clear message) if a `manual` hop is in the `implementer` or
  `planner` chain. It is allowed in `plan-reviewer` and `verifier` only. A manual hop **must** declare `family`
  (the model the human will use), so gate-2 family exclusion still works.
- Freebuff stays manual-only (REVIEW-BRIEF: its terms forbid being scripted); this hop never launches anything.

**D8 — Progress the owner can watch, at zero model cost.** Nothing here calls a model or needs the conductor:
the orchestrator writes the facts, and the owner reads them in his own terminal.
- **`runs/<id>/progress.json`**, rewritten atomically (temp + `Move-Item`) by `Invoke-External`'s wait loop
  **every 30 s** and once at the start and end of every hop: `{ taskId, command, role, adapter, model, phase?,
  startedAt, elapsedSec, lastOutputAgoSec, lastLine, estimate: { typicalSec, basis }, state }` where `state` is
  `running` / `done` / `failed` / `deferred`, and `lastLine` is the last non-empty output line, cut to 160 chars,
  with ANSI escapes stripped (it shows things like `→ Read Src/…/MofidVm.cs`). `Invoke-External` gets the path
  through an optional `-ProgressFile` parameter that `Invoke-Role` passes; with no path it writes nothing (the
  offline tests that do not care stay unchanged).
- **The estimate** is the median `seconds` of the **successful** attempts of the same role (`plan-reviewer`,
  `readback`, `implementer`, `verifier`) and same `adapter:model`, read from every `runs/*/transcript/*.json`
  (they already record `seconds` per attempt), scaled by plan size: `typical × (this prompt's length ÷ the median
  prompt length of those runs)` when the transcripts store the prompt length, otherwise unscaled. `basis` says
  which (`"median of 7 runs, scaled by size"` / `"median of 3 runs"` / `"no history"`). Fewer than 2 runs →
  `typicalSec = null`. It is a hint, never a timeout.
- **`run.ps1 watch [taskId] [-Every <sec, default 90>]`** — a loop that clears the screen and prints one block per
  running task (all tasks with a `progress.json` in `running` state when no id is given), until Ctrl+C:
  ```
  T-AGENT-002  revise → plan-reviewer  opencode:glm-5.3-flash-free
    elapsed 12:40   typical ≈ 14:00 (median of 5 runs, scaled by size)   ~90%
    last output 8 s ago: → Read Src/03.Endpoint/ProjectB.Agent/Pages/Mofid/MofidVm.cs
  ```
  The percentage is `elapsed ÷ typical`, capped at 99 % while running and shown as `over by m:ss` past it. It reads
  files only; it never starts, stops or changes a run.
- The 60 s host heartbeat line of D1 prints the same numbers, so a log tail shows them too.

## 3. Phases and gates

Each phase: `run-offline.ps1` green with the new tests, count up, nothing outside `orchestrator/` touched.

1. **Phase 1 — D3, D4, D5, D6** (parsers and small rules). Tests: T-F1 a numbered `1. \`FINDING [blocker] …` counts
   as a blocker; T-F2 a reply without `VERDICT:` records `NO_VERDICT` and `approve` refuses it; T-B1 the plan's
   `git switch -c plan-0081-…` wins over the fallback; T-G1 an uncommitted `.md` newer than the graph leaves the
   repo `fresh`, an uncommitted `.cs` still makes it `stale`; T-F3 `FINDING should …` (no brackets) counts as a
should. **+5.**
2. **Phase 2 — D1, D2, D8.** Teach `mock-agent.ps1` two behaviours: print a line every N ms for M ms, and sleep
   silently for M ms. Tests: T-T1 a mock that prints every 200 ms for 3 s with `idleTimeoutSec 1` and cap 10 s
   finishes OK; T-T2 a silent mock with `idleTimeoutSec 1` is killed with `TimeoutKind 'idle'` well before the cap;
   T-T3 `idleTimeoutSec 0` + cap 2 s kills a silent mock with `'total'`; T-W1 a read-only hop during which a
   **different** configured repo changes succeeds (a note, no throw), and one during which the target repo changes
   still throws; **D8:** T-P1 a printing mock leaves a `progress.json` whose `lastLine` is the mock's last line, whose
`state` ends `done`, and which is valid JSON at every read during the run (read it 5 times while it runs); T-P2 the
estimate from three fake transcripts of 60/90/120 s is 90 with `basis` naming 3 runs, and from one is null;
T-P3 `watch` with `-Every 1` over a fake `progress.json` prints the block once and exits when the file says
`done` (give `watch` an internal `-Once` switch for the test). **+7.**
3. **Phase 3 — D7.** Tests: T-M1 first call writes `inbox/<key>.md` + fingerprint and exits 3 with the task state
   unchanged; T-M2 after `answer`, the rerun consumes it and records the review; T-M3 an answer for `v7` is ignored
   by a `v8` review (the key differs); T-M4 a repo change after the prompt → violation; T-M5 `answer` refuses an
   empty file and one without `VERDICT:`; T-M6 a config with `manual` in `implementer` fails to load. **+6.**
4. **Phase 4 — docs.** `CONDUCTOR.md` («When something goes wrong»: idle vs total timeout, `NO_VERDICT`, `DEFERRED`
   + `answer`), `REVIEW-BRIEF.md` item 9 (no longer a stub), `DESIGN.md` §3 manual bullet (non-blocking, keyed),
   and this file's Status → Implemented with the real count.

**Target: baseline + 18.** Phase 4's `CONDUCTOR.md` change also tells the conductor to point the owner at
`run.ps1 watch` when a long hop starts, instead of polling it itself. Stop and show the owner after each phase.

## 4. Stop and ask

- A test needs a real model, a network call, or a write outside `orchestrator/` (backup folder excepted).
- `OutputDataReceived` loses the BOM-less stdin behaviour or breaks the `.cmd` → `cmd.exe` launch path.
- Any existing offline test has to change its assertion (beyond being re-pointed at a renamed function).
- `Get-PlanBranch` or `Get-Counts` do not exist under those names — find the real ones and say which.

## 5. Out of scope

Polling inside the manual hop, a GUI for answers, automating any interactive tool (freebuff included), changing
role chains or models, and the Trade/Agent code.

## 6. Implementation notes (what differed, and why)

- **D1, one mechanism changed.** `OutputDataReceived` / `ErrorDataReceived` deliver nothing while the runspace
  is busy (measured on PS 5.1: 0 lines captured from a child that printed every 200 ms for 3 s), so the wait
  loop drains `ReadLineAsync` instead. Same semantics: `lastActivity` per line, idle kill only when
  `idleTimeoutSec > 0`, absolute cap, 60 s heartbeat, `TimeoutKind` on the result.
- **D1, `idleTimeoutSec: 0` for `codebuddy` too.** Its transcripts (`verifier`, `readback`) show 0 stderr lines:
  it streams no progress, so an idle timeout would kill a working hop. `opencode` keeps the default (its stderr
  carries hundreds of lines per run); `claude` is off as the plan says.
- **D1, line endings.** The reader joins lines with LF, not `Environment.NewLine`: callers split captured output
  on `` `n ``, and CRLF put a `\r` inside every git porcelain path (`Test-Path` then failed with "Illegal
  characters in path").
- **D2, the guard is scoped, the fingerprints are not.** Detecting that another repo moved (the note) needs that
  repo's fingerprint, so a hop still takes all three; what changed is that only the target repo can throw.
  `Get-WorkspaceFingerprint -Repos` exists for callers that want one repo.
- **D7, the verifier key is a hash of what it was given**, as specified; that means a repo change also changes the
  key, so the `-3` path is reachable in practice through a round-keyed hop (`plan-reviewer.v<round>`) and was
  tested there (`Invoke-Manual` directly).
- **D7, one bug found by the new test:** `Get-Content -Raw` returns a string carrying `PSPath`/`PSDrive` notes, and
  serializing it into a transcript pulled the provider object graph in (98 MB of JSON for a 46-character answer —
  it looked like a hang). The answer is read with `[System.IO.File]::ReadAllText` now.
- **D7, config errors moved inside `run.ps1`'s try**, so a config that must not load is one red line instead of an
  uncaught stack trace.

### Follow-up by the conductor session (2026-10-21)

- **D1, a regression fixed: stdin deadlock.** The first implementation wrote the whole prompt to stdin
  *before* it started reading, so a child that prints more than a pipe buffer before reading its stdin
  deadlocked with the parent, and no timeout could fire (reproduced: 300 KB in, 800 KB out, hung past 90 s).
  stdin is now written with `WriteAsync` after both readers start, and closed inside the wait loop.
- **D1, throughput.** `ReadLineAsync` per line plus a 500 ms wait between drains moved about 190 lines/s
  (800 KB took over 100 s with the child blocked on a full pipe; the verify gate's `dotnet build/test` and every
  `git` call go through the same path). `Receive-PendingLines` now reads 8 KB chunks with `ReadAsync`, drops
  every `\r`, and the loop re-polls at once while data flows: the same case takes ~5 s.
- **D3, T-F3 added:** a level without brackets (`FINDING should …`) counts; T-AGENT-004's v2 read-back, counted
  0/0/0 before, now parses as 3 should + 9 nit.
- **D1, opencode idle timeout 600 s:** a heavy implement on T-AGENT-004 went 305 s silent and was cut at 300.
- New test T-T4 (the deadlock case). Offline suite: **55/55**.
