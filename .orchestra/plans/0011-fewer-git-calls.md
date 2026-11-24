# Plan 0011 — One git call per repo per snapshot

**Status:** Done
**Created:** 2026-11-23 · **Updated:** 2026-11-24 · **Done:** 2026-11-24
**Preview:** none — no screen; the engine does the same checks with fewer `git` processes
**Run:** — (implemented by an agent in the chat, phases 1–3)
**Commit:** `a2f36bf` on `alireza/fewer-git-calls` · **Merge:** `f75781b` into `main`, 2026-11-24
**Planned by:** Claude Opus 5.5, 2026-11-23. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (pasted by the owner, 2026-11-24): 4 must-fix, 7 should-fix. All are applied in v2 (D2–D9, §6, §7, §9). One correction to the review is recorded in D4: the field counts before the path are 9 for `2` and 10 for `u`, checked against git 2.56 output. Second pass (same reviewer, 2026-11-24): all fixes confirmed against the code and git; three small points applied (line numbers in §0, the v1 leftovers in §6.1, the printed baseline in §0 and §9).
**Decided by:** owner in the chat, 2026-11-23 ("four git calls is really too many; nobody touches the repo at that moment". The check stays and its cost goes; see D1.) Approved for execution by the owner, 2026-11-24.
**Branch:** `alireza/fewer-git-calls` — the implementer creates it.
**Base:** `main` @ the commit that adds plan 0011 v2 (above `f96c60a`)
**Companion:** None
**Steps changed:** None

---

## فهم من از درخواست

موتور قبل و بعد از هر hop (هر بار که یک مدل کاری انجام می‌دهد) از هر سه ریپو یک «اثر انگشت» می‌گیرد تا
بفهمد کسی در این فاصله چیزی را عوض کرده یا نه. هر اثر انگشت الان چهار بار `git` اجرا می‌کند. پس هر hop
۲۴ پروسهٔ `git` می‌سازد و یک `new` حدود ۶۰ تا. روی ویندوز هر پروسه هزینه دارد، و سوئیت تست هم به همین
دلیل کند است.

خود بررسی حذف نمی‌شود. یک بار جلسهٔ دیگری وسط read-back در ریپوی کناری commit کرد، و یک بار مالک وسط
gate 2 پلن قبلی را بست، و همین بررسی جلوی تصمیم غلط را گرفت. فقط ارزان‌تر می‌شود: برای هر ریپو یک
فراخوانی `git` به‌جای چهار، و محتوای فایل‌های تغییرکرده مستقیم از دیسک خوانده می‌شود.

## جریان کار

شما کاری متفاوت نمی‌کنید. موتور همان دستورها را با همان نتیجه اجرا می‌کند، فقط سریع‌تر.

- برای هر ریپو یک `git status` گرفته می‌شود که شاخه، commit فعلی و فهرست فایل‌های تغییرکرده را با هم
  می‌دهد.
- محتوای فایل‌های تغییرکرده و فایل‌های تازه از روی دیسک hash می‌شود، به‌جای `git diff`.
- اگر ریپوی هدف عوض شده باشد، رفتار همان قبلی است و نقش فقط‌خواندنی شکست می‌خورد. ریپوی کناری هم مثل قبل
  فقط یک یادداشت می‌گیرد.
- اثر انگشت‌های قدیمی‌ای که در inbox مانده‌اند مقایسه نمی‌شوند و یک یادداشت می‌گیرند. جواب دستیِ در
  جریان رد نمی‌شود.

فازها:
1. اندازه‌گیری: یک شمارندهٔ فراخوانی‌های `git`، و یک چک که عدد فعلی را ثبت می‌کند.
2. اثر انگشت تازه با یک فراخوانی، به‌همراه سازگاری با فایل‌های قدیمی.
3. پایین آوردن سقف چک به عدد تازه، و به‌روز کردن مستندها.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

یک hop به‌جای ۲۴ فراخوانی `git` فقط ۶ تا دارد. همهٔ تست‌های قبلی بدون تغییر پاس می‌شوند، و زمان سوئیت
موتور کمتر می‌شود.

چک‌لیست شما:
1. سوئیت را اجرا کنید (`powershell -NoProfile -File engine/tests/offline/run-offline.ps1`). باید ۱۴۲ چک
   پاس شوند و زمان دیواری کمتر از حدود ۴۴۰ ثانیه باشد.
