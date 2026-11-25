# Plan 0010 — Every growing table is a server-side Gridify table

**Status:** Done 2026-11-25 — run 0010-gridify-tables, merged as 4694ec9 (not pushed)
**Created:** 2026-11-23 · **Updated:** 2026-11-25 · **Done:** 2026-11-25
**Preview:** none — no new screen; the tables keep their columns and gain a pager and sortable headers
**Run:** `0010-gridify-tables`
**Planned by:** Claude Opus 5.5, 2026-11-23. **Executor:** another agent, ideally a cheaper model.
**Plan review:** v1 reviewed on 2026-11-25 by opencode / DeepSeek V4.1 (a different family): 7 blockers, 16 should, 3 nits. v2 applied them; v3 answered the second round (0 blockers, 4 should, 2 nits); v4 answers the third (0 blockers, 3 should, 8 nits). §11 records the rounds and why three v1 points were not applied. The earlier chat review of 2026-11-23 (3 must-fix, 4 should-fix) is already in v1.
**Decided by:** owner in the chat, 2026-11-23 ("wherever a table is needed, use Gridify", the Project B pattern)
**Branch:** `alireza/gridify-tables` — created by the orchestrator; deleted on close.
**Base:** `main` @ `36489e4` (plan 0014 merged)
**Companion:** None. The pattern is ported from Project B (`Web/Src/01.Framework/Common.Framework/Paging`,
`Ui/src/components/app/data-grid`, `Ui/src/lib/gridify`); nothing in that workspace changes.
**Steps changed:** no preview (no new screen)

---

## فهم من از درخواست

صفحه‌ی Runs همه‌ی runها را یک‌جا می‌آورد و فهرست خیلی بلند شده است. در Project B و Project A جدول‌ها با
Gridify ساخته شده‌اند: صفحه‌بندی، مرتب‌سازی و فیلتر سمت سرور. می‌خواهید اینجا هم **هر جا جدول لازم است** همین
الگو باشد.

برداشت من از «هر جا جدول لازم است»: هر فهرستی که با گذشت زمان بزرگ می‌شود. یعنی:
- **Runs**،
- **Import log**،
- **Library** (plans، previews، ideas، research؛ evidence همان گالری می‌ماند),
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
2. روی سر ستون کلیک می‌کنید: مرتب‌سازی سمت سرور، بار دوم برعکس، بار سوم بی‌ترتیب (همان ترتیب پیش‌فرض).
3. پروژه، Status یا Repo را عوض می‌کنید: جدول به صفحه‌ی ۱ برمی‌گردد و کارت‌های بالا برای کل نتیجه حساب می‌شوند.
4. جستجو: یک کادر که در task id، شماره و عنوان پلن، و متن task می‌گردد.
5. صفحه، مرتب‌سازی و فیلترها در آدرس صفحه می‌مانند؛ با refresh یا باز کردن همان آدرس در تب تازه همان را
   می‌بینید. عوض کردن صفحه یا فیلتر یک تاریخچه می‌سازد، پس Back به حالت قبلی برمی‌گردد؛ تایپ در جستجو برای
   هر حرف تاریخچه نمی‌سازد.
6. Import log، Library و تاریخچه‌ی پروفایل همین رفتار را دارند (تاریخچه‌ی پروفایل جستجو ندارد).

فازها:
- **A:** پایه‌ی API (Gridify، TableMapper، پیش‌فرض‌ها، خطا) و Runs (جدول + خلاصه).
- **B:** پایه‌ی UI (جدول، سر ستون، صفحه‌بند، query در آدرس) و صفحه‌ی Runs.
- **C:** Import log، Library، نسخه‌های پروفایل.
- **D:** قاعده در مستندات.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

هیچ صفحه‌ای همه‌ی ردیف‌ها را یک‌جا نمی‌آورد؛ هر جدول بزرگ‌شونده صفحه دارد و مرتب‌سازی و فیلترش در سرور است.

چک‌لیست شما:
- Runs: ۲۰ ردیف و «۱–۲۰ از ۵۳». صفحه‌ی ۳ آخرین ۱۳ ردیف را نشان می‌دهد.
- روی «Cost» کلیک کنید: گران‌ترین run بالا می‌آید؛ دوباره کلیک کنید: ارزان‌ترین؛ بار سوم: ترتیب پیش‌فرض.
- پروژه را روی Project A بگذارید: کارت‌ها عدد همان پروژه را نشان می‌دهند (نه فقط صفحه‌ی فعلی) و گزینه‌های Repo
  فقط `Ui` و `Web` هستند.
