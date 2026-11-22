# Plan 0001 — Skeleton, fa/en, and phase-0 import

**Status:** Done — phases A–E implemented and reviewed (2026-11-19); committed on `alireza/skeleton-and-import` ("Plan 0001: skeleton, fa/en and phase-0 import"), not merged into `main` yet
**Created:** 2026-11-19 · **Updated:** 2026-11-19 · **Done:** 2026-11-19
**Preview:** [`../previews/0001-skeleton-and-import/`](../previews/0001-skeleton-and-import/index.html) — approved by the owner 2026-11-19 (v3)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** DeepSeek (owner, 2026-11-19) — a different model family than the planner's.
- Round 1, 2026-11-19 (relayed by the owner): changes requested. 3 blockers, 5 important and 7 minor
  points, all applied (the UI versions in round 3); §11 lists where.
- Round 2, 2026-11-19 (relayed by the owner): changes requested. 1 blocker (UI versions, still open)
  and 2 minor points (applied).
- Round 3, 2026-11-19 (relayed by the owner): changes requested. 2 blockers (UI versions; stale `Base`)
  and 3 minor points. All applied (versions pinned from Project B's lock file).
- Round 4, 2026-11-19 (relayed by the owner): changes requested. 1 blocker (`cn` had no source) and 3
  minor points. Applied: `cn` 0.4.0 kept, as Project B uses it.
**Decided by:** Alireza, 2026-11-19, in the preview. Every choice below is already decided — **decide
nothing new.** If reality contradicts this plan, STOP and ask.
**Branch:** `alireza/skeleton-and-import` — the implementer creates it.
**Base:** `alireza/orchestra-layout` @ `7f49400` (the commit with all plan-review fixes and the `data-model.md` changes)
**Companion:** None
**Steps changed:** None

---

## فهم من از درخواست

Orchestra ابزاری است که بعداً public می‌شود و برای هر پروژه‌ای که ارکستر دارد، runها، هزینه و کیفیت
مدل‌ها را یک‌جا نشان می‌دهد. به دو زبان فارسی و انگلیسی، با تم روشن و تاریک، روی `localhost:3500`.

این پلن **قدم اول** است:
- اسکلت repo را می‌سازد (API، UI و Docker).
- فایل‌هایی را که دو ارکستر فعلی (Project A و Project B) همین امروز می‌نویسند **فقط می‌خواند**، در همان
  ساختار امروزشان (`orchestrator/runs`):
  - `_ledger.md`،
  - `task.json` runهای باز،
  - و بایگانی `close` در `runs/_archive/` (تصمیم تازه‌ی شما، ۲۰۲۶-۱۱-۱۹).
- متن مرورها، read-backها و verify هر run بایگانی‌شده را در صفحه‌ی جزئیات نشان می‌دهد.
- چهار صفحه نشان می‌دهد: runها به تفکیک پروژه (با تعداد session و call)، جزئیات یک run، هزینه، و کیفیت
  مدل‌ها. یک صفحه‌ی «گزارش خواندن» هم دارد.

این پلن کارهای زیر را **انجام نمی‌دهد**:
- کتابخانه (پلن‌ها، پیش‌نمایش‌ها، ایده‌ها، تحقیق‌ها، شواهد): پلن 0002.
- ساخت پروژه از داشبورد و پرامپت ساخت `.ai/` و `.orchestra/`: پلن 0003.
- پروفایل‌ها، انتقال موتور، و انتقال دو پروژه به ساختار تازه: فاز ۲.
- گزارش زنده: فاز ۱.
- هیچ تغییری در دو ارکستر و هیچ نوشتنی در پوشه‌ی هیچ پروژه‌ای.

برداشت من از دو نکته:
- بایگانی نتیجه‌ی هر دور (PASS یا FINDINGS) و متن آن را دارد، ولی **تعداد blockerهای هر دور** را ندارد. ماکاپ پیش‌نمایش «میانگین blocker در هر مرور» را نشان می‌داد. این پلن به جایش «تعداد دور تا PASS»، «سهم دورهای FINDINGS» (برای runهای باز و بایگانی‌شده) و تعداد پایانی را نشان می‌دهد.
- فایل بایگانی فقط با شماره‌ی تسک نام‌گذاری شده است. پس runی که شماره‌ی تسکش دوباره استفاده شود، بایگانی run قبلی را رونویسی می‌کند. runهای قدیمی‌تر فقط بلوک ledger دارند.
- هزینه‌ی conductor و hopهای بدون session (codebuddy، manual) در ledger نیست. پس در داشبورد هم نمی‌آید.

## جریان کار

1. شما در فایل `appsettings.Local.json` (که در git نمی‌رود) برای هر پروژه یک شناسه، یک اسم و مسیر پوشه‌ی
   `runs` را می‌نویسید.
2. داشبورد را اجرا می‌کنید (`dotnet run` یا Docker) و `http://localhost:3500` را باز می‌کنید. صفحه با
   زبانی که قبلاً انتخاب کرده‌اید باز می‌شود. اگر انتخابی نکرده باشید، با زبان مرورگر، و اگر آن هم فارسی
   یا انگلیسی نبود، با انگلیسی.
3. داشبورد موقع شروع، و هر وقت دکمه‌ی «خواندن دوباره» را بزنید، برای هر پروژه ledger، runهای باز و
   بایگانی را می‌خواند و داده‌های آن پروژه را از نو می‌سازد. هیچ فایلی را تغییر نمی‌دهد. خواندن دوباره چیزی را دو بار
   حساب نمی‌کند، و runی که فایلش پاک شده از داشبورد هم حذف می‌شود.
4. این صفحه‌ها را می‌بینید:
   - **runها:** با فیلتر پروژه، وضعیت و repo. هر run با شماره و عنوان پلنش نشان داده می‌شود و شماره‌ی تسک کنارش می‌آید.
   - **جزئیات run:** نتیجه‌ی مرور، read-back، فازها و verify، و مصرف هر session و هر مرحله.
   - **هزینه:** به تفکیک روز، ماه، مدل و پروژه.
   - **کیفیت مدل‌ها:** مرورکننده، read-back، پیاده‌ساز، زمان هر مرحله، و نرخ cache.
   - **گزارش خواندن:** آخرین خواندن هر پروژه، و هر فایل یا بلوکی که خوانده نشد با دلیلش.
5. اگر فایلی یا بلوکی خراب باشد، فقط همان رد می‌شود و در «گزارش خواندن» ثبت می‌شود. اگر مسیر یک پروژه
   وجود نداشته باشد، همان پروژه خطا می‌گیرد و بقیه خوانده می‌شوند.

فازها:
- **A:** اسکلت: API، UI با هر دو زبان و هر دو تم، پوسته‌ی خالی، و قواعد کد در `.ai/`.
- **B:** مدل داده و خواندن فایل‌ها.
- **C:** صفحه‌های runها، جزئیات run و گزارش خواندن.
- **D:** صفحه‌های هزینه و کیفیت مدل‌ها.
- **E:** Docker و راهنمای اجرا.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

یک داشبورد محلی دوزبانه دارید که همه‌ی runهای هر دو پروژه را با هزینه و کیفیت مدل‌ها نشان می‌دهد. در
کار روزمره‌ی ارکستر هیچ تغییری ایجاد نمی‌شود.

- `http://localhost:3500` را باز کنید. با زبان مرورگر باز می‌شود. زبان را عوض کنید، صفحه را ببندید و
  دوباره باز کنید: زبان انتخابی می‌ماند.
- در صفحه‌ی runها، هر دو پروژه را می‌بینید. فیلتر را روی Project A بگذارید: `T-WEB-001` سه run جدا است
  (سه شماره‌ی `.1`، `.2` و `.3`)، نه یکی.
- یک run بسته‌شده را باز کنید، مثلاً Project B `T-AGENT-030`. مرور، read-back، چهار فاز، verify و
  هزینه‌ی کل ($0.387) با آخرین بلوک آن در ledger یکی است.
- در صفحه‌ی هزینه، جمع ماه با جمع ردیف‌های **total** آخرین بلوک هر run می‌خواند.
- در صفحه‌ی کیفیت، glm و deepseek به‌عنوان مرورکننده کنار هم آمده‌اند.
- دو بار پشت‌سرهم «خواندن دوباره» را بزنید. تعدادها تغییر نمی‌کند.
- یک run بایگانی‌شده را باز کنید، مثلاً Project A `T-WEB-001.3`. متن مرورهای دور ۱ و ۲، read-back و verify در جزئیات آن هست. `T-WEB-001.1` و `.2` متن ندارند و پیام توضیحی نشان می‌دهند.
- در یک **کپی** از ledger یک خط جدول را خراب کنید، `RunsPath` یک پروژه‌ی آزمایشی را به آن کپی بدهید، و
  دوباره بخوانید. فقط همان بلوک در «گزارش خواندن» خطا دارد.
- هر صفحه را در فارسی و انگلیسی، در تم روشن و تاریک، و در عرض موبایل چک کنید. در فارسی تاریخ‌ها جلالی‌اند
  و رقم‌ها لاتین.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md): branch before the first edit (from the base above);
   never commit/push/merge unless asked in that message; one phase = one uncommitted diff; stop after
   each phase.