2. در یکی از پروژه‌ها فایلی را در ریپوی هدف عوض کنید و ذخیره کنید، ولی commit نکنید. بعد یک `new` بزنید و
   وسط review همان فایل را **دوباره** ویرایش کنید. باید مثل قبل خطای «the repo changed» بگیرید. این
   همان موردی است که قبلاً فقط `git diff` آن را می‌گرفت.
3. همین کار را با فایلی در ریپوی کناری بکنید. باید فقط یادداشت `note: <repo> changed meanwhile` ببینید.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md):
   - Branch before the first edit.
   - Never commit, push or merge unless asked in that message.
   - One phase is one uncommitted diff.
2. This plan touches the gates' evidence. **The meaning of a fingerprint does not change.** Two snapshots
   differ if and only if one of these happened outside the ignored paths:
   - a commit;
   - a branch switch;
   - a content change in a tracked or untracked file;
   - a new file;
   - a deleted file.

   D3 records the one bounded exception, files over 4 MB. If any other step would weaken this rule, stop and
   ask.
3. Windows PowerShell 5.1 only. No new module and no new dependency.
4. Do not touch `Get-DirtyPaths` (`gates.ps1:40`) or `Get-GraphStatus` (`gates.ps1:296`). They read v1
   porcelain on their own and are not part of this plan. Do not "unify" them with the new parser.

### Baseline

```
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

Record the check count **as the suite prints it** (`<config>: N/N checks passed`) and the wall time. Do not
count the `Check` calls in the file: some sit in branches or loops, so the static count (144) is not the
printed one. On 2026-11-23 these were 140 checks and about 440 s. The count
never drops. This plan adds exactly two checks, T-GIT1 and T-GIT2, so the count becomes 142.

## 1. Goal

`Get-RepoFingerprint` makes one `git` call instead of four. Its answer to "did this repo change?" stays the
same. A check measures and holds the number of `git` calls one `new` makes.

## 2. Out of scope

- **Dropping the sibling repos from the snapshot.** The sibling note is tested by S11, T-W1 and T13. After
  this plan it costs one call per sibling. Dropping it would change behaviour, which is a separate decision.
- **`Get-RepoState`, `Get-DirtyPaths`, `Get-GraphStatus` and every other `git` call.** `Get-RepoState` is
  the repo summary in a prompt; it makes six calls, once per prompt.
- **Caching a snapshot across hops or across processes.** A cache is exactly what would miss a change.
- **The dashboard, the API and the UI.**

## 3. Rules that apply

- [ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md): nothing in this plan
  reaches the dashboard.
- [`../../.ai/ci.md`](../../.ai/ci.md): the offline suite is the gate, and its time table gets a row.
- [`engine/docs/history/RUN-RELIABILITY-PLAN.md`](../../engine/docs/history/RUN-RELIABILITY-PLAN.md), the
  engine's history note (P2): it explains why paths under `cleanTree.ignore` stay out of the fingerprint.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | The before/after check stays. It caught real changes: another session's commit in a sibling repo during a read-back, and the owner closing a plan during gate 2. Only its cost goes down. |
| D2 | **One call per repo:** `git -C <repo> status --porcelain=v2 --branch --untracked-files=all`. The `# branch.oid` line replaces `rev-parse HEAD`. The `# branch.head` line replaces `branch --show-current`. The entry lines replace `status --porcelain`. In a repo with no commit, `# branch.oid` is the literal `(initial)`; it is hashed as is, and the first commit changes it. Other `#` lines, such as `branch.upstream` and `branch.ab`, are not hashed: a `fetch` must not look like a change. |
| D3 | **Content from disk instead of `git diff HEAD`.** Every kept path (D4) that is a file gets a line in the hash input. The line depends on the file's size: up to 4 MB (4,194,304 bytes) it is `<path>\|<SHA-256 of the bytes>`, and larger it is `<path>\|big\|<length>\|<LastWriteTimeUtc.Ticks>`. A path that no longer exists is written as `<path>\|deleted`, and a directory as `<path>\|dir`. No git call is involved. The accepted risk: an edit to a file over 4 MB that keeps both its size and its mtime goes unseen. Repos in this workspace keep no such file under version control, and before this plan untracked files of any size were checked by size and mtime only. |
| D4 | **Parsing.** Only lines starting with `1 `, `2 `, `u ` or `? ` are entries. The path is never found by "the last space-separated field", because v2 paths may contain spaces and v2 does not quote them. Each kind splits with a fixed field limit: `1` is `-split ' ', 9`, and the 9th part is the path. `2` is `-split ' ', 10`, and the 10th part is `<path><TAB><origPath>`; the path is the part before the tab. `u` is `-split ' ', 11`, and the 11th part is the path. `?` is everything after `? `. These counts were checked against git 2.56 output; the review's 8 and 9 for `2` and `u` miss the score field and one hash field. `Trim('"')` stays on the path only for git builds that quote special characters (`core.quotePath`). v2 does not quote spaces. |
| D5 | **One parser:** `Split-PorcelainV2 [string]$Text` returns `@{ Oid; Head; Entries = @(@{ Kind; Path; Line }) }`. `Get-RepoFingerprint` and `Test-FingerprintIgnoredLine` both use it. Its only caller today is `Get-RepoFingerprint` (`gates.ps1:139`). It takes a parsed path instead of a raw line, and keeps its name. |
| D6 | **The prefix lives in one place.** `Get-RepoFingerprint` returns `v2:<hex>`, with the hash input starting with `v2` as well. `(missing)` stays exactly `(missing)`. Every value written or compared — the manual `<key>.fingerprint`, `<key>.workspace.json`, `Invoke-Role`'s before/after — comes from that one function, so fresh and saved values always have the same shape. |
| D7 | **One rule for old files.** A saved value that neither starts with `v2:` nor equals `(missing)` comes from an older engine. When the manual adapter (`agents.ps1:549`) finds one, it does not compare it, accepts the answer, and prints `note: fingerprint from an older engine, not compared`. When the implement post-check (`run.ps1` ~1757–1767) finds one, it treats that repo as unchanged. A sibling with an old value is not reported. The target repo with an old value gets the same note. A manual answer pending across an upgrade is never refused. |
| D8 | **Counting.** When `ORCHESTRA_GIT_COUNT_FILE` is set, `Invoke-Git` appends one line to the file: the first git argument. The value must be an absolute path. When the variable is unset, no file is written and the cost is nothing. The append sits in `try {} catch {}`, so counting never fails a run. This is a test hook only. |
| D9 | **T-GIT1** runs one `new` through gate 2 in a fresh workspace and asserts the call count is at most a ceiling. Phase 1 sets the ceiling to the measured current count and writes that number into this plan (§9). Phase 3 lowers the ceiling to the new measured count plus 10% headroom, rounded up, without measuring the old count again. |

