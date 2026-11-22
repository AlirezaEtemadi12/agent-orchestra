# Plan: a faster offline suite (SUITE-SPEED)

**Status:** draft. Waiting for the owner's review. Nothing has been implemented.
**Author:** Claude Opus 5.5 (conductor session, 2026-11-14). **Executor:** another agent.
**Scope:** only `orchestrator/tests/offline/`. No orchestrator code (`lib/`, `run.ps1`, `orchestrator.json`) changes.

## فهم من از درخواست

- **حالا:** هر اجرای کامل تست‌های آفلاین ارکستر (۱۰۵ چک) حدود ۱۰ دقیقه طول می‌کشد. پیاده‌سازی PAID-CHAINS به همین خاطر چهار ساعت طول کشید.
- **قانون جدید:** قانون «فقط یک بار در آخر» از قبل در سرِ `run-offline.ps1` ثبت شده است. این پلن خودِ اجرا را سریع‌تر می‌کند.
- **اجرای کامل:** موازی و در حدود ۳ تا ۴ دقیقه.
- **اجرای بخشی:** فقط تست‌های یک بخش، مثلاً `-Only P-` یا `-Only T13`، در کمتر از یک دقیقه.
- **محدودیت:** هیچ تستی حذف یا ضعیف نمی‌شود و تعداد چک‌ها همان ۱۰۵ می‌ماند.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

```
powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1 -List
powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1 -Only T13,T14
powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1            # parallel, all 105
powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1 -Serial    # the old way, for comparison
```

- `-List` sections and their ids are printed.
- `-Only` only those sections run, and the count is lower.
- The default run reports `105/105`, with the wall time, in about a third of today's time.

## §0 Preconditions

- **Branch:** `alireza/paid-chains` is at `d326d40` or later.
  - **Tree:** must be clean apart from this untracked plan.
  - **New branch:** create `alireza/suite-speed` from it.
