# Plan 0004 — Live: the runner reports events; runs board, pools, latest line

**Status:** Done — A–D (2026-11-20 `d0bbd39` / 2026-11-21 `e06c141`, `ed99ef2`, `fc40d6d` on `alireza/live-events`,
merged as `fb217bc` into `alireza/skeleton-and-import`; api 217/217, ui 56/56, engine suite 124/124 per config)
**Created:** 2026-11-19 · **Updated:** 2026-11-21 · **Done:** 2026-11-21
**Preview:** none of its own — the roadmap's phase 1 and preview 0001 §7 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** pre-gate review 2026-11-20 (B1–B9 + 4 smaller points, checked against the code); fixes in this version
**Decided by:** owner's §8 answers 2026-11-20; D10–D15 proposed by the planner and approved by Alireza 2026-11-20
**Branch:** `alireza/live-events` — the implementer creates it.
**Base:** `alireza/skeleton-and-import` at plan 0005's merge commit (0005 runs first — owner, 2026-11-20)
**Companion:** None. The reporter is written once in `engine/lib/report.ps1` (§6.6). Each project only
gets a `report` block in its `orchestrator.json`, which the owner pastes (phase D); without it the
reporter is off.
**Steps changed:** no preview of its own (owner, 2026-11-19: "write the plans for the rest")

---

## فهم من از درخواست

از فاز ۱، ارکستر هر اتفاق را همان لحظه به داشبورد خبر می‌دهد: شروع و پایان هر مرحله، نتیجه‌ی هر دور
مرور با تعداد blockerها، انتظار برای pool، و آخرین خط خروجی hop. داشبورد این‌ها را **مستقیم در SQLite**
ذخیره می‌کند (تصمیم ۲۰۲۶-۱۱-۱۹).

با این کار سه چیز ممکن می‌شود:
- **تخته‌ی runها**، با ستون‌های «در حال اجرا»، «منتظر شما»، «تمام‌شده» و «ناموفق».
- **وضعیت poolها**: چه کسی هر جا را گرفته، و کدام pool تا کی خالی است.
- **جزئیات هر دور مرور برای runهای تازه**، که ledger نگهش نمی‌دارد.

قاعده‌ی ADR 0002 برقرار می‌ماند. گزارش‌دادن fire-and-forget است: timeout کوتاه، بدون تکرار. اگر داشبورد
خاموش باشد، run دقیقاً همان‌طور اجرا می‌شود. فقط رویدادهای آن مدت از دست می‌روند و ledger همچنان پشتیبان است.

برداشت من این است که رویدادها، برخلاف بقیه‌ی داشبورد، **داده‌ی خود داشبورد** هستند و از فایل دوباره
ساخته نمی‌شوند. این یک استثنای آگاهانه بر ADR 0005 است و این پلن آن را در ADR 0006 ثبت می‌کند. نتیجه‌اش
این است که پاک کردن دیتابیس، جزئیات دورها را پاک می‌کند، ولی هیچ runی را نه.

این پلن کارهای زیر را **نمی‌کند**:
- داشبورد به ارکستر فرمان نمی‌دهد (ADR 0002).
- اعلان تلگرام یا push ندارد (ایده‌ی `notifications`).
- روی سرور نمی‌رود (فاز ۳).

## جریان کار

1. یک توکن گزارش می‌سازید و آن را در یک متغیر محیطی ویندوز و در تنظیمات داشبورد می‌گذارید.
2. هر بار که ارکستر یک اتفاق را در `task.json` ثبت می‌کند، یک درخواست کوتاه به
   `POST /api/events` هم می‌فرستد. همین کار را برای خط تازه‌ی hop (حداکثر هر ۱۰ ثانیه یک بار)،
   برای انتظار و گرفتن جای pool، و برای نوشتن بلوک ledger و `close` هم می‌کند.
3. داشبورد رویداد را ذخیره می‌کند. صفحه‌ها هر ۵ ثانیه به‌روز می‌شوند.
4. **تخته‌ی runها:** هر run یک کارت دارد، با پروژه، پلن، مرحله، مدل، زمان سپری‌شده، آخرین خط، و اینکه
   منتظر چیست. «منتظر شما» یعنی یکی از این‌ها:
   - تأیید پلن (`AwaitingApproval`)،
   - جواب دستی (`manual inbox`)،
   - `NEED_INPUT`،
   - FAIL در verify.