2. **Read-only on every project folder.** The importer opens files with `FileAccess.Read` and
   `FileShare.ReadWrite | FileShare.Delete` (the orchestrator may be writing). Nothing under
   `Project ProjectA/` or `Project ProjectB/` is edited by this plan.
3. **No real run data in the repo.** Test fixtures are the synthetic files in §6.7, written as given
   here. Never copy a real ledger or `task.json` into the repo, not even "to check".
4. Project B's `Web/.ai/` and `Ui/.ai/` are the style reference — read them, copy patterns, never edit
   them. Where this plan differs from them, this plan wins.
5. Next.js 16: read `ui/node_modules/next/dist/docs/` before using an API not used in Project B's Ui
   (static export, `generateStaticParams`). next-intl: v4 docs on static export without a proxy.
6. Every Persian string is in §6.12 — **copy it, never retype or "improve" it** (AGENTS.md rule 6).

### Baseline

The repo has no code: there is no baseline to keep green. From phase A on, every phase's gate is the
baseline of the next, and the test count never drops.

## 1. Goal

- `api/` (.NET 10, EF Core, SQLite) and `ui/` (Next.js 16 static export, fa + en, light + dark) build,
  test and run; the API serves the UI on port 3500.
- The importer reads each configured project's `_ledger.md`, open `task.json` and `_archive/*.json` files into the tables
  of [`../../.ai/architecture/data-model.md`](../../.ai/architecture/data-model.md), by rebuilding the
  project's rows, and logs every unreadable file or block.
- Five screens: runs (per project), run detail, cost, model quality, import log.
- `deploy/Dockerfile` + `docker-compose.yml` run it with the project folders mounted read-only.
- `.ai/api/`, `.ai/ui/` and `.ai/ci.md` exist, trimmed from Project B's.

## 2. Out of scope

- Library, plan tracking, "copy prompt" (plan 0002). New-project form and scaffold prompt (plan 0003).
- Live events, the runner reporter (phase 1). Profiles, the engine move, the move of Project A and
  Project B to `.ai/` + `.orchestra/` (phase 2). Sign-in, HTTPS, server deploy (phase 3).
- Any change to any orchestrator. Any write into a project folder.
- Re-pricing old runs, conductor cost, transcripts, `progress.json`.
- An OpenAPI/Swagger page, Serilog, MediatR, axios, a chart library.

## 3. Rules that apply