## 5. File manifest

| # | File | NEW/EDIT |
|---|---|---|
| 1 | `engine/lib/gates.ps1` | EDIT |
| 2 | `engine/lib/agents.ps1` | EDIT |
| 3 | `engine/run.ps1` | EDIT |
| 4 | `engine/tests/offline/run-offline.ps1` | EDIT |
| 5 | `.ai/ci.md` | EDIT |
| 6 | `.orchestra/plans/README.md` | EDIT (row 0011) |

## 6. Per-file specification

### 6.1 `engine/lib/gates.ps1`

- **`Invoke-Git`:** add the counter from D8.
- **`Split-PorcelainV2` (new, next to `Get-GitPorcelain`):** the parser from D4 and D5.
- **`Get-RepoFingerprint`:** same name, same parameters, and it still returns a string.
  1. If the repo path is missing, return `'(missing)'`.
  2. Make one `Invoke-Git` call with the arguments from D2, then call `Split-PorcelainV2`.
  3. Drop every entry whose path `Test-FingerprintIgnoredLine` matches.
  4. For each kept entry, build its D3 line. The path is relative to the repo root.
  5. Return `'v2:' +` the SHA-256 hex of `"v2`n$oid`n$head`n<entry lines>`n<D3 lines>"`.
- **`Test-FingerprintIgnoredLine`:** takes `-Path` (already parsed) and `-Ignore`, and keeps its
  prefix-match logic and its `\` → `/` normalisation. Delete its two v1 leftovers:
  - the `$Line.Length -lt 4` guard, which would silently skip short paths;
  - the `' -> '` rename split, because `Split-PorcelainV2` already returns the new path.
- **`Get-WorkspaceFingerprint`, `Compare-WorkspaceFingerprint`, `Get-DirtyPaths` and `Get-GraphStatus`:**
  unchanged.

### 6.2 `engine/lib/agents.ps1`

- **`Invoke-Manual`:**
  - At the `$then`/`$now` comparison (~549), apply D7.
  - The `.fingerprint` file is still written with `-NoNewline` (~604), and the saved value is read with
    `.Trim()` as now. Otherwise `v2:<hex>` gains a trailing newline and never matches.
  - The `workspace.json` write (~599) is unchanged; its values carry the prefix from `Get-RepoFingerprint`.
- **`Invoke-Role`:** no change. It gets cheaper through `gates.ps1`.

### 6.3 `engine/run.ps1`

- Where `implementer.phase-<n>.workspace.json` is read back (~1757), apply D7 to each repo before
  `Compare-WorkspaceFingerprint`. For a repo with an old value, the "before" value is set to the fresh
  "after" value, so that repo shows as unchanged.

### 6.4 `engine/tests/offline/run-offline.ps1`

- **T-GIT1** is a new section placed near the other `new` scenarios.
  1. Create a fresh `New-WorkSpace`.
  2. Set `ORCHESTRA_GIT_COUNT_FILE` to an absolute temp path.
  3. Run `new` through gate 2 with the mock in its default mode.
  4. Count the lines in the file.
  5. Remove the variable and the file in a `finally`.
  6. Make **one** `Check`: `count -le <ceiling>`, with the detail `"git calls: $count (ceiling <ceiling>)"`.
- **T-GIT2** is a new section: a unit test of `Get-RepoFingerprint` in a scratch repo. It loads
  `gates.ps1` the way the suite already does, and the scratch repo has a path with a space in it
  (`dir with space/file one.txt`).
  - It runs these cases in order:
    - Two calls with nothing in between give the same value.
    - Re-editing an already modified tracked file, *with its size and mtime restored* via
      `Set-ItemProperty LastWriteTime`, changes the value (D3: content, not size and mtime).
    - Editing the file with a space in its path changes the value.
    - A commit with a clean tree before and after changes the value.
    - A branch switch changes the value.
    - Deleting a tracked file changes the value.
    - A new untracked file changes the value.
    - A file under an ignored prefix does not change the value.
    - The value starts with `v2:`.
    - One call makes exactly one `git` process, counted with D8.
  - It makes **one** `Check`. The check passes when every case holds, and its detail names each failed case
    (for example `"failed: re-edit restored mtime, space path"`).
  - With one `Check` each, the suite total is 140 + 2 = 142.

## 7. Tests

The whole offline suite plus T-GIT1 and T-GIT2 makes 142 checks on the 2026-11-23 baseline. No existing
check is edited. S11, T-W1, T13, T6 and the read-only guard checks must pass as they are.

## 8. Stop and ask

- A test in §7 fails for a reason other than a bug in the new code. For example, a check depends on the
  output of `git diff` or on an old hash value.
- The installed git's porcelain v2 output differs from D2 or D4, for example because a field count does not
  match.
- The count measured after phase 2 is not at least 30% below the count from phase 1.

## 9. Phases

### Phase 1 — measure

**Steps:**
1. Add the D8 counter to `Invoke-Git`.
2. Add T-GIT1, with its ceiling set to the measured current count.
3. Measure with `-Shards 1` so that nothing else is running, and write the number here:
   **phase-1 count: 61** (one `new` through gate 2, measured alone on 2026-11-24).

**Gate:** the offline suite, all green (141 checks). If the printed baseline was not 140, every count in
§0, §7 and §9 moves by the same difference: write the real numbers here before going on.

**Owner checks:** none.

Stop here — show the owner the diff.

### Phase 2 — one call per repo

**Steps:** §6.1 (except the counter), §6.2, §6.3, and T-GIT2.

**Gate:** the offline suite, all green (142 checks). **phase-2 count: 25** (from 61) and **wall time: 320 s**, measured 2026-11-24; both also in `.ai/ci.md`.

**Owner checks:** items 2 and 3 of the checklist above.

Stop here — show the owner the diff.

### Phase 3 — hold it

**Steps:**
1. Lower T-GIT1's ceiling as D9 says, from the phase-2 count. Do not measure again.
2. Add a row to the `.ai/ci.md` table: git calls per `new` before and after, the check count (142), and
   the wall time.
3. Update row 0011 in the plans index.

**Gate:** the offline suite, all green (142 checks).

**Owner checks:** item 1 of the checklist above.

Stop here — show the owner the diff.

## 10. Definition of done

- Everything in §7 is green. The counts and the times are recorded in `.ai/ci.md` and in §9.
- The header has `Status: Done`, the `Done:` date, the commit and the merge.