5. **poolها:** هر pool با جاهای پر و خالی، صاحب هر جا، و زمان خنک شدن یا تمام شدن سهمیه.
6. اگر داشبورد در دسترس نباشد، ارکستر حداکثر ۲ ثانیه صبر می‌کند، یک خط هشدار کم‌رنگ چاپ می‌کند (در هر
   run فقط یک بار)، و ادامه می‌دهد.

فازها:
- **A:** API رویدادها و ذخیره.
- **B:** ترکیب رویدادها با runهای خوانده‌شده از فایل.
- **C:** تخته‌ی runها و poolها.
- **D:** بلوک `report` در `orchestrator.json` هر پروژه، که خودتان می‌چسبانید — reporter یک بار در
  `engine/lib/report.ps1` نوشته می‌شود، نه در repoهای ارکستر (§6.6).

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- یک run تازه در Project B شروع کنید. کارتش در «در حال اجرا» می‌آید و خط آخرش هر چند ثانیه عوض می‌شود.
- وقتی run به تأیید می‌رسد، کارت به «منتظر شما» می‌رود.
- در جزئیات run، جدول «دورها» برای هر دور مرور تعداد blocker، should و nit را دارد.
- دو run هم‌زمان بزنید. در صفحه‌ی poolها یکی جا را دارد و دیگری «منتظر» است، با اسم صاحب جا.
- داشبورد را خاموش کنید و یک run بزنید. run بدون تأخیر محسوس کامل می‌شود و یک خط هشدار
  `[report] dashboard not reachable` دارد. بعد از روشن کردن داشبورد و «خواندن دوباره»، run از روی ledger
  دیده می‌شود.
- با یک توکن اشتباه، `POST /api/events` جواب 401 می‌دهد. با توکن درست، هیچ endpoint دیگری کار نمی‌کند.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md) and plan 0001's ground rules 2–6.
2. Phases A–C change only this repo. Phase D is **not** done from here: it is a companion plan in each
   orchestrator repo, written from §6.6 in a session opened in that repo.
3. The report token is never logged, never returned, never in a committed file.

### Baseline

Plan 0003's gates green; counts as recorded in `.ai/ci.md`.

## 1. Goal

A report-only, fire-and-forget event channel from the runner to the API; events stored as the
dashboard's own record; live views built from them and merged with the file import.

## 2. Out of scope

Commands from the dashboard to the runner; notifications; server hosting and sign-in (0008); SSE or
WebSockets (polling is enough for one user); storing full plan or review texts (only verdicts, counts,
models, usage and the latest line).

## 3. Rules that apply

