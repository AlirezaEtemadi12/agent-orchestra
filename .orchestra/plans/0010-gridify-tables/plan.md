# Plan 0010 — Every growing table is a server-side Gridify table

**Status:** Draft
**Created:** 2026-11-23 · **Updated:** 2026-11-25 · **Done:** —
**Preview:** none — no new screen; the tables keep their columns and gain a pager and sortable headers
**Run:** `0010-gridify-tables`
**Planned by:** Claude Opus 5.5, 2026-11-23. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (review pasted by the owner, 2026-11-23): 3 must-fix, 4 should-fix — all applied in this version (Base, D1, D4, D6, D8, D10, D12, manifest rows 4, 7, 8, 9, 10)
**Decided by:** owner in the chat, 2026-11-23 ("wherever a table is needed, use Gridify", the Project B pattern)
**Branch:** `alireza/gridify-tables` — the implementer creates it.
**Base:** `main` after plan 0014 is merged — the implementer records the hash
**Companion:** None. The pattern is ported from Project B (`Web` `Common.Framework/Paging`, `Ui`
`components/app/data-grid`, `lib/gridify`); nothing in that workspace changes.
**Steps changed:** no preview (no new screen)

---

## فهم من از درخواست

صفحه‌ی Runs همه‌ی runها را یک‌جا می‌آورد و فهرست خیلی بلند شده است. در Project B و Project A جدول‌ها با
Gridify ساخته شده‌اند: صفحه‌بندی، مرتب‌سازی و فیلتر سمت سرور. می‌خواهید اینجا هم **هر جا جدول لازم است** همین
الگو باشد.

برداشت من از «هر جا جدول لازم است»: هر فهرستی که با گذشت زمان بزرگ می‌شود. یعنی:
- **Runs**،
- **Import log**،
- **Library** (plans، previews، ideas، research؛ evidence همان گالری می‌ماند)،
- **تاریخچه‌ی نسخه‌های پروفایل**.

جدول‌هایی که بزرگ نمی‌شوند یا جمع‌بندی‌اند، همان‌طور می‌مانند: Projects و Profiles (چند ردیف)، Pools، Cost و
Model quality (جمع‌ها)، Runs board (ستون‌های زنده) و جدول مرحله‌های داخل یک run. این قاعده در مستندات نوشته
می‌شود تا جدول‌های بعدی هم از اول همین‌طور ساخته شوند.

یک نکته: کارت‌های بالای Runs (تعداد runها، sessionها، callها و هزینه) امروز از جمع همان فهرست حساب می‌شوند. با
صفحه‌بندی باید از سرور بیایند، برای **کل** نتیجه‌ی فیلترشده، نه یک صفحه. گزینه‌های فیلتر Status و Repo هم همین‌طور.

این پلن کارهای زیر را **نمی‌کند**:
- ذخیره‌ی فیلترها، خروجی Excel، انتخاب چند ردیف یا فیلترساز پیشرفته‌ی Project B را نمی‌آورد.
- ستون تازه اضافه نمی‌کند.

## جریان کار

1. Runs را باز می‌کنید: ۲۰ ردیف اول، مرتب بر اساس «آخرین تغییر». پایین جدول: تعداد کل، صفحه‌ی فعلی، اندازه‌ی
   صفحه (۱۰، ۲۰، ۵۰، ۱۰۰).
2. روی سر ستون کلیک می‌کنید: مرتب‌سازی سمت سرور، بار دوم برعکس.
3. پروژه، Status یا Repo را عوض می‌کنید: جدول به صفحه‌ی ۱ برمی‌گردد و کارت‌های بالا برای کل نتیجه حساب می‌شوند.
4. جستجو: یک کادر که در task id، شماره و عنوان پلن، و متن task می‌گردد.
5. صفحه، مرتب‌سازی و فیلترها در آدرس صفحه می‌مانند؛ با Back یا refresh همان را می‌بینید.
6. Import log، Library و تاریخچه‌ی پروفایل همین رفتار را دارند.

