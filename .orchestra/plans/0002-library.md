# Plan 0002 — Library: plans, previews, ideas, research, evidence

**Status:** Done — phases A–C implemented and verified 2026-11-19; commit `1022f5e` on `alireza/library` ("Plan 0002: library — plans, previews, ideas, research, evidence"), merge `861dd2a` into `alireza/skeleton-and-import`
**Created:** 2026-11-19 · **Updated:** 2026-11-19 · **Done:** 2026-11-19
**Preview:** [`../previews/0001-skeleton-and-import/`](../previews/0001-skeleton-and-import/index.html) §4 and the "Plans" mock — approved 2026-11-19 (v3)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:**
- Round 1, 2026-11-19 (relayed by the owner): 6 blockers, 10 important and 4 minor points, all applied.
- Round 2, 2026-11-19 (relayed by the owner): changes requested. 4 blockers, 6 important and 3 minor
  points, all applied.
- Round 3, 2026-11-19 (relayed by the owner): changes requested. 2 blockers, 5 important and 6 minor
  points, all applied.
- Round 4, 2026-11-19 (relayed by the owner): changes requested. 1 blocker, 4 important and 4 minor
  points, all applied.
- **Reviewing model (all rounds): DeepSeek** (owner, 2026-11-19) — a different model family
  from the planner's (Claude).
- Round 5, 2026-11-19 (relayed by the owner): clean, no open points.
- §11 lists where each point went.

**Decided by:** Alireza, 2026-11-19. He approved preview v3 ("v3 is enough" for this plan) and, in
round 2, the deviations of D12. Every choice below is already decided — **decide nothing new.** If
reality contradicts this plan, STOP and ask.
**Branch:** `alireza/library` — the implementer creates it.
**Base:** `alireza/skeleton-and-import` after plan 0001 is done (its merge commit)
**Companion:** None
**Steps changed:** no preview of its own — preview 0001 covers it (owner, 2026-11-19)

---

## فهم من از درخواست

می‌خواهید همه‌ی خروجی‌های کار ایجنت‌ها را، دسته‌بندی‌شده، در داشبورد ببینید:
- پلن‌ها، با شماره، تاریخ و وضعیت،
- پیش‌نمایش‌های HTML،
- ایده‌ها،
- تحقیق‌ها،
- شواهد تست ایجنت (اسکرین‌شات و گزارش).

برای هر پلن هم دکمه‌ای می‌خواهید که پرامپت آماده برای ارکستر را کپی کند. وضعیت پلن‌ها از داشبورد **دیده**
می‌شود و عوض نمی‌شود، و دکمه run را شروع نمی‌کند (ADR 0003).

برداشت من این است که کتابخانه باید **ساختار امروز** پروژه‌ها را هم بخواند، نه فقط `.orchestra/`:
- پلن‌ها الان در `<repo>/.ai/plans/` و `done/` هستند.
- پیش‌نمایش‌ها فایل‌های HTML پراکنده در ریشه‌ی پروژه یا در `runs/_drafts/` هستند.
- اسکرین‌شات‌ها در `screenshots/` هستند.
- ایده‌ها در یک `ideas.md` هستند.

برای همین هر پروژه می‌تواند مسیرها را در تنظیمات بدهد. اگر ندهد، ساختار `.orchestra/` پیش‌فرض است.

این پلن کارهای زیر را **نمی‌کند**:
- فایلی را تغییر نمی‌دهد، و وضعیت پلن را از داشبورد عوض نمی‌کند.
- جستجوی متن کامل ندارد (ایده‌ی `search-findings`).
- مقایسه‌ی نسخه‌های پلن ندارد (ایده‌ی `plan-version-diff`).

یک محدودیت که از اول می‌گویم: پیش‌نمایش HTML داخل یک قاب جدا اجرا می‌شود و **هیچ اتصال شبکه‌ای ندارد**.
پیش‌نمایشی که برای کار کردنش به داده‌ای از اینترنت نیاز دارد، درست کار نمی‌کند. فقط فونت‌های Google و
اسکریپت‌های چند CDN شناخته‌شده مجازند (D6).

## جریان کار

1. برای هر پروژه، در `appsettings.Local.json`، ریشه‌ی پروژه و (اختیاری) الگوی مسیر هر دسته را
   می‌نویسید.
2. هر «خواندن دوباره»، کنار runها، فهرست کتابخانه‌ی هر پروژه را هم از نو می‌سازد. فقط نام، سربرگ و تاریخ
   فایل‌ها خوانده می‌شود و متن کامل ذخیره نمی‌شود. «گزارش خواندن» تعداد موارد کتابخانه را هم نشان می‌دهد.
3. در منوی «کار»، صفحه‌ی «کتابخانه» پنج تب دارد: پلن‌ها، پیش‌نمایش‌ها، ایده‌ها، تحقیق‌ها، شواهد.
   - **پلن‌ها:** شماره، عنوان، پروژه، وضعیت، تاریخ ساخت، تاریخ به‌روزرسانی، تاریخ پایان، و run مربوط (با لینک).
     مرتب‌سازی بر اساس هر کدام از این ستون‌ها، و فیلتر بر اساس پروژه و وضعیت.
   - روی هر پلن: متن پلن (markdown)، پیش‌نمایشش (اگر هست)، runهای مربوطش، شواهدش، و دکمه‌ی «کپی پرامپت».
   - **ایده‌ها:** وضعیت ایده و پلنی که از آن ساخته شده (اگر هست) دیده می‌شود.
   - **پیش‌نمایش‌ها:** در یک قاب جدا و بدون دسترسی شبکه باز می‌شوند.
   - **شواهد:** به تفکیک پلن و فاز، به شکل گالری تصویر. گزارش‌های `report.md` هم کارت خودشان را دارند.
