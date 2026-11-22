# Data model

> **AUTHORITATIVE** for what is imported, how a run is identified, and the tables. Names follow the
> orchestrator's own words (run, stage, hop, session) so the import is a mapping, not a translation.
> Read from both orchestrators' real files on 2026-11-19 (Project A and Project B, read-only).
> The database is a **read model** ([ADR 0005](../decisions/0005-ai-is-knowledge-orchestra-is-work.md)):
> it can be deleted and rebuilt from the files at any time — with two exceptions, the dashboard's own
> record: the events the runner reports ([ADR 0006](../decisions/0006-events-are-the-dashboards-own-record.md))
> and the profiles edited here, which are exported to `<data>/profiles/<id>.json` for the engine
> ([ADR 0007](../decisions/0007-profiles-exported-to-a-file.md)).

## Import sources (phase 0)

| Source | Where | Survives `close`? | Holds |
|---|---|---|---|
| `_ledger.md` | `<runsPath>/` | **yes** | one block per `verify` and per `close` (below) |
| `task.json` | `<runsPath>/<taskId>/` | **no** | the open run: status, plan, review, readback, approved, implement, phases[], verify, history[] |
| `_archive/<taskId>.json` | `<runsPath>/_archive/` | **yes** (since 2026-11-19) | written by `close`: `schema` (1), `archivedAt` (with offset), `project`, `task` (the whole `task.json`), `reviews[]` / `readbacks[]` (`{ name, round, text }`, one per round), `verify[]` (`{ name, round: null, text }`), `usage` (per session: exact tokens and per-turn timings, **no cost**) |