- `T-WEB` را جستجو کنید: فقط runهای Web.
- آدرس را کپی کنید و در تب تازه باز کنید: همان صفحه، مرتب‌سازی و فیلتر.
- بعد از عوض کردن صفحه یا فیلتر، Back شما را به حالت قبلی برمی‌گرداند.
- در آدرس `orderBy=passwordHash` یا `filter=foo=1` بگذارید: پیام «این مرتب‌سازی یا فیلتر معتبر نیست»، نه خطای ۵۰۰.
- Import log: جدول صفحه دارد؛ کارت‌های «آخرین خواندن» همان آخرین خواندن را نشان می‌دهند، هر صفحه‌ای که باز باشد.
- Library (هر دسته): صفحه‌بندی و مرتب‌سازی؛ ستون Project هم مرتب می‌شود. Evidence: گالری، و اگر بیشتر از ۱۰۰ شد
  صفحه‌بند زیرش.
- تاریخچه‌ی پروفایل: نسخه‌ها صفحه دارند و روی نسخه و دلیل مرتب می‌شوند؛ مقایسه‌ی a/b همان‌طور کار می‌کند.

## 0. Ground rules for the executor

1. Follow [`../../workflow.md`](../../workflow.md): branch before the first edit; never commit/push/merge unless asked
   in that message; one phase = one uncommitted diff.
2. **The row DTO is the allow-list** (D3). Never Gridify's default mapper.
3. Port, don't reinvent: read Project B's `Web/Src/01.Framework/Common.Framework/Paging/*` and
   `Ui/src/components/app/data-grid/{data-grid-table,data-grid-column-header,data-grid-pagination,data-grid-toolbar}.tsx`
   and `Ui/src/lib/gridify/build-query.ts` first. Copy only what §6 names; drop what this repo does not have
   (`Result<>`, saved filters, export, row select, filter builder, `toLatinDigits`, `applyActiveFiltersToBuilder`).

### Baseline

`dotnet build` + `dotnet test` in `api/` (0 warnings); `typecheck`, `lint`, `test`, `build` in `ui/`. Record both
counts; neither may drop. The baseline after plan 0014 is **313 api tests / 85 ui tests** ([`.ai/ci.md`](../../../.ai/ci.md)).

## 1. Goal

Runs, Import log, Library and profile versions page, sort, filter and search on the server through one Gridify
pattern, with totals computed on the server; and a written rule that makes every future growing table do the same.

## 2. Out of scope

Projects, Profiles, Pools, Cost, Model quality, Runs board, stage tables inside a run, the evidence **gallery**
(it pages, but stays a gallery — D10); saved filters, export, row selection, a generic filter builder; new columns.

## 3. Rules that apply