4. در جزئیات هر run، اگر پلنی به آن وصل باشد، لینکش آمده است. متن مرورها و verify هم آن‌جا به شکل markdown
   نشان داده می‌شود.
5. فایلی که پاک شود، بعد از خواندن دوباره از کتابخانه هم حذف می‌شود.

فازها:
- **A:** تنظیمات و خواندن فهرست.
- **B:** API.
- **C:** صفحه‌ها و دکمه‌ی پرامپت.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- «گزارش خواندن» برای هر پروژه تعداد موارد کتابخانه را نشان می‌دهد، بدون خطا.
- در Project B، پلن 0032 با وضعیت «انجام‌شده» و تاریخ پایان 2026-11-18 آمده و به run `T-WEB-002` لینک
  شده است. در جزئیات `T-WEB-002` هم لینک برگشت به پلن 0032 هست.
- در Project A، پلن‌هایی که شماره ندارند (مثلاً `ad-title-encoding-and-image-attach`) با نامشان آمده‌اند.
- پلن‌ها را بر اساس هر ستون مرتب کنید، و فیلتر «انجام‌شده» را بزنید.
- «کپی پرامپت» را بزنید و متن را در یک ویرایشگر paste کنید. مسیر پلن و وضعیتش در آن هست.
- یک پیش‌نمایش HTML را باز کنید. داخل قاب کار می‌کند، فونتش درست است، و در console مرورگر هر درخواست
  شبکه‌ای از آن با خطای CSP رد شده است.
- یک run بایگانی‌شده را باز کنید. متن مرورهایش به شکل markdown (جدول، فهرست، کد) نشان داده می‌شود.
- یک فایل ایده را پاک کنید و دوباره بخوانید. از فهرست حذف می‌شود.
- همه را در فارسی و انگلیسی، و در تم روشن و تاریک، چک کنید.

Orchestra's own library (its plan 0001 and preview) is **not** an owner check here: Orchestra has no
runs folder until plan 0007, so it is not a configured project yet. Its `.orchestra/` layout is covered
by the fixture tests.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md) and plan 0001's ground rules 2–6 (read-only, no real data
   in fixtures, Project B as style reference, Next docs first, copy Persian strings from §6.6).
2. **The file endpoint is a security boundary.** Implement §6.4 exactly; its tests are not optional.

### Baseline

Plan 0001's gates green, test counts as recorded in `.ai/ci.md`. The counts never drop.

## 1. Goal

Index five categories of work products per project, show them in one Library screen with sorting,
filtering and plan ↔ run links in both directions, render documents safely (including the archived run
texts plan 0001 shows as plain text), and offer a copy-ready prompt per plan.

## 2. Out of scope