- **Baseline:** run the current suite once with `-Serial` semantics (today's script as is). Record the count (105) and the wall time.
  - **If red:** STOP.
  - **If one red passes on an immediate rerun:** it is flaky (P-9 is known to be). Note it and continue.
- **No real model:** never call one. S15 runs the free `opencode models` listing, as today.
- **Suite runs:** the baseline, then **once at the end** of each of phases 2 and 3, plus the `-Only` smoke checks in T-S1. Never more.

## §1 Facts the plan relies on (verified 2026-11-14)

- **Layout:** `run-offline.ps1` (2387 lines) is one linear script.
  - **Prologue:** helpers and setup end just before the first `# ---------------- S1/...` marker (line 526).
  - **Sections:** each test section starts with a line `# ---------------------------------------------------------------- <ids>: <title>`.
  - **Report:** the `report` section is last.
- **Isolated workspaces:** every workspace is a fresh `%TEMP%\project-b-orch-<tag>-<rand>` with its own `config.json`. Pool state lives under that workspace's `runs/_pool`. Two workspaces never share files.
- **Reused workspaces:** some sections create no workspace and reuse the previous section's `$ws` or variables. As of today: T-F1, T-F3, T-T4, T5, T6, T17, S15 and the "quota-pool helpers" block, which only defines functions.
- **Env vars:** `Run-Orch` sets `MOCK_MODE` / `MOCK_LOG` / `MOCK_PROMPT_DIR` as process env vars. Parallel sections can therefore not share one PowerShell process; they need separate processes.
- **Timing-sensitive sections:** the `P-*` sections assert wall-clock timings. P-9 failed once under load (6.4 s against a 3 s limit). So do `T-T1..T-T4` (activity timeouts) and `T-P1..T-P3` (progress/watch).
- **Where the time goes:** about 8.4 of the ~11 minutes is the 152 `run.ps1` child processes (about 3.3 s each).
  - **Workspaces:** each of the 68 workspaces also runs ~18 `git` processes (`init`, `checkout`, `config`, `add`, `commit`, per repo).

## §2 Decisions

- **D1. Split by markers, never rewrite the test bodies.** The runner reads its own file:
  - prologue = everything before the first section marker;
  - sections = marker to the next marker;
  - the `report` section is replaced by the runner's own report.
  Test code is moved only where D2 requires it.
- **D2. Groups.**
  - **Joining the previous group:** a section with no `New-WorkSpace` call joins the group of the section before it. The "quota-pool helpers" block defines only functions, so it moves into the prologue.
  - **The groups table:** written as a literal array at the top of the runner, not computed at run time. Each group has an id list taken from its markers and a `timing` flag.
  - **Tags make the ids unique:** today T1–T8 and T-W1 appear twice: graph tests and paid-chains tests, two T-W1 sections. Each group carries a unique tag (`graph-T7`, `sess-T7`, `W1-guard`, `W1-watch`, …). `-Only` matches tags and check ids alike.
- **D3. The timing group:** every group whose markers include `P-`, `T-T` or `T-P` is flagged `timing`. In a parallel run, the timing groups run **alone, one after another, after** the parallel groups have finished, so no load is on the box. That is slower than mixing them in, but P-9 stays honest.
- **D4. Parallel run:**
  - **Workers:** `-Workers N` (default `[Math]::Min(4, [Environment]::ProcessorCount)`).
  - **How a worker runs:** each worker is a `powershell -NoProfile -File run-offline.ps1 -Group <tag>[,<tag>…] -ResultFile <tmp.json>` child. Its own output goes to `<tmp>.log`.
  - **Merging:** the parent assigns groups to workers longest-first, using a literal per-group cost estimate (in seconds, from the baseline log) kept in the groups table. It then waits and merges the result files into one table, in the original section order.
  - **When a child fails:** if a child exits without writing its result file, every check of its groups is reported FAIL with `Detail = "worker crashed; see <log>"`.
- **D5. Switches** (new, all optional):
  - `-List` prints the groups table (tag, ids, timing, cost) and exits 0.
  - `-Only <tag-or-id>[,…]` runs only the matching groups, serially.
  - `-Serial` runs every group in one process, in order (today's behaviour).
  - `-Workers N`: see D4.
  - `-Group` and `-ResultFile` are internal, for workers.
  - `-Keep` keeps today's meaning and is passed on to the workers.
  - With no switch, the run is parallel (D4).
- **D6. A workspace template:** `New-WorkSpace` builds the three git repos once per process into `%TEMP%\project-b-orch-template-<rand>`. After that it copies the template folder with `Copy-Item -Recurse` and writes `config.json` and `mock-agent.ps1` as today.
  - **What stays the same:** the repos, `.gitignore`, the commit identity and the `init` commit are identical to today.
  - **Cleanup:** the template is deleted with the other workspaces unless `-Keep` is given.
- **D7. Totals never change.** The runner's report prints the same table plus `N/N checks passed` and the wall time. Exit 0 only if every check passed and every expected group reported. A group that reported no checks is a FAIL row `"group <tag> produced no checks"`.
- **D8. Docs:**
  - **Script header:** the header comment of `run-offline.ps1` gains the four example commands from «نتیجه» above.
  - **The rule:** the existing "When to run it" rule is unchanged, except one line: "while working, run `-Only <your area>`; the full parallel run once at the end".

## §3 Phases

1. **D1, D2 and D5 (`-List`, `-Only`, `-Serial`, `-Group`, `-ResultFile`) with serial execution only.**
   - **Splitting:** the marker splitter and the groups table.
   - **Each section alone:** check every group with `-Only <tag>`. A group that fails alone but passes in `-Serial` has a hidden dependency. Merge it into its predecessor's group in the table, and say so in the hand-over.
   - **Gate:** `-Serial` = 105/105.
2. **D3, D4 and D7 (parallel run).**
   - **Gate:** a default run = 105/105, with the wall time recorded.
   - **If P-9 still flakes when the timing groups run alone:** STOP and report it. Do not raise its threshold.
3. **D6 (the template) and D8 (docs).**
   - **Gate:** a default run = 105/105, and the time compared with phase 2.

## §4 Tests (manual checks; no new check rows, the count stays 105)

- **T-S1:**
  - `-List` exits 0 and lists every marker id exactly once.
  - `-Only T13,T14` reports exactly the checks of those groups, and no other.
  - `-Only nosuchid` exits 1 with "no group matches".
- **T-S2:** the default (parallel) and `-Serial` runs report the same set of check ids and results.
- **T-S3:** kill one worker mid-run (manually, once).
  - **Report:** its checks come back as FAIL "worker crashed".
  - **Exit code:** 1.
- **T-S4:** the wall time goes down from the baseline. Record all three times (baseline, phase 2, phase 3) in the hand-over. **Target:** ≤ 40% of the baseline. A miss is not a STOP: report it.

## §5 Hand-over

- **Report:**
  - the branch and the changed files;
  - the three wall times;
  - every group merged because of a hidden dependency;
  - any flaky check and how it behaved.
- **No commit, no push.**
