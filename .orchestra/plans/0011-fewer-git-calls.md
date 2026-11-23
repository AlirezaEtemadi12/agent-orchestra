# Plan 0011 — One git call per repo per snapshot

**Status:** Draft
**Created:** 2026-11-23 · **Updated:** 2026-11-23 · **Done:** —
**Preview:** none — no screen; the engine does the same checks with fewer `git` processes
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-23. **Executor:** another agent, ideally a cheaper model.
**Plan review:** — (gate 2 by another model family, before the run starts)
**Decided by:** owner in the chat, 2026-11-23 ("four git calls is really too many; nobody touches the repo at that moment" — the check stays, its cost goes, see D1)
**Branch:** `alireza/fewer-git-calls` — the implementer creates it.
**Base:** `main` @ the commit that merges `alireza/easy-onboarding` — the conductor writes the hash here when the run starts
**Companion:** None
**Steps changed:** None

---

## فهم من از درخواست

موتور قبل و بعد از هر hop (هر بار که یک مدل کاری انجام می‌دهد) از هر سه ریپو یک «اثر انگشت» می‌گیرد تا
بفهمد کسی در این فاصله چیزی را عوض کرده یا نه. هر اثر انگشت الان چهار بار `git` اجرا می‌کند، پس هر hop
۲۴ پروسهٔ `git` می‌سازد و یک `new` حدود ۶۰ تا. روی ویندوز هر پروسه هزینه دارد و سوئیت تست هم به همین
دلیل کند است.

خود بررسی حذف نمی‌شود: یک بار جلسهٔ دیگری وسط read-back در ریپوی کناری commit کرد، و یک بار مالک وسط
gate 2 پلن قبلی را بست. این بررسی همان‌جاها جلوی تصمیم غلط را گرفت. فقط ارزان‌تر می‌شود: یک فراخوانی
`git` برای هر ریپو به‌جای چهار تا، و بقیه از روی خود فایل‌ها خوانده می‌شود.

## جریان کار

شما کاری متفاوت نمی‌کنید. موتور همان دستورها را با همان نتیجه اجرا می‌کند؛ فقط سریع‌تر است.

- برای هر ریپو یک `git status` گرفته می‌شود که شاخه، commit فعلی و فهرست فایل‌های تغییرکرده را با هم
  می‌دهد.
- برای فایل‌های تغییرکرده، اندازه و زمان آخرین تغییرشان از روی دیسک خوانده می‌شود، نه با `git diff`.
- اگر چیزی در ریپوی هدف عوض شده باشد، رفتار همان قبلی است: نقش فقط‌خواندنی شکست می‌خورد. ریپوی کناری هم
  مثل قبل فقط یک یادداشت می‌گیرد.

فازها:
1. اندازه‌گیری: شمارندهٔ فراخوانی‌های `git` و یک چک که عدد فعلی را ثبت می‌کند.
2. اثر انگشت تازه با یک فراخوانی، و سازگاری با فایل‌های اثر انگشت قدیمی در inbox.
3. پایین آوردن سقف چک به عدد تازه، و به‌روز کردن مستندها.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

یک hop به‌جای ۲۴ فراخوانی `git` فقط ۶ تا دارد. همهٔ تست‌های قبلی، از جمله S11، T-W1 و T13 که تغییر ریپوی
کناری را می‌سنجند، بدون تغییر پاس می‌شوند، و زمان سوئیت موتور کمتر می‌شود.

چک‌لیست شما:
1. سوئیت را اجرا کنید (`powershell -NoProfile -File engine/tests/offline/run-offline.ps1`). باید همهٔ چک‌ها
   پاس شوند و زمان دیواری کمتر از عدد قبلی (حدود ۴۴۰ ثانیه) باشد.
2. در یکی از پروژه‌ها یک `new` واقعی بزنید و وسط review یک فایل در ریپوی هدف را ذخیره کنید. باید مثل قبل
   خطای «the repo changed» بگیرید.
3. همین کار را در ریپوی کناری بکنید. باید فقط یادداشت `note: <repo> changed meanwhile` ببینید.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md): branch before the first edit; never
   commit/push/merge unless asked in that message; one phase = one uncommitted diff.
2. This plan touches the gates' evidence. **The meaning of a fingerprint does not change**: two snapshots
   differ if and only if a commit, a branch switch, an edit, a new file or a deleted file happened outside
   the ignored paths. If a step would weaken that, stop and ask.