Editing, moving or renaming any file; changing a plan's status; full-text search; plan diffs;
transcripts; the new-project form (0003); previews that need the network; localized import-error texts
(plan 0001's errors are English too).

## 3. Rules that apply

Plan 0001 §3, plus [ADR 0003](../../.ai/decisions/0003-read-only-dashboard-first.md) (show, never
act) and [ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) (files are the truth;
the index is rebuilt).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | Indexed with the project's import, in the **same transaction**, by rebuild (plan 0001 D3) |
| D2 | Only metadata is stored (path, title, header fields, size, mtime). Content is read from disk on request |
| D3 | Per-project globs per category; default = the `.orchestra/` layout (§6.1) |
| D4 | Plan and idea headers are parsed tolerantly: the three plan header styles found in the real projects, and the ideas' one-line header (§6.2) |
| D5 | Plan ↔ run link, same project only. **(a) By number:** `Run.PlanNumber` = the plan's `Number`, and when both have a repo, `Run.Repo` = the plan's `Repo` (ordinal, ignore case). **(b) Else by slug:** the run's branch, last `/` segment, with a leading `plan-` and then a leading `\d{4}-` removed, equals the plan's `Slug` (ignore case). (b) is tried only for plans with no number; a numbered plan needs no (b), because plan 0001's `PlanName.From` already takes the number from a `plan-NNNN-…` branch. Several runs match one plan → the plan's `RunKey` is the **newest** (highest `n`, compared **as a number**, so `.10` beats `.9`; then latest `UpdatedAt`); the item detail lists all of them |
| D6 | HTML previews are served with the CSP of §6.4 rule 4: script sandbox, **no network** (`connect-src 'none'`), no forms; only Google Fonts and the scripts of cdnjs, jsDelivr and unpkg may load. Shown in `<iframe sandbox="allow-scripts">`, never same-origin with the dashboard. A preview that needs other hosts does not work — accepted (preview v3, owner) |
| D7 | Markdown renders with `react-markdown` 10.1.0 + `remark-gfm` 4.0.1, **no raw HTML** (`skipHtml`), through one shared `Markdown` component in `components/app/` (plan 0001 §6.13). It renders library documents, evidence reports **and** the run detail's `StageText` (this closes plan 0001 D23) |
| D8 | The copy-prompt text is the approved mock's, filled server-side (§6.5); English (it is for an agent). The button says «کپی پرامپت» / "Copy prompt" as in the mock; its tooltip names the conductor |
| D9 | Evidence is images (`png`, `jpg`, `jpeg`, `webp`, `gif`) and `report.md` files **only**; any other file matched by an evidence glob is skipped silently. Grouped by the folder segments below the glob's fixed prefix (`.orchestra/evidence/` by default, `screenshots/` in today's layout), at most two, e.g. `NNNN-topic/phase-X`; one folder → that folder; a file directly under the prefix → `Group` null (shown as ungrouped, linked to no plan) |
| D10 | No item may have a symbolic link or junction in its path **below** `RootPath` (the root itself may be one). Checked at indexing and again on every file request (§6.4) |
| D11 | `Repo` is set **for plans only** (null for every other category): the path segment matched by the **first `*` that is a whole folder segment** of the glob (not the file name, never a `**`). `*/.ai/plans/*.md` → the repo folder; `.orchestra/plans/*/*.md` → the folder under `plans/` (plan 0007's layout); `.orchestra/plans/*.md` → none. A matched segment named `done` (any case) is **never** a repo → `Repo` null, so D5(a) is not blocked by it |
| D12 | Deviations from the v3 mock, approved by the owner in round 2: plans show **No.** and **Title** as two columns and add a **Done** date column; each item has a detail page (`/library/view/`) that holds the Copy prompt button, the runs, the preview and the evidence. The prompt text itself stays the mock's (D8) |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `RootPath`, `Library` per project | EDIT | A |
| 2 | `api/src/Orchestra.Api/appsettings.Local.example.json` — the two example projects get `RootPath` + `Library` for today's layout; `deploy/.env.example` and `deploy/docker-compose.yml` (plan 0001 §6.14) — a commented `Orchestra__Projects__0__RootPath` and its read-only mount | EDIT | A |
| 3 | `api/src/Orchestra.Api/Modules/Library/LibraryItemEntity.cs`, `LibraryCategory.cs` | NEW | A |
| 4 | `api/src/Orchestra.Api/Data/Configurations/LibraryItemConfiguration.cs` + **one** migration `Library` that creates `LibraryItem` and adds `ImportLog.LibraryItems` (`int`, default 0) | NEW | A |
| 4a | `api/src/Orchestra.Api/Modules/Library/LibraryDefaults.cs` — the default globs of §6.1 (plan 0007 edits this file) | NEW | A |
| 5 | `api/src/Orchestra.Api/Data/OrchestraDbContext.cs` — `DbSet<LibraryItemEntity> LibraryItems` | EDIT | A |
| 6 | `api/src/Orchestra.Api/Modules/Library/Indexing/{GlobMatcher,SafePath,PlanHeaderParser,IdeaHeaderParser,MarkdownTitle,LibraryIndexer,PlanRunLinker}.cs` | NEW | A |
| 7 | `api/src/Orchestra.Api/Modules/Import/ImportService.cs` — call `LibraryIndexer` inside the project transaction; `ImportLogEntity.LibraryItems` (column from migration `Library`, file 4) | EDIT | A |
| 8 | `api/src/Orchestra.Api/Modules/Import/ImportController.cs` — `ImportResultDto` and `ImportLogDto` get `libraryItems` | EDIT | A |
| 9 | `ui/src/features/imports/**` — a "Library items" column; message `imports.libraryItems` | EDIT | A |
| 10 | `api/src/Orchestra.Api/Modules/Library/Features/{ListLibrary,GetLibraryItem,ReadLibraryFile,PlanPrompt}/*` | NEW | B |
| 11 | `api/src/Orchestra.Api/Modules/Library/{LibraryController,LibraryModuleInstaller}.cs` (Order 60) | NEW | B |
| 12 | `api/src/Orchestra.Api/Modules/Runs/Features/GetRun/*` — `RunDetailDto.plan` (§6.3) | EDIT | B |
| 13 | `api/src/Orchestra.Api/Common/Errors/ErrorMessages.cs` — the codes of §6.4 | EDIT | B |
| 14 | `api/tests/Orchestra.Api.Tests/Library/**` + `Fixtures/library/**` | NEW | A–B |
| 15 | `ui/package.json` + `package-lock.json` — `react-markdown` 10.1.0, `remark-gfm` 4.0.1 (exact) | EDIT | C |
| 16 | `ui/src/components/app/markdown.tsx` — the shared `Markdown` component (D7) | NEW | C |
| 17 | `ui/src/features/library/**` (manifest, api, queries, types, i18n fa/en, components) | NEW | C |
| 18 | `ui/src/app/[locale]/library/page.tsx`, `ui/src/app/[locale]/library/view/page.tsx` | NEW | C |
| 19 | `ui/src/features/registry.ts`, `ui/src/lib/i18n/messages/common.{fa,en}.ts` (menu category `work`) | EDIT | C |
| 20 | `ui/src/features/runs/runs.types.ts` (`RunDetailDto.plan`), the runs feature's mock data if it has one, and `ui/src/features/runs/components/run-detail-page.tsx` — "Plan" link from `RunDetailDto.plan`; the `StageText` blocks render through `Markdown` (still collapsed by default) | EDIT | C |
| 21 | `.ai/architecture/data-model.md` — the `LibraryItem` table (now **nine** tables), its FK, the link rule (D5), `ImportLog.LibraryItems`; plan 0001's "eight tables" sentence in `OrchestraDbContext` follows the doc | EDIT | A |
| 22 | `.ai/ui/design.md` — the sandboxed preview frame, the gallery, the shared `Markdown` component | EDIT | C |
| 23 | `.ai/ci.md` — the test counts after each phase | EDIT | A–C |

## 6. Per-file specification

### 6.1 Configuration

Per project, new optional keys:

| Key | Default |
|---|---|
| `RootPath` | derived **only** when the last two segments of `RunsPath` are exactly `orchestrator/runs` or `.orchestra/runs` (either separator, `/` or `\`; ignore case on Windows): the parent of that `orchestrator` / `.orchestra` folder. Any other `RunsPath` (also `<root>/runs`) → not set |
| `Library:Plans` | `[".orchestra/plans/*.md", ".orchestra/plans/*/*.md"]` |
| `Library:Previews` | `[".orchestra/previews/*/index.html", ".orchestra/previews/*.html"]` |
| `Library:Ideas` | `[".orchestra/ideas/*.md"]` |
| `Library:Research` | `[".orchestra/research/**/*.md"]` |
| `Library:Evidence` | `[".orchestra/evidence/**/*"]` |

Globs are relative to `RootPath`, forward slashes, `*` = one segment, `**` = any depth; `README.md` and
`TEMPLATE.md` are never items. A configured list **replaces** the default. Example for today's layout
(in `appsettings.Local.example.json`, placeholder root):
`Plans: ["*/.ai/plans/*.md", "*/.ai/plans/done/*.md"]`, `Previews: ["*.html", "orchestrator/runs/_drafts/*.html"]`,
`Ideas: ["ideas.md"]`, `Research: ["research/**/*.md"]`, `Evidence: ["screenshots/**/*"]`.

The defaults live in `LibraryDefaults.cs` (file 4a); plan 0007 changes them there and nowhere else.

**`RootPath` not set, missing, not a folder, or a filesystem root** (`/`, `C:\`): one `ImportError` for that
project — "library root not set" when there is no path, else "library root not usable: <path>"; its runs import as usual; it gets no library items
(`LibraryItems = 0`). This is the security guard: plan 0001's Docker setup mounts only the runs folder
(`RunsPath=/projects/p1`), so nothing may be derived from it and the library is empty in Docker until
the owner sets `RootPath` and mounts that folder read-only (file 2).

### 6.2 Indexing

`LibraryItemEntity`: `Id`, `ProjectId` (**FK → `Project`, cascade delete**), `Category` (`Plan` ·
`Preview` · `Idea` · `Research` · `Evidence`), `RelPath` (forward slashes, ≤ 500), `Title` (≤ 300),
`Number` (`NNNN` or null), `Slug` (file name without extension and without a leading `NNNN-`), `Repo`
(D11), `Status` (plans: `Draft` · `Approved` · `Running` · `Done` · `Dropped` · `Unknown`; ideas: `raw` ·
`ready` · `planned` · `dropped`, or null; other categories: null), `CreatedOn`, `UpdatedOn`, `DoneOn`
(dates, null when absent), `PlannedBy` (≤ 120), `PreviewRelPath` (plans), `LinkedPlanNumber` (ideas),
`RunKey` (plans: D5), `Group` (evidence only: `NNNN-topic/phase-X` or the first folder), `SizeBytes`,
`ModifiedAt` (UTC). Unique index `(ProjectId, Category, RelPath)`. Because of the cascade, a project
removed from the configuration (plan 0001's `ProjectRegistry` deletes its `Project` row) loses its items.

**Walking the tree.** For each glob, `LibraryIndexer` takes its **fixed prefix** (the segments before
the first `*`) and enumerates only under `RootPath/<prefix>`; a glob that starts with `*` enumerates the
root's direct subfolders one level and continues from each. It never descends into `.git`,
`node_modules`, `bin`, `obj`, `.next`, `out` or `graphify-out`. It caps at 5 000 items per project
(more → one `ImportError` "library truncated").

**`SafePath.IsInside(root, fullPath)`** (D10): `Path.GetFullPath` on both; the file path must start
with the root + separator (ordinal, ignore case on Windows, case-sensitive elsewhere); then every
existing segment from below the root down to the file is checked with `FileSystemInfo` —
`Attributes.HasFlag(FileAttributes.ReparsePoint)` or a non-null `LinkTarget` → **not inside**. Used by
the indexer (such a file is skipped and logged once as an `ImportError` "link skipped") and by §6.4.

`PlanHeaderParser.Parse(text, relPath)` reads the title line and the first 25 lines only:

| Field | First match wins |
|---|---|
| Title | `^# Plan (\d{4})\s*[—-]\s*(.+)$` → number + title; `^# Plan\s*[—-]\s*(.+)$` → title; `^# (.+)$` → title |
| Number | the title's; else `^(\d{4})-` in the file name |
| Status | `^\*\*Status:\*\*\s*(\w+)` (first word, case-insensitive, mapped to the set; unknown word → `Unknown`); else `^\*\*done\*\*\s+(\d{4}-\d{2}-\d{2})` → `Done` with that date; else **legacy layout only**: `Done` when the path contains `/done/` (today's two projects; Orchestra's own plans have no `done/` folder — [`README.md`](README.md)); else `Unknown` |
| Created / Updated / Done | `\*\*Created:\*\*\s*(\d{4}-\d{2}-\d{2})`, `\*\*Updated:\*\*…`, `\*\*Done:\*\*…`, anywhere on a line (a `—` means null) |
| PlannedBy | `^\*\*(Planned by|Author):\*\*\s*([^,(]+)`, trimmed of a trailing `.` (`Claude Opus 5.5, 2026-11-19.` → `Claude Opus 5.5`) |
| Preview | `^\*\*Preview:\*\*\s*` followed by either a markdown link `[…](target)` or a bare path in backticks or plain (`` `../previews/0001-x/` `` or `../previews/0001-x/`); the target is resolved against the plan's folder; a folder means `<folder>/index.html` |

`PreviewRelPath` is set **only when** the resolved target equals the `RelPath` of an indexed `Preview`
item of the **same project** — so `previewUrl` can never point at a file the file endpoint refuses.

`IdeaHeaderParser.Parse(text, relPath)` reads the first 25 lines. The ideas' header is one line of fields
separated by ` · ` (`**Status:** raw · **Added:** 2026-11-19 · **Plan:** —`), so each field is matched
**anywhere on a line**:

| Field | Pattern |
|---|---|
| Title | the first `^# (.+)$`, else the file name |
| Status | `\*\*Status:\*\*\s*(\w+)` → one of `raw` · `ready` · `planned` · `dropped` (case-insensitive); any other word, or none → null |
| CreatedOn | `\*\*Added:\*\*\s*(\d{4}-\d{2}-\d{2})`, else `\*\*Created:\*\*…`; else null |
| LinkedPlanNumber | `\*\*Plan:\*\*\s*\D*?(\d{4})` (a `—` → null) |

A single `ideas.md` (today's layout) is one item with these rules. Research and previews take the first
`# ` heading or `<title>` as title, else the file name. A missing created date → null (the UI shows the
file's modified date, marked as such).

`PlanRunLinker` runs after the runs of the same transaction are built and applies D5. Evidence links to
a plan when its `Group` starts with the plan's `Number` + `-`, or, for a plan with no number, with its
`Slug`.

### 6.3 API

`{category}` in a path is `plan` · `preview` · `idea` · `research` · `evidence` (case-insensitive;
other → 404 `Library.Item.NotFound`). Every lookup is by the unique key `(project, category, path)`.

| Endpoint | Returns |
|---|---|
| `GET api/library?project=&category=&status=&sort=number\|title\|project\|status\|created\|updated\|done\|run&dir=asc\|desc` | `LibraryItemDto[]` (all fields of §6.2 except `SizeBytes`; plus `projectName`, `runStatus`, `linkedPlanTitle` for ideas — the title of the plan with that number **in the same project**; if several repos have it, the lowest `RelPath` (ordinal)). No `project` → every project. `status` applies only to plans and ideas (ignored elsewhere). Default sort: `updated desc`. **Nulls last in both directions.** `sort=run`, and every "newest first" list of runs, orders `RunKey` by task id and then `n` **as a number** (D5). The ideas' linked-plan column is not a sort key. Text sorts use SQLite's binary order (Persian titles sort by code point — accepted). Unknown `project` → 404 `Projects.Project.NotFound` |
| `GET api/library/{project}/{category}/item?path=` | `LibraryItemDetailDto` = the item + `previewUrl` (when `PreviewRelPath` is set) + `runs: { runKey, status, updatedAt }[]` (every D5 match, newest first) + `evidence: LibraryItemDto[]` (§6.2 rule). Unknown key → 404 `Library.Item.NotFound` |
| `GET api/library/{project}/{category}/file?path=` | the file's bytes (§6.4) |
| `GET api/library/{project}/plan/prompt?path=` | `{ text }` (§6.5). Unknown path → 404 `Library.Item.NotFound`. Only the `plan` category has this route, so `Library.Prompt.NotAPlan` is dropped |
| `GET api/runs/{project}/{runKey}` (plan 0001, EDIT) | `RunDetailDto.plan: { relPath, number, title, status } \| null` — the plan whose `RunKey` is this run; else the plans this run matches by D5, choosing a number match over a slug match and then the lowest `RelPath` (ordinal), so the answer is always the same |

### 6.4 The file endpoint

1. `(category, path)` must equal an **indexed** item of that project. Nothing else is servable — not
   even a file next to it. Otherwise 404 `Library.Item.NotFound`.
2. `SafePath.IsInside(RootPath, fullPath)` (§6.2) must hold **now**, at request time; otherwise 404
   (same code). This catches a file replaced by a link after indexing.
3. Size cap 20 MB → 413 `Library.File.TooLarge`.
4. Headers by type, always with `X-Content-Type-Options: nosniff`:
   - `.html` → `text/html; charset=utf-8` and exactly
     `Content-Security-Policy: sandbox allow-scripts; default-src 'none'; script-src 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://unpkg.com; style-src 'unsafe-inline' https://fonts.googleapis.com; font-src https://fonts.gstatic.com; img-src data: blob:; media-src data: blob:; connect-src 'none'; form-action 'none'; base-uri 'none'; frame-src 'none'`
   - `.md` → `text/markdown; charset=utf-8` and `Content-Security-Policy: default-src 'none'`;
   - images by extension, and `Content-Security-Policy: default-src 'none'`;
   - anything else → `application/octet-stream` with `Content-Disposition: attachment`.
5. Opened read-only with `FileShare.ReadWrite | FileShare.Delete`.

Why `connect-src 'none'` and `form-action 'none'`: the sandbox isolates the DOM and storage, **not the
network**; without them a preview could send a `POST` to the unauthenticated local API (CORS hides the
answer, not the request).

**One file per preview.** Only indexed items are served, and a relative link inside a preview
(`style.css`, `shot.png`) resolves against the file endpoint's URL, not the preview's folder. So a
preview works only when everything it needs is inline, a `data:` URL, or from the allowed hosts. The
approved preview 0001 qualifies (it only loads Google Fonts). A multi-file preview shows broken parts —
accepted, like D6.

Error texts (added to plan 0001's §6.5 table):

| Code | Status | en | fa |
|---|---|---|---|
| `Library.Item.NotFound` | 404 | This item is not in the library. | این مورد در کتابخانه نیست. |
| `Library.File.TooLarge` | 413 | The file is too large to show. | فایل برای نمایش خیلی بزرگ است. |

### 6.5 The prompt template

The approved mock's text (D8):

```text
Run plan {relPath}
through the orchestrator (repo: {repo|projectName}).
Status: {status}. Preview: {previewFolder|none}
Read the plan header first; decide nothing new.
```

Placeholders are filled server-side; a null becomes the value after `|`. `previewFolder` is
`PreviewRelPath` with a trailing `index.html` removed (`.orchestra/previews/0001-x/`), else the file
path as it is.

### 6.6 UI

`features/library` — route `/{locale}/library/` (tabs by category, from `?tab=`; filters project and —
for plans and ideas only — status; a sort select with the sort keys of §6.3; a table for plans, ideas,
research and previews, a gallery for evidence) and `/{locale}/library/view/?project=&category=&path=`
(header fields; for plans a **Copy prompt** button with the tooltip `library.copyPromptHint`, the runs
list, the markdown body, the preview in a sandboxed iframe at 70vh with a short note and an "open in a
new tab" link to the file endpoint, the plan's evidence gallery; for ideas the linked plan). Status and
the status filter are hidden for previews, research and evidence.

Evidence gallery: images as `<img loading="lazy">` from the file endpoint; a `report.md` as a card (an
icon, `library.evidenceReport` and its group). Click → a dialog: an image at full size, a report as
markdown fetched from the file endpoint. Copy uses `navigator.clipboard.writeText` and falls back to
selecting the text.

Run detail (file 20): the plan link (`runs.detail.plan`) in the header when `plan` is set; the
`StageText` blocks of plan 0001 D23 render through `Markdown`, still collapsed by default.

Messages (copy exactly):

| Key | en | fa |
|---|---|---|
| `common.menu.work` | Work | کار |
| `imports.libraryItems` | Library items | موارد کتابخانه |
| `library.title` | Library | کتابخانه |
| `library.tabs.plans` | Plans | پلن‌ها |
| `library.tabs.previews` | Previews | پیش‌نمایش‌ها |
| `library.tabs.ideas` | Ideas | ایده‌ها |
| `library.tabs.research` | Research | تحقیق‌ها |
| `library.tabs.evidence` | Evidence | شواهد |
| `library.columns.number` | No. | شماره |
| `library.columns.title` | Title | عنوان |
| `library.columns.project` | Project | پروژه |
| `library.columns.status` | Status | وضعیت |
| `library.columns.created` | Created | ساخت |
| `library.columns.updated` | Updated | به‌روز |
| `library.columns.done` | Done | پایان |
| `library.columns.run` | Run | run |
| `library.columns.linkedPlan` | Plan | پلن |
| `library.sort.label` | Sort by | مرتب‌سازی |
| `library.status.Draft` | Draft | پیش‌نویس |
| `library.status.Approved` | Approved | تأییدشده |
| `library.status.Running` | Running | در run |
| `library.status.Done` | Done | انجام‌شده |
| `library.status.Dropped` | Dropped | کنار گذاشته |
| `library.status.Unknown` | Unknown | نامشخص |
| `library.ideaStatus.raw` | Raw | خام |
| `library.ideaStatus.ready` | Ready | آماده |
| `library.ideaStatus.planned` | Planned | پلن شده |
| `library.ideaStatus.dropped` | Dropped | کنار گذاشته |
| `library.modifiedHint` | file date | تاریخ فایل |
| `library.runs` | Runs of this plan | runهای این پلن |
| `library.copyPrompt` | Copy prompt | کپی پرامپت |
| `library.copyPromptHint` | A ready prompt for the orchestrator's conductor | پرامپت آماده برای conductor ارکستر |
| `library.copied` | Prompt copied | پرامپت کپی شد |
| `library.copyFailed` | Could not copy; the text is selected — press Ctrl+C. | کپی نشد. متن انتخاب شده است، Ctrl+C را بزنید. |
| `library.openInTab` | Open in a new tab | باز کردن در تب جدید |
| `library.previewSandboxed` | The preview runs in a sandbox with no network access: it cannot send anything to the dashboard or the internet. Only Google Fonts and scripts from cdnjs, jsDelivr and unpkg load. | پیش‌نمایش در یک قاب جدا و بدون دسترسی شبکه اجرا می‌شود و نمی‌تواند چیزی به داشبورد یا اینترنت بفرستد. فقط فونت‌های Google و اسکریپت‌های cdnjs، jsDelivr و unpkg بارگذاری می‌شوند. |
| `library.empty` | Nothing in this category. Check the project's Library paths in appsettings.Local.json and re-import. | در این دسته چیزی نیست. مسیرهای Library پروژه را در appsettings.Local.json بررسی کنید و دوباره بخوانید. |
| `library.evidenceFor` | Evidence | شواهد |
| `library.evidenceReport` | Report | گزارش |
| `runs.detail.plan` | Plan | پلن |

`library.evidenceFor` is the heading of the evidence section on a plan's page (the same word as the
tab, a different place). Status badges: `Done` → success, `Approved`/`Running` → info, `Draft` →
warning, `Dropped`/`Unknown` → secondary; ideas: `planned` → success, `ready` → info, `raw` → warning,
`dropped` → secondary; an idea with no status shows no badge.

## 7. Tests

| File | Facts |
|---|---|
| `Library/GlobMatcherTests.cs` | `*` one segment, `**` any depth, `*/.ai/plans/*.md` matches `Web/.ai/plans/0001-x.md` and not `Web/.ai/plans/done/0001-x.md`; README/TEMPLATE excluded; the fixed prefix of each default glob; D11 repo for `*/.ai/plans/*.md`, `.orchestra/plans/*/*.md`, and none for `.orchestra/plans/*.md` |
| `Library/SafePathTests.cs` | inside / outside by prefix; `..` outside; case on Windows; a junction or symlink segment below the root → not inside (skipped with the reason where links cannot be created) |
| `Library/PlanHeaderParserTests.cs` | the three header styles (Orchestra template; `**done** 2026-11-18 — …` + `**Author:**`; `# Plan — title` + `**Status:** Done — …` with a slug file name); number from title vs file name; `—` dates are null; preview as a markdown link, in backticks, and bare; a folder target becomes `index.html` |
| `Library/IdeaHeaderParserTests.cs` | the one-line header `**Status:** raw · **Added:** 2026-11-19 · **Plan:** —` → raw, 2026-11-19, no plan; `**Plan:** [0002](…)` → `0002`; an unknown status word → null; `**Created:**` used when `**Added:**` is absent |
| `Library/LibraryIndexerTests.cs` | default globs index the fixture `.orchestra/`; configured globs replace them; `node_modules` and `.git` are never entered (a file there with a matching name is not indexed); a preview link to an **unindexed** file leaves `PreviewRelPath` null; a link into another project's folder is null; `Repo` set for plans and null for a preview under `previews/*/index.html`; evidence groups and the slug link; truncation at the cap logs one error; a link segment → skipped and logged; a missing `RootPath` → one error, zero items, the runs still imported; `RunsPath=/projects/p1` (Docker shape) → no derived root, one error, **nothing under `/` enumerated**; `RunsPath=…/orchestrator/runs` and `…\orchestrator\runs` → the root two levels up; `RunsPath=<root>/runs` → "library root not set", not `<root>`'s parent; `.orchestra/plans/done/0003-x.md` → `Repo` null and still links by number; evidence `screenshots/shot.png` → `Group` null, `screenshots/a/shot.png` → `a`; a `RootPath` of `/` or `C:\` → one error; evidence `notes.txt` and `dump.zip` → not indexed; `PlannedBy` `Claude Opus 5.5, 2026-11-19.` → `Claude Opus 5.5` |
| `Library/PlanRunLinkTests.cs` (runs built with **real-shaped** branches) | branch `plan-0007-demo` + `Run.PlanNumber` 0007 + repo `Web` → `Web/.ai/plans/0007-demo.md`, **not** `Api/.ai/plans/0007-other.md`; branch `feature/plan-demo-slug` → the unnumbered `demo-slug.md`; a plain branch `demo-slug` also links; three runs of one task id → `RunKey` = the newest and the detail lists all three; `.10` is newer than `.9`; an idea's linked plan resolves in its own project only; no link when nothing matches; `RunDetailDto.plan` is the same on repeated calls when two plans match |
| `Library/ReadLibraryFileTests.cs` | an unindexed path → 404; `../` → 404; a known path under the wrong category → 404; the same path indexed under two categories is served by each; an indexed file replaced by a link to outside the root after indexing → 404; html carries **exactly** the §6.4 CSP and nosniff; md and images carry `default-src 'none'`; other types are attachments; oversized → 413 |
| `Library/PlanPromptTests.cs` | the mock's template filled; a preview folder shown with its trailing `/`; null repo → the project name; null preview → `none`; an unknown path → 404 |
| `Library/ListLibraryTests.cs` | no project → all projects; every sort key; nulls last in both directions; `status` ignored for research |
| `Import/ImportServiceLibraryTests.cs` | runs and library rebuild in one transaction; a deleted idea disappears; import twice → identical items; `ImportLog.LibraryItems` counted; removing a project from the configuration removes its items (cascade) |
| `ui: features/library/**/*.test.tsx` | tabs switch from the URL; sort and status filter (status hidden for research); copy prompt success and fallback; the iframe carries `sandbox="allow-scripts"` and no `allow-same-origin`; markdown with a `<script>` renders no script element; evidence image dialog and report dialog; idea status badge and linked plan; the detail URL carries `category` |
| `ui: features/runs/**` (EDIT) | the plan link renders when `plan` is set, and not otherwise; a `StageText` with a table and a `<script>` renders the table and no script element |

Fixtures: `Fixtures/library/`:
- **A synthetic project with `.orchestra/`:**
  - two plans: one links to an indexed preview, the other to a missing file;
  - one preview;
  - two ideas in the real one-line header style, one of them with `**Plan:**`;
  - one research note;
  - evidence `0001-demo/phase-A/shot.png` and `report.md`;
  - a trap file `node_modules/x/0099-trap.md`.
- **A synthetic "today's layout":** `Web/.ai/plans/0007-demo.md`, `Api/.ai/plans/0007-other.md`,
  `Web/.ai/plans/done/demo-slug.md`, `ideas.md` and `preview.html`.
- **Runs:** built in the test with repos `Web`/`Api` and the branches named above.

## 8. Stop and ask

- A real plan or idea header the owner checks against matches none of §6.2's patterns.
- A real run branch of an unnumbered plan matches neither form of D5(b).
- `react-markdown` 10.1.0 or `remark-gfm` 4.0.1 does not install against the React version plan 0001
  pinned.

## 9. Phases

Gates as plan 0001 §9; after each phase, the counts go into `.ai/ci.md`.

- **A — config and indexing.** Files 1–9 (with 4a), 14 (indexer, parser, safe-path and link tests), 21, 23.
  **Gate.** **Owner checks:** point both projects' `Library` at today's layout, re-import: the import
  log page shows a library-items count for each project and no errors. Stop.
- **B — API.** Files 10–13, 14 (endpoint tests). **Gate.** **Owner checks:**
  `GET api/library?project=project-b&category=Plan` lists plan 0032 as Done with `runKey` set, and
  `GET api/runs/project-b/<its run key>` has `plan` filled. Stop.
- **C — screens.** Files 15–20, 22. **Gate.** **Owner checks:** the items of «نتیجه». Stop.

## 10. Definition of done

All gates green and counts recorded; both projects index with zero errors; the file endpoint's tests
pass; session log written; this header `Status: Done` with the date, commit and merge.

## 11. Plan reviews — where each point went

### Round 1

| Point | Fixed in |
|---|---|
| B1 symlinks not caught by `GetFullPath` | D10, §6.2 `SafePath`, §6.4 rule 2, `SafePathTests`, `ReadLibraryFileTests` |
| B2 sandbox does not block the network | D6, §6.4 rule 4 and its note, `library.previewSandboxed` (en + fa rewritten), «فهم» and «نتیجه» |
| B3 `PreviewRelPath` to an unindexed file; bare-path header | §6.2 Preview row and the "only when" rule, parser and indexer tests |
| B4 no API for the run → plan link | §6.3 `RunDetailDto.plan`, file 12, link tests |
| B5 owner check A not visible | files 7–9, `imports.libraryItems`, phase A owner check |
| B6 sort scope | §6.3 sort keys, `ListLibraryTests`, «جریان کار» |
| Index walks `node_modules` | §6.2 *Walking the tree*, indexer test |
| `Repo` vs plan 0007 | D11, default Plans glob |
| one plan, several runs | D5, §6.3 `runs`, link test |
| idea `**Plan:**` unused, no idea status keys | §6.2 `IdeaHeaderParser`, `library.ideaStatus.*`, `library.columns.linkedPlan` |
| `library.empty` points at settings | its text names `appsettings.Local.json` |
| button label vs mock | D8, `library.copyPrompt` = «کپی پرامپت», hint as tooltip |
| unspecified parameters | §6.3 (no project, status scope, prompt 404) |
| `Done` from `/done/` vs no `done/` | §6.2 Status row: legacy only |
| manifest: `ci.md`, exact versions, lock file | files 15 and 23, D7 |
| minor: nulls last, slug evidence, `evidenceFor`, missing tests | §6.3, §6.2 last paragraph, §6.6 note, §7 |

### Round 2

| Point | Fixed in |
|---|---|
| B1 UI deviates from the mock | owner decided: D12 records the screen deviations; the prompt is the mock's text again (D8, §6.5, `PlanPromptTests`) |
| B2 plan 0001 D23 expects the markdown renderer | D7, file 16 (shared `Markdown`), file 20, §6.6 run detail, runs UI test, «جریان کار» 4, «نتیجه» |
| B3 D5 fails on real data (repo, `plan-NNNN-` branches) | D5 (a) with repo, (b) with the prefix strip; §6.3 deterministic `RunDetailDto.plan`; `PlanRunLinkTests` with real-shaped branches; §8 new stop |
| B4 `OrchestraDbContext` missing | file 5; file 21 says nine tables (corrected after plan 0001's review) |
| `report.md` has no display | §6.6 evidence gallery and dialog, `library.evidenceReport`, UI test |
| ideas use `**Added:**` | §6.2 `IdeaHeaderParser`, `IdeaHeaderParserTests`, fixtures |
| lookup key ≠ unique key | §6.3 `{category}` in every route, §6.4 rule 1, the detail URL, `ReadLibraryFileTests`; `NotAPlan` dropped |
| missing `RootPath` | §6.1 last paragraph, indexer test |
| FK and cascade | §6.2 `ProjectId`, `ImportServiceLibraryTests` |
| D11 for other categories | D11: plans only; indexer test |
| minor: linked-plan sort, idea status set, English import errors | §6.3 (not a sort key; binary collation noted), §6.2 idea table, §2 out of scope |

### Round 3

| Point | Fixed in |
|---|---|
| B1 derived `RootPath` can become `/` | §6.1 `RootPath` row (derive only from `<x>/runs`) and the guard paragraph (also rejects a filesystem root), file 2 (Docker env + mount), indexer tests |
| B2 `markdown.tsx` outside plan 0001's component folders | file 16 → `components/app/`, D7 |
| UI type for `RunDetailDto.plan` | file 20 names `runs.types.ts` and the mock data |
| D9 vs evidence glob | D9: other extensions skipped; indexer test |
| `linkedPlanTitle` scope | §6.3: same project, lowest `RelPath`; link test |
| multi-file previews | §6.4 "One file per preview" |
| migration names | file 4: one migration `Library` holds both changes |
| minor: `PlannedBy` regex, numbered plan without `PlanNumber`, numeric `n`, evidence root, 0007 defaults, duplicate D12 note | §6.2 PlannedBy row; D5 (no such case: `PlanName.From` reads the branch); D5; D9; file 4a + §6.1; header `Preview:` line trimmed |

### Round 4

| Point | Fixed in |
|---|---|
| B1 `<root>/runs` still derives a wide root | §6.1 `RootPath` row: only `orchestrator/runs` or `.orchestra/runs`; indexer test |
| «نتیجه» expects Orchestra's own plan, but Orchestra is not a project yet | that bullet removed; note under «نتیجه» (after plan 0007) |
| reviewer's model family not named | header: a line for the owner to fill |
| `done` folder becomes `Repo` | D11; indexer test |
| evidence file directly under the prefix | D9; indexer test |
| minor: "not set" error text, numeric `RunKey` sort, separator-neutral check, 0007 file 1 | §6.1; §6.3; §6.1; plan 0007 file 1 → `EDIT` |