فازها:
- **A:** پایه‌ی API (Gridify، TableMapper، پیش‌فرض‌ها، خطا) و Runs (جدول + خلاصه).
- **B:** پایه‌ی UI (جدول، سر ستون، صفحه‌بند، query در آدرس) و صفحه‌ی Runs.
- **C:** Import log، Library، نسخه‌های پروفایل.
- **D:** قاعده در مستندات.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

هیچ صفحه‌ای همه‌ی ردیف‌ها را یک‌جا نمی‌آورد؛ هر جدول بزرگ‌شونده صفحه دارد و مرتب‌سازی و فیلترش در سرور است.

چک‌لیست شما:
- Runs: ۲۰ ردیف و «۱–۲۰ از ۵۳». صفحه‌ی ۳ آخرین ۱۳ ردیف را نشان می‌دهد.
- روی «Cost» کلیک کنید: گران‌ترین run بالا می‌آید؛ دوباره کلیک کنید: ارزان‌ترین.
- پروژه را روی Project A بگذارید: کارت‌ها عدد همان پروژه را نشان می‌دهند (نه فقط صفحه‌ی فعلی) و گزینه‌های Repo
  فقط `Ui` و `Web` هستند.
- `T-WEB` را جستجو کنید: فقط runهای Web.
- آدرس را کپی کنید و در تب تازه باز کنید: همان صفحه، مرتب‌سازی و فیلتر.
- در آدرس `orderBy=passwordHash` یا `filter=foo=1` بگذارید: پیام «این مرتب‌سازی یا فیلتر معتبر نیست»، نه خطای ۵۰۰.
- Import log، Library (هر دسته) و تاریخچه‌ی پروفایل: صفحه‌بندی و مرتب‌سازی دارند.

## 0. Ground rules for the executor

1. Follow [`../../workflow.md`](../../workflow.md): branch before the first edit; never commit/push/merge unless asked
   in that message; one phase = one uncommitted diff.
2. **The row DTO is the allow-list** (D3). Never Gridify's default mapper.
3. Port, don't reinvent: read Project B's `Web/Src/01.Framework/Common.Framework/Paging/*` and
   `Ui/src/components/app/data-grid/{data-grid-table,data-grid-column-header,data-grid-pagination,data-grid-toolbar}.tsx`
   and `Ui/src/lib/gridify/build-query.ts` first. Copy only what §6 names; drop what this repo does not have
   (`Result<>`, saved filters, export, row select, filter builder, `toLatinDigits` unless it exists here).

### Baseline

`dotnet build` + `dotnet test` in `api/` (0 warnings); `typecheck`, `lint`, `test`, `build` in `ui/`. Record both
counts; neither may drop.

## 1. Goal

Runs, Import log, Library and profile versions page, sort, filter and search on the server through one Gridify
pattern, with totals computed on the server; and a written rule that makes every future growing table do the same.

## 2. Out of scope

Projects, Profiles, Pools, Cost, Model quality, Runs board, stage tables inside a run, the evidence gallery
(bounded or aggregates — D10); saved filters, export, row selection, a generic filter builder; new columns.

## 3. Rules that apply