Plan 0001 §3; [ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md) (the
whole contract of §6.6 follows from it); ADR 0006, written by this plan.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | One endpoint `POST api/events`, bearer token from `Orchestra:ReportToken` (env var in practice). The token authorizes **only** this endpoint; every other endpoint ignores it |
| D2 | Token check by constant-time comparison of SHA-256 hashes; missing token configured → the endpoint answers 404 (the feature is off) |
| D3 | Events are stored in `RunEvent` and are **never deleted by an import** (ADR 0006) |
| D4 | The runner sends; it never reads an answer beyond the status code. Timeout 2 s, no retry, one warning line per run |
| D5 | `progress` events at most every 10 s per hop; `lastLine` ≤ 160 chars (as `progress.json` already caps it) |
| D6 | The UI polls (`refetchInterval` 5 s) only on the board, pools and an open run's detail |
| D7 | A run from events is matched to a run from files by (project, task id, branch) with the rules of data-model § *How a run is identified*; an event-only run (no file yet) is shown with `Source = Events` — a **new value** added to `RunSource` (`Ledger · Live · Archive · Both · Events`, `Modules/Runs/Entities/Enums.cs`) and to the `Run` row of `data-model.md` |
| D8 | Event times carry their offset (`2026-11-19T09:37:00+03:30`); stored as UTC |
| D9 | Retention: `EventRetentionService` is a hosted service that deletes `progress` events older than 7 days once at app start and then every 24 h; the import never runs it. All other events are kept |
| D10 | **Pool state is derived, not stored:** `GET api/pools` reads the latest `pool.*` events; there is no `PoolState` table (the name is dropped from the data model) |
| D11 | **Each event has an `id`** (a GUID the engine makes with `[guid]::NewGuid()`); unique index on `RunEvent.Id`. A duplicate (resume, a hook firing twice) answers 202 and is ignored. Order is `(at, seq, receivedAt)` — `seq` restarts with the engine process, `receivedAt` breaks ties |
| D12 | **Pools has its own controller** (`PoolsController` → `GET api/pools`), because `ApiRoutes.Controller` makes anything on `RunsController` `api/runs/…` |
| D13 | **Reporter off is visible:** `GET api/runs/board` answers `{ reporter: "on" \| "off", lanes }`; `off` when `Orchestra:ReportToken` is not set. The UI shows `board.reporterOff` from it |
| D14 | **Lane windows:** `needsYou` only for runs with an event in the last **30 days**; `done` 7 days; `running` 24 h (as §6.2) |
| D15 | **Status words** are the engine's: `AwaitingApproval`, `Approved`, `Implemented`, `Verified`, `VerifyFailed`, `Violation`, `NeedsInput` (from `run.ps1`). An unknown word puts a run in `running` if it had an event in 24 h, else nowhere; the card shows the raw word (as `StatusBadge` already falls back) |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `.ai/decisions/0006-events-are-the-dashboards-own-record.md` + `decisions/README.md` | NEW/EDIT | A |
| 2 | `.ai/architecture/overview.md` — the reporter arrow now exists; `data-model.md` — `RunEvent`, the event schema, `Events` in the `Run` row's `Source` (D7), pools derived from events (D10) | EDIT | A |
| 3 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `ReportToken` | EDIT | A |
| 4 | `api/src/Orchestra.Api/Modules/Events/{RunEventEntity,EventKind,EventEnvelope}.cs`; `Modules/Runs/Entities/Enums.cs` — `RunSource.Events` | NEW/EDIT | A |
| 5 | `api/src/Orchestra.Api/Modules/Events/ReportTokenFilter.cs` | NEW | A |
| 6 | `api/src/Orchestra.Api/Modules/Events/Features/ReceiveEvent/*` (validate, store) | NEW | A |
| 7 | `api/src/Orchestra.Api/Modules/Events/{EventsController,EventsModuleInstaller,EventRetentionService}.cs` (Order 70) | NEW | A |
| 8 | `api/src/Orchestra.Api/Data/Configurations/RunEventConfiguration.cs` (unique `Id`, index `(ProjectId, TaskId, At)`) + migration `Events` | NEW | A |
| 9 | `api/src/Orchestra.Api/Modules/Import/RunAssembler.cs` — events become non-final `StageResult` rows with counts; event-only runs | EDIT | B |
| 10 | `api/src/Orchestra.Api/Modules/Runs/Features/{Board,Pools}/*`; `RunsController.cs` (`GET api/runs/board`); new `Modules/Runs/PoolsController.cs` (`GET api/pools`, D12) | NEW/EDIT | B |
| 11 | `api/tests/Orchestra.Api.Tests/Events/**`, `Runs/BoardTests.cs`, `Runs/PoolsTests.cs` | NEW | A–B |
| 12 | `ui/src/features/board/**`, `ui/src/features/pools/**` (each with manifest, api, queries, types, `i18n/{fa,en}.ts`, components), pages `/{locale}/board/`, `/{locale}/pools/` | NEW | C |
| 12a | `ui/src/lib/features/define-feature.ts` (`board`, `pools` in `FEATURE_KEYS`); `ui/src/features/registry.ts` (both in `FEATURES`, menu `work`, after `runs`); `ui/src/i18n/messages.ts` (`board`, `pools` catalogues). `app-sidebar.tsx` needs no change (no new category) | EDIT | C |
| 13 | `ui/src/features/runs/components/run-detail-page.tsx` — live block (current hop, latest line) when the run is live | EDIT | C |
| 14 | `deploy/.env.example` — `Orchestra__ReportToken=` (empty); `api/src/Orchestra.Api/appsettings.Local.example.json` — `"ReportToken": ""` | EDIT | A |
| 15 | `engine/lib/report.ps1` + the hooks in `engine/run.ps1` and `engine/lib/{bus,agents,pool}.ps1` + offline-suite cases + `engine/examples/orchestrator.json` `report` block (§6.6) | NEW/EDIT | D |