ADR 0001 (one repo), ADR 0004 (fa/en strings), `.ai/api/conventions.md`, `.ai/ui/conventions.md`,
`.ai/ui/i18n.md`, `.ai/ui/vertical-slice.md` (imports across features only through `index.ts`), `.ai/ci.md`.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | Packages: `Gridify` and `Gridify.EntityFramework` **2.19.1** (the version Project B pins) in `api/Directory.Packages.props` (2.20.0 exists; 2.19.1 is pinned on purpose, to match Project B so the ported code behaves the same); `@tanstack/react-table` **8.21.3** and `gridify-client` **2.2.0**, exact versions, in `ui/package.json` |
| D2 | Wire format: Gridify's own query — `page` (1-based), `pageSize`, `orderBy`, `filter` — bound with `[FromQuery] GridifyQuery`; plus endpoint-specific plain parameters where the page has a fixed filter (`project`, `category`). Response: **`Gridify.Paging<T>` itself** (`using Gridify;`, `{ count, data }`) — no local `Paging.cs`; the name already means this shape in the client (`gridify-client`'s `IPaging`) |
| D3 | **`TableMapper<TEntity, TRow>`** ported from Project B: `private static readonly IGridifyMapper<TEntity> Mapper = Build()`; reflects over the row DTO's public properties, maps each whose name matches a public scalar entity property **of the same underlying type**, camelCase keys, `autoGenerateMappings: false`, built once per closed type; `[NotFilterable]` drops a property; `Build(Action<GridifyMapper<TEntity>>? extra = null)` is the only way to add a differently named map. `NotFilterableAttribute` is kept even though no row property uses it today: the ported reflection calls `IsDefined(typeof(NotFilterableAttribute))`, so the type is part of the port, not dead code |
| D4 | **`TableQueryDefaults`** ported, with the tiebreak as a parameter: page ≤ 0 → 1; pageSize ≤ 0 → **20** (here, not Project B's 10); max **100**; empty `orderBy` → the endpoint's default; `Apply(query, defaultOrder, tiebreak)` appends `tiebreak` unless the order **already ends with the whole tiebreak as its last keys** — compare every key of the suffix, not only the last one (Project B's `EndsInId` would skip `projectId, runKey` for `orderBy=runKey` and make paging unstable). Tiebreaks: **runs** `projectId, runKey`; **library** `projectId, relPath`; **profile versions** `version`; **import log** `id`. All are mapped keys, so the appended order passes `IsValid`. `IsValid(mapper)` false → error `Table.Query.Invalid` (400; en "This sort or filter is not valid."; fa «این مرتب‌سازی یا فیلتر معتبر نیست.»), never a 500 |
| D5 | **Endpoints are new `…/table` routes; the old list routes are deleted** with their handlers when the grep finds no caller left: `GET api/runs` → `GET api/runs/table`; `GET api/import/logs` → `GET api/import/logs/table`; `GET api/library` → `GET api/library/table`; `GET api/profiles/{id}/versions` keeps its `?diff=` form and gains `GET api/profiles/{id}/versions/table` for the list. **The `?diff=` route now returns only the diff** (`{ diff: [...] }`; 400 without `?diff=a,b`) because the list moved to `/table`; the history page runs two queries. `GET api/runs/{project}/{runKey}`, `…/board`, the library item/file/prompt routes are unchanged |
| D6 | **Runs.** Row = today's `RunListItemDto` (all 19 fields). Mapped through D3 (same name and type on `RunEntity`): `projectId`, `runKey`, `taskId`, `repo`, `status`, `planNumber`, `planTitle`, `task`, `branch`, `rounds`, `updatedAt`, `closedAt` — exactly these. **Extra map** (D3 `extra`): `costUsd` → `CostMicros` for ordering only. Gridify cannot make a key sort-only, so a hand-typed `filter=costUsd>…` is accepted and compares **micros, not dollars**; that is accepted as is, the UI never offers a cost filter, and the DTO comment says so. **Computed, not mapped** (enriched per page): `projectName` (`Project.Name`), `sessions`, `calls`, `requests` (SQL aggregates), `models` (one second query for the page's run ids), `source` (select the `RunSource` **enum** in SQL and call `ToString()` in memory — do not translate an enum `ToString()`). Computed fields are neither sortable nor filterable; their headers show no sort control. Default order `updatedAt desc`. Fixed parameter `project` (as today: unknown → `Projects.Project.NotFound`) |
| D7 | **Runs summary.** `GET api/runs/summary?project=&filter=` (the same `filter` string the table uses, validated by the same mapper) → `{ runs, sessions, calls, costUsd, statuses[], repos[] }` over the **whole filtered set** — `filter` is applied with `ApplyFiltering` to the source, **never** read off the page query. `statuses`/`repos` are computed with the project filter only (not `filter`), so the dropdowns never empty themselves. The KPI cards and the two dropdowns read it; the table never sums a page |
| D8 | **Search** is a UI-side expansion into the Gridify `filter`, built with **`gridify-client`'s own builder** (not string concatenation): `startGroup()`, then `addCondition(field, op.Contains, term, /*caseSensitive:*/ false)` for each searchable field joined with `or()`, then `endGroup()`. `false` makes the builder append `/i` (case-insensitive; SQLite's `instr` is case-sensitive) and the builder escapes `( ) , | /i` itself — there is no separate escaping helper to call. Searchable fields: Runs `taskId, planNumber, planTitle, task`; Import log `projectId`; Library `title, slug, number`. Other filters are separate conditions joined by AND. **Fallback, decided now:** if `/i` does not translate on SQLite (throws, or evaluates in memory), drop `/i` everywhere — search becomes case-sensitive — and note it in the hand-over; do not stop |
| D9 | **URL state.** `page`, `pageSize`, `orderBy`, `q` and the page's own filters (`project`, `status`, `repo`, `tab`) live in the page's query string. `use-table-query` owns the first four: `setPage`, `setPageSize`, `setOrderBy` and a deliberate filter change use `router.push` (Back works); the debounced (300 ms) search term uses `router.replace` (no history entry per keystroke). Page, page size, order, search **and a page-owned filter change** reset `page` to 1 (a filter change goes through the page's own `router.push`, which sets `page=1`). **Every other query parameter is preserved**: the hook reads and writes only its keys; each page merges its own filters (`project`/`status`/`repo` on Runs, `tab`/`project`/`status` on Library) into the hook's `params` and calls `router.push` itself, so the hook never owns or resets them. Library keeps `library-params.ts` for `tab`↔`category` and the detail links. Defaults are omitted from the URL (`page=1`, `pageSize=20`, the page's default order, empty `q`); `state.orderBy` is always the **effective** order, so the default column shows its sort indicator |
| D10 | **The rule** (written in phase D, one owning doc: `.ai/api/conventions.md`, with a one-line pointer in `.ai/ui/conventions.md`): *a list that grows with use — including the versions of one profile, which grow with every save — is a `…/table` endpoint through `TableMapper`, returning `Paging<T>`, with an unknown field refused as `Table.Query.Invalid`; in the UI it is read by a `DataTable`, or for evidence by the existing gallery with the same pager under it. A list bounded by configuration (projects, profiles, pools), an aggregate (cost, quality) or a live view (board) is not a table.* `.ai/ui/conventions.md`'s "minus … data grids" clause is deleted in the same phase |
| D11 | **Import log** row = today's `ImportLogDto` exactly: `id`, `projectId`, `startedAt`, `finishedAt`, `filesRead`, `blocksRead`, `runsBuilt`, `libraryItems`, `errorCount`, `errors`. `id` matches `ImportLogEntity.Id` (both `Guid`), so it maps **automatically** — no extra map; it is the tiebreak. `errors` is not scalar and is skipped by D3; the handler loads the page with `Include(log => log.Errors)` and projects it in memory from the page's entities. Default `startedAt desc`. No `[NotFilterable]` needed |
| D12 | **Library** row = today's `LibraryItemDto` exactly (all 20 fields: `projectId, projectName, category, relPath, title, number, slug, repo, status, createdOn, updatedOn, doneOn, plannedBy, previewRelPath, linkedPlanNumber, runKey, group, modifiedAt, runStatus, linkedPlanTitle`). Mapped: `projectId, relPath, title, number, slug, repo, status, createdOn, updatedOn, doneOn, plannedBy, previewRelPath, linkedPlanNumber, runKey, group, modifiedAt` (the DTO, like today, does **not** carry `sizeBytes`). **Not mapped:** `projectName` (navigation; added through D3 `extra` as `m.AddMap("projectName", item => item.Project.Name)` so the Project column still sorts and filters), `runStatus` and `linkedPlanTitle` (computed). **`category` is not mapped**: the row carries the string, the entity the `LibraryCategory` enum, so D3 skips it — that is why the tiebreak is **`projectId, relPath`** (unique once the required `category` fixes the category; the unique index is `ProjectId, Category, RelPath`). Fixed parameters: `project` optional, `category` **required** (unknown → `Library.Item.NotFound`). Default `updatedOn desc`. Today's `status`/`sort`/`dir` parameters are replaced by `filter`/`orderBy`; the status select still works because it sends `filter=status=Done` (exact value from a fixed list) inside one category. The page's entities are materialized with `Include(item => item.Project)` and built by the existing `LibraryDtos.ToDto`, so list and detail share one DTO; `runStatus` and `linkedPlanTitle` use two small lookups for the page's rows only. SQLite's null order (nulls first ASC, last DESC) replaces today's "nulls last in both directions". The `runKey` sort is **lexicographic** now (`orderBy=runKey` on the raw key); today's numeric `TaskId`→`RunNumber` order is not reproduced, and the UI keeps the column and its link. The evidence category keeps its **gallery** but reads the same route: `GET api/library/table?category=Evidence&project=…&pageSize=100`, with the shared pager under the gallery when `count` > 100. `GET api/library` is deleted |
| D13 | **Profile versions** row = today's `ProfileVersionSummaryDto` (`version`, `reason`, `createdAt`); all three map to `ProfileVersionEntity`; default `version desc`; tiebreak `version`, already mapped. Existence check first (`Profiles.Profile.NotFound`). The history keeps its four columns (version, reason, a/b, revert) — `createdAt` stays in the row but is not a column; **no search** on this table. `GET …/versions?diff=a,b` returns only the diff (D5) |
| D14 | **UI** in `ui/src/components/app/data-table/`: `data-table.tsx`, `column-header.tsx`, `pagination.tsx`, `toolbar.tsx`, `index.ts`; `ui/src/lib/gridify/{build-query.ts,use-table-query.ts}`. **Column contract:** `{ id: string /* the column's id; when `sortable`, the mapped Gridify key */, header: string /* translated */, sortable?: boolean /* default false */, cell: (row) => ReactNode, className?, headClassName? }`. `data-table` props: `{ columns, rows, count, page, pageSize, orderBy, onPageChange, onPageSizeChange, onOrderChange, onRetry, loading, error, empty }`; TanStack `manualPagination` + `manualSorting`; skeleton rows while loading; `empty` when `count` is 0; on `error`, an `ErrorState` whose title is **`error.message` when it is an `ApiError`** (so `Table.Query.Invalid` shows the server's localized text), else `common('errors.network')`, with `retryLabel={common('actions.retry')}` and the page's `refetch` as `onRetry`. Sort header cycles asc → desc → none (`orderBy` = `field`, `field desc`, `""`) and only when `sortable`. Pager: range text, page size 10/20/50/100, first/prev/next/last with RTL-aware icons, all numbers through `useAppFormatter` |
| D15 | **New shared strings** live in `ui/src/lib/i18n/messages/common.{en,fa}.ts` under `table`, both languages, no literal in a component (`.ai/ui/conventions.md`): search, searchPlaceholder, pageSize, range (`{from}–{to} of {total}`), first, previous, next, last, sortAsc, sortDesc, sortNone. The Persian values are written here and approved with this plan: «جستجو»، «جستجو…»، «ردیف»، «{from}–{to} از {total}»، «صفحه‌ی اول»، «صفحه‌ی قبل»، «صفحه‌ی بعد»، «صفحه‌ی آخر»، «مرتب‌سازی صعودی»، «مرتب‌سازی نزولی»، «حذف مرتب‌سازی». Feature strings that change: **add** `imports.columns.finishedAt` (en "Finished", fa «پایان»); **remove** the now-unused `library.sort.label` from `ui/src/features/library/i18n/{en,fa}.ts`; the rest reuse existing keys |
| D16 | **Exact signatures** (the only deviations from Project B): `TableQueryDefaults.Apply(GridifyQuery query, string defaultOrderBy, string tiebreak)`; `TableMapper<TEntity,TRow>.Query(IQueryable<TEntity> source, GridifyQuery request, string defaultOrderBy, string tiebreak, IGridifyMapper<TEntity>? mapper = null)` → `(QueryablePaging<TEntity>? Paging, Error? Error)`; `Build(Action<GridifyMapper<TEntity>>? extra = null)` → `IGridifyMapper<TEntity>`. The runs mapper with `costUsd` and the library mapper with `projectName` are `static readonly` fields next to their handlers (`RunsTableMapping.Mapper`, `LibraryTableMapping.Mapper`), built once |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/Directory.Packages.props`, `api/src/Orchestra.Api/Orchestra.Api.csproj` (D1) | EDIT | A |
| 2 | `api/src/Orchestra.Api/Common/Paging/{TableMapper,TableQueryDefaults,NotFilterableAttribute}.cs` | NEW | A |
| 3 | `api/src/Orchestra.Api/Common/Errors/{Error,ErrorMessages}.cs` — `Table.Query.Invalid` (en + fa) | EDIT | A |
| 4 | `api/src/Orchestra.Api/Modules/Runs/Features/RunDtos.cs` (moved from `Features/ListRuns/RunDtos.cs`, namespace `…Runs.Features`, plus `RunDtos.ToUsd`); then delete `Features/ListRuns/` — its handler and the now-empty folder | NEW/DELETE | A |
| 5 | `api/src/Orchestra.Api/Modules/Runs/Features/RunsTable/{RunsTableMapping,RunsTableHandler,RunsSummaryHandler}.cs`; `RunsController.cs` (`/table`, `/summary`, drop `List`); `RunsModuleInstaller.cs` (register the two, drop `ListRunsHandler`); `Features/GetRun/GetRunHandler.cs` (`RunDtos.ToUsd`, using `…Runs.Features`) | NEW/EDIT | A |
| 6 | `api/tests/Orchestra.Api.Tests/{Common/TableMapperTests,Runs/RunsTableTests}.cs`; `Dashboard/DashboardTests.cs` (its two `ListRunsHandler` tests rewritten for `RunsTableHandler`) | NEW/EDIT | A |
| 7 | `ui/package.json` + `package-lock.json` (D1); `ui/src/components/app/data-table/*`; `ui/src/lib/gridify/{build-query,use-table-query}.ts` (+ their tests) (D14, D16) | NEW/EDIT | B |
| 8 | `ui/src/lib/i18n/messages/common.{en,fa}.ts` (D15) | EDIT | B |
| 9 | `ui/src/app/[locale]/page.tsx`, `ui/src/app/[locale]/imports/page.tsx` — wrap the client component in `<Suspense fallback={<TableSkeleton …/>}>` (they have none today; `useSearchParams` fails the static export without it) | EDIT | B |
| 10 | `ui/src/features/runs/{api,queries,types}.ts`, `components/runs-list.tsx`, `runs.test.tsx` — **keep** plan 0009's empty-state link to the start page (`runs.noProjects`) | EDIT | B |
| 11 | `api/src/Orchestra.Api/Modules/Import/{ImportController,ImportModuleInstaller}.cs`; `ImportLogsHandler.cs` → `ImportLogsTableHandler.cs` (`logs/table`); `ImportDtos.cs` unchanged; `ui/src/features/imports/{api,queries,types}.ts`, `components/import-log.tsx`, `i18n/{en,fa}.ts` (`columns.finishedAt`) (D11) | EDIT | C |
| 12 | `api/src/Orchestra.Api/Modules/Library/{LibraryController,LibraryModuleInstaller}.cs`; `Features/ListLibrary/{ListLibraryHandler.cs → LibraryTableHandler.cs, +LibraryTableMapping.cs}`; **`LibraryDtos.cs` stays** (the detail route uses it); `ui/src/features/library/{api,queries,types,library-params}.ts`, `components/{library-page,evidence-gallery}.tsx`, `i18n/{en,fa}.ts` (`sort.label` removed) (D12) | EDIT | C |
| 13 | `api/src/Orchestra.Api/Modules/Profiles/{ProfilesController,ProfilesModuleInstaller}.cs`; `Features/GetVersions/{GetVersionsHandler.cs → GetVersionDiffHandler.cs, +ProfileVersionsTableHandler.cs}`; `ui/src/features/profiles/{api,queries,types}.ts`, `components/profile-history.tsx` (D13) | EDIT | C |
| 14 | Tests for rows 11–13: `api/tests/Orchestra.Api.Tests/{Import/ImportLogsTableTests,Library/LibraryTableTests,Profiles/ProfileVersionsTableTests}.cs`; `Library/ListLibraryTests.cs` rewritten; UI tests in `ui/src/features/{runs/runs,imports,library,profiles}/*.test.tsx` | NEW/EDIT | C |
| 15 | `.ai/api/conventions.md` (D10, D3, D4 — the rule and the boundary), `.ai/ui/conventions.md` (the "data grids" clause removed, the pointer added), `.ai/ui/design.md` (the library screens: the sort/direction selects become the search box plus sortable column headers) | EDIT | D |
| 16 | `.ai/ci.md` — the baseline counts after each phase (the file asks for this at every phase end) | EDIT | A–D |

## 6. Per-file specification

### 6.1 Shared API shape (every table)

```csharp
public static (QueryablePaging<TEntity>? Paging, Error? Error) Query(
    IQueryable<TEntity> source, GridifyQuery request, string defaultOrderBy, string tiebreak,
    IGridifyMapper<TEntity>? mapper = null)
{
    var effective = mapper ?? Mapper;
    TableQueryDefaults.Apply(request, defaultOrderBy, tiebreak);
    if (!request.IsValid(effective)) return (null, Error.TableQueryInvalid);
    return (source.GridifyQueryable(request, effective), null);
}
```

`TableQueryDefaults.Apply` clamps page/size (default 20, max 100), fills the order, and appends the tiebreak with
the suffix check of D4. `Table.Query.Invalid` is added to both `ErrorMessages.En` and `.Fa` (D4).

`QueryablePaging<TEntity>.Count` is the filtered count; `.Query` is the ordered/skipped/taken query. The response is
`new Paging<TRow>(count, rows)` where `Paging<T>` is Gridify's own type.

### 6.2 Runs (D6)

- `source` = `database.Runs.AsNoTracking()` + the `project` filter (unknown → `Projects.Project.NotFound`). No `Include`s:
  the page is projected in SQL into a private `RunRow` with `Id`, all the DTO's scalar fields, `Project.Name` as
  `ProjectName`, `Source` **as the enum**, `Sessions.Count`, `Sessions.Sum(…Calls)`, `Sessions.Sum(…Requests)`,
  `CostMicros`.
- After `ToListAsync`, `models` for the page's `Id`s is one query on `StageResults` (`IsFinal && Model != null`),
  grouped in memory; then each `RunListItemDto` is built: `CostUsd = RunDtos.ToUsd(CostMicros)`,
  `Source = row.Source.ToString()`, `Models = …`.
- `RunsTableMapping.Mapper = TableMapper<RunEntity, RunListItemDto>.Build(m => m.AddMap("costUsd", run => run.CostMicros))`.
- `RunsSummaryHandler` (D7) validates `project`, calls `Apply` + `IsValid` with the same mapper, then
  `source.ApplyFiltering(request, mapper)`, and runs the counts/sums and the project-only `statuses`/`repos` queries
  (`Repos` drops nulls). `RunsSummaryDto` is declared in `RunsSummaryHandler.cs`, beside its handler (the repo's
  DTO-beside-the-feature rule). Return it.

### 6.3 Import log (D11)

`source` = `database.ImportLogs.AsNoTracking()` + the optional `project` filter. `Query(source, query, "startedAt desc",
"id")`; the page is loaded with `.Include(log => log.Errors)` and mapped in memory to `ImportLogDto` (errors ordered by
`File`, as today). `ImportLogsHandler` is renamed `ImportLogsTableHandler`; the installer registers it in place of the
list handler; the old route is deleted.

### 6.4 Library (D12)

- Parse `category` with `LibraryCategoryNames` (required; unknown → `Library.Item.NotFound`); validate `project` with
  the registry; `source` = `LibraryItems.Where(item => item.Category == wanted)` + optional project.
- `Query(source, query, "updatedOn desc", "projectId, relPath", LibraryTableMapping.Mapper)`; the page is loaded with
  `Include(item => item.Project)`; two page-scoped lookups (run statuses by the page's projects/run keys, linked plan
  titles by the page's idea numbers/plans) feed `LibraryDtos.ToDto`. Return `Paging<LibraryItemDto>`.
- `LibraryTableMapping.Mapper` adds `projectName` → `item.Project.Name` (D12) so the column sorts and filters.

### 6.5 Profile versions (D13)

`ProfileVersionsTableHandler`: existence check, then `Query(source, query, "version desc", "version")` over
`ProfileVersions` for the id, projecting `ProfileVersionSummaryDto`. `GetVersionDiffHandler` keeps only the `?diff=`
half: parse `a,b` (else 400 `Profiles.Profile.Invalid` with today's detail), load both, return `ProfileDiffDto`
(a new `IReadOnlyList<ConfigChangeDto> Diff`, declared in that file); the old `ProfileVersionsDto` is deleted.

### 6.6 UI components and pages (D9, D14)

- `build-query.ts`: `buildGridifyQuery({ page, pageSize, orderBy, search, searchableFields, filters })` → `{ page,
  pageSize, orderBy, filter }`, all through `GridifyQueryBuilder` (D8); exact filters with `op.Equal` and
  `caseSensitive: true`.
- `use-table-query.ts`: reads/validates the four URL keys, returns `{ state, params, setPage, setPageSize,
  setOrderBy, setSearch }` (D9); `params` is the page's query object for its own links.
- Runs page: project chips, KPI cards and the Status/Repo options read the **summary**; the table reads `/table`; while
  the summary loads the cards show `—`, and an error from either query shows the page's one `ErrorState` (retry
  refetches both); the toolbar holds the search box left of the selects; the project selection is URL state; the empty
  state keeps the `/start/` link (plan 0009 D15).
- Library page: tab → `category`, project chips, status select (`filter=status=…` on Plan/Idea only), search
  (`title, slug, number`); **the sort and direction selects are gone** — the numbered column headers sort. Columns as
  today, with number/title/projectName/status/createdOn/updatedOn/doneOn/runKey sortable and the custom cells (links,
  badges, run link) kept. Any filter change (tab, project, status) sets `page=1`. Evidence renders the gallery from the
  same query (pageSize 100) plus the pager; the gallery's open-full-size dialog is unchanged.
- Import log page: the KPI/errors block has its **own** `pageSize=1&orderBy=startedAt desc` query (D11), the table
  pages, and the toolbar's search filters `projectId`.
- Profile history: the versions table uses the shared table (paging; sort on version and reason, no createdAt header,
  no search); the a/b selection and the diff table are unchanged; the diff is its own query.

## 7. Tests

**API.** `TableMapperTests`: the runs map's key set equals D6's mapped list plus `costUsd`, and has no `id`,
`projectName`, `sessions`, `calls`, `requests`, `models`, `source`; an empty `orderBy` is valid and orders by
`updatedAt desc, projectId, runKey`; `orderBy=runKey` gets `projectId` appended too (the whole-suffix check); the
import-log map contains `id` with no extra map; the library map contains `projectName` and its tiebreak
`projectId, relPath` passes; `costUsd` orders by micros; `passwordHash`, `sessions` and `foo` → `Table.Query.Invalid`.
`RunsTableTests` on the SQLite fixture: default order; page 3 of 20 with 53 rows → 13; `filter=repo=Web`;
`orderBy=costUsd desc`; a search `filter=taskId=*T-WEB/i` (the `/i` fallback check); summary equals every filtered
row, not one page, and `statuses`/`repos` ignore `filter`. Same pattern (count, default order, one filter, one invalid
field) for import logs, library per category (plus a null-order assertion) and versions. `DashboardTests` keeps its two
guarantees in their new form: an exact `filter=status=Verified` and an unknown project → `Projects.Project.NotFound`.

**UI.** `build-query` (page/size/sort/search/filters, `/i`, escaping of `(`, `,`, `|`); `use-table-query` (URL →
state → URL, a filter change resets the page, search replaces instead of pushing); `data-table` (clicking Next/Last
shows the next/last page's rows and the range text changes, the sort header cycles asc/desc/none and the visible order
follows, a non-sortable header has no control, empty at 0, `ApiError.message` on a 400);
the Runs page (cards and dropdown options from the summary, not the rows); the Library page (tab/project preserved in
the URL, evidence pager over 100); the import log (the KPI block uses its own single-row query); the profile history
(paging and sort, no search). `architecture.test.ts` stays green.

## 8. Stop and ask

- Projecting `Sessions.Sum` in SQL fails on SQLite for `decimal`/`long?` and the only fix changes a column type.
- A caller of a deleted list route exists outside the files named in §5.
- `gridify-client` 2.2.0 cannot express the search group or the exact filters of D8 without hand-built strings.

## 9. Phases

**A — API base + Runs.** Files 1–6. **Gate:** `dotnet build` (0 warnings) + `dotnet test`. Stop here — show the
owner the diff.

**B — UI base + Runs page.** Files 7–10. **Gate:** `typecheck`, `lint`, `test`, `build`. **Owner checks:** the Runs
items of the checklist. Stop here — show the owner the diff.

**C — The other tables.** Files 11–14. **Gate:** both sides' gates. **Owner checks:** the remaining items. Stop
here — show the owner the diff.

**D — The rule.** File 15. Re-read the conventions around it and merge or delete what D10 overlaps; the hand-over
names what was removed. Stop here — show the owner the diff.

## 10. Definition of done

All phases' gates green with counts at or above the baseline; no list route of D5 left with a caller; the owner's
checklist passes; `Status: Done`, the `Done:` date, the commit and the merge in the header.

## 11. What v2, v3 and v4 did with the review

v2 applied, in full: the library row (D12, the existing `LibraryItemDto` plus `projectName` through the extra hook);
the library tiebreak (D4/D12 — `projectId, relPath`, because `category` cannot map across string/enum); `ToUsd`
moving to `RunDtos` with `DashboardTests` and `GetRunHandler` in the manifest; `ListLibraryTests` in the manifest
with the new guarantees (SQLite null order, Project sortable through the extra map, the status filter inside one
category); the two missing `<Suspense>` wrappers; §6.4 deleted (the start page has no "any run exists" step since
plan 0012 D16); the i18n catalogue and the Persian strings (D15); `Gridify.Paging<T>` instead of a local record;
the whole-suffix tiebreak check; exact signatures and where the two extra mappers live; the summary applying the
filter to the whole source; the profile `?diff=` shape; the exact `ImportLogDto` row and the redundant extra map
dropped; the DataTable column contract; `LibraryDtos.cs` staying; `tab`/`project` preserved by the URL state; the
gallery exception in D10 and the "data grids" clause; the import log's own single-row query; the builder instead of
"gridify-client's escaping"; the enum `Source`; `push` for deliberate changes and `ApiError.message`; the profile
history without search; the header's `Base`.

v3 answers the second round (0 blockers, 4 should, 2 nits): the profile history keeps **no createdAt column** and
does not claim to sort by date (D13, §6.6, the checklist); `data-table` gains the required `onRetry` and the pages
pass their `refetch` (D14); `ui/src/features/runs/runs.test.tsx` is in the manifest and the test list; the imports
`i18n/{en,fa}.ts` are in row 11; `RunsSummaryDto` is declared in `RunsSummaryHandler.cs`; and D9 says the pages
merge their own parameters into the hook's `params` and call `router.push` themselves.

v4 answers the third round (0 blockers, 3 should, 8 nits): `.ai/ui/design.md` is in row 15 (the library's sort and
direction selects become the search box and sortable headers); a page-owned filter change sets `page=1` (D9, §6.6);
the library's `runKey` sort is stated as lexicographic (D12); D6 and §6.2 name `Projects.Project.NotFound`; D15 says
`imports.columns.finishedAt` is **added** and `library.sort.label` is **removed**; the column `id` is only the Gridify
key when `sortable` (D14); the default column shows its sort indicator (D9); the runs summary's loading/error state is
specified (§6.6); the diff response is named `ProfileDiffDto` (§6.5); and the data-table test asserts what the user
sees, not a callback (§7).

Not applied, with reasons — **do not raise them again without new facts**:

1. **A `**Repos:**` line** — ADR 0001 and `skills/orchestrate/SKILL.md` say a plan of this one repository names no
   repos; the engine already read the single repo. The TEMPLATE's line is for a workspace with more than one.
2. **Dropping `NotFilterableAttribute`** — the ported `TableMapper` calls `IsDefined(typeof(NotFilterableAttribute))`;
   removing the type would mean editing the port for no gain. It stays unused today, and D3 says so.
3. **Putting the routes in `.ai/architecture/overview.md`** — that file has no route list; adding one is new scope and
   upkeep. Routes stay in the code and this plan; the **rule** goes in its one owning doc (D10).