**The archive file is named by task id only**, so a reused task id overwrites the previous run's archive
(Project A's first two `T-WEB-001` runs left none). The importer also accepts `<taskId>.<anything>.json`
in case the orchestrators start writing unique names, and matches an archive to its run by branch (below).

Not imported in phase 0: `progress.json` (live hop — phase 1), `transcript/*.json`, `_model-rates.json`
(costs are taken as recorded; nothing is re-priced), `_pool/`, `_drafts/`.

Both files are **UTF-8 with a BOM**; `task.json` is PowerShell `ConvertTo-Json` output (wide
indentation, `null` for stages not reached). Paths inside it are absolute Windows paths; they are stored
relative to the project root.

## The ledger block

```
## 2026-11-19 09:37 - T-AGENT-030 - close            ← heading: local time, task id, kind (verify | close)

repo Agent | status Verified | plan v3 | branch plan-0158-…

- review: PASS (blocker 0, should 0, nit 0) by opencode:opencode-go/deepseek-v4.1-flash (round 3)
- read-back: FINDINGS (blocker 0, should 1, nit 2) by opencode:opencode-go/glm-5.3-flash (round 3)
- phase 1: PASS by opencode:opencode-go/deepseek-v4.1-flash | changed 13 | not in plan 0 | violations 0
- phase 3: NEED_INPUT by … | changed 16 | not in plan 0 | violations 0
- verify: PASS (gates green, model said PASS) by opencode:opencode-go/glm-5.3-flash
- verify: FAIL (gates red: dotnet test, model said PASS) by …

| session | first role | model | calls | requests | input | output | reasoning | cache read | cost |
… one row per session, then a **total** row
<footnote lines: "Hops without a session …", "Token counts: …", "Cost: …">

| stage | model | sessions | calls | requests | input | output | reasoning | cache read | time | cost |

```text
<per-model summary in a code fence — ignored: it repeats the session table>
```
```

- **Three session-table shapes** exist: without `requests` and `cost` (oldest), with `requests`
  only, with both. The stage table exists only in the newest blocks. A missing column is `null`, never 0.
- Numbers: tokens are `529.9k`, `2.2m` (also `K`/`M` in the code fence) → `long`, rounded; time is
  `m:ss` or `h:mm:ss` → seconds; cost `$0.170` → `decimal`.
- **Only the final review and read-back round is in the ledger** (its counts and its round number).
  Per-round verdicts are in `history[]` — of an open run's `task.json`, or of an archived run's `task`.
  Per-round **counts** exist nowhere (each round's text is archived, its counts are not), so reviewer
  quality is rounds needed, FINDINGS share per round (open and archived runs) and the final counts.
- Hops without a session (the conductor, codebuddy, manual) have no usage row: their cost is unknown,
  not zero.
- A model may be written `opencode:opencode-go/glm-5.3-flash` or `opencode-go/glm-5.3-flash`. The part
  before the first `:` is the **adapter**; the rest is the **model**.

## How a run is identified

**A task id is reused after `close`** (Project A's `T-WEB-001` is three different runs). So:

1. Walk the ledger's blocks in file order, grouped by task id.
2. A block joins the task id's current run, unless that run is **closed and the block's branch
   differs** — then it starts a new run. (A second `close` with the same branch, seen in Project A, joins
   the closed run.)
3. A later block **replaces** the run's snapshot: status, plan version, the stage lines and the usage
   tables. Usage is never summed across blocks — the blocks are cumulative.
4. An open run folder (`task.json`) joins the task id's last run when that run is not closed and the
   branch matches (or either branch is empty); otherwise it is a new run. Where both speak, `task.json`
   wins.
4a. An archive joins the **closed** run of the same task id whose branch equals `task.branch`; when
   several do, the one whose last `close` block is nearest `archivedAt`. No match → it is a run of its
   own (`Source = Archive`). Where the archive and the ledger both speak, the archive's `task` wins
   for state and history; usage stays the ledger's (it has the cost).
5. **RunKey** = `<taskId>.<n>`, `n` = 1, 2, … in order of first appearance — stable while the ledger is
   only appended to.

Plan number and title: from the task text (`Plan 0155: …`), else from the branch (`plan-0155-…`), else
none.

## Import = rebuild per project

Each import of a project deletes that project's rows (runs and everything under them) and inserts them
again from the files, **in one transaction**. That is idempotent by construction, and a run whose files
are gone disappears. A file or block that cannot be read is skipped and logged; the rest is imported. The
import log itself is the dashboard's own data and is kept (last 50 per project).

A project comes from the configuration file or from the dashboard's `projects.json`; a dashboard
project is imported right after it is saved, and removing it deletes its rows and its import logs (the
dashboard's own data, below).

The one thing an import does **not** delete is the project's `RunEvent` rows
([ADR 0006](../decisions/0006-events-are-the-dashboards-own-record.md)): it reads them.

## Live events (phase 1)

The runner posts one envelope per event to `POST api/events` (bearer token, `Orchestra:ReportToken`;
no token configured means the route answers 404). Schema 1:

```json
{ "schema": 1, "id": "<guid>", "project": "project-b", "taskId": "T-AGENT-031",
  "branch": "plan-0159-…", "at": "2026-11-20T10:02:11+03:30", "seq": 17,
  "kind": "stage.finished", "data": { } }
```

- **Kinds:** `run.created`, `history`, `status`, `progress`, `pool.waiting`, `pool.acquired`,
  `pool.released`, `ledger.written`, `run.closed`. An unknown kind or project is 422; a malformed body
  400; over 64 KB 413; accepted or already stored 202 with no body.
- **Identity and order:** `id` is the engine's GUID, so a resume or a hook firing twice stores one row;
  events are ordered by `(at, seq, receivedAt)`, because `seq` restarts with the engine process.
- **Retention:** `progress` events older than seven days are deleted once at start and then daily. Every
  other kind is kept.
- **Pool state is derived**, not stored: the pools screen reads the latest `pool.*` events; there is no
  `PoolState` table.
- A run the dashboard knows only from events has `Source = Events` until a file reaches it.

## Tables

| Table | One row per | Columns |
|---|---|---|
| `Project` | registered project (from configuration or the dashboard's `projects.json`) | `Id` (slug), `Name`, `RunsPath` |
| `Run` | run | `Id`, `ProjectId`, `RunKey`, `TaskId`, `Repo`, `Task`, `Status`, `Branch`, `PlanVersion`, `PlanNumber`, `PlanTitle`, `Rounds`, `CreatedAt`, `UpdatedAt`, `ClosedAt`, `CostMicros`, `Source` (`Ledger` · `Live` · `Archive` · `Both` · `Events`), `SourceRef` |
| `RunEvent` | event the runner reported (phase 1) | `Id` (the engine's GUID, unique), `ProjectId`, `TaskId`, `Branch`, `At` (UTC), `Seq`, `Kind`, `DataJson`, `ReceivedAt` |
| `Profile` | a shared profile (plan 0006) | `Id` (slug), `Name`, `CurrentVersion` |
| `ProfileVersion` | one immutable version of a profile | `Id`, `ProfileId` (FK → `Profile`, cascade), `Version`, `Json`, `Reason`, `CreatedAt` |
| `StageText` | archived review / read-back / verify text | `Id`, `RunId`, `Stage`, `Round`, `Name`, `Text` (≤ 200 000 chars, cut with a note beyond) |
| `StageResult` | review / read-back / phase / verify line, and each `history[]` entry | `Id`, `RunId`, `Stage` (`Review` · `ReadBack` · `Implement` · `Verify`), `Round`, `Phase`, `Adapter`, `Model`, `Verdict`, `Blockers`, `Should`, `Nits`, `Changed`, `OutsidePlan`, `Violations`, `GatesGreen`, `GatesNote`, `ModelVerdict`, `IsFinal`, `At` |
| `SessionUsage` | session of a run | `Id`, `RunId`, `SessionId`, `FirstRole`, `Model`, `Calls`, `Requests`, `Input`, `Output`, `Reasoning`, `CacheRead`, `CostMicros` |
| `StageUsage` | stage × model of a run | `Id`, `RunId`, `Stage`, `Model`, `Sessions`, `Calls`, `Requests`, `Input`, `Output`, `Reasoning`, `CacheRead`, `Seconds`, `CostMicros` |
| `ImportLog` | import pass of a project | `Id`, `ProjectId`, `StartedAt`, `FinishedAt`, `FilesRead`, `BlocksRead`, `RunsBuilt`, `LibraryItems`, `ErrorCount` |
| `ImportError` | file or block that could not be read | `Id`, `ImportLogId`, `File`, `Location`, `Message` |
| `LibraryItem` | indexed work product — plan, preview, idea, research note or evidence file | `Id`, `ProjectId` (FK → `Project`, cascade), `Category` (`Plan` · `Preview` · `Idea` · `Research` · `Evidence`), `RelPath`, `Title`, `Number`, `Slug`, `Repo`, `Status`, `CreatedOn`, `UpdatedOn`, `DoneOn`, `PlannedBy`, `PreviewRelPath`, `LinkedPlanNumber`, `RunKey`, `Group`, `SizeBytes`, `ModifiedAt` |

Twelve tables: the eleven above plus `LibraryItem`. `Run` also carries `EffectiveConfigJson` (plan 0006
D10), the config a run started with, filled from its archive or from `runs/<id>/config.effective.json`.

`IsFinal` is true for a ledger line and for the last `history[]` entry of each stage; queries over
"the outcome" use only `IsFinal` rows, queries over "per round" use `history[]` rows.

## The library

`LibraryItem` is **metadata only** (plan 0002 D2): path, title, header fields, size and modification
time. The file itself is read from disk on request, and nothing in a project folder is ever written.

- **Sources.** Per project, a glob list per category (`Orchestra:Projects:N:Library`); without one, the
  defaults are the `.orchestra/` layout (`Modules/Library/LibraryDefaults.cs`). Globs are relative to the
  project's `RootPath`, which is configured or derived **only** from an `<x>/orchestrator/runs` or
  `<x>/.orchestra/runs` RunsPath. No usable root → one import error and no items; the runs still import.
- **Rebuild.** Each import deletes the project's rows and inserts them again, in the same transaction
  as its runs (D1), so a deleted file disappears and importing twice changes nothing.
- **Plan ↔ run (D5).** A run belongs to a plan of the same project: by number when `Run.PlanNumber`
  equals the plan's `Number` and, if both name a repo, `Run.Repo` equals the plan's `Repo` (which is the
  path segment a plan glob's first whole-segment `*` matched, never a `done` folder); otherwise, for a
  plan with no number, by the branch's last segment after a leading `plan-` and `NNNN-` removed. The
  plan keeps the newest match (`RunKey`'s `n` compared as a number, then `UpdatedAt`).
- **Evidence** is images and `report.md` only, grouped by the folder segments below the glob's prefix
  (at most two).

## The dashboard's own data

`projects.json` sits beside the database and lists the projects added in the dashboard: `id`, `name`,
`rootPath`, `runsPath`, `repos`, `profile`, `createdAt`. It is **not** the database: the database is
rebuildable, this file is not (plan 0003 D2). It is written atomically (a temp file, then a replace), a
file that does not parse is reported on the projects page and never overwritten, and a configuration
project with the same id wins with a warning logged. Custom library globs stay configuration-only.

Import logs are the dashboard's own data too: kept per project, last 50, without a foreign key to
`Project`, so they survive a project that leaves the configuration.

## Derived views (queries, not tables)

| View | From |
|---|---|
| Runs per project: count, sessions, calls, requests, cost | `Run`, `SessionUsage` |
| Cost per day / month / model / project | `SessionUsage.CostMicros`, dated by the run's last block |
| Reviewer and read-back: rounds needed, FINDINGS rate per round (open and archived runs), final counts | `StageResult` |
| Implementer: phase verdicts, changed, outside the plan, violations, verify FAIL rate | `StageResult` |
| Time per stage per model | `StageUsage.Seconds` |
| Cache hit rate per model | `CacheRead / (Input + CacheRead)` over `SessionUsage` |