## 6. Per-file specification

### 6.1 The event envelope (schema 1)

```json
{ "schema": 1, "id": "5b0c…-guid", "project": "project-b", "taskId": "T-AGENT-031", "branch": "plan-0159-…",
  "at": "2026-11-20T10:02:11+03:30", "seq": 17, "kind": "stage.finished",
  "data": { … } }
```

| `kind` | When (engine hook) | `data` |
|---|---|---|
| `run.created` | `Cmd-New` after `task.json` is written | `repo, task, status, planFamily` |
| `history` | every `Add-History` (`lib/bus.ps1`) | `event` (plan · review · readback · approve · implement · verify), `round`, `phase`, `model` (`adapter:model`), `verdict`, and from the state at that moment: `blockers, should, nits` (review/readback), `changed, outsidePlan, violations` (implement), `gates, modelVerdict` (verify) |
| `status` | every write of `task.json` whose `status` changed | `from, to` |
| `progress` | `Write-ProgressFile` (`lib/agents.ps1`), throttled per D5 | `role, adapter, model, phase, state, elapsedSec, lastOutputAgoSec, lastLine, pool, heldBy` |
| `pool.waiting` / `pool.acquired` / `pool.released` | `Enter-PoolSlot` / `Exit-PoolSlot` (`lib/pool.ps1`) | `pool, slot, heldBy, cooldownUntil, exhaustedUntil` |
| `ledger.written` | after `Write-LedgerEntry` | `kind` (verify · close), `usage` (the same numbers as the ledger's tables) |
| `run.closed` | `Cmd-Close`, before the folder is deleted | `status` |

`seq` is a per-run counter kept in memory by the engine (not persisted); the API stores it and orders by
`(at, seq)`. `project` is the project id configured in the dashboard, set in the engine's config.

### 6.2 API

- `ReportTokenFilter` — an endpoint filter on `POST api/events` only: `Authorization: Bearer <token>`;
  wrong or missing → 401 with an empty body (no ProblemDetails, nothing to probe); feature off → 404.
- `ReceiveEventHandler` — validates: `schema == 1`; `id` a GUID; `project` is known to `ProjectRegistry.Find` (config and dashboard projects alike) → else 422
  `Events.Project.Unknown`; `kind` known → else 422 `Events.Kind.Unknown`; body ≤ 64 KB → else 413;
  stores `RunEventEntity { Id, ProjectId, TaskId, Branch, At (UTC), Seq, Kind, DataJson, ReceivedAt }`;
  answers **202** with no body; a duplicate `id` is ignored and also answers 202 (D11).
- `GET api/runs/board?project=` → lanes `running`, `needsYou`, `done`, `failed`, each a list of cards
  `{ projectId, runKey, taskId, planNumber, planTitle, stage, model, startedAt, elapsedSec, lastLine,
  waitingFor (approval · manual · needInput · verifyFail · pool · null), pool, heldBy }`. Lane rules:
  `needsYou` (event in the last 30 days, D14) = status `AwaitingApproval` or `NeedsInput`, or the last history verdict `NEED_INPUT`, or a progress state
  `inbox`, or verify FAIL not yet followed by another verify; `failed` = `VerifyFailed`/`Violation`
  without a later event; `done` = closed or `Verified` in the last 7 days; `running` = the rest that had
  an event in the last 24 h.
- `GET api/pools` → `{ pool, tier, slots: { n, heldBy, since }[], waiting: { taskId, since }[],
  cooldownUntil, exhaustedUntil }[]` from the latest pool events (a slot whose last event is older than
  6 h is shown as `unknown`, never as held). Derived from events only (D10).
- `GET api/runs/board` wraps the lanes: `{ reporter: "on" | "off", lanes }` (D13).
- Error texts:

| Code | Status | en | fa |
|---|---|---|---|
| `Events.Project.Unknown` | 422 | Unknown project id in the event. | شناسه‌ی پروژه در رویداد ناشناخته است. |
| `Events.Kind.Unknown` | 422 | Unknown event kind. | نوع رویداد ناشناخته است. |

### 6.3 Merge with the import

`RunAssembler` gets the project's events. Per run (D7): every `history` event becomes a non-final
`StageResult` with its counts (replacing the `history[]` rows of `task.json` for the same `(event,
round, phase)`, since the event has the counts; when several events share it, the latest by D11's order wins); the newest `status` event wins over the ledger's
status when it is newer than the last ledger block; a run seen only in events becomes `Source =
Events`. Because events are not rebuilt, the import **reads** them and never deletes them.

### 6.4 ADR 0006 (written in phase A)

*Events are the dashboard's own record.* Context: ADR 0005 makes the database rebuildable; the owner
decided (2026-11-19) that from phase 1 run details are stored directly in SQLite (the close archive
remains a file the importer reads). Decision: `RunEvent` rows are not derived from files and are never deleted by an import; losing
the database loses per-round detail and the live history, never a run (the ledger still has every
run). Consequences: backups matter from now on (plan 0008); the "drop and re-import" advice in
`data-model.md` gets the exception.

### 6.5 UI

`board` — four lane columns (stack on mobile), cards as in the preview's mock (plan number + title, task
id, project, stage, model, elapsed with `DurationText`, latest line in a monospace line clamped to 2,
`waitingFor` as a warning badge). `pools` — a card per pool with slot chips and the waiting list. Both
poll every 5 s while visible (`refetchIntervalInBackground: false`).

| Key | en | fa |
|---|---|---|
| `board.title` | Runs board | تخته‌ی runها |
| `board.lanes.running` | Running | در حال اجرا |
| `board.lanes.needsYou` | Needs you | منتظر شما |
| `board.lanes.done` | Done | تمام‌شده |
| `board.lanes.failed` | Failed | ناموفق |
| `board.waiting.approval` | Waiting for your approval | منتظر تأیید شما |
| `board.waiting.manual` | Waiting for a manual answer | منتظر جواب دستی |
| `board.waiting.needInput` | The agent needs input | ایجنت ورودی لازم دارد |
| `board.waiting.verifyFail` | Verify failed | verify ناموفق شد |
| `board.waiting.pool` | Waiting for {pool}, held by {heldBy} | منتظر {pool}، در اختیار {heldBy} |
| `board.elapsed` | Elapsed | زمان سپری‌شده |
| `board.lastLine` | Latest line | آخرین خط |
| `board.empty` | No run had an event in the last 24 hours. | در ۲۴ ساعت گذشته هیچ runی رویدادی نداشته است. |
| `board.reporterOff` | Live events are off. Set Orchestra:ReportToken and configure the reporter. | رویدادهای زنده خاموش است. Orchestra:ReportToken را تنظیم کنید و reporter را راه بیندازید. |
| `pools.title` | Pools | poolها |
| `pools.slot` | Slot {n} | جای {n} |
| `pools.free` | free | خالی |
| `pools.unknown` | unknown | نامشخص |
| `pools.waiting` | Waiting | در انتظار |
| `pools.cooldownUntil` | Cooling down until {time} | در حال خنک شدن تا {time} |
| `pools.exhaustedUntil` | Quota exhausted until {time} | سهمیه تا {time} تمام است |

### 6.6 The reporter (in `engine/`, phase D)

Written once in this repo's `engine/` (plan 0005 is done). Behaviour-preserving rules of
`.ai/engine/conventions.md` apply.

- New `lib/report.ps1` with one public function `Send-OrchestraEvent -Ctx -State -Kind -Data`. It does
  nothing when `report.url` is empty or `report.enabled` is false. Otherwise it builds (with a new `id` GUID per event) the §6.1 envelope
  and calls `Invoke-RestMethod -Method Post -TimeoutSec <report.timeoutSec, default 2>` (Windows
  PowerShell 5.1 syntax — no `??`) with
  `Authorization: Bearer $env:<report.tokenEnv>` (default `ORCHESTRA_REPORT_TOKEN`). The whole call is
  inside `try/catch`; on any failure it writes `[report] dashboard not reachable` **once per process**
  (DarkGray) and returns. It never throws and never retries.
- Config `report: { enabled, url (e.g. http://localhost:3500/api/events), project (the dashboard's
  project id), tokenEnv, timeoutSec, progressEverySec (10) }`. No token in the file.
- Hooks: `Add-History` (after the state is updated, so counts are present), the `status` change in the
  state writer, `Write-ProgressFile` (throttled), `Enter-PoolSlot`/`Exit-PoolSlot`, after
  `Write-LedgerEntry`, and in `Cmd-Close` before deletion. One call per hook; no hook changes the run's
  behaviour or exit code.
- Offline suite: a fake listener is **not** used; the tests set `report.url` to an unreachable
  `http://127.0.0.1:9/` and assert (a) the run's result is unchanged, (b) the warning line appears once,
  (c) the extra time per run is under 3 s; and with `report.enabled` false, that `Invoke-RestMethod` is
  never called (mocked).
- `engine/VERSION` → `1.1.0`; `engine/examples/orchestrator.json` gets the `report` block (disabled). The owner pastes the block into each project's `orchestrator.json` and sets the token env var; no file in a project is edited from here.

## 7. Tests

| File | Facts |
|---|---|
| `Events/ReportTokenFilterTests.cs` | no header / wrong token → 401 empty body; right token → passes; token not configured → 404; the token never appears in a log line (captured logger) |
| `Events/ReceiveEventTests.cs` | each kind stored; unknown project/kind → 422; a dashboard-origin project accepted; > 64 KB → 413; offset time stored as UTC; 202 with no body; a duplicate `id` stored once |
| `Events/TokenScopeTests.cs` | the report token on `GET api/runs` grants nothing extra (the endpoint behaves as without it) |
| `Events/RetentionTests.cs` | progress older than 7 days deleted; other kinds kept; an import does not run it |
| `Import/RunAssemblerEventsTests.cs` | history events give per-round counts; events win over `task.json` history for the same round; an event-only run; an import deletes no event |
| `Runs/BoardTests.cs` | each lane rule; a verify FAIL followed by a PASS leaves `needsYou`; 24 h cut-off; `needsYou` older than 30 days dropped; an unknown status word; `reporter` on/off |
| `Runs/PoolsTests.cs` | held, waiting, cooldown, exhausted; a 6 h old slot is `unknown` |
| `ui: features/board/**`, `features/pools/**` | lanes render; `waitingFor` badges; polling paused when hidden; reporter-off notice when the API says the feature is off |

## 8. Stop and ask — open questions for the owner

1. ~~The close archive~~ — **answered 2026-11-19:** kept; committed in both orchestrators
   (`c4b9ca6`, `a947857`) and read by plan 0001. Once this plan's events are stored, should `archive.enabled`
   be turned off (events carry per-round counts, the archive carries texts), or keep both?
   — **answered 2026-11-20:** keep both; the archive stays the texts and the back-fill.
2. ~~Order of phases 1 and 2~~ — **answered 2026-11-20:** 0005 (engine move) goes first; this plan's reporter is written once, in `engine/`, and its Base becomes `main` after 0005.
   Original question: The reporter is written twice (once per orchestrator) if this plan comes
   before the engine move (0005), once if after. Keep the roadmap order, or do 0005 first?
3. **The latest line** may hold code or file contents from the hop's output. Store it (locally that is
   fine), or strip it until phase 3 decides what leaves the machine?
   — **answered 2026-11-20:** store it locally as-is; phase 3 decides what leaves the machine.

## 9. Phases

Gates as plan 0001 §9.

- **A — endpoint and store.** Files 1–8, 11 (events), 14. **Gate.** **Owner checks:** `curl` an event
  with and without the token. Stop.
- **B — merge, board and pools API.** Files 9–11. **Gate.** Stop.
- **C — screens.** Files 12–13. **Gate.** **Owner checks:** posting a few hand-written events moves a
  card between lanes. Stop.
- **D — reporter in the engine.** File 15. **Gate:** the offline suite (Windows, owner-run, `.ai/ci.md`
  engine section). **Owner checks:** paste the `report` block into each project, then the items of «نتیجه».

## 10. Definition of done

All gates green and counts recorded; both projects report through the engine; a run with the
dashboard down behaves identically; session log written; this header `Status: Done` with the date,
commit and merge.