3. Windows PowerShell 5.1 only; no new module, no new dependency.

### Baseline

```
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

Record the check count and the wall time (it was 140 checks and about 440 s on 2026-11-23). The count never
drops; phase 1 adds one check.

## 1. Goal

`Get-RepoFingerprint` makes one `git` call instead of four, with the same answer to the question "did this
repo change?". The number of `git` calls a `new` makes is measured and held by a check.

## 2. Out of scope

- Dropping the sibling repos from the snapshot. The sibling note is tested (S11, T-W1, T13) and costs one call
  per sibling after this plan; dropping it would be a behaviour change and its own decision.
- `Get-RepoState` (the repo summary in a prompt, six calls once per prompt) and every other `git` call.
- Caching a snapshot across hops or across processes: a cache is exactly what would miss a change.
- The dashboard, the API and the UI.

## 3. Rules that apply

- [ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md): nothing here reaches the
  dashboard.
- [`../../.ai/ci.md`](../../.ai/ci.md): the offline suite is the gate; its time table gets a row.
- The engine's history note [`engine/docs/history/RUN-RELIABILITY-PLAN.md`](../../engine/docs/history/RUN-RELIABILITY-PLAN.md)
  (P2): why paths under `cleanTree.ignore` stay out of the fingerprint.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | The before/after check stays. It caught real changes, such as another session's commit in a sibling repo during a read-back and the owner closing a plan during gate 2. Only its cost goes down. |
| D2 | One call per repo: `git -C <repo> status --porcelain=v2 --branch --untracked-files=all`. Its `# branch.oid` line replaces `rev-parse HEAD`, its `# branch.head` line replaces `branch --show-current`, and its entries replace `status --porcelain`. |
| D3 | `git diff HEAD` is replaced by the size and `LastWriteTimeUtc.Ticks` of every changed or untracked path, read from disk. Untracked files are already fingerprinted this way. A deleted path is written as `<path>\|deleted`. Ticks are 100 ns on NTFS, so an edit that keeps both the size and the tick is not a real case. |
| D4 | The porcelain v2 entry kinds are `1` (changed), `2` (renamed or copied; the path is the part before the tab), `u` (unmerged) and `?` (untracked). The path is the last field. A quoted path is unquoted exactly as now (`Trim('"')`). Ignored paths are dropped by a v2-aware `Test-FingerprintIgnoredLine`. |
| D5 | The hash input starts with `v2`, so an old and a new fingerprint can never be equal by accident. |
| D6 | Old inbox files: a manual `<key>.fingerprint` without the `v2` prefix is not compared. The answer is accepted, and the console prints `note: fingerprint from an older engine, not compared`. A `<key>.workspace.json` with old values is treated the same way, repo by repo. One pending manual answer across an upgrade must not be refused. |
| D7 | Measuring: when `ORCHESTRA_GIT_COUNT_FILE` is set, `Invoke-Git` appends one line (the first git argument) to that file. Unset means no file and no cost. This is a test hook only, and the env var is read once per `Invoke-Git` call. |
| D8 | The new check `T-GIT1` runs one `new` through gate 2 in a fresh workspace and asserts the call count is at most a ceiling. Phase 1 sets the ceiling to the measured current number. Phase 3 lowers it to the new measured number plus 10% headroom, rounded up. |

## 5. File manifest

| # | File | NEW/EDIT |
|---|---|---|
| 1 | `engine/lib/gates.ps1` | EDIT |
| 2 | `engine/lib/agents.ps1` | EDIT |
| 3 | `engine/run.ps1` | EDIT (only if the `workspace.json` read needs the D6 rule there) |
| 4 | `engine/tests/offline/run-offline.ps1` | EDIT |
| 5 | `.ai/ci.md` | EDIT |
| 6 | `engine/README.md` | EDIT (one line in Tests, only if a number there changes) |
| 7 | `.orchestra/plans/README.md` | EDIT (row 0011) |

## 6. Per-file specification

### 6.1 `engine/lib/gates.ps1`

- `Invoke-Git`: after the call, if `$env:ORCHESTRA_GIT_COUNT_FILE` is set, append `$GitArgs[0]` to the file
  with `[System.IO.File]::AppendAllText`. Wrap it in `try {} catch {}`, because counting must never fail a run.