ADR 0001 (one repo), ADR 0004 (fa/en strings), `.ai/api/conventions.md`, `.ai/ui/conventions.md`,
`.ai/ui/vertical-slice.md` (imports across features only through `index.ts`).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | Packages: `Gridify` and `Gridify.EntityFramework` **2.19.1** (the version Project B pins) in `api/Directory.Packages.props` (2.20.0 exists; 2.19.1 is pinned on purpose, to match Project B so the ported code behaves the same); `@tanstack/react-table` **8.21.3** and `gridify-client` **2.2.0**, exact versions, in `ui/package.json` |
| D2 | Wire format: Gridify's own query — `page` (1-based), `pageSize`, `orderBy`, `filter` — bound with `[FromQuery] GridifyQuery`; plus endpoint-specific plain parameters where the page has a fixed filter (`project`, `category`). Response: `{ count, data }` (`Paging<T>`, a record in `Common/Paging`) |
| D3 | **`TableMapper<TEntity, TRow>`** ported from Project B: reflects over the row DTO's public properties, maps each whose name matches a public scalar entity property of the same type, camelCase keys, `autoGenerateMappings: false`, built once per closed type; `[NotFilterable]` drops a property; `Build(extra)` is the only way to add a differently named map |
| D4 | **`TableQueryDefaults`** ported: page ≤ 0 → 1; pageSize ≤ 0 → **20** (here, not 10); max **100**; empty `orderBy` → the endpoint's default; the order always ends in the endpoint's **tiebreak** (stable paging) — `Apply(query, defaultOrder, tiebreak)`, where `tiebreak` is one or more mapped keys and is appended unless the order already ends with it; it must be mapped, because `IsValid` checks `orderBy` too. Tiebreaks: **runs** `projectId, runKey` (unique together, both already mapped — readable order, no Guid); **library** `projectId, category, relPath` (the unique index; all mapped); **profile versions** `version`; **import log** `id` — the only one that maps an extra key, `id → x.Id`, through D3's `extra` hook (its rows have no natural unique column; ties on `startedAt` are rare). `IsValid(mapper)` false → error `Table.Query.Invalid` (400; en "This sort or filter is not valid."; fa «این مرتب‌سازی یا فیلتر معتبر نیست.»), never a 500 |
| D5 | **Endpoints are new `…/table` routes; the old list routes are deleted** with their handlers when the grep finds no caller left: `GET api/runs` → `GET api/runs/table`; `GET api/import/logs` → `GET api/import/logs/table`; `GET api/library` → `GET api/library/table`; `GET api/profiles/{id}/versions` keeps its `?diff=` form and gains `GET api/profiles/{id}/versions/table` for the list. `GET api/runs/{project}/{runKey}`, `…/board`, the library item/file/prompt routes are unchanged |
| D6 | **Runs.** Row DTO = today's `RunListItemDto` fields. Mapped through D3 (same name and type on `RunEntity`): `projectId`, `runKey`, `taskId`, `repo`, `status`, `planNumber`, `planTitle`, `task`, `branch`, `rounds`, `updatedAt`, `closedAt`. **Extra maps** (D3 `extra`): `costUsd` → `CostMicros` for **ordering only**. Gridify cannot make a key sort-only, so a hand-typed `filter=costUsd>…` is accepted and compares **micros, not dollars**; that is accepted as is, the UI never offers a cost filter, and the DTO comment says so. `sessions`, `calls`, `requests`, `models` are computed and therefore neither sortable nor filterable (the column header shows no sort control for them). Default order `updatedAt desc`. Fixed parameter `project` (as today: unknown → `Runs.Project.NotFound`) |
| D7 | **Runs summary.** `GET api/runs/summary?project=&filter=` (the same `filter` string the table uses, validated by the same mapper) → `{ runs, sessions, calls, costUsd, statuses[], repos[] }` over the **whole filtered set** — `statuses`/`repos` are computed with the project filter only (not `filter`), so the dropdowns never empty themselves. The KPI cards and the two dropdowns read it; the table never sums a page |
| D8 | **Search** is a UI-side expansion into the Gridify `filter`: the term becomes `(taskId=*term/i|planNumber=*term/i|planTitle=*term/i|task=*term/i)` (`=*` = contains; `/i` = Gridify's case-insensitive flag, needed because SQLite's `instr` is case-sensitive). **Fallback, decided now:** if the first Runs test shows `/i` does not translate on SQLite (throws, or evaluates in memory), drop `/i` everywhere — search becomes case-sensitive — and note it in the hand-over; do not stop, combined with the other filters by `,` (AND); escaped with gridify-client's escaping. Import log: `projectId`; Library: `title|slug|number` |
| D9 | **URL state.** `page`, `pageSize`, `orderBy`, the per-page filters and `q` live in the page's query string (`useSearchParams` + `router.replace`, no history entry per keystroke; the search input is debounced 300 ms). Changing a filter or `q` resets `page` to 1 |
| D10 | **The rule** (written in phase D, one place: `.ai/api/conventions.md`, linked from `.ai/ui/conventions.md`): *a list that grows with use — including the versions of one profile, which grow with every save — is a `…/table` endpoint through `TableMapper` and a `DataTable` in the UI; a list bounded by configuration (projects, profiles, pools), an aggregate (cost, quality) or a live view (board) is not.* |
| D11 | **Import log** row: `projectId`, `startedAt`, `finishedAt`, `filesRead`, `blocksRead`, `runsBuilt`, `libraryItems`, `errorCount`; default `startedAt desc`. Errors stay a per-row expansion loaded with the row (as today), `[NotFilterable]` not needed (a list is not scalar) |
| D12 | **Library** row: `projectId`, `category`, `relPath`, `title`, `number`, `slug`, `repo`, `status`, `createdOn`, `updatedOn`, `doneOn`, `plannedBy`, `runKey`, `sizeBytes`; fixed parameters `project` (optional) and `category` (required; unknown → existing library error); default `updatedOn desc`. Today's `status`/`sort`/`dir` parameters are replaced by `filter`/`orderBy`. The evidence category keeps its **gallery** but reads the same route: `GET api/library/table?category=Evidence&project=…&pageSize=100`, with the shared pager under the gallery when `count` > 100. No list route is kept for it; `GET api/library` is deleted |
| D13 | **Profile versions** row: `version`, `reason`, `createdAt`; default `version desc` (tiebreak `version`, already a row property — D4) |
| D14 | **UI components** in `ui/src/components/app/data-table/`: `data-table.tsx` (TanStack table, manual pagination and sorting, skeleton rows while loading, the existing `EmptyState` when `count` is 0), `column-header.tsx` (sort toggle asc → desc → none, only when the column is sortable), `pagination.tsx` (range text, page size select 10/20/50/100, first/prev/next/last, RTL-aware icons), `toolbar.tsx` (search input + slot for the page's filters); `ui/src/lib/gridify/build-query.ts` (D8, D9) and `use-table-query.ts` (URL state ↔ query) |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/Directory.Packages.props`, `api/src/Orchestra.Api/Orchestra.Api.csproj` (D1) | EDIT | A |
| 2 | `api/src/Orchestra.Api/Common/Paging/{TableMapper,TableQueryDefaults,NotFilterableAttribute,Paging}.cs` | NEW | A |
| 3 | `api/src/Orchestra.Api/Common/Errors/{Error,ErrorMessages}.cs` — `Table.Query.Invalid` | EDIT | A |
| 4 | `api/src/Orchestra.Api/Modules/Runs/Features/RunsTable/{RunsTableHandler,RunsSummaryHandler}.cs`; `RunsController.cs`; `RunsModuleInstaller.cs` (`AddScoped<RunsTableHandler>()`, `AddScoped<RunsSummaryHandler>()`, and remove `ListRunsHandler`'s line with the handler); delete `Features/ListRuns/ListRunsHandler.cs` if unused (keep `RunDtos.cs` — the row DTO and `ToUsd`, moved if the folder goes) | NEW/EDIT | A |
| 5 | `api/tests/Orchestra.Api.Tests/{Common/TableMapperTests,Runs/RunsTableTests}.cs` | NEW | A |
| 6 | `ui/package.json` + lock (D1); `ui/src/components/app/data-table/*`; `ui/src/lib/gridify/{build-query,use-table-query}.ts` (D14) | NEW/EDIT | B |
| 7 | `ui/src/features/runs/{api,queries,types}.ts`, `components/runs-list.tsx` — **keep** plan 0009's empty-state link to the start page (`runs.noProjects`) | EDIT | B |
| 8 | `api/src/Orchestra.Api/Modules/Import/{ImportController,ImportLogsHandler,ImportModuleInstaller}.cs` → table (the installer registers the table handler in place of the list one); `ui/src/features/imports/**` | EDIT | C |
| 9 | `api/src/Orchestra.Api/Modules/Library/{LibraryController,LibraryModuleInstaller,Features/ListLibrary/*}.cs` → table (installer as above); `ui/src/features/library/**` (`library-params.ts` replaced by the shared URL state) | EDIT | C |
| 10 | `api/src/Orchestra.Api/Modules/Profiles/{ProfilesController,ProfilesModuleInstaller,Features/GetVersions/*}.cs` (installer registers the versions-table handler); `ui/src/features/profiles/components/profile-history.tsx` and its api/queries | EDIT | C |
| 11 | API and UI tests for rows 8–10 | NEW/EDIT | C |
| 12 | `.ai/api/conventions.md` (D10, D3, D4 — the rule and the boundary), `.ai/ui/conventions.md` (link + the components of D14), `.ai/architecture/overview.md` (the routes of D5) | EDIT | D |

## 6. Per-file specification

### 6.1 Handler shape (every table)

```csharp
var result = TableMapper<RunEntity, RunListItemDto>.Query(source, query, "updatedAt desc", "projectId, runKey", RunsMapper);
if (result.Error is not null) return (null, result.Error);
var count = result.Paging.Count;
var rows = await result.Paging.Query.Select(/* row projection */).ToListAsync(ct);
return (new Paging<RunListItemDto>(count, rows), null);
```

This repo has no `Result<>`; `Query` returns `(QueryablePaging<T>? Paging, Error? Error)`, the tuple style the
handlers already use. `source` applies the fixed parameters (`project`, `category`) first. Runs: the `Include`s
of today's handler stay only for the projection's computed columns — project the row in SQL
(`Sessions.Count`, `Sessions.Sum(s => (long?)s.Calls)`, …) rather than loading the graph; `models` (distinct final
stage models) is loaded for the page's run ids in one second query and joined in memory.

### 6.2 Tests (API)

`TableMapperTests`: the Runs map's key set equals D6's list exactly (`id` is not in it); a request with an empty `orderBy` is valid and orders by `updatedAt desc, projectId, runKey` (the appended tiebreak passes `IsValid`); the import-log map contains `id`; `costUsd` orders by micros; an unknown field
(`passwordHash`, `sessions`) → `Table.Query.Invalid`. `RunsTableTests` on the SQLite test fixture: default order;
page 3 of 20 with 53 rows → 13; `filter=repo=Web`; `orderBy=costUsd desc`; the summary equals the sum of every
filtered row, not one page; `statuses`/`repos` unaffected by `filter`. Same pattern (count, default order, one
filter, one invalid field) for import logs, library (per category) and versions.

### 6.3 UI

The Runs page keeps its header, project chips, KPI cards and column set; the cards and dropdown options come from
`summary` (D7). Column order and texts unchanged. `sessions`/`calls`/`models` headers have no sort control. The
search box sits in the toolbar left of the Status/Repo selects. Mobile: the table scrolls horizontally inside its
card, the pager wraps.

### 6.4 Plan 0009's start page

Its step 4 reads "any run exists": after this plan it calls `GET api/runs/table?pageSize=1` and uses `count`.

## 7. Tests

§6.2 for the API. UI: `build-query` (page/size/sort/search/filters → the Gridify query; escaping); `use-table-query`
(URL → state → URL; a filter change resets the page); `data-table` (manual pagination calls back, sort toggles, no
sort control on a non-sortable column, empty state on 0); the Runs page (cards read the summary, not the rows).
`architecture.test.ts` stays green.

## 8. Stop and ask

- Projecting `Sessions.Sum` in SQL fails on SQLite for `decimal`/`long?` and the only fix changes a column type.
- A caller of a deleted list route exists outside the feature named in §5.

## 9. Phases

**A — API base + Runs.** Files 1–5. **Gate:** `dotnet build` (0 warnings) + `dotnet test`. Stop here — show the
owner the diff.

**B — UI base + Runs page.** Files 6–7. **Gate:** `typecheck`, `lint`, `test`, `build`. **Owner checks:** the Runs
items of the checklist. Stop here — show the owner the diff.

**C — The other tables.** Files 8–11. **Gate:** both sides' gates. **Owner checks:** the remaining items. Stop
here — show the owner the diff.

**D — The rule.** File 12; re-read the conventions around it and merge or delete what D10 overlaps; the hand-over
names what was removed. Stop here — show the owner the diff.

## 10. Definition of done

All phases' gates green with counts at or above the baseline; no list route of D5 left with a caller; the owner's
checklist passes; `Status: Done`, the `Done:` date, the commit and the merge in the header.
