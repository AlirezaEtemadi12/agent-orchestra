# Plan 0012 — A local app that sets a project up by itself

**Status:** Done
**Created:** 2026-11-24 · **Updated:** 2026-11-24 · **Done:** 2026-11-24
**Preview:** none — the owner asked for the plan directly (2026-11-24); the screens are described in §6.5. If
the reviewer finds them too vague to build without guessing, the preview is made before phase D.
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-24. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2, 2026-11-24 (review pasted by the owner): 6 must-fix (M1–M6), 4 should-fix (S1–S4), 4 minor, all verified against the code and applied in this version (D13, D14, D16, §5 rows 9, 14, 16, 22, 29, 35, 37–39, §6.1, §6.3.1, §6.3.3, §6.6, §6.7, §7, §8, §9)
**Decided by:** owner in the chat, 2026-11-24 (plan approved after the gate-2 fixes; "no server", "simple for me first", "adding a project must do
every step itself"). Every choice below is already decided — **decide nothing new.** If reality contradicts
this plan, STOP and ask.
**Branch:** `alireza/local-app`. The implementer creates it. **Commit:** `461328f` on `alireza/local-app`
**Merge:** `c74b46c` into `main`, 2026-11-24 (not pushed)
**Base:** `bdc8840` (the plan's own commit on `main`, after the plan 0011 merge `f75781b`), 2026-11-24. Phase C edits the offline suite,
so do not start before that work is merged; record the base hash here when you start.
**Companion:** None. Project A and Project B are set up again from the app (phase D, owner checks), not by
a companion plan.
**Steps changed:** no preview (see above).
**Verified:** Claude Opus 5.5, 2026-11-24, after the implementation. Gates were re-run: api 300 tests with 0 warnings; ui typecheck, lint, 83 tests and build; engine 146/146. `scripts/publish.ps1` ran and the zip holds the four files of §7. Through the published exe, the following were checked:
health and HTML; one instance; Origin → 403; `C:\Windows` and `%TEMP%` refused; setup of a single repo and of a two-repo workspace (all rows created, a second plan all `exists`, `run.ps1 agents` exit 0, `.orchestra/` a repo with no commit, the local json git-ignored); a `-DryRun` event reached the dashboard; closing the window stops the app.
**Fixed in verification:** `EngineLocator.TemplatesRoot` pointed at `<root>/templates/project` instead of `<root>/engine/templates/project`. As a result, setup found no templates and answered `engineFound: false`, both in the exe and in `dotnet run`. `EngineLocatorTests` had asserted the wrong path; it now asserts the right one.
The owner's first "Check setup" on Project A turned up three more bugs, all fixed:
(1) The setup screen sent each repo's commands as the textarea text instead of a list, so every plan after the first, and every apply, failed with `400 One or more validation errors`. The type is now `Record<string, string[]>`, and `setup.test.tsx` covers it.
(2) `.orchestra/` counted as a repo once D8 made it one, so setup planned `.orchestra/AGENTS.md` and `plans/.orchestra/`. `FolderCheck` and `SetupPlanner.NormalizeRepos` now leave it out; tests in `FolderCheckTests` and `SetupPlannerTests`.
(3) The runs board still showed "Live events are off" when `Orchestra:ReportToken` was unset, but since plan 0009 D6 the dashboard keeps its own token, so the reporter is always on. The `reporter` field, its banner, its two strings and `The_reporter_says_whether_a_token_is_configured` are removed.
**Left for the owner:** the window checks of «نتیجه» (folder dialog, both languages and themes), and "Check setup" for Project A and Project B, which also closes plan 0007. The data folder's `projects.json` holds **one** project; the checklist expects two. The publish prints one SDK warning, `NETSDK1236` (`Orchestra.Api.runtimeconfig.json` left out of the bundle); the exe runs anyway.

---

## فهم من از درخواست

Orchestra دیگر سرور نیست. یک برنامه‌ی محلی است که روی همان ویندوزی اجرا می‌شود که پروژه‌ها و موتور روی
آن هستند، و هدف اول این است که برای خود شما ساده و قابل‌استفاده باشد.

- **یک فایل exe با پنجره‌ی خودش.** Orchestra یک برنامه‌ی Photino.NET می‌شود: همان API و همان UI فعلی، داخل
  یک پنجره‌ی مستقل (WebView2 روی ویندوز). با دوبار کلیک روی `Orchestra.exe` پنجره باز می‌شود و با بستن
  پنجره برنامه بسته می‌شود. یک اسکریپت publish خروجی self-contained ویندوز (`win-x64`) می‌سازد. Docker دیگر لازم نیست و پوشه‌ی `deploy/`
  حذف می‌شود.
- **انتخاب پوشه با دیالوگ خود سیستم‌عامل.** داخل پنجره، دکمه‌ی «انتخاب» دیالوگ واقعی انتخاب پوشه را باز
  می‌کند. اگر UI در مرورگر معمولی باز شده باشد (حالت توسعه)، همان مرورگر پوشه‌ی فعلی داشبورد می‌ماند.
- **افزودن پروژه همه‌ی کارها را خودش انجام می‌دهد.** شما پوشه را انتخاب می‌کنید و Orchestra فهرست کارهایی
  را که انجام خواهد داد نشان می‌دهد: ساختن `.orchestra/` با workflow، قالب پلن، promptها و نقش‌ها،
  `orchestrator.json` (با بلوک `report` و دستورهای build و test که خودش از روی ریپوها حدس می‌زند و شما
  می‌توانید همان‌جا عوضشان کنید)، `orchestrator.local.json` با مسیر موتور، مسیر پروفایل‌ها و توکن، shim
  موتور، و اگر نبودند، اسکلت `AGENTS.md` و `.ai/README.md`. شما «راه‌اندازی» را می‌زنید و پروژه آماده و وصل
  است. نه paste لازم است، نه متغیر محیطی، نه پرامپت.
- **برای پروژه‌های موجود** (Project A و Project B) همان صفحه فقط چیزهایی را که کم است اضافه یا درست
  می‌کند، مثلاً بلوک `report` یا مسیر تازه‌ی پروفایل‌ها.
- **مرز نوشتن:** فقط فایلی ساخته می‌شود که وجود ندارد. دو فایل موجود ممکن است تغییر کنند و در فهرست نشان
  داده می‌شوند: `orchestrator.json` (فقط اضافه شدن بلوک `report` اگر نباشد) و `orchestrator.local.json`
  (فقط سه کلید). هیچ فایلی پاک نمی‌شود و هیچ commitی انجام نمی‌شود.
- **داده‌های فعلی حفظ می‌شوند.** پایگاه داده، فهرست پروژه‌ها، پروفایل‌ها و توکن از Docker به پوشه‌ی محلی
  منتقل می‌شوند، پیش از آن‌که `deploy/` حذف شود. توکن همان می‌ماند تا پروژه‌ها بدون تغییر رویداد بفرستند.

دو جمله که دو معنی داشتند:
- «multi-platform»: فعلاً فقط ویندوز (تصمیم شما، 2026-11-24). موتور با Windows PowerShell 5.1 نوشته شده و
  خروجی لینوکس بدون آن فقط داشبورد بود. کد برنامه چیزی مخصوص ویندوز اضافه نمی‌کند (Photino روی لینوکس و مک هم
  هست)، تا بعداً فقط موتور و اسکریپت publish کار داشته باشند. آن کار در نقشه‌ی راه است
  ([`engine-cross-platform.md`](../ideas/engine-cross-platform.md)).
- «همه‌ی مراحل خودکار»: یعنی ساختن اسکلت و وصل کردن. نوشتن **محتوای** دانش پروژه (`.ai/`) کار یک ایجنت است؛
  پرامپتش بعد از راه‌اندازی به‌صورت اختیاری نشان داده می‌شود و شرط وصل شدن نیست.

این پلن کارهای زیر را **نمی‌کند**:
- داشبورد همچنان پلن تأیید نمی‌کند، run شروع نمی‌کند و به موتور فرمان نمی‌دهد (ADR 0002 و بخش فرمان ADR
  0003 سر جایشان هستند).
- چیزی commit نمی‌کند و git را جز یک `git init` برای `.orchestra/` در workspace بدون repo لمس نمی‌کند.
- hosting، sign-in، دیدن از گوشی و جواب دادن ریموت را نمی‌سازد؛ همه در ایده‌ها هستند.
- نصب‌کننده (installer)، امضای کد و به‌روزرسانی خودکار نمی‌سازد: خروجی یک پوشه‌ی zip است که باز می‌شود و
  اجرا می‌شود.

## جریان کار

0. **ساختن خروجی (یک بار، یا بعد از هر تغییر کد):** `scripts/publish.ps1` را اجرا می‌کنید؛ در
   `dist/Orchestra-win-x64/` پوشه‌ی آماده و کنارش zip آن ساخته می‌شود.
1. **اجرا:** روی `Orchestra.exe` دوبار کلیک می‌کنید. پنجره‌ی Orchestra باز می‌شود. اگر Orchestra از قبل
   باز باشد، پیام «Orchestra از قبل باز است» می‌آید و برنامه‌ی دوم بسته می‌شود.
2. **افزودن پروژه:** «Projects ← New project». مسیر را paste می‌کنید یا با «انتخاب» از دیالوگ سیستم‌عامل
   پیدا می‌کنید. اسم، id و ریپوها خودشان پر می‌شوند (همان رفتار امروز).
3. **«ادامه»:** صفحه فهرست کارها را نشان می‌دهد. هر سطر یک فایل یا کار است با برچسب «ساخته می‌شود»،
   «اضافه می‌شود»، «به‌روز می‌شود» یا «هست». سطرهای دانش (`AGENTS.md`، `.ai/README.md`) تیک دارند و
   می‌شود برشان داشت. زیرش برای هر ریپو دستورهای build و test حدس‌زده‌شده هست و قابل ویرایش است.
4. **«راه‌اندازی»:** کارها انجام می‌شوند، نتیجه‌ی هر سطر (انجام شد / خطا با دلیل) نشان داده می‌شود و پروژه
   ثبت و import می‌شود. جلوی پروژه «منتظر اولین رویداد» نوشته شده و با اولین run سبز می‌شود. یک جعبه‌ی
   اختیاری پرامپت پر کردن `.ai/` را برای ایجنت می‌دهد.
5. **دوباره اجرا کردن** همان کار امن است: همه‌ی سطرها «هست» می‌شوند و چیزی نوشته نمی‌شود.
6. **پروژه‌ی موجود:** در فهرست پروژه‌ها، دکمه‌ی «بررسی راه‌اندازی» همان صفحه‌ی قدم ۳ را برای آن پروژه باز
   می‌کند.
7. **خطا:** پوشه‌ای که نیست، ریشه‌ی درایو، پوشه‌های ویندوز و Program Files، خود checkout Orchestra و پوشه‌ی
   داده‌ی Orchestra رد می‌شوند و پیام روشن می‌گیرند. اگر موتور پیدا نشود، «راه‌اندازی» غیرفعال است و دلیلش
   نوشته شده.

فازها:
- **A:** تصمیم‌ها و مستندات (ADR 0009 و اصلاح قانون‌های قدیمی)، انتقال داده از Docker، حذف `deploy/`.
- **B:** API: حذف تبدیل مسیر و حالت container، پوشه‌ی داده‌ی محلی، endpointهای راه‌اندازی، ایمنی.
- **C:** موتور: قالب‌های پروژه در `engine/templates/project/` و یک تست offline.
- **D:** UI: صفحه‌ی راه‌اندازی، حذف صفحه‌ی اتصال، «شروع کار» دو قدمی.
- **E:** پنجره و خروجی: پروژه‌ی `Orchestra.Desktop` با Photino، دیالوگ انتخاب پوشه، اسکریپت publish ویندوز.
- **F:** README فارسی و انگلیسی، `.ai/`، لاگ session.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

Orchestra یک exe با پنجره‌ی خودش است، داده‌های قبلی‌اش سر جایش است، و یک پوشه‌ی تازه با دو کلیک
(«ادامه» و «راه‌اندازی») پروژه‌ای آماده و وصل می‌شود که `run.ps1 health` آن سبز است.

چک‌لیست شما:
- Docker را متوقف کنید (`docker compose -f deploy/docker-compose.yml stop`) و دستورهای §6.1 را اجرا
  کنید. در `%LOCALAPPDATA%\Orchestra\` باید `orchestra.db`، `projects.json`، `report-token` و `profiles\`
  باشد.
- `scripts/publish.ps1` را اجرا کنید: `dist/Orchestra-win-x64.zip` ساخته می‌شود.
- `dist\Orchestra-win-x64\Orchestra.exe` را دوبار کلیک کنید: پنجره باز می‌شود؛ Runs همان تعداد قبل را
  دارد؛ Projects دو پروژه را نشان می‌دهد. یک بار دیگر کلیک کنید: پیام «Orchestra از قبل باز است» و
  چیز دیگری باز نمی‌شود. پنجره را ببندید: `http://localhost:3500/api/health` دیگر جواب نمی‌دهد.
- در New project روی «انتخاب» بزنید: دیالوگ انتخاب پوشه‌ی ویندوز باز می‌شود و مسیر انتخاب‌شده در فیلد
  می‌نشیند.
- یک پوشه‌ی خالی آزمایشی بسازید با یک `git init` و یک `package.json` که `scripts.test` دارد. New project ←
  مسیر ← «ادامه»: فهرست `.orchestra/...` را «ساخته می‌شود» نشان می‌دهد و دستور `npm run test` حدس زده شده.
  «راه‌اندازی»: همه سبز. در آن پوشه `powershell -File .orchestra\run.ps1 health` بدون خطا تمام می‌شود.
- دوباره «بررسی راه‌اندازی» همان پروژه: همه‌ی سطرها «هست»؛ «راه‌اندازی» چیزی نمی‌نویسد.
- یک workspace آزمایشی با دو زیرپوشه که هر کدام repo هستند: `.orchestra/` خودش repo می‌شود (`git init`)،
  ولی هیچ commitی ندارد؛ `plans/` برای هر ریپو یک پوشه دارد.
- «بررسی راه‌اندازی» برای Project A: بلوک `report` اگر نبود «اضافه می‌شود»، `profilesPath` «به‌روز می‌شود».
  بعد از راه‌اندازی یک run یا `run.ps1 new … -DryRun` بزنید: رویداد روی Runs board می‌آید.
- مسیر `C:\Windows` و `C:\` را امتحان کنید: پیام رد شدن.
- از یک تب دیگر با `curl -X POST http://localhost:3500/api/projects/setup/apply` بدون هدر Origin: پاسخ 403.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md): branch before the first edit; never commit/push/merge unless
   asked in that message; one phase = one uncommitted diff.
2. **The dashboard writes into a project only through the setup feature (§6.3), and only what §4 D7 allows.**
   No other handler gains a write into a project folder.
3. The engine never calls the API to get anything (ADR 0002). Reporting stays fire-and-forget.
4. **A secret never goes into a prompt or a response body.** The report token is written to the project's
   git-ignored `orchestrator.local.json` by the server; no endpoint returns it (the connect endpoint that did
   is deleted).
5. **No private content enters this repo.** Templates (phase C) are written from this repo's own public
   docs and the outlines in §6.4 — never copied from Project A or Project B. Examples use `F:/Projects/MyApp`.
6. Persian strings are written in the fa catalogue exactly as given in §6.6; never machine-translated from
   the en ones (ADR 0004).
7. Phase A's data move is the **owner's** step (§6.1). Do not delete `deploy/` until the owner says the move
   is done.

### Baseline

Before the first edit: `dotnet build` + `dotnet test` in `api/` (zero warnings), `npm run typecheck`,
`lint`, `test`, `build` in `ui/`, and the engine offline suite
(`powershell -NoProfile -File engine/tests/offline/run-offline.ps1`, in the background with a log). Record the
three counts in the hand-over. Tests deleted with the code they cover (§7) are listed by name; no other
count may drop.

## 1. Goal

Orchestra is a self-contained desktop app (Photino.NET window around the existing API and UI) published for
Windows, keeps its existing data, and turns a folder into a ready, connected project without the
owner editing any file.

## 2. Out of scope

- An installer, code signing, auto-update, a tray icon — idea [`desktop-window.md`](../ideas/desktop-window.md).
- Linux and macOS builds, and the engine on PowerShell 7 — roadmap "Later", idea
  [`engine-cross-platform.md`](../ideas/engine-cross-platform.md). Nothing Windows-only is added to the app
  code (no WinForms, no registry), so that work stays in the engine and the publish script.
- Hosting, sign-in, HTTPS, phone access — plan 0008 is dropped; ideas [`hosting.md`](../ideas/hosting.md),
  [`mobile-view.md`](../ideas/mobile-view.md).
- Answering an agent from the dashboard — idea [`remote-answers.md`](../ideas/remote-answers.md).
- Writing the *content* of `.ai/` (an agent's job; the optional prompt stays).
- Changing how the engine runs, merges profiles or reports (only templates and one test are added).
- Gridify tables (plan 0010), git-call reduction (plan 0011).

## 3. Rules that apply

- [ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md) — unchanged.
- [ADR 0003](../../.ai/decisions/0003-read-only-dashboard-first.md), [ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md),
  [ADR 0008](../../.ai/decisions/0008-host-paths-and-own-report-token.md), [ADR 0001](../../.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md) —
  amended by the new ADR 0009 (§6.2).
- [ADR 0004](../../.ai/decisions/0004-persian-and-english.md), [ADR 0006](../../.ai/decisions/0006-events-are-the-dashboards-own-record.md)
  (events cannot be re-imported, hence the data move), [ADR 0007](../../.ai/decisions/0007-profiles-exported-to-a-file.md).
- `.ai/api/*`, `.ai/ui/*` conventions as today.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | **Local only.** Docker, `deploy/`, path mapping and the container mode are deleted. Hosting becomes an idea; plan 0008 is `Dropped` ("the owner wants no server, 2026-11-24"). |
| D2 | **A desktop host, `api/src/Orchestra.Desktop`** (§6.7): a new project (`Microsoft.NET.Sdk.Web`, `OutputType` `WinExe`, assembly name `Orchestra`) that references `Orchestra.Api` and `Photino.NET` (newest stable 3.x, pinned in `Directory.Packages.props`). It builds the same web host through a new public `OrchestraHost.Create(args, contentRoot)` that `Orchestra.Api/Program.cs` also uses, starts it (`StartAsync`), opens one `PhotinoWindow` on `http://localhost:3500/`, waits for the window to close, then stops the host. `Orchestra.Api` stays a plain web app for `dotnet run` in development and for the tests; it gains no Photino reference. |
| D2a | **One instance.** Before building the host, the desktop app calls `GET http://localhost:3500/api/health` (2 s). An answer → a Photino message "Orchestra is already open." and exit code 0. No answer but the port is taken → a message naming the port and exit code 1. |
| D2b | **Publish = `scripts/publish.ps1`**, `win-x64` only: `npm ci` (only when `ui/node_modules` is missing) and `npm run build` once; `dotnet publish api/src/Orchestra.Desktop -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/Orchestra-win-x64`; copy `ui/out` to `dist/Orchestra-win-x64/wwwroot`, `engine/` (without `tests/`) to `dist/Orchestra-win-x64/engine`, and `api/src/Orchestra.Api/appsettings.json` to `dist/Orchestra-win-x64/` (it holds `Urls=http://localhost:3500`; without it Kestrel takes its default port and the window stays blank); zip to `dist/Orchestra-win-x64.zip`. `dist/` is git-ignored. |
| D2c | **Content root = the exe's folder** (`AppContext.BaseDirectory`), set explicitly in `OrchestraHost.Create` for the desktop app, so `wwwroot` and `engine/` beside the exe are found wherever the app is started from. |
| D3 | **Data folder = `<LocalApplicationData>/Orchestra/`** (`Environment.GetFolderPath(SpecialFolder.LocalApplicationData)`: `%LOCALAPPDATA%`). An empty `DatabasePath` (the new default) resolves to `<that>/orchestra.db`; a set one works as today. `projects.json`, `report-token`, `profiles/` already sit beside the database and follow it. `appsettings.Development.json` sets `"DatabasePath": "data/orchestra.db"` so `dotnet run` in development keeps today's folder. |
| D3b | **Folder dialog.** In the desktop window the UI sends `{"type":"pickFolder","start":"<path or empty>"}` through `window.external.sendMessage`; the host answers with `{"type":"pickedFolder","path":"<path or empty>"}` from `PhotinoWindow.ShowOpenFolder`. The UI uses it when `window.external?.sendMessage` exists, else the in-app folder browser of today (development in a normal browser). |
| D4 | **Data moves out of Docker before `deploy/` goes** (§6.1, owner's step). A token configured in `deploy/.env` (`Orchestra__ReportToken`) is written to `report-token`, so every project's existing `reportToken` keeps working. |
| D5 | **Deleted:** `PathMapper`, `HostingInfo`, `IHostingReader`, `PathMap`/`PublicUrl` options, `GET api/system/hosting`, `GET api/projects/{id}/connect`, the `NotVisible` errors, the UI connect page and hosting line. `BrowseRoots` and `GET api/system/folders` stay (direct mode only, no mapper). Paths are used as typed. |
| D6 | **Setup is one feature in the Projects module:** `POST api/projects/setup/plan` (dry run, never writes) and `POST api/projects/setup/apply` (recomputes the plan server-side, then executes it). Same input for both (§6.3). Apply then creates or updates the project entry and imports it. |
| D7 | **Write policy.** Create a file only when it is missing. Exactly two existing files may change, and each shows as its own row: `.orchestra/orchestrator.json` gets a top-level `report` block **only when it has none** (text insert after the first `{`, the rest of the file byte-identical); `.orchestra/orchestrator.local.json` gets `enginePath`, `profilesPath`, `reportToken` set when missing or different, other keys kept (parsed and re-written indented; it is a git-ignored machine file). Never delete, never overwrite anything else, never commit. Every write is temp-file then move. |
| D8 | **`git init`** runs only in `.orchestra/` and only when the root has no `.git` (a multi-repo workspace, as Project A and B). No commit, no config, no remote. When the root is itself a repo, `.orchestra/` is part of it and its own `.gitignore` (inside `.orchestra/`) keeps runs and secrets out — the root `.gitignore` is never touched. |
| D9 | **Templates** live in `engine/templates/project/` (§6.4), written for this repo, synthetic, English. The setup copies them; it never generates prose. |
| D10 | **Two config shapes** (§6.3.4): single repo (root is a repo: `repos = { "<Name>": "." }`, no `{repo}` in paths) and multi-repo (`{repo}` in `plansDir`, `agentsDoc`, `indexDoc`). Prompts and roles are **one shared set** in both (`.orchestra/prompts`, `.orchestra/roles`), so a new project has one copy to edit. `profile` is `default`, so chains, tiers and pools come from the profile. |
| D11 | **`verifyCommands` are detected, then editable in the form.** Per repo folder: a `*.slnx` or `*.sln` at its root → `dotnet build`, `dotnet test`; a `package.json` → `npm run <s>` for each of `typecheck`, `lint`, `test`, `build` present in `scripts`, in that order; both → dotnet first. Nothing found → empty list. What the form sends is what is written. |
| D12 | **`enginePath`** = `Orchestra:EnginePath` when set, else the first folder found by walking up from the content root that contains `engine/run.ps1` (the published app finds its own `engine/` copy; `dotnet run` finds the checkout). A developer who edits the engine sets `Orchestra:EnginePath` to the checkout in `appsettings.Local.json` beside the exe, so "Check setup" does not repoint existing projects to the copy; the `update` row shows the change either way. Not found → the plan answers `engineFound: false` and apply returns 400 `Projects.Setup.EngineNotFound`. **`profilesPath`** = `ResolveProfilesPath`. **Report URL** = the first `localhost` address in `Urls`, + `/api/events` (default `http://localhost:3500/api/events`). |
| D13 | **Safety.** `AllowedHosts` becomes `localhost;127.0.0.1;[::1]`; `Urls` stays loopback. Both setup routes require an `Origin` header whose host is `localhost`, `127.0.0.1` or `[::1]` and whose port is the request's port; anything else (missing included) → 403 `Projects.Setup.Origin`. `Urls` on a non-loopback address is unsupported (ADR 0009 says so): the Origin rule accepts only the three loopback names, so setup then refuses every request. Refused roots (400 `Projects.Setup.PathRefused`): not rooted, missing, a drive root, under `%WINDIR%`, `%ProgramFiles%`, `%ProgramFiles(x86)%`, `%ProgramData%`, `%LOCALAPPDATA%`, `%APPDATA%`, the engine folder (D12) or the data folder (D3). |
| D14 | **The scaffold prompt shrinks** to the knowledge part (today's sections 1–3: existing instruction files, `.ai/`, `AGENTS.md`), returned in `SetupResultDto.KnowledgePrompt` and shown after a successful apply as an optional copy box. Section 4 (the config) is deleted: the setup writes it. "Nothing missing" now means `.ai/` and `AGENTS.md` are there (the config no longer counts); its text becomes `Nothing to create: .ai/ and AGENTS.md are already there.` and the result screen hides the box when the prompt is that text. The closing line becomes `4. Commit nothing. Tell me what you created and what you could not verify.` The route `POST api/projects/scaffold-prompt`, `ScaffoldPromptHandler`, the UI `api.ts` call and `useScaffoldPrompt` are **deleted**: the prompt has no other caller. |
| D15 | **Existing projects** use the same screen through "Check setup" on the projects table; nothing about them is special-cased. A row the owner unticks is skipped and stays "missing" next time. |
| D16 | **Getting started** has two steps: "Orchestra is running" (always done when the page loads) and "Set up your first project" (done when one project has an event). The Docker and mapping texts are deleted. `startSteps` takes `{ projects }` only and returns `{ project, connected, pending }`; `dashboard`, `run`, `docker`, the `hosting` input and `runCount` are removed, and `start/queries.ts` stops reading hosting and the run count. |

## 5. File manifest

| # | File | NEW/EDIT/DELETE |
|---|---|---|
| 1 | `.ai/decisions/0009-local-app-sets-projects-up.md` | NEW (phase A) |
| 2 | `.ai/decisions/0001-…`, `0003-…`, `0005-…`, `0008-…` | EDIT — status line "Amended by 0009" + the sentences in §6.2 |
| 3 | `.ai/decisions/README.md` | EDIT — row for 0009 |
| 4 | `.ai/vision/roadmap.md` | EDIT — phase 3 removed, 0012 in the order |
| 5 | `AGENTS.md` | EDIT — Status, Shape, rule 3 (§6.2) |
| 6 | `.orchestra/plans/0008-hosting.md`, `.orchestra/plans/README.md` | EDIT — 0008 `Dropped`; 0012 row status |
| 7 | `deploy/` (whole folder) | DELETE — after the owner confirms §6.1 |
| 8 | `api/src/Orchestra.Api/Common/Hosting/PathMapper.cs`, `HostingInfo.cs` | DELETE |
| 9 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs`, `OrchestraOptionsValidator.cs` | EDIT — drop `PathMap`, `PublicUrl`, `PathMapEntry`; add `EnginePath`; D3 default and resolution. The validator's "`Orchestra:DatabasePath` must not be empty" check is deleted (empty is now the default); the `UiPath` check stays |
| 10 | `api/src/Orchestra.Api/Common/Hosting/LocalPaths.cs` | NEW — `LooksAbsolute`, `Normalize`, `Covers` moved from `PathMapper` (used by folders and setup) |
| 11 | `api/src/Orchestra.Api/Modules/Setup/*` | EDIT — no mapper, no hosting route; `HostingHandler.cs` DELETE |
| 12 | `api/src/Orchestra.Api/Modules/Projects/ProjectInput.cs`, `ProjectRegistry.cs`, `ProjectStore.cs`, `Features/CreateProject/*`, `Features/UpdateProject/*`, `Features/CheckFolder/*` | EDIT — no mapper; `FolderCheck` doc comment no longer says "never a write" but "the check never writes; setup does (ADR 0009)" |
| 13 | `api/src/Orchestra.Api/Modules/Projects/Features/Connect/` | DELETE |
| 14 | `api/src/Orchestra.Api/Modules/Projects/Features/Setup/SetupPlanner.cs`, `SetupApplier.cs`, `SetupDtos.cs`, `SetupGuard.cs`, `VerifyCommandDetector.cs`, `EngineLocator.cs`, `TemplateSet.cs` | NEW (§6.3). `SetupApplier` depends on `ReportTokenStore` (the token) and on `ProjectRegistry` + `ImportService` (the entry); `TemplateSet` takes its root folder in the constructor (the installer passes `<engine>/templates/project`) |
| 15 | `api/src/Orchestra.Api/Modules/Projects/ProjectsController.cs`, `ProjectsModuleInstaller.cs` | EDIT — two setup routes, connect route removed |
| 16 | `api/src/Orchestra.Api/Modules/Projects/Features/ScaffoldPrompt/ScaffoldPromptBuilder.cs`; `api/tests/Orchestra.Api.Tests/Projects/ScaffoldPromptBuilderTests.cs` | EDIT — D14; the test changes are listed in §7 |
| 17 | `api/src/Orchestra.Api/Common/Errors/Error.cs`, `ErrorMessages.cs` | EDIT — 3 new codes (en + fa, §6.6), `NotVisible` codes removed |
| 18 | `api/src/Orchestra.Api/appsettings.json`, `appsettings.Development.json`, `appsettings.Local.example.json` | EDIT — D3, D13 |
| 19 | `api/tests/Orchestra.Api.Tests/Common/PathMapperTests.cs`, `Projects/ConnectHandlerTests.cs`, `Setup/HostingHandlerTests.cs` | DELETE (listed in the hand-over) |
| 20 | `api/tests/Orchestra.Api.Tests/Projects/Setup*Tests.cs`, `Common/LocalPathsTests.cs` | NEW (§7) |
| 21 | `api/tests/Orchestra.Api.Tests/Fixtures/Fixtures.cs`, `Projects/ProjectsFixture.cs`, `Setup/FoldersHandlerTests.cs` | EDIT — no mapper |
| 22 | `engine/templates/project/**` | NEW (§6.4) — in phase B: the planner and its tests read them |
| 23 | `engine/tests/offline/` — a new case file and its line in `run-offline.ps1` | NEW/EDIT (§7) |
| 24 | `engine/examples/orchestrator.json` | EDIT — the `note` says new projects are set up from the app |
| 25 | `ui/src/features/projects/components/setup-step.tsx`, `setup-result.tsx` | NEW (§6.5) |
| 26 | `ui/src/features/projects/components/project-form.tsx`, `projects-table.tsx`, `api.ts`, `queries.ts`, `types.ts`, `index.ts`, `i18n/en.ts`, `i18n/fa.ts`, `projects.manifest.ts` | EDIT |
| 27 | `ui/src/features/projects/components/connect-page.tsx`, `hosting-line.tsx`, `connect.test.tsx`, `ui/src/app/[locale]/settings/projects/connect/page.tsx`, `ui/src/lib/hosting/queries.ts` | DELETE |
| 28 | `ui/src/app/[locale]/settings/projects/setup/page.tsx` | NEW — `?id=` for an existing project, else from the form |
| 29 | `ui/src/features/start/**` (components, i18n) | EDIT — D16; state and tests in row 39 |
| 30 | `ui/src/features/projects/setup.test.tsx` | NEW (§7) |
| 31 | `api/src/Orchestra.Desktop/Orchestra.Desktop.csproj`, `Program.cs`; `api/Orchestra.slnx`; `api/Directory.Packages.props` (Photino.NET) | NEW/EDIT (§6.7) |
| 32 | `README.md`, `README.fa.md`, `.ai/README.md`, `.ai/architecture/overview.md`, `.ai/api/testing.md`, `.ai/ui/design.md` | EDIT — Docker and mapping text removed; publish, the exe, `Orchestra:EnginePath` for a developer, and setup described |
| 33 | `api/src/Orchestra.Api/Extensions/OrchestraHost.cs` NEW; `api/src/Orchestra.Api/Program.cs` EDIT | §6.7 |
| 34 | `ui/src/lib/desktop/bridge.ts`, `bridge.test.ts` | NEW (§6.7) |
| 35 | `scripts/publish.ps1`, `.gitignore` | NEW / EDIT (§6.7) — one `dist/` line covers the folders and the zips, on purpose |
| 36 | `api/tests/Orchestra.Api.Tests/OrchestraHostTests.cs` | NEW (§7) |
| 37 | `.ai/ci.md` | EDIT — a "Baseline after plan 0012" table: the engine count written in phase C, the api and ui counts in phase E |
| 38 | `api/src/Orchestra.Api/Modules/Projects/Features/ScaffoldPrompt/ScaffoldPromptHandler.cs` and the `scaffold-prompt` action in `ProjectsController.cs`; `ui/src/features/projects/api.ts` call and `useScaffoldPrompt` in `queries.ts` | DELETE (D14) — the UI part in phase D |
| 39 | `ui/src/features/start/start.test.tsx`, `start-steps.ts`, `queries.ts` | EDIT (D16, §7) |

## 6. Per-file specification

### 6.1 Moving the data out of Docker (owner, phase A)

Run with the container **stopped** (SQLite WAL):

```powershell
docker compose -f deploy/docker-compose.yml stop
$dest = Join-Path $env:LOCALAPPDATA 'Orchestra'
New-Item -ItemType Directory -Force $dest | Out-Null
docker cp orchestra-orchestra-1:/data/. $dest
if (Test-Path deploy/profiles) { Copy-Item -Recurse -Force deploy/profiles/* (Join-Path $dest 'profiles') }
```

Then, if `deploy/.env` has a non-empty `Orchestra__ReportToken`, write that value (no newline) to
`$dest\report-token`. Check: `orchestra.db`, `projects.json`, `report-token`, `profiles\default.json` exist.
The implementer prints these commands for the owner and waits; it does not run Docker itself. The paths
inside `projects.json` were stored as typed in Windows form (ADR 0008 item 1), so they need no change.

### 6.2 ADR 0009 and the amended rules

ADR 0009 "The local app sets a project up": Context (the onboarding cost, no server wanted); Decision = D1,
D3, D6–D8, D12, D13 in prose; Consequences (two existing files may change; the token is written by the
server; plan 0008 dropped; hosting would need a new ADR, since 0009 assumes loopback).

Amendments (the hand-over lists each as "removed/changed"):
- **ADR 0001:** "One container in production" → "One local process; the API serves the exported UI
  (ADR 0009)". The Docker sentence in Context stays as history.
- **ADR 0003:** Decision gains: "…and it does not edit project files **except through the setup feature
  of ADR 0009**." Everything about approving and starting runs is unchanged.
- **ADR 0005:** the clause that the dashboard never writes into a project folder points to ADR 0009.
- **ADR 0008:** items 1, 2, 3 and 6 and the consequences about Docker, IIS, `PublicUrl` and plan 0008 are
  marked "Superseded by 0009"; items 4 and 5 (own token, engine reads it from the local file) stay.
- **`AGENTS.md`:** Status line (0007 running in the projects, 0012 next); Shape row ("hosted locally" →
  "a desktop app: Photino.NET window around the API and the exported UI, published by `scripts/publish.ps1`"); Owner row drops Docker and personal server; rule 3 becomes
  "The dashboard shows and compares; it never approves, implements or starts runs, and it writes into a
  project only through setup (ADR 0003, 0009)".
- **Roadmap:** the Phase 3 row and section are removed; one line points to [`hosting.md`](../ideas/hosting.md).
  The "Later — Linux and macOS" section (added 2026-11-24) stays.

### 6.3 The setup feature (phase B)

#### 6.3.1 Input and output (`SetupDtos.cs`)

```csharp
public sealed record SetupRequest(
    string RootPath, string Name, string Id,
    IReadOnlyList<string> Repos,                                  // as the form has them; "." = root is the repo
    IReadOnlyDictionary<string, IReadOnlyList<string>>? VerifyCommands,  // null on the first plan call → detected
    IReadOnlyList<string>? Skip);                                 // item ids the owner unticked

public sealed record SetupItemDto(
    string Id,          // stable: "orchestra/workflow.md", "local-json", "report-block", "git-init", "knowledge/Ui/AGENTS.md"
    string Path,        // relative to the root, forward slashes
    string Action,      // "create" | "add" | "update" | "exists"
    bool Optional,      // true only for knowledge rows
    string? Detail);    // e.g. which local keys change; never the token

public sealed record SetupPlanDto(
    bool EngineFound, bool IsSingleRepo,
    IReadOnlyList<SetupItemDto> Items,
    IReadOnlyDictionary<string, IReadOnlyList<string>> VerifyCommands);

public sealed record SetupResultItemDto(
    string Id, bool Ok,
    string? Error,      // a code: Setup.Item.WriteFailed | Setup.Item.GitInitFailed | Setup.Item.InvalidJson | Setup.Item.Skipped
    string? Detail);    // the technical text (exception message, git's first stderr line), English, shown in mono

public sealed record SetupResultDto(IReadOnlyList<SetupResultItemDto> Items, ProjectSavedDto Project, string KnowledgePrompt);
```

#### 6.3.2 `SetupPlanner`

Pure apart from reading the file system. Order of items, each `create` or `exists` by file presence:

1. `git-init` (only when the root has no `.git`): `create` if `.orchestra/.git` is missing.
2. Every template file of §6.4 under `.orchestra/` (not `knowledge/`), plus, in the multi-repo shape, one
   `plans/<Repo>/.gitkeep` row per repo (the engine files plans under `plansDir` with `{repo}`).
3. `orchestra/orchestrator.json`: `create` when missing; when present and it has no top-level `report`
   property (parse with `JsonDocument`), an extra row `report-block` with `add`.
4. `local-json` (`.orchestra/orchestrator.local.json`): `create` when missing; `update` when any of the
   three keys is missing or different (`Detail` names the keys); else `exists`.
5. Knowledge rows (`Optional: true`): single repo → `AGENTS.md`, `.ai/README.md` at the root; multi-repo →
   the same two inside each repo. Only rows whose file is missing are `create`; present ones are `exists`.

`VerifyCommands`: the request's value when given, else `VerifyCommandDetector` (D11). `IsSingleRepo` is
`Repos == ["."]`.

#### 6.3.3 `SetupApplier`

1. `SetupGuard` checks the root (D13); then the planner runs again on the request (never trusts a plan the
   browser kept).
2. Rows with `exists` or in `Skip` are skipped (not reported as errors). Each other row runs in plan order;
   a failure is recorded and the next row still runs, **except** that a failed `git-init` or a failed
   `.orchestra/` folder creation stops everything after it: those later rows answer `Setup.Item.Skipped`.
   An existing `orchestrator.json` or local json that does not parse fails its row with
   `Setup.Item.InvalidJson` and is not touched.
3. `git-init`: `git init` with `-C <root>/.orchestra`, via `Process` with a 30 s timeout; a non-zero exit is
   the row's error (stderr's first line).
4. Template rows copy the file's bytes from `TemplateSet` (it reads `engine/templates/project/` under the
   engine folder of D12). `orchestrator.json` is rendered from §6.3.4, written UTF-8 without BOM, indented
   with 4 spaces.
5. `report-block`: insert `\n    "report": <block>,` right after the first `{` of the existing text; the
   block is the one in §6.3.4; the rest of the file is unchanged.
6. `local-json`: parse (or start from `{}`), set the three keys, write indented. `enginePath` comes from
   `EngineLocator` (D12), `profilesPath` from `OrchestraOptions.ResolveProfilesPath`, `reportToken` from
   `ReportTokenStore.Get()`, the same token `POST api/events` accepts.
7. Then create or update the project entry (`Id`, `Name`, `RootPath`, `RunsPath = <root>/.orchestra/runs`,
   `Repos`) through the existing registry and import it, exactly as `CreateProjectHandler` /
   `UpdateProjectHandler` do today (an existing id is updated, not refused). `Profile` is the `profile`
   value of the project's `orchestrator.json` after the writes, else `default` (the store's default).
8. `KnowledgePrompt` = `ScaffoldPromptBuilder.Build(...)` after the writes (D14).

#### 6.3.4 The written `orchestrator.json`

Multi-repo (`Repos = ["Ui","Web"]` here):

```json
{
    "profile": "default",
    "root": "..",
    "description": { "title": "<Name>", "repos": "  - Repos: Ui, Web" },
    "planFiling": "number",
    "repos": { "Ui": "Ui", "Web": "Web" },
    "plansDir": ".orchestra/plans/{repo}",
    "workflow": {
        "gatesPath": ".orchestra/workflow.md",
        "rolesPath": ".orchestra/roles",
        "rolesIndex": ".orchestra/roles/README.md",
        "promptsPath": ".orchestra/prompts",
        "agentsDoc": "{repo}/AGENTS.md",
        "indexDoc": "{repo}/.ai/README.md",
        "planTemplate": ".orchestra/plans/TEMPLATE.md"
    },
    "verifyCommands": { "Ui": ["npm run typecheck", "npm run test"], "Web": ["dotnet build", "dotnet test"] },
    "report": {
        "enabled": true,
        "url": "http://localhost:3500/api/events",
        "project": "<Id>",
        "tokenEnv": "ORCHESTRA_REPORT_TOKEN",
        "timeoutSec": 2,
        "progressEverySec": 10
    }
}
```

Single repo: `"repos": { "<Name>": "." }`, `"description.repos": "  - Repo: <Name>"`, `plansDir`
`.orchestra/plans`, `agentsDoc` `AGENTS.md`, `indexDoc` `.ai/README.md`, `verifyCommands` keyed by
`<Name>`. Everything else is identical.

#### 6.3.5 Routes and errors

`POST api/projects/setup/plan` → 200 `SetupPlanDto`, or 400 `Projects.Setup.PathRefused`.
`POST api/projects/setup/apply` → 200 `SetupResultDto`, 400 `Projects.Setup.PathRefused` /
`Projects.Setup.EngineNotFound` / the existing project validation errors, 403 `Projects.Setup.Origin`.
Both go through `SetupGuard` for the Origin rule.

### 6.4 Templates (phase C) — `engine/templates/project/`

| File | Content |
|---|---|
| `.gitignore` | `runs/`, `evidence/`, `orchestrator.local.json`, `*.tmp`, `graphify-out/`, each with a one-line comment (shape of the existing project `.gitignore`s, written fresh) |
| `run.ps1` | the shim, byte for byte the text below |
| `README.md` | one paragraph and a table: what `plans/`, `previews/`, `ideas/`, `research/`, `prompts/`, `roles/`, `runs/`, `evidence/` hold |
| `workflow.md` | this repo's [`../workflow.md`](../workflow.md) with these removed: the "Adapted from Project B" sentence, the phase-2 profile sentence, the `ci.md` reference (gates line becomes "the repo's build and test commands, as listed in `orchestrator.json` → `verifyCommands`"), and the `.ai/context/` closing section's link (kept as text) |
| `plans/README.md` | this repo's plans README up to the table, Orchestra-specific bullets (`api/ui/deploy`, companion repos) removed; an empty table with the same columns |
| `plans/TEMPLATE.md` | this repo's `TEMPLATE.md` unchanged |
| `ideas/README.md` | this repo's ideas README up to the table; empty table |
| `previews/.gitkeep`, `research/.gitkeep` | empty |
| `prompts/plan.md` | ≤ 30 lines: read the request and the repo docs; write the understanding, then the plan from `plans/TEMPLATE.md`; decide everything; name the files |
| `prompts/check-plan.md` | ≤ 30 lines: review a plan for gaps an implementer would have to guess; list findings as must-fix / should-fix / minor with the section each refers to |
| `prompts/implement.md` | ≤ 30 lines: implement exactly the plan phase; touch only manifest files; run the verify commands; report what was not verified |
| `prompts/verify.md` | ≤ 30 lines: check the diff against the plan and the verify commands' output; report every file outside the manifest |
| `roles/README.md` | four sections `## planner`, `## plan-reviewer`, `## implementer`, `## verifier`, 3–6 lines each (the engine reads these sections, `Get-RoleSection`) |
| `knowledge/AGENTS.md` | the pointer shape of this repo's `AGENTS.md`: a title line `# <Name> — Agent Entry Point` and three sections (What, Start here → `.ai/README.md`, `.orchestra/workflow.md`; The rules that bite first: empty list with a comment) — `<Name>` is replaced on copy |
| `knowledge/.ai/README.md` | `# Knowledge index` and an empty "doing X → read Y" table |

Prompts carry guidance only: the engine appends the answer protocol itself (`Get-RoleProtocol`). Files under
`knowledge/` are copied to the knowledge rows' paths, not into `.orchestra/`.

The shim (`run.ps1`):

```powershell
# Calls the shared engine in Orchestra with this project's orchestrator.json.
function Get-EnginePath {
    $local = Join-Path $PSScriptRoot 'orchestrator.local.json'
    if (-not (Test-Path -LiteralPath $local)) { return $null }
    $path = (Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json).enginePath
    if ($path -and (Test-Path -LiteralPath (Join-Path $path 'engine\run.ps1'))) { return $path }
    return $null
}
$engine = Get-EnginePath
if (-not $engine) {
    Write-Host 'Open Orchestra and run "Check setup" for this project: enginePath in .orchestra\orchestrator.local.json is missing or wrong.' -ForegroundColor Red
    exit 1
}
if ($args -contains '-Config') {
    Write-Host 'Do not pass -Config: the shim always uses this project''s orchestrator.json.' -ForegroundColor Red
    exit 1
}
& (Join-Path $engine 'engine\run.ps1') -Config (Join-Path $PSScriptRoot 'orchestrator.json') @args
exit $LASTEXITCODE
```

### 6.5 Screens (phase D)

- **New project** (`project-form.tsx`): unchanged fields and auto-fill; "Browse" opens the system folder
  dialog in the desktop window (D3b) and today's folder browser elsewhere; the hosting line is gone; the
  submit button reads "Continue" and goes to the setup step with the form's values (no save yet). The
  scaffold-prompt box on this page is removed.
- **Setup step** (`setup-step.tsx`, page `settings/projects/setup`): header "Set up <Name>" and the root
  path. A list, one row per item: a status chip (create / add / update / exists, neutral for exists),
  the path in mono, the detail under it; optional rows have a checkbox, others none. Under the list,
  "Build and test commands", one textarea per repo (one command per line), prefilled. Buttons: "Set up"
  (primary, disabled when `engineFound` is false or nothing is left to do; the reason shown beside it),
  "Back". The plan is fetched again when a checkbox or a command changes (debounced 400 ms).
- **Result** (`setup-result.tsx`): the same rows with ✓ or the error text; "Waiting for the first event"
  that turns green on the project's first event (the polling the connect page did, moved here); a
  collapsed "Fill the knowledge with an agent" box with the prompt and a copy button; "Done" → projects.
- **Projects table:** a row action "Check setup" opening the setup step with `?id=`; the "Connect" action
  is removed.
- **Getting started:** D16; each step a card as today.

### 6.6 Strings

| Key | en | fa |
|---|---|---|
| `projects.setup.title` | Set up {name} | راه‌اندازی {name} |
| `projects.setup.continue` | Continue | ادامه |
| `projects.setup.apply` | Set up | راه‌اندازی |
| `projects.setup.back` | Back | بازگشت |
| `projects.setup.action.create` | will be created | ساخته می‌شود |
| `projects.setup.action.add` | will be added | اضافه می‌شود |
| `projects.setup.action.update` | will be updated | به‌روز می‌شود |
| `projects.setup.action.exists` | already there | هست |
| `projects.setup.commands` | Build and test commands (one per line) | دستورهای build و test (هر خط یک دستور) |
| `projects.setup.nothing` | Everything is already set up. | همه‌چیز از قبل راه‌اندازی شده است. |
| `projects.setup.noEngine` | The engine folder was not found. Set Orchestra:EnginePath. | پوشه‌ی موتور پیدا نشد. مقدار Orchestra:EnginePath را تنظیم کنید. |
| `projects.setup.waiting` | Waiting for the first event | منتظر اولین رویداد |
| `projects.setup.knowledge` | Fill the knowledge with an agent (optional) | پر کردن دانش پروژه با یک ایجنت (اختیاری) |
| `projects.table.check` | Check setup | بررسی راه‌اندازی |
| `start.running` | Orchestra is running | Orchestra در حال اجراست |
| `start.firstProject` | Set up your first project | اولین پروژه را راه‌اندازی کنید |
| error `Projects.Setup.PathRefused` | This folder cannot be set up: {reason}. | این پوشه قابل راه‌اندازی نیست: {reason}. |
| error `Projects.Setup.EngineNotFound` | The engine folder was not found. | پوشه‌ی موتور پیدا نشد. |
| error `Projects.Setup.Origin` | Setup is allowed only from Orchestra's own page. | راه‌اندازی فقط از صفحه‌ی خود Orchestra ممکن است. |

Row errors (the code is translated; the `Detail` under it stays as the system wrote it, like a log line):

| Code | en | fa |
|---|---|---|
| `Setup.Item.WriteFailed` | Could not write this file. | این فایل نوشته نشد. |
| `Setup.Item.GitInitFailed` | git init failed; nothing after it was done. | دستور git init انجام نشد؛ کارهای بعد از آن انجام نشدند. |
| `Setup.Item.InvalidJson` | This JSON file could not be read, so it was left unchanged. | این فایل JSON خوانده نشد و دست‌نخورده ماند. |
| `Setup.Item.Skipped` | Not done: an earlier step failed. | انجام نشد: یک قدم قبلی خطا داد. |

The desktop host's two messages (§6.7) are English only: they appear before the UI and its locale exist.

`{reason}` values (en / fa): `not a folder` / `پوشه نیست`; `a drive root` / `ریشه‌ی درایو است`; `a system
folder` / `پوشه‌ی سیستمی است`; `Orchestra's own folder` / `پوشه‌ی خود Orchestra است`.

### 6.7 The desktop host and the publish script (phase E)

**`OrchestraHost` (in `Orchestra.Api`, `Extensions/OrchestraHost.cs`):**
`public static WebApplication Create(string[] args, string? contentRoot = null)` — builds
`WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = contentRoot })`,
calls today's `ConfigureServices()`, `Build()`, `ConfigureApp()`, and returns the app without running it.
`Program.cs` becomes `OrchestraHost.Create(args).Run();` inside today's try/catch.

**`Orchestra.Desktop/Program.cs`**, `[STAThread] static int Main(string[] args)`:
1. D2a health check with `HttpClient` (2 s). Running → `new PhotinoWindow().ShowMessage("Orchestra",
   "Orchestra is already open.")`, return 0. Port taken but no health → message "Port 3500 is used by another
   program.", return 1.
2. `var app = OrchestraHost.Create(args, AppContext.BaseDirectory);` then `app.StartAsync().GetAwaiter().GetResult()`.
   A startup exception → `ShowMessage` with its first line, return 1.
3. `new PhotinoWindow()` with title "Orchestra", size 1280×860, centered, resizable, the icon
   `wwwroot/favicon.ico` when it exists, the web-message handler of D3b, `.Load("http://localhost:3500/")`,
   `.WaitForClose()`.
4. `app.StopAsync()` with a 5 s timeout, return 0.

`appsettings.json` in `Orchestra.Desktop` is not added: the API's settings are the ones used. The desktop
project copies nothing itself; the publish script places `appsettings.json`, `wwwroot/` and `engine/`.

**UI bridge (`ui/src/lib/desktop/bridge.ts`, NEW):** `isDesktop()` (true when `window.external?.sendMessage`
is a function) and `pickFolder(start: string): Promise<string | null>` (sends D3b's message, resolves on the
first `pickedFolder` answer, `null` for an empty path, rejects after 5 min). `project-form.tsx`'s "Browse"
calls `pickFolder` when `isDesktop()`, else opens today's `folder-browser.tsx`.

**`scripts/publish.ps1`:** D2b, in that order, one printed line per step; any failing command stops the
script with its exit code. 

**`.gitignore`:** `dist/`.

## 7. Tests

- **Deleted with their code:** `PathMapperTests`, `ConnectHandlerTests`, `HostingHandlerTests`, the UI
  `connect.test.tsx`. Listed by name in the hand-over.
- **Changed by D14 (`ScaffoldPromptBuilderTests`):** `The_embedded_config_is_the_engines_orchestrator_json`
  and `A_missing_orchestra_folder_embeds_the_config_and_the_folder_work` are **deleted** (section 4 is gone);
  `Nothing_missing_is_one_line` asserts D14's new text and is called with `.ai/` and `AGENTS.md` found but no
  config; a new `The_last_line_is_numbered_4` asserts the closing line; the other three stay as they are.
- **Changed by D16 (`start.test.tsx`):** the `dashboard`, `run` and `docker` assertions go with those
  fields; the "done on step 1 without Docker" test is renamed "lists the projects still waiting for an
  event" and keeps its `project` / `connected` / `pending` assertions. No test is deleted.
- **API, new:**
  - `LocalPathsTests` — the moved `LooksAbsolute`/`Normalize`/`Covers` cases from `PathMapperTests`.
  - `SetupPlannerTests` — empty folder that is a repo (single shape, every row `create`, no `git-init`);
    workspace with two repos (`git-init` row, `plans/<Repo>/` rows, knowledge rows per repo); second run
    after apply (all `exists`); existing `orchestrator.json` without `report` (`report-block` `add`); local
    json with a different `profilesPath` (`update`, detail names the key, never the token).
  - `SetupApplierTests` — writes only the planned files (snapshot of the folder before/after); `report-block`
    leaves every other byte identical; local json keeps an unknown key; skipped optional row not written;
    a read-only file makes its row fail and later rows still run; `git-init` failure stops the rest;
    project entry created then updated on a second apply.
  - `SetupGuardTests` — each refused root of D13; Origin missing / other host / other port → 403.
  - `VerifyCommandDetectorTests` — sln, slnx, package.json with a subset of scripts, both, none.
  - `EngineLocatorTests` — option wins; walk-up finds `engine/run.ps1`; none → not found.
  - The orchestrator.json render for both shapes matches §6.3.4 exactly.
- **Engine, new:** one offline case file. Per shape (single repo, two repos): a temp workspace with
  `git init` repos, `engine/templates/project/` copied to `.orchestra/` as setup would, the §6.3.4 config
  for that shape, a temp profiles folder holding `engine/tests/offline/profile-fixtures/profile.json` as
  `default.json`, and a local json with `enginePath` = this checkout and that `profilesPath`. Then
  `run.ps1 agents` exits 0 (config and profile load) and `run.ps1 new t1 <repo> "check" -DryRun` exits 0
  and writes a prompt that contains the template's workflow text and its planner role section (the
  workflow paths resolve). **Not** `health`: it pings every role's agent. No mock agent is needed, because
  neither command calls a model.
- **UI, new (`setup.test.tsx`):** rows render with their chips; unticking an optional row re-plans with
  `skip`; "Set up" disabled when `engineFound` is false; result shows a row error; "Check setup" opens with
  `?id=`. Start page: two steps, the second done when a project has `lastEventAt`.
- **UI, new (`bridge.test.ts`):** `isDesktop()` false without `window.external`; with a fake
  `sendMessage`/`receiveMessage`, `pickFolder` sends the D3b message and resolves the answered path; an
  empty answer resolves `null`. The form's Browse uses the bridge when `isDesktop()` and the folder browser
  otherwise.
- **API, new:** `OrchestraHostTests` — `Create(args, <temp folder>)` uses that folder as content root and
  resolves `wwwroot` under it.
- **Publish:** phase E's gate runs `scripts/publish.ps1`; the zip exists and holds
  `Orchestra.exe`, `appsettings.json`, `wwwroot/index.html` and `engine/run.ps1`; then the script starts the
  exe, waits up to 30 s for `GET http://localhost:3500/api/health` and for `GET /` to return HTML, and stops
  the process.

## 8. Stop and ask

- `docker cp` fails, or the copied `projects.json` has paths that are not Windows paths.
- `run.ps1 agents` or `new … -DryRun` fails on a scaffolded config because of the engine, not the template:
  stop; do not change engine code in this plan.
- `Resolve-WorkflowPath` or another engine function needs `{repo}` in a path this plan writes without it.
- Any test outside §7's deleted list would have to be deleted or weakened.
- The pinned Photino.NET version has no `ShowOpenFolder`, no web-message API, or no `ShowMessage`, or the
  single-file publish cannot load its native library on `win-x64`.
- `OrchestraHost.Create` cannot be shared by both entry points without changing how a test builds the API.

## 9. Phases

**A — decisions, data, deploy.** §6.2 docs; print §6.1 for the owner and wait; after the owner's "done",
delete `deploy/`. **Gate:** none (docs only) — `dotnet build` still green. **Owner checks:** the data folder
has the four items. Stop here — show the owner the diff.

**B — templates and API.** Manifest rows 8–22, 38 (the templates first: the planner and its tests read them). **Gate:** `dotnet build` + `dotnet test` in `api/`, zero warnings.
**Owner checks:** `dotnet run` (Development) still lists the projects; `POST api/projects/setup/plan` from
the browser console on a test folder returns the rows. Stop here — show the owner the diff.

**C — engine test.** Rows 23, 24, 37. **Gate:** the offline suite in the background with a log; the new
cases pass, no count drops. Stop here — show the owner the diff.

**D — UI.** Rows 25–30, 39, and row 38's UI part. **Gate:** `npm run typecheck`, `lint`, `test`, `build` in `ui/`.
**Owner checks:** with `dotnet run` and the browser: the setup checklist items of «نتیجه», including "Check
setup" for Project A and Project B. Stop here — show the owner the diff.

**E — desktop and publish.** Rows 31, 33–37. **Gate:** API and UI gates again; `scripts/publish.ps1`
succeeds and §7's publish check passes. **Owner checks:** the exe items of «نتیجه» (window, one instance,
the folder dialog, closing stops the app). Stop here — show the owner the diff.

**F — docs.** Row 32 and the session log. **Gate:** links resolve (`git grep` for `deploy/`,
`PathMap`, `ORCHESTRA_HOST_ROOT`, `connect` page names returns only history: session logs, done plans,
superseded ADR text). Stop here — show the owner the diff.

## 10. Definition of done

Every phase's gate green; the owner's checklist passed; Project A and Project B report events from the local
app; the hand-over lists the removed rules (§6.2) and deleted tests (§7); `Status: Done`, the `Done:` date,
the commit and the merge in the header; plan 0008 `Dropped`; the ideas named in §2 exist.