- `Get-RepoFingerprint` (same name, same parameters, same return type):
  1. If the repo path is missing, return `'(missing)'` as now.
  2. Make one `Invoke-Git` call with D2's arguments and split the output into lines.
  3. `oid` is the value of `# branch.oid`, and `head` is the value of `# branch.head`.
  4. For every entry line, take the path (D4) and drop it if `Test-FingerprintIgnoredLine` says so.
  5. For every kept path, write `<path>|<length>|<ticks>`, or `<path>|deleted` if `Test-Path` is false.
  6. Return the SHA-256 of `"v2`n$oid`n$head`n<entry lines>`n<stat lines>"`, hex as now.
- `Test-FingerprintIgnoredLine`: accept a v2 line. The path is the last space-separated field for `1` and `u`
  entries, the part after `?` for `?` entries, and the part before the tab for `2` entries. Leave the v1
  callers (if any remain after a grep) on a separate small function, not a flag.
- `Get-WorkspaceFingerprint` and `Compare-WorkspaceFingerprint`: unchanged.

### 6.2 `engine/lib/agents.ps1`

- `Invoke-Manual`, at the `$then`/`$now` comparison: if `$then` does not start with `v2:`, skip the
  comparison and print D6's note. To make that possible, the manual adapter writes the fingerprint as
  `v2:<hash>` and compares `v2:<hash>` with `v2:<hash>`.
- No other change. The `Invoke-Role` before/after stays as it is and gets cheaper through `gates.ps1`.

### 6.3 `engine/run.ps1`

- Where `implementer.phase-<n>.workspace.json` is read back (near line 1757): a value without the `v2:`
  prefix is treated as "unknown" for that repo. If the repo is a sibling, it is not reported. If it is the
  target, it is compared as if unchanged and gets D6's note. Write the snapshot values with the `v2:`
  prefix (`agents.ps1` line ~599).

### 6.4 `engine/tests/offline/run-offline.ps1`

- New section `T-GIT1` near the other `new` scenarios. It uses a fresh `New-WorkSpace`, sets
  `ORCHESTRA_GIT_COUNT_FILE` to a temp file, runs `new` through gate 2 with the mock in its default mode,
  counts the lines, removes the env var in a `finally`, and asserts `count -le <ceiling>`. The message names
  the count: `"git calls: $count (ceiling <ceiling>)"`.
- New section `T-GIT2`, a unit check of `Get-RepoFingerprint` in a scratch repo. It loads `gates.ps1` as the
  suite already does. It asserts each line below:
  - Two calls with nothing in between give the same hash.
  - Editing an already-modified tracked file changes the hash (the case `git diff` used to catch).
  - A commit with a clean tree before and after changes the hash.
  - A branch switch changes the hash.
  - Deleting a tracked file changes the hash.
  - A new untracked file changes the hash.
  - A file under an ignored prefix does not change the hash.
  - One call makes exactly one `git` process (by the counter).

## 7. Tests

The whole offline suite plus `T-GIT1` and `T-GIT2`. The count is 140 + 2 = 142 on the baseline of
2026-11-23. Existing checks are not edited; S11, T-W1, T13, T6 and the read-only guard checks must pass as
they are.

## 8. Stop and ask

- A test in §7 fails for a reason that is not a bug in the new code. For example, a check depends on
  `git diff` output or on the old hash value.
- Porcelain v2 output on the installed git differs from D4 (for example, a field count that does not match).
- The measured count after phase 2 is not at least 30% below the phase-1 count.

## 9. Phases

### Phase 1 — measure

Steps: the counter in `Invoke-Git` (§6.1, first item), and `T-GIT1` with its ceiling set to the measured
current count.
**Gate:** the offline suite, all green; record the count.
**Owner checks:** none.
Stop here — show the owner the diff.

### Phase 2 — one call per repo

Steps: §6.1 (fingerprint and ignore), §6.2, §6.3, and `T-GIT2`.
**Gate:** the offline suite, all green; record the new count and the wall time.
**Owner checks:** the checklist above, items 2 and 3.
Stop here — show the owner the diff.

### Phase 3 — hold it

Steps: lower `T-GIT1`'s ceiling (D8); add the `.ai/ci.md` table row (before/after calls per `new`, wall
time); update the `engine/README.md` Tests line if its number changed; row 0011 in the plans index.
**Gate:** the offline suite, all green.
**Owner checks:** checklist item 1.
Stop here — show the owner the diff.

## 10. Definition of done

- All of §7 is green, and the counts and times are recorded in `.ai/ci.md`.
- `Status: Done`, the `Done:` date, the commit and the merge are in the header.