| Doc | Why here |
|---|---|
| [`../workflow.md`](../workflow.md) | the gates |
| [`../../.ai/architecture/overview.md`](../../.ai/architecture/overview.md) | pieces, data flow, read-only mounts |
| [`../../.ai/architecture/data-model.md`](../../.ai/architecture/data-model.md) | **sources, run identity, rebuild, tables — the importer implements it exactly** |
| ADRs [0001](../../.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md)–[0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) | stack, one-way flow, read-only, fa + en, files are the truth |
| Project B `Web/.ai/coding/conventions.md`, `naming.md`, `testing.md`, `ci.md` | API style, trimmed in §6.15 |
| Project B `Ui/.ai/architecture/vertical-slice.md`, `i18n.md`, `app-shell.md`, `design/design.md`, `coding/conventions.md`, `naming.md`, `testing.md` | UI style, trimmed in §6.15 |

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | One repo: `api/`, `ui/`, `deploy/` (ADR 0001) |
| D2 | SQLite through EF Core; the file is `data/orchestra.db` (git-ignored), path from `Orchestra:DatabasePath` |
| D3 | The database is a read model; **an import rebuilds a project's rows in one transaction** (data-model) |
| D4 | A bad file or block is logged and skipped, never fatal; a missing `RunsPath` fails only that project |
| D5 | Run identity, merge and RunKey exactly as data-model § *How a run is identified*; `task.json` wins where both speak |
| D6 | Projects come from configuration (`Orchestra:Projects`), from a git-ignored `appsettings.Local.json`; no UI to edit them |
| D7 | UI: shadcn/ui from the official registry, Tailwind v4, TanStack Query, vertical slices, tokens only — as Project B's Ui |
| D8 | No sign-in; Kestrel binds `http://localhost:3500`; in Docker the container listens on `:3500` and compose publishes `127.0.0.1:3500` only |
| D9 | Costs as recorded in the ledger; nothing is re-priced. A missing cost column is `null` ("—"), never 0 |
| D10 | Names: solution `Orchestra`, project `Orchestra.Api`, namespace root `Orchestra.Api`, npm package `orchestra-ui`, image `orchestra` |
| D11 | Port 3500 (API + UI). `next dev` runs on **3501** and calls the API at `http://localhost:3500`; the API allows CORS from `http://localhost:3501` **in Development only**. (The preview said `/api` is forwarded; a static export cannot use Next rewrites, so CORS replaces it — same result for the owner.) |
| D12 | Themes light, dark, system (default system), next-themes, toggle in the header |
| D13 | The importer reads `_ledger.md`, open `task.json` **and** `_archive/*.json` (changed 2026-11-19: both orchestrators' `close` now writes the archive). Archive usage is ignored — the ledger's has the cost |
| D14 | Public-ready: no project path, no real run data and no secret in committed files |
| D15 | fa + en with next-intl locale routing in a static export (ADR 0004); `/` is `public/index.html` |
| D16 | Dates: fa = Persian calendar, Latin digits (`fa-u-nu-latn`); en = `en`. The **browser's** time zone (no `timeZone` option is passed) |
| D17 | The ledger and `task.json` carry no UTC offset: they are read in `Orchestra:SourceTimeZone` (IANA id; default: the machine's local zone) and stored as UTC `DateTime` |
| D18 | Money is stored as `long` micro-dollars (`CostMicros`, exact sums in SQLite); DTOs carry `decimal` USD |
| D19 | No MediatR (12.x is the last free line; not worth a pinned dependency for a read API): one handler class per query, registered by its module. No FluentValidation: query parameters are checked by hand |
| D20 | Test packages as Project B, **pinned**: xunit 2.9.3, FluentAssertions **7.0.0** (8.x has a commercial licence) |
| D21 | A run's dynamic page uses a query string (`/fa/runs/view/?project=…&run=…`) — a static export cannot pre-render unknown run keys |
| D22 | Reviewer quality = rounds needed + final counts (all runs) + FINDINGS share per round (open and archived runs); no "blockers per round" (no source has per-round counts) |
| D23 | Archived texts are stored in `StageText` and shown in the run detail as plain preformatted text (plan 0002 brings the markdown renderer), each collapsed by default |

## 5. File manifest

All NEW — the repo has no code. Paths from the repo root.

| # | File | Phase |
|---|---|---|
| 1 | `.gitignore` (EDIT — 2 lines) | A |
| 2 | `api/global.json` | A |
| 3 | `api/Directory.Build.props` | A |
| 4 | `api/Directory.Packages.props` | A |
| 5 | `api/Orchestra.slnx` | A |
| 6 | `api/src/Orchestra.Api/Orchestra.Api.csproj` | A |
| 7 | `api/src/Orchestra.Api/Program.cs` | A |
| 8 | `api/src/Orchestra.Api/appsettings.json` | A |
| 9 | `api/src/Orchestra.Api/appsettings.Development.json` | A |
| 10 | `api/src/Orchestra.Api/appsettings.Local.example.json` | A |
| 11 | `api/src/Orchestra.Api/Properties/launchSettings.json` | A |
| 12 | `api/src/Orchestra.Api/Extensions/AppBuilderExtensions.cs` | A |
| 13 | `api/src/Orchestra.Api/Extensions/AppSetupExtensions.cs` | A |
| 14 | `api/src/Orchestra.Api/Common/ApiRoutes.cs` | A |
| 15 | `api/src/Orchestra.Api/Common/IModuleInstaller.cs` | A |
| 16 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` | A |
| 17 | `api/src/Orchestra.Api/Common/Errors/Error.cs` | A |
| 18 | `api/src/Orchestra.Api/Common/Errors/ErrorMessages.cs` | A |
| 19 | `api/src/Orchestra.Api/Common/Errors/ProblemResults.cs` | A |
| 20 | `api/src/Orchestra.Api/Common/Errors/GlobalExceptionMiddleware.cs` | A |
| 21 | `api/src/Orchestra.Api/Common/Ui/UiHosting.cs` | A |
| 22 | `api/src/Orchestra.Api/Modules/Health/HealthController.cs` | A |
| 23 | `api/src/Orchestra.Api/Data/OrchestraDbContext.cs` | B |
| 24 | `api/src/Orchestra.Api/Data/Configurations/*.cs` (one per entity, 8 files) | B |
| 25 | `api/src/Orchestra.Api/Data/Migrations/*` (generated `Initial`) | B |
| 26 | `api/src/Orchestra.Api/Data/DataModuleInstaller.cs` | B |
| 27 | `api/src/Orchestra.Api/Modules/Projects/ProjectEntity.cs` | B |
| 28 | `api/src/Orchestra.Api/Modules/Projects/ProjectRegistry.cs` | B |
| 29 | `api/src/Orchestra.Api/Modules/Projects/ProjectsController.cs` + `Features/ListProjects/*` | C |
| 30 | `api/src/Orchestra.Api/Modules/Runs/Entities/{Run,StageResult,StageText,SessionUsage,StageUsage}Entity.cs` + enums | B |
| 31 | `api/src/Orchestra.Api/Modules/Import/Ledger/{LedgerParser,LedgerBlock,LedgerNumbers}.cs` | B |
| 32 | `api/src/Orchestra.Api/Modules/Import/Tasks/{TaskJsonReader,TaskSnapshot}.cs`, `Import/Archive/{ArchiveReader,ArchiveSnapshot}.cs` | B |
| 33 | `api/src/Orchestra.Api/Modules/Import/{ModelName,PlanName,SourceClock,RunAssembler,ImportService,ImportHostedService}.cs` | B |
| 34 | `api/src/Orchestra.Api/Modules/Import/{ImportLogEntity,ImportErrorEntity}.cs` | B |
| 35 | `api/src/Orchestra.Api/Modules/Import/ImportController.cs` + `ImportModuleInstaller.cs` | B |
| 36 | `api/src/Orchestra.Api/Modules/Runs/Features/{ListRuns,GetRun}/*` + `RunsController.cs` + `RunsModuleInstaller.cs` | C |
| 37 | `api/src/Orchestra.Api/Modules/Usage/Features/{CostSummary,ModelQuality}/*` + `UsageController.cs` + `UsageModuleInstaller.cs` | D |
| 38 | `api/tests/Orchestra.Api.Tests/Orchestra.Api.Tests.csproj` + `GlobalUsings.cs` | A |
| 39 | `api/tests/Orchestra.Api.Tests/**` (tests, §7) | A–D |
| 40 | `api/tests/Orchestra.Api.Tests/Fixtures/**` (synthetic, §6.7) | B |
| 41 | `ui/package.json`, `package-lock.json`, `next.config.ts`, `tsconfig.json`, `eslint.config.mjs`, `postcss.config.mjs`, `.prettierrc`, `components.json`, `vitest.config.mts`, `vitest.setup.ts`, `.env.example` | A |
| 42 | `ui/public/index.html` (locale picker) | A |
| 43 | `ui/src/app/layout.tsx`, `ui/src/app/not-found.tsx`, `ui/src/app/[locale]/layout.tsx` | A |
| 44 | `ui/src/app/[locale]/{page,runs/view/page,cost/page,quality/page,imports/page}.tsx` | A (stubs) → C/D |
| 45 | `ui/src/i18n/{routing,navigation,request,messages}.ts` | A |
| 46 | `ui/src/lib/i18n/{config,formatter,page-metadata}.ts`, `messages/common.{fa,en}.ts` | A |
| 47 | `ui/src/lib/{api/client.ts,api/api-error.ts,features/define-feature.ts,query/query-client.ts,env.ts,utils.ts}` | A |
| 48 | `ui/src/components/{providers,layout,app}/**`, `ui/src/components/ui/**` (shadcn CLI) | A–D |
| 49 | `ui/src/styles/{tokens,globals}.css`, `ui/src/config/brand.ts` | A |
| 50 | `ui/src/features/registry.ts` + `features/{runs,cost,quality,imports}/**` | A (manifests) → C/D |
| 51 | `ui/src/architecture.test.ts`, `ui/src/i18n/messages.test.ts`, `ui/src/global.d.ts` | A |
| 51a | `ui/src/**/*.test.ts(x)` — the UI tests of §7 (each feature's tests sit next to its files) | A–D |
| 52 | `deploy/Dockerfile`, `deploy/docker-compose.yml`, `deploy/.env.example` | E |
| 53 | `.ai/api/conventions.md`, `.ai/api/naming.md`, `.ai/api/testing.md` | A |
| 54 | `.ai/ui/conventions.md`, `.ai/ui/vertical-slice.md`, `.ai/ui/i18n.md`, `.ai/ui/design.md`, `.ai/ui/testing.md` | A |
| 55 | `.ai/ci.md` | A |
| 56 | `README.md` (EDIT — "Run it" section), `.ai/README.md` (EDIT — the three "written by plan 0001" rows become links) | E / A |
| 57 | `.ai/decisions/0001-*.md`, `0002-*.md`, `0003-*.md` (EDIT — `Status: Accepted` with the gate-2 date; done by the planner at gate 2, before phase A starts) | gate 2 |

## 6. Per-file specification

### 6.1 Repo root

- `.gitignore` — add `appsettings.Local.json` (the existing `appsettings.*.local.json` does not match it). `out/`, `data/`, `*.db`, `.env` are already there.
- `README.md` (phase E) — a "Run it" section: copy `appsettings.Local.example.json` to
  `appsettings.Local.json`, fill the projects, then either `npm ci && npm run build` in `ui/` +
  `dotnet run --project api/src/Orchestra.Api`, or `docker compose -f deploy/docker-compose.yml up --build`.
  Open `http://localhost:3500`. No owner path appears in it.

### 6.2 `api/` build files

- `global.json` — `{"sdk":{"version":"10.0.302","rollForward":"latestFeature"}}` (as Project B).
- `Directory.Build.props` — `TreatWarningsAsErrors`, `Nullable enable`, `ImplicitUsings enable`,
  `TargetFramework net10.0`, `ManagePackageVersionsCentrally true`.
- `Directory.Packages.props` — exact versions: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10,
  `Microsoft.EntityFrameworkCore.Design` 10.0.10, `Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.3,
  `xunit.runner.visualstudio` 2.8.2, `FluentAssertions` 7.0.0 (comment: 8.x is commercial — D20).
  Comments use `—`, never `--` (Project B `ci.md`: a `--` in an XML comment voids the file).
- `Orchestra.slnx` — the two projects.
- `Orchestra.Api.csproj` — `Microsoft.NET.Sdk.Web`; packages EF Sqlite + Design (`PrivateAssets all`);
  `InternalsVisibleTo Orchestra.Api.Tests`.

### 6.3 Composition (`Program.cs`, `Extensions/`)

- `Program.cs` — the four-line shape of Project B (`CreateBuilder().ConfigureServices().Build()
  .ConfigureApp().Run()`) inside `try/catch/finally`; the `catch` writes the exception to
  `Console.Error` **and rethrows** (Web `conventions.md` → *Startup must fail loudly*).
- `AppBuilderExtensions.ConfigureServices` — configuration order: `appsettings.json`,
  `appsettings.{Environment}.json`, `appsettings.Local.json` (optional, reload off), environment
  variables. Binds `OrchestraOptions` and validates on start (`ValidateOnStart`). Registers
  `TimeProvider.System` as the `TimeProvider` singleton, then
  controllers (`AddControllers`, JSON camelCase, enums as strings), `ProblemDetails`, the CORS policy
  `ui-dev` (origin `http://localhost:3501`, only when `IsDevelopment()`), then every `IModuleInstaller`
  found in the assembly, ordered by `Order`.
- `AppSetupExtensions.ConfigureApp` — pipeline in this order: `GlobalExceptionMiddleware` →
  (Development) `UseCors("ui-dev")` → `UiHosting.UseUi` (default files + static files from the UI
  folder) → `MapControllers` → `UiHosting.MapUiFallback` → migrate the database (`Database.Migrate()`)
  before `Run`. `IModuleInstaller` orders: Data 10, Projects 20, Import 30, Runs 40, Usage 50.
- `launchSettings.json` — one profile `Orchestra.Api`, `applicationUrl` `http://localhost:3500`,
  `ASPNETCORE_ENVIRONMENT=Development`, `launchBrowser false`.

### 6.4 Configuration

`OrchestraOptions` (section `Orchestra`):

| Key | Type | Default | Rule |
|---|---|---|---|
| `DatabasePath` | string | `data/orchestra.db` | relative → against `ContentRootPath`; its folder is created |
| `UiPath` | string | `wwwroot` | relative → against `ContentRootPath`; missing folder → one warning at start, API still serves |
| `SourceTimeZone` | string? | `null` = `TimeZoneInfo.Local` | must resolve with `TimeZoneInfo.FindSystemTimeZoneById` (D17) |
| `ImportOnStart` | bool | `true` | |
| `Projects` | list | empty | each: `Id` (`^[a-z0-9-]{1,40}$`, unique), `Name` (1–80), `RunsPath` (non-empty). Invalid → start fails with the key named |

- `appsettings.json` — `Urls: http://localhost:3500`, `Orchestra` with the defaults, `Projects: []`,
  logging levels. No path of the owner.
- `appsettings.Development.json` — `Orchestra:UiPath` = `../../../ui/out`.
- `appsettings.Local.example.json` — two example projects with placeholder paths
  (`C:\\path\\to\\ProjectA\\orchestrator\\runs`), `SourceTimeZone: "Asia/Tehran"`, and a comment key
  `"_readme"` saying to copy the file to `appsettings.Local.json`.

### 6.5 Common

- `ApiRoutes` — `Base = "api"`, `Controller = Base + "/[controller]"`. Never a literal `"api"` in a route.
- `Error` — `record Error(string Code, int Status)`; codes `<Module>.<Thing>.<Reason>`.
- `ErrorMessages` — two dictionaries, `Fa` and `En`, keyed by code, plus `Get(code, cultures)`. It
  picks `fa` when the request's `Accept-Language` prefers `fa`, otherwise `en`. Codes and texts:

| Code | Status | en | fa |
|---|---|---|---|
| `Runs.Run.NotFound` | 404 | Run not found. | run پیدا نشد. |
| `Projects.Project.NotFound` | 404 | Project not found. | پروژه پیدا نشد. |
| `Usage.Query.InvalidGroupBy` | 400 | groupBy must be day, month, model or project. | groupBy باید یکی از day، month، model یا project باشد. |
| `Usage.Query.InvalidRange` | 400 | The date range is not valid. | بازه‌ی تاریخ معتبر نیست. |
| `Usage.Query.InvalidTimeZone` | 400 | Unknown time zone. | منطقه‌ی زمانی ناشناخته است. |
| `Import.AlreadyRunning` | 409 | An import is already running. | یک خواندن در حال انجام است. |
| `Api.Unexpected` | 500 | Something went wrong on the server. | خطایی در سرور رخ داد. |
| `Api.NotFound` | 404 | No such API route. | چنین مسیری در API نیست. |

- `ProblemResults.From(Error, HttpContext)` — `ProblemDetails` with `title` = localized text,
  `status`, `extensions.code` = the code.
- `GlobalExceptionMiddleware` — logs and returns `Api.Unexpected` as `ProblemDetails`. No
  `UseExceptionHandler` (Web `conventions.md` trap).
- `UiHosting` — serves `UiPath` with `UseDefaultFiles` + `UseStaticFiles` (one
  `PhysicalFileProvider`). `MapUiFallback`: a GET outside `/api` that matched no file returns
  `404.html` from the UI folder with status 404; a request under `/api` that matched nothing returns
  `Api.NotFound`.

### 6.6 Data and projects

- `OrchestraDbContext` — `DbSet`s for the eight tables of data-model § *Tables*, configured in
  `Data/Configurations/<Name>Configuration.cs` (Web's Audit/Identity shape). Table names are those of
  the data model. Every string column has a max length (ids 40, keys 80, names/models 120, branch 200,
  task 1000, messages 2000, paths 500). Children cascade-delete from `Run`, `ImportError` from
  `ImportLog`. Indexes: `Run(ProjectId, RunKey)` unique, `Run(ProjectId, Status)`,
  `StageResult(RunId, Stage)`, `ImportLog(ProjectId, StartedAt)`.
- Times are `DateTime` with `Kind = Utc` (a value converter enforces it on read). **Not**
  `DateTimeOffset`: EF's SQLite provider cannot order or compare it in SQL.
- Money is `long CostMicros` (D18); `null` when the source had no cost column.
- Migration `Initial` generated with `dotnet ef migrations add Initial -o Data/Migrations`. No
  design-time factory with a connection string in it.
- `ProjectRegistry` — singleton over `IOptions<OrchestraOptions>`: `All`, `Find(id)`. On each import
  the `Project` table is made equal to the configuration (insert, update, delete — deleting a project
  deletes its runs; its import logs stay).

### 6.7 Import

**`LedgerParser.Parse(string text, string file) → LedgerParseResult { Blocks, Errors }`** — pure, no
I/O. Strips a BOM, accepts `\r\n` and `\n`. A block starts at a line matching
`^## (\d{4}-\d{2}-\d{2} \d{2}:\d{2}) - ([A-Z]+-[A-Z0-9]+-\d+) - (verify|close)\s*$` and ends before
the next one. Inside a block:

| Line | Pattern (anchored) | Into |
|---|---|---|
| repo | `^repo (\S+) \| status (\S+) \| plan v(\d+) \| branch (\S+)$` | Repo, Status, PlanVersion, Branch |
| review / read-back | `^- (review|read-back): (\w+) \(blocker (\d+), should (\d+), nit (\d+)\) by (\S+) \(round (\d+)\)$` | Stage, Verdict, counts, model, round |
| phase | `^- phase (\d+): (\w+) by (\S+) \| changed (\d+) \| not in plan (\d+) \| violations (\d+)$` | Implement, Phase, Verdict, … |
| verify | `^- verify: (\w+) \(gates (green|red)(?:: ([^,]+))?, model said (\w+)\) by (\S+)$` | Verdict, GatesGreen, GatesNote, ModelVerdict, model |
| table | a `\|`-row whose first cell is `session` or `stage` starts a table; the header names the columns (any of the three session shapes); the separator row is skipped; a `**total**` row is the total | SessionUsage / StageUsage rows, the run's `CostMicros` |
| fence | from a ```` ``` ```` line to the next | ignored |
| anything else | footnotes, blank lines | ignored |

A block with no `repo` line, or a table row whose cell count differs from its header, or a number that
does not parse, becomes **one `LedgerError { File, Heading, Line, Message }`** and the whole block is
dropped; parsing continues with the next block.

**`LedgerNumbers`** — `Tokens("529.9k")` → 529 900, `"2.2m"`/`"2.2M"` → 2 200 000, `"0"` → 0,
`""`/`"—"` → `null`; `Duration("22:05")` → 1 325 s, `"2:38:01"` → 9 481 s; `Money("$0.170")` →
170 000 micros. Invariant culture.

**`TaskJsonReader.Read(Stream, string file) → TaskSnapshot | error`** — `System.Text.Json`, BOM
tolerated, case-insensitive. Reads `id, repo, task, status, createdAt, updatedAt, round, branch, plan,
review, readback, implement, phases[], verify, history[]`; every stage may be `null`; `approved` is
not read (the history rows carry the approval). Artifact paths are made relative to the project root
(the parent of `RunsPath`'s parent) when under it, else the file name only. **This root is for display
only:** nothing is ever opened or served through it (plan 0002 §6.1 has the rule for opening files).

**`ArchiveReader.Read(Stream, string file) → ArchiveSnapshot | error`** — `schema` must be 1 (other →
one error naming the file); `archivedAt` with its offset → UTC; `task` through the same mapping as
`TaskJsonReader` (shared code, not a copy); `reviews`, `readbacks`, `verify` → `{ Stage, Round, Name,
Text }`; `usage` is not read (D13). Files: `<RunsPath>/_archive/*.json`; the task id is the file name up
to its first `.`.

**`ModelName.Split("opencode:opencode-go/glm-5.3-flash")`** → (`opencode`, `opencode-go/glm-5.3-flash`);
no `:` → (adapter given separately or `null`, whole string). **`PlanName.From(task, branch)`** →
(`0155`, `Android icon …`) from `^Plan (\d{4}):\s*(.+)$`, else (`0155`, null) from `^plan-(\d{4})-`
in the branch, else (null, null). **`SourceClock.ToUtc(local)`** converts with `SourceTimeZone` (D17).

**`RunAssembler.Build(projectId, blocks, snapshots) → IReadOnlyList<RunEntity>`** — pure. Implements
data-model § *How a run is identified* steps 1–5 exactly. For a ledger run: `CreatedAt` = first
block's time, `UpdatedAt` = last block's, `ClosedAt` = the last `close` block's time, `Rounds` = the
final review's round, stage lines become `StageResult` rows with `IsFinal = true`, usage from the
**last** block only. For a live run: fields from `task.json`, each `history[]` entry becomes a
`StageResult` (`IsFinal = false`; the last entry per stage `true`), phases and verify become final rows,
no usage (open runs have no ledger block yet). Merged run: `Source = Both`, live fields win, ledger
usage is kept. Archives: matched per data-model step 4a; the archived `task` gives history rows (as a live
run's) and the final stage rows where the ledger block has none; its texts become `StageText` rows; an
unmatched archive is a run with `Source = Archive`.

**`ImportService.ImportAllAsync(ct)`** — guarded by one `SemaphoreSlim(1)`; `TryImportAllAsync`
returns `Import.AlreadyRunning` when held. Per project, in order: sync the `Project` row; read
`<RunsPath>/_ledger.md` (missing ledger = zero blocks, not an error), every
`<RunsPath>/T-*/task.json` and every `<RunsPath>/_archive/*.json` (missing folder = none); parse; assemble; then **one transaction**: delete the project's runs,
insert the new ones; write an `ImportLog` (+ `ImportError`s) and trim to the last 50 per project. A
missing `RunsPath` = one `ImportError` and no change to that project's runs. Files are opened as in
§0 rule 2.

**`ImportHostedService`** — `BackgroundService`; when `ImportOnStart`, runs `ImportAllAsync` once
after start; an exception is logged, never crashes the host.

**`ImportController`** (`api/import`) — `POST` → 200 `ImportResultDto[]`
(`projectId, startedAt, finishedAt, filesRead, blocksRead, runsBuilt, errorCount`) or 409.
`GET logs?project=&take=20` → `ImportLogDto[]` with `errors: { file, location, message }[]`.

**Fixtures** (`api/tests/Orchestra.Api.Tests/Fixtures/`, synthetic, task ids `T-DEMO-*`, invented
text, model names as in real files):

| File | Holds |
|---|---|
| `ledger.shapes.md` | one block per shape: oldest session table (no requests, no cost), requests only, requests + cost + stage table + fence; review/read-back FINDINGS; phase NEED_INPUT; verify FAIL with `gates red: dotnet test` and with `model said FAIL`; a BOM and CRLF line ends |
| `ledger.lifecycle.md` | `T-DEMO-001`: verify (VerifyFailed) → verify (Verified) → close; then verify + close on a **new branch** (second run); then a second close on that branch; `T-DEMO-002`: verify only (still open) |
| `ledger.corrupt.md` | three blocks; the middle one has a table row with a missing cell |
| `runs/T-DEMO-002/task.json` | open run matching `T-DEMO-002`'s branch; PowerShell indentation; `history[]` with review FINDINGS → PASS, readback FINDINGS → PASS |
| `runs/T-DEMO-003/task.json` | `status: Planning`, `review: null`, `implement: null`, `verify: null`, `phases: []`, `branch: ""` |
| `runs/_archive/T-DEMO-001.json` | schema 1, `archivedAt` with `+03:30`, `task` of `T-DEMO-001`'s **second** branch (closed), two reviews (FINDINGS → PASS), one read-back, one verify, a `usage` block (ignored) |
| `runs/_archive/T-DEMO-009.json` | an archive with no ledger block → `Source = Archive` |
| `runs/_archive/T-DEMO-010.json` | `schema: 2` → one import error |

Each fixture's ledger totals are hand-computed and written in a comment line at the top of the test
that uses it.

### 6.8 Runs endpoints

- `GET api/projects` → `ProjectDto[]`: `id, name, runCount, openRunCount, sessions, calls, requests,
  costUsd` (null when no run has a cost), `lastImport: { finishedAt, errorCount } | null`.
- `GET api/runs?project=&status=&repo=` → `RunListItemDto[]`, newest `updatedAt` first. `status` and
  `repo` match case-insensitively. `Run.Status` is the **raw status word from the file** (ledger heading
  or `task.json`), not a fixed set; the UI's status filter offers the distinct values of the loaded
  list, and §6.12's badge mapping applies by name, anything else → secondary:
  `projectId, projectName, runKey, taskId, repo, status, planNumber, planTitle, task, branch,
  sessions, calls, requests, costUsd, rounds, models: string[]` (distinct short model names of the
  final stage rows), `updatedAt, closedAt, source`. Unknown `project` → 404
  `Projects.Project.NotFound`.
- `GET api/runs/{project}/{runKey}` → `RunDetailDto`: the list fields + `planVersion,
  createdAt`, `stages: StageResultDto[]` (final rows, in review → read-back → implement (by phase) →
  verify order), `rounds: StageResultDto[]` (non-final history rows, by time), `sessions:
  SessionUsageDto[]`, `stageUsage: StageUsageDto[]`, `texts: { stage, round, name, text }[]`, `totalCostUsd`. Unknown → 404 `Runs.Run.NotFound`.
- Handlers: `ListRunsHandler`, `GetRunHandler` (`AsNoTracking`, projection to DTOs in the query).

### 6.9 Usage endpoints

- `GET api/usage/cost?project=&from=&to=&groupBy=day|month|model|project&tz=` → `CostRowDto[]`:
  `key, costUsd, runs, sessions, calls, input, output, cacheRead`. A run is dated by its `UpdatedAt`
  (its last ledger block) **in the browser's zone**: the UI sends `tz` =
  `Intl.DateTimeFormat().resolvedOptions().timeZone` (D16); missing → UTC; unknown →
  `Usage.Query.InvalidTimeZone` (§6.5). `from`/`to` are dates in that zone, inclusive; `to < from` →
  `Usage.Query.InvalidRange`. SQLite cannot do zone arithmetic, so the handler loads the runs of the
  range widened by one day and groups in memory (tens of runs). `model` groups `SessionUsage`; `day`,
  `month`, `project` group runs by their total (`Run.CostMicros`, from the ledger's **total** row).
- `GET api/usage/quality?project=` → `ModelQualityDto`:
  - `reviewers[]`: `model, runs, avgRounds, passFirstRound` (share of runs with `Rounds = 1`),
    `findingsShare` (share of non-final review rows with verdict FINDINGS — open runs; `null` when
    none), `finalBlockers, finalShould, finalNits` (sums);
  - `readBacks[]`: `model, runs, finalFindingsShare` (share of runs whose **final** read-back verdict ≠
    PASS — a different measure from the reviewers' `findingsShare`), `finalBlockers, finalShould,
    finalNits`;
  - `implementers[]`: `model, phases, needInput, changedAvg, outsidePlan, violations, verifyRuns,
    verifyFailRate`;
  - `stageTimes[]`: `stage, model, runs, avgSeconds`; `cache[]`: `model, input, cacheRead, hitRate`.

### 6.10 `ui/` configuration

- `package.json` — `name: orchestra-ui`, `private`, scripts: `dev: next dev -p 3501`, `build: next
  build`, `lint: eslint .`, `typecheck: next typegen && tsc --noEmit`, `test: vitest run`, `test:watch`,
  `format`. Dependencies: the same packages as Project B's `Ui/package.json` minus `axios`, `gridify-client`,
  `@tanstack/react-table`, `write-excel-file`, `react-hook-form`, `@hookform/resolvers`, `zod`,
  each at the **exact** version below (no `^` or `~`). The versions are those installed in Project B's
  `Ui/package-lock.json` (read 2026-11-19, owner's leave):
  - dependencies: `@fontsource-variable/vazirmatn` 5.3.0, `@tanstack/react-query` 5.103.2,
    `class-variance-authority` 0.7.1, `cn` 0.4.0, `lucide-react` 1.47.0, `next` 16.3.6, `next-intl` 4.14.6,
    `next-themes` 0.4.6, `radix-ui` 1.6.7, `react` 19.2.8, `react-dom` 19.2.8, `shadcn` 4.21.0,
    `sonner` 2.0.8, `tw-animate-css` 1.4.0, `use-intl` 4.14.7;
  - devDependencies: `@tailwindcss/postcss` 4.3.3, `@testing-library/dom` 10.4.2,
    `@testing-library/jest-dom` 7.0.1, `@testing-library/react` 16.3.3, `@testing-library/user-event`
    14.6.7, `@types/node` 22.20.4, `@types/react` 19.3.0, `@types/react-dom` 19.3.0, `eslint` 9.39.5,
    `eslint-config-next` 16.3.6, `jsdom` 29.1.1, `prettier` 3.9.9, `prettier-plugin-tailwindcss` 0.8.1,
    `tailwindcss` 4.3.3, `typescript` 5.9.3, `vitest` 5.0.1.
  - `ui/src/lib/utils.ts` (file 47) is exactly `export { cn } from 'cn';`, as in Project B (the `cn`
    package replaces `clsx` + `tailwind-merge`; shadcn components import `cn` from `@/lib/utils`).
  - `use-intl` 4.14.7 satisfies `next-intl` 4.14.6's own range (`^4.14.6`), so npm installs one copy.
  `engines.node >=22`. Commit `package-lock.json`.
- `next.config.ts` — `output: 'export'`, `trailingSlash: true`, `images: { unoptimized: true }`,
  next-intl plugin pointing at `./src/i18n/request.ts`.
- `components.json` — as Project B (`radix-nova`, `rtl: true`, lucide).
- `.env.example` — `NEXT_PUBLIC_API_BASE_URL=http://localhost:3500` (dev); empty in a production
  build = same origin.
- `vitest.config.mts` / `vitest.setup.ts` — as Project B (`globals: false`, explicit `cleanup`,
  `matchMedia` and `ResizeObserver` stand-ins).

### 6.11 i18n and the shell

- `i18n/routing.ts` — `defineRouting({ locales: ['fa','en'], defaultLocale: 'en', localePrefix:
  'always' })`. `navigation.ts` — `createNavigation(routing)`. No `proxy.ts`.
- `i18n/request.ts` — `getRequestConfig` over `requestLocale`, messages from `messages.ts` (`common`
  + each registered feature's `fa`/`en`); exports the callback for tests (Project B `testing.md`).
- `lib/i18n/config.ts` — `LOCALES`, `LOCALE_DIRECTION = { fa: 'rtl', en: 'ltr' }`,
  `FORMATTING_LOCALE = { fa: 'fa-u-nu-latn', en: 'en' }`, `LOCALE_STORAGE_KEY = 'orchestra.locale'`.
- `lib/i18n/formatter.ts` — `useAppFormatter()`: `dateTime(value, style)`, `date(value)`,
  `number(value)`, `money(usd | null)` (`$0.387`, null → `—`), `tokens(n)` (`529.9k`, `2.2m`),
  `duration(seconds)` (`22:05`, `2:38:01`), `percent(x)`. Built on `Intl` with `FORMATTING_LOCALE` and
  **no** `timeZone` (D16). The only file allowed to call `Intl.*Format` (architecture test).
- `app/layout.tsx` — returns `children` (next-intl's pass-through root layout).
- `app/[locale]/layout.tsx` — `generateStaticParams` → both locales; `setRequestLocale`; `<html
  lang={locale} dir={LOCALE_DIRECTION[locale]} suppressHydrationWarning>`, Vazirmatn, `AppProviders`,
  `AppShell`. Unknown locale → `notFound()`.
- `app/not-found.tsx` — static 404 with links to `/fa/` and `/en/` (both languages on the page; the
  locale is unknown there). The export writes it as `404.html`, which the API serves (§6.5).
- `public/index.html` — plain HTML, no React. Inline script: read `localStorage[LOCALE_STORAGE_KEY]`
  (in `try/catch`); else the first of `navigator.languages` starting with `fa` or `en`; else `en`;
  `location.replace('/' + locale + '/')`. `<noscript>`: two links, «فارسی» and `English`.
- `components/providers/app-providers.tsx` — `ThemeProvider` (class, system default) →
  `QueryClientProvider` → `DirectionProvider` (from the locale) → `TooltipProvider` → children +
  `Toaster` (bottom-start).
- `components/layout/app-shell.tsx`, `app-sidebar.tsx` (from `MENU_ITEMS`; on the start side; sheet on
  mobile), `locale-switcher.tsx` (swaps the first path segment, keeps the query string, writes
  `LOCALE_STORAGE_KEY`), `theme-switcher.tsx`, `reimport-button.tsx` (POST `api/import`, spinner while
  pending, toast with the result, invalidates every query; 409 → the localized `title` as an error
  toast).
- `lib/api/client.ts` — `apiClient.get/post` over `fetch`, base `env.apiBaseUrl`, sends
  `Accept-Language` = the current locale, parses `ProblemDetails` into `ApiError { status, code, title }`.
  The only place `fetch` is called (architecture test).
- `styles/tokens.css` — the token set of Project B's `design.md` (both `:root` and `.dark`), brand
  colours from the preview: primary indigo (`#3b5bdb` light / `#7c94f5` dark), sidebar dark in both themes,
  semantic `success`, `warning`, `info`, `destructive`, `chart-1…5`. `config/brand.ts` — name `Orchestra`.

### 6.12 Features and their text

Each feature: `index.ts`, `<name>.manifest.ts`, `<name>.api.ts`, `<name>.queries.ts`, `<name>.types.ts`
(DTOs of §6.8–6.9, camelCase), `i18n/fa.ts`, `i18n/en.ts`, `components/`. Menu categories (`common.menu`):
`overview` (runs, cost, quality), `system` (imports).

| Feature | Route | Page |
|---|---|---|
| `runs` | `/{locale}/` and `/{locale}/runs/view/?project=&run=` | list: project chips (All + one per project) with KPI cards (runs, sessions, calls, cost) for the selection; filters status, repo; table: plan number + title (or the task text when there is no plan), the task id + project under it, repo, status badge, sessions, calls, cost, updated. Row click → detail. Detail: header (plan, status, task id, project/repo, plan version, branch), stages table (stage, model, verdict badge, findings or changed/not in plan/violations, at), rounds table (only when present), sessions table, stage usage table, total cost |
| `cost` | `/{locale}/cost/` | project selector, month range (default: the current month); bar lists by model and by project, columns by day; a note that hops without a session are not included |
| `quality` | `/{locale}/quality/` | project selector; reviewer table + bar of avg rounds; read-back table; implementer table + verify FAIL rate bars; stage times table; cache hit rate bars |
| `imports` | `/{locale}/imports/` | per project: last import time, files, blocks, runs, error count; the errors table (file, location, message); the re-import button |

Status badge mapping: `Verified` → success, `VerifyFailed`/`Violation` → destructive, `Approved` →
default, `Implementing` → info, `Planning`/`AwaitingApproval` → warning, anything else → secondary
showing the raw value. Verdict badge: `PASS` → success, `FINDINGS`/`NEED_INPUT` → warning, `FAIL` →
destructive.

Message catalogue — `common` (shell) and each feature. Copy exactly:

| Key | en | fa |
|---|---|---|
| `common.appName` | Orchestra | Orchestra |
| `common.menu.overview` | Overview | نمای کلی |
| `common.menu.system` | System | سیستم |
| `common.actions.reimport` | Re-import | خواندن دوباره |
| `common.actions.retry` | Try again | تلاش دوباره |
| `common.actions.back` | Back | بازگشت |
| `common.theme.label` | Theme | تم |
| `common.theme.light` | Light | روشن |
| `common.theme.dark` | Dark | تاریک |
| `common.theme.system` | System | سیستم |
| `common.locale.label` | Language | زبان |
| `common.states.loading` | Loading… | در حال بارگذاری… |
| `common.states.empty` | Nothing here yet. | هنوز چیزی نیست. |
| `common.states.noProjects` | No project is configured. Add one to appsettings.Local.json and re-import. | هیچ پروژه‌ای تنظیم نشده. یکی را به appsettings.Local.json اضافه کنید و دوباره بخوانید. |
| `common.errors.network` | The server did not answer. Is the API running? | سرور جواب نداد. آیا API در حال اجراست؟ |
| `common.errors.unexpected` | Something went wrong. | خطایی رخ داد. |
| `common.import.done` | Imported {runs} runs from {projects} projects. | {runs} run از {projects} پروژه خوانده شد. |
| `common.import.doneWithErrors` | Imported with {errors} errors — see the import log. | با {errors} خطا خوانده شد. گزارش خواندن را ببینید. |
| `common.filters.allProjects` | All projects | همه‌ی پروژه‌ها |
| `common.filters.project` | Project | پروژه |
| `common.notAvailable` | — | — |
| `runs.title` | Runs | runها |
| `runs.kpi.runs` | Runs | runها |
| `runs.kpi.sessions` | Sessions | sessionها |
| `runs.kpi.calls` | Calls | callها |
| `runs.kpi.cost` | Cost | هزینه |
| `runs.filters.status` | Status | وضعیت |
| `runs.filters.repo` | Repo | repo |
| `runs.filters.all` | All | همه |
| `runs.columns.plan` | Plan / task | پلن / تسک |
| `runs.columns.repo` | Repo | repo |
| `runs.columns.status` | Status | وضعیت |
| `runs.columns.sessions` | Sessions | session |
| `runs.columns.calls` | Calls | call |
| `runs.columns.cost` | Cost | هزینه |
| `runs.columns.updated` | Updated | به‌روز |
| `runs.empty` | No runs. Check the import log if you expected some. | runی نیست. اگر انتظارش را داشتید، گزارش خواندن را ببینید. |
| `runs.detail.notFound` | This run is not in the dashboard. It may have been removed from the files. | این run در داشبورد نیست. شاید از فایل‌ها حذف شده باشد. |
| `runs.detail.planVersion` | Plan version | نسخه‌ی پلن |
| `runs.detail.branch` | Branch | برنچ |
| `runs.detail.stages` | Stages | مرحله‌ها |
| `runs.detail.rounds` | Rounds | دورها |
| `runs.detail.sessions` | Sessions | sessionها |
| `runs.detail.stageUsage` | Usage per stage | مصرف هر مرحله |
| `runs.detail.totalCost` | Total cost | هزینه‌ی کل |
| `runs.stage.review` | Review | مرور |
| `runs.stage.readBack` | Read-back | read-back |
| `runs.stage.implement` | Phase {phase} | فاز {phase} |
| `runs.stage.verify` | Verify | verify |
| `runs.columns.stage` | Stage | مرحله |
| `runs.columns.model` | Model | مدل |
| `runs.columns.verdict` | Result | نتیجه |
| `runs.columns.findings` | Findings / changes | یافته‌ها / تغییرات |
| `runs.columns.at` | At | زمان |
| `runs.columns.session` | Session | session |
| `runs.columns.firstRole` | First role | نقش اول |
| `runs.columns.input` | Input | input |
| `runs.columns.output` | Output | output |
| `runs.columns.reasoning` | Reasoning | reasoning |
| `runs.columns.cacheRead` | Cache read | cache read |
| `runs.columns.time` | Time | زمان |
| `runs.detail.texts` | Review, read-back and verify texts | متن مرور، read-back و verify |
| `runs.detail.textRound` | Round {round} | دور {round} |
| `runs.detail.noTexts` | No texts: this run was closed before the archive existed, or its archive was overwritten by a later run with the same task id. | متنی نیست: یا این run پیش از وجود بایگانی بسته شده، یا بایگانی‌اش را run بعدی با همان شماره‌ی تسک رونویسی کرده است. |
| `cost.title` | Cost | هزینه |
| `cost.byModel` | By model | به تفکیک مدل |
| `cost.byProject` | By project | به تفکیک پروژه |
| `cost.byDay` | By day | به تفکیک روز |
| `cost.range` | Months | ماه‌ها |
| `cost.note` | Hops without a session (the conductor, codebuddy, manual) are not included. | hopهای بدون session (conductor، codebuddy، manual) حساب نشده‌اند. |
| `quality.title` | Model quality | کیفیت مدل‌ها |
| `quality.reviewers` | Reviewers | مرورکننده‌ها |
| `quality.readBacks` | Read-back | read-back |
| `quality.implementers` | Implementers | پیاده‌سازها |
| `quality.stageTimes` | Time per stage | زمان هر مرحله |
| `quality.cache` | Cache hit rate | نرخ cache |
| `quality.columns.runs` | Runs | runها |
| `quality.columns.avgRounds` | Rounds to PASS (avg) | دور تا PASS (میانگین) |
| `quality.columns.passFirstRound` | PASS in round 1 | PASS در دور اول |
| `quality.columns.findingsShare` | FINDINGS share | سهم FINDINGS |
| `quality.columns.finalCounts` | Final blocker / should / nit | blocker / should / nit پایانی |
| `quality.columns.phases` | Phases | فازها |
| `quality.columns.needInput` | NEED_INPUT | NEED_INPUT |
| `quality.columns.outsidePlan` | Outside the plan | بیرون از پلن |
| `quality.columns.violations` | Violations | تخلف‌ها |
| `quality.columns.verifyFailRate` | Verify FAIL rate | نرخ FAIL در verify |
| `quality.columns.avgTime` | Average time | میانگین زمان |
| `quality.note` | Per-round results come from open and archived runs; older closed runs only have their final round. | نتیجه‌ی هر دور از runهای باز و بایگانی‌شده می‌آید. runهای بسته‌ی قدیمی‌تر فقط دور آخر را دارند. |
| `imports.title` | Import log | گزارش خواندن |
| `imports.lastImport` | Last import | آخرین خواندن |
| `imports.files` | Files | فایل‌ها |
| `imports.blocks` | Ledger blocks | بلوک‌های ledger |
| `imports.runs` | Runs | runها |
| `imports.errors` | Errors | خطاها |
| `imports.columns.file` | File | فایل |
| `imports.columns.location` | Where | کجا |
| `imports.columns.message` | Problem | مشکل |
| `imports.none` | No errors in the last import. | آخرین خواندن خطایی نداشت. |

Each feature also has `title` used by `pageMetadata` and the menu, equal to its `<feature>.title`.

### 6.13 Shared UI components (`components/app/`)

`PageHeader` (title, description, actions at the inline end), `KpiCard`, `StatusBadge`,
`VerdictBadge`, `BarList` (label · bar · value rows, bar width from the max, token colours only, works
in RTL), `DayColumns` (vertical bars per day, `dir="ltr"` on the axis), `ProjectChips`,
`MoneyText`/`TokensText`/`DurationText` (via the formatter, `tabular-nums`, `data-ltr`), `EmptyState`,
`ErrorState` (destructive alert + retry), `TableSkeleton`. Latin values (task ids, models, branches,
session ids) render with `data-ltr` / `dir="ltr"`.

### 6.14 Deploy

- `deploy/Dockerfile` — multi-stage: `node:22-alpine` runs `npm ci && npm run build` in `ui/`;
  `mcr.microsoft.com/dotnet/sdk:10.0` runs `dotnet publish api/src/Orchestra.Api -c Release -o /app`;
  final `mcr.microsoft.com/dotnet/aspnet:10.0` with `/app` and the UI's `out/` copied to `/app/wwwroot`.
  `ENV ASPNETCORE_URLS=http://+:3500 Orchestra__DatabasePath=/data/orchestra.db`, `EXPOSE 3500`,
  non-root user.
- `deploy/docker-compose.yml` — service `orchestra`, image `orchestra`, `ports: "127.0.0.1:3500:3500"`,
  volume `orchestra-data:/data`, `env_file: .env`, and **read-only** bind mounts for each project's runs
  folder taken from `.env` (`PROJECT1_RUNS`, `PROJECT2_RUNS` → `/projects/p1`, `/projects/p2`, `:ro`).
- `deploy/.env.example` — `PROJECT1_RUNS=`, `PROJECT2_RUNS=`, and the matching
  `Orchestra__Projects__0__Id/Name/RunsPath=/projects/p1`, `…__1__…`, `Orchestra__SourceTimeZone=Asia/Tehran`.
  `.env` is git-ignored already.

### 6.15 Rule docs (phase A)

Trimmed from Project B, each **AUTHORITATIVE** for its topic, each linking to its source doc there:

| Doc | Content |
|---|---|
| `.ai/api/conventions.md` | §6.3–6.5 as rules: four-line `Program.cs`, fail loudly, one `IModuleInstaller` per module with sparse `Order`, `ApiRoutes`, controllers only route, DTOs beside their feature, `ProblemDetails` + localized error codes, UTC `DateTime`, `long` micros, no `UseExceptionHandler`, no MediatR/FluentValidation (D19), read-only file access |
| `.ai/api/naming.md` | folder `Modules/<Module>/Features/<Action>/<Action>Query.cs` + `<Action>Handler.cs`; `<Name>Entity`; `<Name>Configuration`; dependency field = `_` + type name; error codes; test names state the guarantee |
| `.ai/api/testing.md` | xunit + FluentAssertions 7 (pinned), no mocking library, no server or socket; SQLite **in-memory** (`DataSource=:memory:`, connection kept open) is allowed as the real provider; synthetic fixtures only (§0 rule 3); the count never drops |
| `.ai/ui/conventions.md` | Project B's, minus axios/forms/grids; `apiClient` over `fetch` |
| `.ai/ui/vertical-slice.md` | Project B's, without permissions and guarded routes |
| `.ai/ui/i18n.md` | ADR 0004 in practice: routing, `public/index.html`, catalogues and key parity, the formatter, direction, finished sentences |
| `.ai/ui/design.md` | tokens, brand, components and states of §6.11–6.13 |
| `.ai/ui/testing.md` | Project B's setup notes that apply; what to test |
| `.ai/ci.md` | the gates of §9 and the test-count baseline (filled at the end of each phase) |

The `.ai/README.md` rows "written by plan 0001" become links.

## 7. Tests

Names state the guarantee (Project B `naming.md`). Counts are recorded in `.ai/ci.md` per phase.

### API (`api/tests/Orchestra.Api.Tests/`)

| File | Facts |
|---|---|
| `Import/LedgerNumbersTests.cs` | k/m/K/M tokens; `0`; empty → null; `m:ss` and `h:mm:ss`; money to micros; invariant culture under `fa-IR` current culture |
| `Import/LedgerParserTests.cs` (over `ledger.shapes.md`) | heading fields; repo line; review and read-back incl. FINDINGS; phase incl. NEED_INPUT; verify PASS, `gates red: dotnet test`, `model said FAIL`; each of the three session-table shapes (missing columns are null); stage table with time and cost; the code fence is ignored; footnotes ignored; BOM + CRLF parse the same as LF |
| `Import/LedgerParserErrorTests.cs` (over `ledger.corrupt.md`) | the broken block becomes one error naming its heading; the blocks before and after parse |
| `Import/TaskJsonReaderTests.cs` | the open run; the Planning run with nulls and empty branch; history events; artifact paths made relative |
| `Import/ArchiveReaderTests.cs` | schema 1 read; `archivedAt` offset → UTC; texts with rounds; usage ignored; schema 2 → one error; `T-X.20261119.json` gives task id `T-X` |
| `Import/ModelNameTests.cs`, `PlanNameTests.cs`, `SourceClockTests.cs` | prefix split with and without adapter; plan from task text, from branch, none; Tehran local → UTC |
| `Import/RunAssemblerTests.cs` (over `ledger.lifecycle.md` + task.json fixtures) | three verify/close blocks → one run whose usage equals the **last** block's (not the sum); a reused task id on a new branch → `T-DEMO-001.1` and `.2`; a second close on the same branch joins `.2`; an open task.json merges with its ledger run (`Source = Both`, live status wins, ledger usage kept); a task.json with no ledger run → `Live`; history rows non-final except the last per stage; `T-DEMO-001`'s archive joins run `.2` (its branch), not `.1`; its texts attach to `.2`; `T-DEMO-009` → `Source = Archive` |
| `Import/ImportServiceTests.cs` (SQLite in-memory, fixture folders copied to a temp dir) | import twice → identical run keys and row counts; delete a run folder → the run is gone after re-import; missing `RunsPath` → one error, the other project imported, the broken project's old rows untouched; corrupt block → one error, the other blocks imported; logs trimmed to 50; a second concurrent import → `Import.AlreadyRunning` |
| `Usage/CostSummaryTests.cs` | a run at 22:00 UTC falls on the next day with `tz=Asia/Tehran` and on the same day without `tz`; unknown `tz` → `Usage.Query.InvalidTimeZone`; month total equals the sum of the fixtures' ledger **total** rows (hand-computed in the test's comment); by model equals the session rows; a run without a cost column counts in runs but adds no cost |
| `Usage/ModelQualityTests.cs` | avg rounds and PASS-in-round-1 per reviewer; FINDINGS share from history only; verify FAIL rate per implementer; cache hit rate |
| `Runs/ListRunsTests.cs`, `Runs/GetRunTests.cs` | filters; ordering; unknown project/run → the error codes; stage order in detail |
| `Common/ErrorMessagesTests.cs` | fa and en have the same codes, none empty; `Accept-Language: fa-IR,fa;q=0.9` → fa; `de` → en |
| `Common/OptionsValidationTests.cs` | bad project id, duplicate id, empty RunsPath, unknown time zone → validation failure naming the key |
| `Architecture/ModuleInstallerTests.cs` | every module has one installer; orders unique |
| `Architecture/NoClockTests.cs` | no `DateTime.Now`/`DateTime.UtcNow` outside `SourceClock` and the import log stamping, which go through `TimeProvider.System` registered in DI (Web trap: `TimeProvider` must be registered) |

### UI (`ui/src/**`, Vitest + RTL)

| File | Facts |
|---|---|
| `architecture.test.ts` | ported from Project B: imports only through a feature's index, no cycles, `lib/`/`components/ui|app` never import a feature, every feature in the registry, no hex/palette classes, only logical direction classes; plus: `fetch` only in `lib/api/client.ts`, `Intl.*Format` and next-intl's `useFormatter` only in `lib/i18n/formatter.ts` |
| `i18n/messages.test.ts` | every namespace has the same keys in fa and en; no message is empty |
| `i18n/request.test.ts` | both locales load `common` and every feature namespace |
| `lib/i18n/formatter.test.ts` | fa date of `2026-11-19T12:00:00Z` → `1405/08/28`-style Jalali with Latin digits; en → `Nov 19, 2026`; money null → `—`; tokens and duration as the ledger writes them |
| `public-index.test.ts` | load `public/index.html` in jsdom: stored `fa` → `/fa/`; no store + `navigator.languages = ['fa-IR']` → `/fa/`; `['de-DE']` → `/en/`; a throwing `localStorage` → still redirects |
| `components/layout/locale-switcher.test.tsx` | `/fa/cost/` → `/en/cost/`, query string kept, choice stored |
| `features/runs/**/*.test.tsx` | list renders plan title + task id, project chip filters, KPI sums; empty state; detail renders stages in order and the rounds table only when present; not-found state on 404 |
| `features/cost/**/*.test.tsx`, `features/quality/**/*.test.tsx`, `features/imports/**/*.test.tsx` | each page renders its sections from a mocked API, its empty state and its error state with retry |
| `app/locale-layout.test.tsx` | fa → `dir="rtl" lang="fa"`, en → `dir="ltr" lang="en"` |

Mock at the feature's API object; wrap with `NextIntlClientProvider` and the locale's messages.

## 8. Stop and ask

All of the outline's questions are answered (preview v3). Stop and ask if:

- Next.js 16 or next-intl 4 cannot produce the static export of §6.11 (pass-through root layout,
  `[locale]` with `generateStaticParams`, `public/index.html` as `/`) — do not invent another routing.
- A real ledger line, seen while running the owner checks, matches none of §6.7's patterns.
- A version pinned in §6.2 or §6.10 cannot be restored.

## 9. Phases

**Gates** (all phases): in `api/`: `dotnet build Orchestra.slnx` (0 warnings, 0 errors) and
`dotnet test Orchestra.slnx`; in `ui/`: `npm run typecheck && npm run lint && npm run test && npm run
build`. Report the real output and the test counts. Browser checks are the **owner's**; the
implementer does not start a dev server or open a browser unless the owner asks.

- **A — skeleton.** Branch. Files 1–22, 38, 41–51 and 51a (its tests). File 57 is done before this phase, at gate 2. Feature pages as `PageHeader` + `EmptyState`
  stubs. Files 53–55; `HealthController` (`GET api/health` → `{ status: "ok" }`); one test per side that
  proves the harness (`ErrorMessagesTests`, `messages.test.ts`, `architecture.test.ts`,
  `public-index.test.ts`, `formatter.test.ts`, `locale-switcher.test.tsx`, `locale-layout.test.tsx`).
  **Gate.** **Owner checks:** `npm run build` then `dotnet run` → `http://localhost:3500` opens in the
  browser's language; switch language and theme; reload keeps both; `/xyz` shows the 404 page;
  `/api/xyz` returns ProblemDetails. Stop — show the owner the diff.
- **B — data and import.** Files 23–28, 30–35, 40 and their tests. **Gate.** **Owner checks:** with
  `appsettings.Local.json` pointing at both real `orchestrator/runs` folders, start the API and call
  `POST /api/import`: both projects import with **0 errors**; Project A's `T-WEB-001` yields three runs;
  call it again — the same numbers. Stop.
- **C — runs screens.** Files 29, 36, the `runs` and `imports` features. **Gate.** **Owner checks:**
  the runs, run-detail and import-log items of «نتیجه». Stop.
- **D — cost and model quality.** File 37, the `cost` and `quality` features. **Gate.** **Owner
  checks:** the cost and quality items of «نتیجه». Stop.
- **E — Docker and README.** Files 52, 56. **Gate** + `docker compose -f deploy/docker-compose.yml
  build`. **Owner checks:** `docker compose up` with `.env` pointing at both runs folders; the same
  pages as in D; confirm the mounts are read-only (`docker inspect`). Stop.

## 10. Definition of done

- All phases' gates green; test counts recorded in `.ai/ci.md`.
- Both real ledgers import with zero errors, and a second import changes nothing.
- No owner path, real run data or secret in any committed file (`git grep -i "Project ProjectA\|Project ProjectB\|F:\\\\"`
  over `api/ ui/ deploy/` finds nothing).
- `.ai/decisions/0001`–`0003` changed from `Proposed` to `Accepted` with the gate-2 date (file 57).
- `.ai/context/session-*.md` written; this header: `Status: Done`, the `Done:` date, the commit and
  the merge.

## 11. Plan reviews — where each point went

### Round 1

| Point | Fixed in |
|---|---|
| B1 seven vs eight tables | §6.6 and file 24 → eight; plan 0002 file 21 → nine |
| B2 `PlanFamily` has no source | dropped from `RunDetailDto` and from `data-model.md` (plan 0004 may bring it back from its events) |
| B3 `CostUsd` vs `CostMicros` | `data-model.md`: every money column is `CostMicros` (D18) |
| ADRs 0001–0003 stay Proposed | file 57, §10 |
| `Run.Status` set undefined | §6.8: raw word, distinct values in the filter, case-insensitive |
| UI versions not pinned | §6.10: exact versions from Project B's `Ui/package-lock.json` (round 3) |
| `TimeProvider` not registered | §6.3 |
| UI tests not in §5 | file 51a |
| minor: `TotalCostMicros`, two `findingsShare` meanings, `ui/out/`, `Updated` note, `approved`, display-only root, case-insensitive status | §6.7; §6.9 `finalFindingsShare`; `.gitignore` line; header; §6.7; §6.7; §6.8 |

### Round 2

| Point | Fixed in |
|---|---|
| UI versions still unpinned | fixed in round 3 |
| "all applied" vs an open row | header and §11 headings |
| file 57 order and phase; file 51a missing in phase A | §5 order, phase `gate 2`; §9 phase A |

### Round 3

| Point | Fixed in |
|---|---|
| UI versions still unpinned | §6.10 |
| `Base` stale (`ea33554`) | header: the commit with the review fixes; `7296504` is not enough, because the round 1 fixes to `data-model.md` are not in it; the hash is filled in at the commit, before gate 2 |
| file 57 phase label, nested brackets in phase A, round 2 row | §5 file 57 → `gate 2`; §9 phase A reworded; round 2 row |

### Round 4

| Point | Fixed in |
|---|---|
| `cn` excluded but `utils.ts` needs it | §6.10: `cn` 0.4.0 kept; `utils.ts` content named (checked in Project B: `export { cn } from 'cn'`, no `clsx`/`tailwind-merge`) |
| `shadcn` in dependencies | kept as Project B has it (copy, decide nothing new) |
| `use-intl` duplicate | §6.10 note: one copy |
| §11 round 3 `Base` row | says when the hash is filled in |
