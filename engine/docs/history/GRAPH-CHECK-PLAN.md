# Graph staleness — port the four DrivingScore fixes

Status: **Implemented (2026-10-20).** `tests/offline/run-offline.ps1` is 36/36 green (T1–T9 included),
and the read-only `run.ps1 graph -Check` on the real repos reports `Agent` stale (its own open
plan-0081 edits) with `Ui` and `Web` fresh. No real graph was refreshed.

Reference (read-only): `F:\Projects\@Source Codes\@Personal\Project DrivingScore\orchestrator\`
`refresh-graph.ps1`, `lib\gates.ps1` (`Get-SemanticIds`, `Get-GraphStatus`, `Format-Age`), `run.ps1` (`Cmd-Graph`).

## What differs today

| # | Problem | DrivingScore | Ours now |
|---|---|---|---|
| 1 | Native stderr under `ErrorActionPreference=Stop` | `Invoke-Graphify` runs with `Continue`, judges by exit code, prints the first `warning` line | **partly done**: `Invoke-Graphify` exists and does not abort, but it only echoes lines matching `community set changed` — a `tree_sitter_sql not installed` style warning is swallowed |
| 2 | Uncommitted edits | `Get-GraphStatus` marks stale when a changed/untracked file (excluding `graphify-out/`) is newer than `graph.json` | not implemented — an in-progress phase leaves the graph "fresh" |
| 3 | HEAD moves / no-op updates | writes `graphify-out/.orch-head` after a successful update; when present it wins over the commit-time check (because `graphify update` does not rewrite `graph.json` when no code changed) | not implemented; only `graph.json` mtime vs commit time |
| 4 | Backup growth | keeps the newest `$KeepBackups` (2) `graph.json.bak-*` per repo | no pruning — Agent already has 2, and every refresh adds one |
| — | Root merge refusal | refuses only when the root graph holds a semantic node **id** no repo graph has | refuses if the root holds *any* semantic node (stricter than needed, and it would block a legitimate re-merge) |

Also: `Format-Age` lives in `run.ps1` here and in `lib/gates.ps1` there; it belongs next to `Get-GraphStatus`.

## Changes

### 1. `lib/gates.ps1`

**Move `Format-Age` here** from `run.ps1` (unchanged body, one definition).

**Add `Get-SemanticIds`** — regex over the raw text, same shape as DrivingScore:
`\{[^{}]*"_origin"\s*:\s*"semantic"[^{}]*\}` then the `id` inside each match. Used only by the
opt-in merge check, so its cost (one extra read of a 20 MB file) is paid only with `-MergeRoot`.

**Replace `Get-GraphStatus`** with the DrivingScore definition:

- `git log -1 --format=%H %ct` for the full sha and commit time; `Head` is the 7-char prefix.
- `missing` when `graph.json` is absent, with `Reason = 'no graph.json'`.
- Read `graphify-out/.orch-head` if present:
  - present and differs from HEAD → stale, `Reason = "HEAD moved since refresh (abc1234 -> def5678)"`;
  - **the sidecar wins over the commit-time check** — no mtime comparison in that branch;
  - absent → fall back to `graph.json` mtime vs commit time (`commit newer than graph`).
- Then, always, uncommitted/untracked files: `git status --porcelain --untracked-files=all`, skip
  `graphify-out/*`, and for every remaining path that exists as a file with
  `LastWriteTime > graph mtime`, collect it; stale reason `"N uncommitted file(s) newer than graph"`.
  The first porcelain line can lose its leading status space because `Invoke-Git` trims, so the parse
  strips `^[ MADRCU?!]{1,2} ` and handles `old -> new` renames, exactly as DrivingScore does.
- Returns `State`, `Reason`, `Head`, `GraphAt`, `Behind`, `Semantic`.

### 2. `run.ps1` — `Cmd-Graph`

- Drop the local `Format-Age` (now from `gates.ps1`).
- Table gains **`Reason`** and loses `Commit` (the reason string carries the detail); width 250.
  Columns: `Repo | State | Head | Graph | Behind | Semantic | Reason`.
- `-Check` footer becomes DrivingScore's: `stale: <repos> - refresh with: run.ps1 graph [<repo>]`,
  or `all graphs match the code.`
- **No `(root merge)` row**: there is no root graph here and `-MergeRoot` is opt-in; adding a row that
  can never render is dead UI. (Differs from DrivingScore deliberately; recorded below.)

### 3. `refresh-graph.ps1`

- Params gain `[int]$KeepBackups = 2`. After copying a backup, prune:
  `Get-ChildItem -Filter 'graph.json.bak-*' | Sort-Object Name -Descending | Select-Object -Skip $KeepBackups | Remove-Item -Force`.
  Names are `yyyyMMdd-HHmmss`, so name order is time order.
- After a successful update, print the **first** line matching `(?i)warning` (instead of only the
  community-label note). Exit code remains the only pass/fail signal.
- After a successful update: **check `graphify-out/` is git-ignored first** —
  `git -C <repo> check-ignore -q graphify-out/graph.json`. If ignored, write the full HEAD sha to
  `graphify-out/.orch-head` (ASCII, no newline semantics needed). If **not** ignored, skip the sidecar
  and print one warning per repo (`graphify-out/ is not git-ignored; .orch-head not written`), so this
  feature can never dirty a repo.
- Merge block (still `-MergeRoot` opt-in): replace the "any semantic node" refusal with the id check —
  collect `Get-SemanticIds` of every **repo** graph into a set; `lost` = semantic ids in the existing
  root graph that are in no repo graph; refuse only when `lost` is non-empty and `-ForceRoot` is absent,
  printing the count and one example. Otherwise merge as today.

### 4. Docs

- `CONDUCTOR.md` step 2: "compares each graph's mtime with HEAD" is no longer the whole truth →
  "compares the HEAD recorded at the last refresh, the graph's mtime, and any uncommitted edit".
- `REVIEW-BRIEF.md`: new `## Changes in 0.5.2` entry (four fixes, sidecar, backup cap, id-based merge
  refusal, the gitignore precondition) and the `graph` command description.
- `WORKING-MODEL.md`: one sentence on the sidecar/mtime nuance (no decision changes).

## Tests — throwaway workspace, then read-only on the real repos

Extend `tests/offline/run-offline.ps1` (currently 23 checks, all green). Temp repos gain a
`.gitignore` with `graphify-out/`; `Run-Orch` gains `-PathPrepend` so a fake `graphify.cmd` can be
put first on `PATH` for the warning/pruning scenarios.

| Id | Scenario | Expected |
|---|---|---|
| T1 | No `graph.json` | `missing`, reason `no graph.json` (already S16; keep) |
| T2 | Tracked file edited after the graph | `stale`, reason `1 uncommitted file(s) newer than graph`; editing a file *inside* `graphify-out/` does not count |
| T3 | Refresh, then `git checkout` an older commit | `stale`, reason `HEAD moved since refresh (… -> …)` — the commit-time check would miss it |
| T4 | Empty commit, then refresh with a **no-op** update (fake graphify leaves `graph.json` untouched because no code changed) | `.orch-head` = new HEAD and `-Check` says **fresh**, although `graph.json` mtime is older than the commit — proves the sidecar wins |
| T5 | Fake `graphify` prints `WARNING: tree_sitter_sql not installed` to stderr, exits 0 | refresh **continues**, prints that first warning line, exit 0, next repo still processed |
| T6 | 4 `graph.json.bak-*` files, then one refresh | exactly 2 remain (the newest two) |
| T7 | Repo whose `.gitignore` lacks `graphify-out/` | no `.orch-head` written, one warning; with the ignore present, the sidecar is written |
| T8 | `-MergeRoot` with a root graph holding a semantic id that no repo graph has | refused, message names the id; with `-ForceRoot` the merge is attempted |
| T9 | Existing 23 checks | still pass (S16 wording updated for the new columns) |

Then, **read-only**, on the real workspace: `run.ps1 graph -Check` plus
`git check-ignore -q graphify-out/graph.json` in each repo. Expectation to report honestly: the real
repos currently have **no `.orch-head`** (the earlier refresh predates this change), so the check falls
back to mtime-vs-commit and the uncommitted-file scan; Agent's open plan-0081 edits may show up as a
stale reason if any is newer than the graph (10:25). **No refresh of the real graphs** — sidecars
appear on the next refresh you ask for.

## Explicitly not changing

- `-MergeRoot` stays opt-in (DrivingScore merges by default; here a root graph is not used).
- `Test-GraphHasSemanticNodes` keeps its name and raw-text implementation (cheap; used on every
  `graph -Check`).
- The status vocabulary (`fresh` / `stale` / `missing`) and the free, no-model guarantees.

## Implemented, and what the tests found

All of the above is in. The tests caught one real defect in the port itself: a `[string[]]`
parameter cannot be passed through `powershell -File` (`-Repos Ui Web` binds `Web` to the next
parameter), so the harness invokes the script with `-Command`. They also caught the LF/CRLF git
warning on the fixture's `.gitignore`, which is the same native-stderr-under-`Stop` family as fix 1.

Real repos after the change: `Agent` stale — `1 uncommitted file(s) newer than graph` (its open
plan-0081 edits); `Ui`/`Web` fresh. `graphify-out/` is git-ignored in all three, so the next refresh
will write `.orch-head`.
