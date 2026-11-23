# Plan 0009 — Adding a project without knowing Docker: host paths, auto-fill, connect, getting started

**Status:** Done
**Created:** 2026-11-23 · **Updated:** 2026-11-23 · **Done:** 2026-11-23
**Preview:** none — the owner asked for the plan directly (2026-11-23); see **Steps changed**
**Run:** — (implemented by an agent in the chat, phases A–D)
**Commit:** `73abf32` on `alireza/easy-onboarding` · **Merge:** `3c3ff5d` into `main`, 2026-11-23
**Code review:** by the planner after hand-over, 2026-11-23 — fixed before the commit: the folder browser compared Windows paths with `/` only (direct installs could not open a subfolder), `..` escaped the browse roots, the connect page polled the wrong query and never turned green; plus small ones (`isAbsolute` on Linux, a relative path reported as "not visible", raw ISO time, the profilesPath note, one hosting query). Those fixes compile (build, typecheck, lint); the test suites were **not** re-run after them, at the owner's request
**Planned by:** Claude Opus 5.5, 2026-11-23. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (review pasted by the owner, 2026-11-23): 8 must-fix, 5 should-fix, 3 minor — all applied in this version (D3, D4, D5, D7, D8, D12, D16, D17, §6.2, §6.8, manifest rows 1, 3, 4, 6, 9, 11, 12, 16)
**Decided by:** owner in the chat, 2026-11-23 (the understanding below, IIS question, "detect, don't ask")
**Branch:** `alireza/easy-onboarding` — the implementer creates it.
**Base:** `alireza/locale-switch-fix` @ `5cc332c` (the locale fix, not yet merged into `main`)
**Companion:** None. Projects change nothing; the owner (or an agent, from the prompt this plan adds) pastes
one block into a project's `orchestrator.json` and one key into its `orchestrator.local.json`.
**Steps changed:** no preview — the owner asked for the plan in the chat. The screens are described in §6.6;
if the reviewer finds them too vague to build without guessing, the preview is made before Phase C.

---

## فهم من از درخواست

امروز برای اضافه کردن یک پروژه باید بدانید داکر پوشه‌ها را با چه اسمی می‌بیند (`/projects/root1`)، مسیر
runs را خودتان بنویسید، ریپوها را با کاما وارد کنید، و بعد توکن را دستی بسازید و در سه جا بگذارید. هیچ‌جا هم
گفته نشده که چه باید کرد. این پلن این کارها را خودکار می‌کند و راهنما می‌گذارد:

- **مسیر ویندوز کافی است.** شما همان مسیری را می‌دهید که در ویندوز می‌بینید
  (`F:\Projects\ProjectA`). داشبورد خودش می‌فهمد داخل داکر است یا مستقیم روی
  ویندوز/IIS اجرا شده (از کاربر نمی‌پرسد)، و در داکر مسیر را به مسیر داخل container تبدیل می‌کند. برای این
  کار در داکر یک پوشه‌ی مادر (مثلاً `F:\Projects`) یک بار و فقط‌خواندنی وصل می‌شود، به‌جای یک mount برای هر
  پروژه.
- **انتخاب پوشه.** کنار فیلد مسیر یک دکمه‌ی «انتخاب» هست که پوشه‌ها را (فقط داخل پوشه‌های مجاز) نشان می‌دهد.
- **پر شدن خودکار.** با انتخاب پوشه، اسم، id، پوشه‌ی runs و ریپوها خودشان پر می‌شوند. ریپوها یک فهرست تیک‌دار
  است، نه متن با کاما.
- **تشخیص درست «آماده».** پروژه‌ای که `.orchestra/orchestrator.json` دارد آماده است و پرامپت راه‌اندازی
  نمی‌گیرد. `.ai/` داخل هر ریپو هم حساب می‌شود (امروز فقط ریشه را می‌دید).
- **اتصال بعد از ذخیره.** داشبورد توکن را خودش می‌سازد و نگه می‌دارد. بعد از ذخیره، صفحه‌ی «اتصال» سه چیز آماده
  می‌دهد: بلوک `report` با id همین پروژه، کلید `reportToken` برای `orchestrator.local.json`، و یک پرامپت برای
  ایجنت که بلوک را اضافه کند. توکن هیچ‌وقت داخل پرامپت نمی‌رود. موتور توکن را از `orchestrator.local.json` هم
  می‌خواند، پس متغیر محیطی ویندوز دیگر لازم نیست.
- **راهنما.** زیر هر فیلد یک خط توضیح با مثال، و یک صفحه‌ی «شروع کار» که قدم‌به‌قدم می‌گوید چه کنید و جلوی هر
  قدم نشان می‌دهد انجام شده یا نه.
- **IIS و اجرای مستقیم** هم کار می‌کند: آنجا تبدیل مسیر لازم نیست و همه‌چیز بالا همان‌طور کار می‌کند.

این پلن کارهای زیر را **نمی‌کند**:
- داشبورد چیزی داخل پوشه‌ی پروژه نمی‌نویسد (ADR 0003). بلوک‌ها را شما یا ایجنت paste می‌کنید.
- پنجره‌ی انتخاب پوشه‌ی خود ویندوز را باز نمی‌کند؛ از مرورگر ممکن نیست.
- فیلد Profile فرم حذف می‌شود، چون موتور پروفایل را از `orchestrator.json` خود پروژه می‌خواند و این فیلد کاری
  نمی‌کرد. انتخاب پروفایل برای پروژه کار جداگانه‌ای است.
- ورود (sign-in) و امنیت سرور مال پلن 0008 است.
- داکر دو container جدا (ui و api) نمی‌شود؛ UI همان خروجی ثابت داخل API می‌ماند.
- جدول‌ها و صفحه‌بندی مال پلن 0010 است.

## جریان کار

1. شما «Projects ← New project» را باز می‌کنید. بالای فرم یک خط می‌گوید داشبورد کجا اجرا شده («در داکر؛ این
   پوشه‌ها را می‌بیند: `F:\Projects`») یا اگر هیچ پوشه‌ای وصل نیست، دقیقاً چه خطی را کجا اضافه کنید.
2. مسیر پوشه را paste می‌کنید یا با «انتخاب» پیدا می‌کنید. داشبورد پوشه را بررسی می‌کند و فیلدهای خالی را پر
   می‌کند: اسم، id، پوشه‌ی runs، و ریپوهای پیداشده (همه تیک‌خورده).
3. اگر پوشه خارج از پوشه‌های قابل‌دیدن باشد، یک پیام ساده می‌گوید چرا و چه کنید.
4. اگر پروژه آماده است، یک خط سبز «آماده برای ارکستر» می‌بینید. اگر نیست، همان پرامپت راه‌اندازی امروز.
5. «ذخیره» شما را به صفحه‌ی «اتصال» همین پروژه می‌برد: بلوک `report`، کلید توکن، و پرامپت ایجنت، هر کدام با
   دکمه‌ی کپی. جلوی آن نوشته «منتظر اولین رویداد»؛ با اولین رویداد از این پروژه سبز می‌شود.
6. صفحه‌ی «شروع کار» (اولین منو) همین مسیر را از نصب تا اولین پلن نشان می‌دهد و هر قدم را با وضعیت واقعی تیک
   می‌زند.

فازها:
- **A:** API — تبدیل مسیر، تشخیص محل اجرا، مرور پوشه، Check بهتر، توکن خودکار، endpoint اتصال؛ داکر.
- **B:** موتور — خواندن `reportToken` از `orchestrator.local.json`.
- **C:** UI — فرم تازه، صفحه‌ی اتصال، صفحه‌ی شروع کار، توضیح فیلدها.
- **D:** مستندات — README (en و fa)، ADR 0008، `.ai/`.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

یک نفر که داکر بلد نیست، با خواندن صفحه‌ی «شروع کار» داشبورد را بالا می‌آورد، پروژه را فقط با مسیر ویندوز
اضافه می‌کند، با کپی دو تکه وصلش می‌کند، و اولین رویداد را روی Runs board می‌بیند.

چک‌لیست شما:
- در `deploy/.env` خط `ORCHESTRA_HOST_ROOT=F:/Projects` را اضافه کنید؛ `ORCHESTRA_PROFILES_DIR` همان‌طور که هست
  می‌ماند، و خط‌های `PROJECT1_*`/`PROJECT2_*` و `ReportToken` دیگر لازم نیستند. داشبورد را بالا بیاورید. «شروع کار» قدم ۱ را سبز نشان می‌دهد و `F:\Projects` را
  نام می‌برد.
- `ORCHESTRA_HOST_ROOT` را خالی کنید و بالا بیاورید: compose با یک پیام روشن متوقف می‌شود.
- New project: مسیر `F:\Projects\ProjectA` را paste کنید. اسم `ProjectA`،
  id `projecta`، runs `F:\Projects\ProjectA\.orchestra\runs` و ریپوهای
  `Ui` و `Web` تیک‌خورده پر می‌شوند، و «آماده برای ارکستر» دیده می‌شود. پرامپت راه‌اندازی نمی‌آید.
- «انتخاب» را بزنید: فقط زیر `F:\Projects` می‌توانید بگردید؛ کنار پوشه‌های آماده یک نشان هست.
- مسیر `C:\Windows` را paste کنید: پیام «این پوشه برای داشبورد قابل دیدن نیست» با راه‌حل.
- ذخیره کنید: صفحه‌ی اتصال باز می‌شود. پرامپت ایجنت توکن ندارد؛ کلید `reportToken` دارد.
- بلوک و کلید را در Project A بگذارید و یک `run.ps1 new … -DryRun` یا یک run واقعی بزنید: «منتظر اولین رویداد»
  سبز می‌شود و run روی Runs board می‌آید، بدون هیچ متغیر محیطی ویندوز.
- داشبورد را restart کنید: توکن همان می‌ماند (رویدادها هنوز قبول می‌شوند).
- همین را با `dotnet run` (بدون داکر) تکرار کنید: مسیر بدون تبدیل قبول می‌شود و «انتخاب» درایوها را نشان می‌دهد.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md): branch before the first edit; never commit/push/merge unless
   asked in that message; one phase = one uncommitted diff.
2. The dashboard never writes inside a project folder (ADR 0003, 0005). It writes only its own data folder
   (the generated token file, `projects.json`, the database, profile exports).
3. The engine never calls the API to get anything (ADR 0002). Reporting stays fire-and-forget.
4. **A secret never goes into a prompt.** The report token appears only in the `orchestrator.local.json`
   snippet the owner pastes by hand, never in text meant for an agent.
5. No personal path enters source: examples use `F:/Projects` only in docs and tests as a synthetic value.
6. Persian strings are written in the fa catalogue as given in §6.7; never machine-translated from the en ones
   (ADR 0004).

### Baseline

Before the first edit: `dotnet build` + `dotnet test` in `api/` (zero warnings), `npm run typecheck`,
`lint`, `test`, `build` in `ui/`, and the engine offline suite
(`powershell -NoProfile -File engine/tests/offline/run-offline.ps1`, in the background with a log). Record
the three counts in the hand-over; none may drop.

## 1. Goal

Adding and connecting a project needs only the Windows path and two pastes, under Docker or not; the form fills
itself; the dashboard explains each step in place.

## 2. Out of scope

Sign-in and protecting the folder/connect endpoints on a server (0008 — it must gate `api/system/*` and
`api/projects/{id}/connect`); a profile picker for a project; editing a project's files; two containers;
tables/paging (0010); changing how runs are imported.

## 3. Rules that apply

ADRs 0002, 0003, 0004, 0005; plan 0003 §0 (Check reads existence only, never content); plan 0004 D1 (the token
opens `POST api/events` and nothing else — kept). **Plan 0004 D2 changes:** "no token configured → 404" is
replaced by D6 below; the phase-D docs say so (rule change reviews the old rules).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | **Hosting is detected, never asked.** `mode = "container"` when the environment variable `DOTNET_RUNNING_IN_CONTAINER` is `true` (set by the official .NET images; verified `true` in the running `orchestra` container, 2026-11-23), else `"direct"` (IIS, `dotnet run`, a service) |
| D2 | **Path map.** New setting `Orchestra:PathMap`: a list of `{ Host, Server }` (e.g. `F:/Projects` → `/host`). `PathMapper.ToServer(path)`: trim; `\` → `/`; drop a trailing `/` (not on a drive root like `F:/`); the entry with the **longest** `Host` that is a whole-segment prefix, compared case-insensitively, is replaced by its `Server`; no match → the input unchanged. `ToHost` is the inverse (case-sensitive on `Server`) and returns the `Host` spelling with `\` when the `Host` contains `:` (a Windows drive). Empty map (IIS, `dotnet run`) → both are the identity |
| D3 | **Projects store what the owner typed** (the host path) in `projects.json`. `ProjectRegistry` builds each dashboard project's `ProjectOptions` with `RootPath`/`RunsPath` passed through `ToServer`; everything that touches the disk keeps using `ProjectOptions` unchanged. **`ProjectDto` returns the stored (host) paths:** the registry's snapshot keeps the `StoredProject` of each dashboard project beside its `ProjectOptions` (a third dictionary, `Stored`), and `ListProjectsHandler` reads `rootPath`/`runsPath` from it for dashboard projects — never from the mapped `ProjectOptions`, so the edit form never shows `/host/…` and a save never stores a server path. Config projects (`Orchestra:Projects`) are not mapped — their paths are already server paths (ToServer leaves them unchanged anyway) |
| D4 | **Validation is after mapping.** `ProjectValidation` checks `Path.IsPathRooted(ToServer(rootPath))` and the same for `runsPath`. A host path that maps to nothing and is not rooted on the server → new error `Projects.Path.NotVisible` (400); the same for `runsPath` → `Projects.RunsPath.NotVisible` (400). **These two codes come only from create and update.** `POST api/projects/check` never fails on visibility: it answers 200 with `visible: false`, and the form shows its own catalogue text (§6.8). This is what makes `F:\…` valid in a Linux container with a map and invalid without one |
| D5 | **Browse roots.** Container mode: the map's `Host` values. Direct mode: `Orchestra:BrowseRoots` (list of paths) when set, else every fixed drive's root (`DriveInfo.GetDrives()` where `DriveType == Fixed` and `IsReady`). `GET api/system/folders` never lists outside them; it lists **folder names only**, never files, skips names starting with `.` and folders it cannot read, sorted case-insensitively, at most 500. The fixed-drives default is acceptable only because a direct install listens on loopback until plan 0008 (D16); 0008 must require `BrowseRoots` explicitly |
| D6 | **The dashboard makes its own token.** `ReportTokenStore` (singleton): `Orchestra:ReportToken` when set (it wins, as today); else the content of `<data folder>/report-token` (the database file's folder, as `ResolveProfilesPath` does); else 32 random bytes from `RandomNumberGenerator`, lowercase hex (64 chars), written atomically (temp file + move) to that file, then used. So the reporter is always on and the token survives restarts. `ReportTokenFilter` reads the store; the "404 when unset" branch is deleted. A wrong or missing token still answers 401 with an empty body; the token still never reaches a log line |
| D7 | **Ready** means `.orchestra/orchestrator.json` or `orchestrator/orchestrator.json` exists under the root. **Knowledge present** means `.ai/` at the root **or** in every detected repo; **AGENTS.md present** means at the root **or** in every detected repo. `ScaffoldPromptBuilder` returns `NothingMissing` when knowledge, AGENTS.md and the **config file** (rule 1 — not merely the folder) are all present by these rules. **The scaffold prompt creates `.orchestra/orchestrator.json`, not `project.json`:** `ProjectJson(…)` is replaced by `OrchestratorJson(name, repos)` producing `{ "profile": "default", "root": "..", "repos": { <repo>: <repo> }, "plansDir": ".orchestra/plans/{repo}", "description": { "title": "<name>", "repos": "<repo list>" }, "workflow": { "gatesPath": ".orchestra/workflow.md", "rolesPath": ".orchestra/roles/{repo}", "rolesIndex": ".orchestra/roles/{repo}/README.md", "promptsPath": ".orchestra/prompts/{repo}", "agentsDoc": "{repo}/AGENTS.md", "indexDoc": "{repo}/.ai/README.md", "planTemplate": ".orchestra/plans/TEMPLATE.md" }, "verifyCommands": {} }` (the shape Project A uses), and the prompt tells the agent to fill `verifyCommands` with each repo's build and test commands. A project that ran the prompt is then ready by rule 1. In the check result, the `.ai/` and `AGENTS.md` items show **found** when present by this rule (with the note "in each repo" when found per repo), so the result and `isReady` never disagree |
| D8 | **Connect, not write.** `GET api/projects/{id}/connect` returns three texts: `reportBlock` (the `"report": { … }` JSON of `engine/README.md`, with `project` = the id and `url` = `Orchestra:PublicUrl` + `/api/events` when that new optional setting is set, else the request's own scheme+host+`/api/events` — behind IIS or a proxy the owner sets `PublicUrl`), `localKeys` (`{ "reportToken": "<token>" }` plus `"profilesPath": "<ToHost(resolved profiles path)>"` only when `ToHost` changes the path, i.e. a map covers it), and `agentPrompt` (D9). The page shows `localKeys` to the owner with the note "add these keys to `.orchestra/orchestrator.local.json`; keep `enginePath`" |
| D9 | **The agent prompt holds no secret.** Text (en only; prompts are for agents): `Add this "report" block at the top level of .orchestra/orchestrator.json in {name} ({hostRoot}). Change nothing else. Do not commit.` + the block. A test asserts the token is not a substring of `agentPrompt` |
| D10 | **The engine reads the token from the local file too.** `engine/lib/report.ps1`: token = the environment variable named by `tokenEnv` (as today); when that is empty, `reportToken` from `orchestrator.local.json` beside the config (read like `Get-ProfilesPath` does). Neither → today's behaviour (the request goes without `Authorization`, the dashboard answers 401, one `[report]` line, the run goes on) |
| D11 | **Last event per project.** `ProjectDto` gains `lastEventAt` (UTC, nullable) = `max(RunEvent.ReceivedAt)` for the project. The connect page and the start page use it for "connected" |
| D12 | **The Profile field leaves the form.** The engine takes the profile from the project's own `orchestrator.json` (`EffectiveConfigHandler` reads it there); the dashboard field was never used. `projects.json` keeps its `profile` value (no schema change): `CreateProjectHandler`/`UpdateProjectHandler` always store `"default"`, so `ProjectStore.IsValid` still holds. `ProjectInput.Profile` stays in the record and is ignored: the `profile != "default"` condition in `ProjectValidation.Validate` is **deleted** |
| D13 | **Docker: one parent mount.** `deploy/docker-compose.yml`: the four `PROJECT*` mounts are replaced by `${ORCHESTRA_HOST_ROOT:?set ORCHESTRA_HOST_ROOT in deploy/.env (the folder that contains your projects, e.g. F:/Projects)}:/host:ro` and the environment `Orchestra__PathMap__0__Host=${ORCHESTRA_HOST_ROOT}`, `Orchestra__PathMap__0__Server=/host`. The profiles bind mount stays. `deploy/.env.example` loses `PROJECT*`, `ReportToken` and the `Orchestra__Projects__*` block, and gains `ORCHESTRA_HOST_ROOT=` with a two-line comment |
| D14 | **Form auto-fill fills only what is empty** (a typed value is never overwritten), except the repo list, which is replaced by the detected repos when the owner has not changed it since the last check. Id keeps today's rule (`slugOf(name)` until typed). Check runs on choosing a folder in the browser and on leaving the path field when it changed; the "Check folder" button stays |
| D15 | **Start page** is a new feature `start`, first in `FEATURES` (menu category `overview`), route `/[locale]/start/`. The app's root redirect (`/` → `/{locale}/`) is unchanged; the start page is reached from the menu and from an empty Runs page ("No projects yet — Getting started") |
| D16 | **Loopback until 0008.** The connect endpoint returns the token and the folder endpoint lists folders, with no sign-in. So until plan 0008: Docker publishes on `127.0.0.1` only (already so in compose — keep it), `dotnet run` keeps `Urls=http://localhost:3500`, and the README's IIS paragraph says to bind the site to `localhost`/`127.0.0.1` only. Nothing in code enforces it; 0008 gates `api/system/*` and `api/projects/{id}/connect` |
| D17 | **Routes come from `ApiRoutes`:** add `public const string System = Base + "/system";` and use `[Route(ApiRoutes.System)]` on `SetupController` (a literal `"api"` is a bug — `.ai/api/conventions.md`) |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `PathMap` (`List<PathMapEntry>`, `PathMapEntry { Host, Server }`), `BrowseRoots` (`List<string>`), `PublicUrl` (`string?`, D8); `Common/ApiRoutes.cs` — `System` (D17) | EDIT | A |
| 2 | `api/src/Orchestra.Api/Common/Hosting/{PathMapper,HostingInfo}.cs` — D1, D2, D5 (`HostingInfo.Mode`, `BrowseRoots()`) | NEW | A |
| 3 | `api/src/Orchestra.Api/Modules/Setup/{SetupController,SetupModuleInstaller}.cs` (route `api/system/*`; the folder and namespace are **not** named `System`, which would shadow `System.*` in sibling `Orchestra.Api.Modules.*` namespaces); `Features/Hosting/HostingHandler.cs`; `Features/Folders/FoldersHandler.cs`. `SetupModuleInstaller` registers `HostingInfo` and `PathMapper` (singletons, from the options), `HostingHandler` and `FoldersHandler` (scoped), like the other installers | NEW | A |
| 4 | `api/src/Orchestra.Api/Modules/Projects/ProjectRegistry.cs` (D3), `ProjectInput.cs` (D4), `Features/CheckFolder/{FolderCheck,CheckFolderHandler}.cs` (map in, D7, host-form out), `Features/ScaffoldPrompt/ScaffoldPromptBuilder.cs` (D7), `Features/ListProjects/{ListProjectsHandler,ProjectDto}.cs` (D11), `ProjectsController.cs` (the connect route), `ProjectsModuleInstaller.cs` (`AddScoped<ConnectHandler>()`), `Features/{CreateProject,UpdateProject}/*Handler.cs` (store `"default"`, D12) | EDIT | A |
| 5 | `api/src/Orchestra.Api/Modules/Projects/Features/Connect/ConnectHandler.cs` (D8, D9) | NEW | A |
| 6 | `api/src/Orchestra.Api/Modules/Events/{ReportTokenStore}.cs` NEW; `ReportTokenFilter.cs`, `EventsModuleInstaller.cs` (`AddSingleton<ReportTokenStore>()`) EDIT (D6) | NEW/EDIT | A |
| 7 | `api/src/Orchestra.Api/Common/Errors/{Error,ErrorMessages}.cs` — the codes of §6.2 | EDIT | A |
| 8 | `api/tests/Orchestra.Api.Tests/{Common/PathMapperTests,Setup/*,Projects/*,Events/ReportTokenStoreTests}.cs` NEW/EDIT; `Events/ReportTokenFilterTests.cs` — the "no token configured → 404" case is **rewritten** to "no token configured → the generated token is accepted", not deleted, so the count does not drop | NEW/EDIT | A |
| 9 | `deploy/docker-compose.yml`, `deploy/.env.example` (D13); `api/src/Orchestra.Api/appsettings.Local.example.json` (`"PathMap": []`, `"BrowseRoots": []`, `"PublicUrl": ""`). `ORCHESTRA_PROFILES_DIR` keeps its `:?` and stays in `.env.example` | EDIT | A |
| 10 | `engine/lib/report.ps1` (D10); `engine/examples/orchestrator.local.json` (`"reportToken": ""`); `engine/README.md` (Reporting section); `engine/tests/offline/run-offline.ps1` — the suite has no HTTP listener, so two resolver-level cases on `Get-ReportToken` (§6.5; the suite already dot-sources `lib
eport.ps1`): env set → the env value; env empty + `reportToken` in the local file → the file value | EDIT | B |
| 11 | `ui/src/features/projects/{api,queries,types,project-schema}.ts`; `components/{project-form,check-result}.tsx`; NEW `components/{folder-browser,repo-picker,hosting-line,connect-page}.tsx`; page `ui/src/app/[locale]/settings/projects/connect/page.tsx` (`?id=`); `ui/src/features/projects/i18n/{fa,en}.ts` (§6.7, §6.8) | NEW/EDIT | C |
| 12 | `ui/src/features/start/**` (manifest, components, `i18n/{fa,en}.ts` with §6.8, `index.ts`); page `ui/src/app/[locale]/start/page.tsx`; `ui/src/features/registry.ts`, `ui/src/lib/features/define-feature.ts` (`start` in `FEATURE_KEYS`), `ui/src/i18n/messages.ts` | NEW/EDIT | C |
| 13 | `ui/src/features/runs/components/runs-list.tsx` — the empty state links to the start page (D15) | EDIT | C |
| 14 | `ui/src/features/**/*.test.tsx` — §7 | NEW/EDIT | C |
| 15 | `README.md`, `README.fa.md` — "Run the dashboard" rewritten around the start page and `ORCHESTRA_HOST_ROOT`; IIS paragraph (Hosting Bundle 10, app-pool read access) | EDIT | D |
| 16 | `.ai/decisions/0008-host-paths-and-own-report-token.md` (D1, D2, D6, D8) + its line in `.ai/decisions/README.md`; `.ai/architecture/overview.md` (System module, token store); `.ai/vision/roadmap.md` if it names the `PROJECT*` mounts; `.ai/architecture/data-model.md` (the "no token configured means the route answers 404" sentence in *Live events*); the comment in `ReportTokenFilter` and the `.env.example` lines that said "empty = off" | NEW/EDIT | D |

## 6. Per-file specification

### 6.1 Hosting and paths

`HostingInfo` (singleton): `Mode` (D1), `Map` (the `PathMap` entries, validated at start: both sides non-empty,
`Server` rooted on this OS — an invalid entry is logged once as a warning and dropped), `BrowseRoots()` (D5, in
host form). `PathMapper` is pure (constructor takes the entries) so it is unit-tested without the host.

### 6.2 API

| Endpoint | Does |
|---|---|
| `GET api/system/hosting` | `{ mode, roots: [hostPath], mapped: bool, tokenSource: "settings"\|"generated" }` — `mapped` = the map has at least one valid entry |
| `GET api/system/folders?path=` | empty `path` → `{ path: null, parent: null, folders: roots }`; else `path` must be inside a root (after `ToServer`, compared on the server form, whole-segment) → `{ path, parent (null at a root), folders: [{ name, path (host form), isReady (D7 rule 1), isRepo (.git dir or file) }] }` |
| `POST api/projects/check` (existing) | body `{ rootPath }` in host form; maps it; response adds `hostPath`, `suggestedName` (the last segment of the host path), `isReady` (D7); `suggestedRunsPath` returned in **host** form; `exists: false` and `visible: false` when the path maps to nothing rooted (the form shows `Projects.Path.NotVisible`'s text) |
| `GET api/projects` (existing) | each item adds `lastEventAt` (D11) |
| `GET api/projects/{id}/connect` | D8; unknown id → `Projects.Project.NotFound` (existing code) |

| Code | Status | en | fa |
|---|---|---|---|
| `Projects.Path.NotVisible` | 400 | The dashboard cannot see this folder. In Docker, set ORCHESTRA_HOST_ROOT in deploy/.env to a folder that contains it, then restart. | داشبورد این پوشه را نمی‌بیند. در داکر، در فایل deploy/.env مقدار ORCHESTRA_HOST_ROOT را پوشه‌ای بگذارید که این پوشه داخل آن است، و دوباره اجرا کنید. |
| `Projects.RunsPath.NotVisible` | 400 | The runs folder must be inside a folder the dashboard can see. | پوشه‌ی runs باید داخل پوشه‌ای باشد که داشبورد آن را می‌بیند. |
| `System.Folder.OutsideRoots` | 400 | This folder is outside the folders the dashboard may list. | این پوشه بیرون از پوشه‌هایی است که داشبورد اجازه‌ی دیدنشان را دارد. |
| `System.Folder.Missing` | 404 | The folder does not exist. | این پوشه وجود ندارد. |

### 6.3 Check (`FolderCheck`)

Input: the server path (the handler maps first). Keep everything it returns today; add D7's `isReady`; make
the knowledge and AGENTS.md rules of D7 the ones `ScaffoldPromptBuilder` uses. `RepoKnowledge` stays as it is
(per repo, shown in the result). Existence only — never read a file's content.

### 6.4 Token

`ReportTokenStore.Get()` returns the token (D6), computed once and cached; `Source` is `settings` or
`generated`. The file is created with the default permissions of the data folder (the image's user owns
`/data`). A test creates a temp data folder: first call writes the file; a second store instance reads the same
value; a configured `ReportToken` wins and no file is written.

### 6.5 Engine

In `report.ps1`, one function `Get-ReportToken $Ctx` returns the env value (by `tokenEnv`, default
`ORCHESTRA_REPORT_TOKEN`) or, when empty, `reportToken` from `orchestrator.local.json` in the config's folder
(missing file or key → empty). The existing call site uses it. Nothing else in the engine changes.

### 6.6 UI

**Project form** (`/settings/projects/edit/`), top to bottom:
1. `hosting-line`: from `GET api/system/hosting` — container + mapped: "Running in Docker. It can see: F:\Projects";
   container + not mapped: the text of `Projects.Path.NotVisible` with the two lines to add; direct: "Running
   directly on this computer; any folder you can open works."
2. **Project folder** (host form) + **Browse** (opens `folder-browser` in a `Sheet`: breadcrumb, up, folders with a
   "ready" badge and a "repo" badge, **Choose this folder**). Choosing or leaving the field (changed) runs Check (D14).
3. Check result (today's `check-result`) with the ready line on top when `isReady`.
4. **Name**, **Id** (as today).
5. **Repos**: `repo-picker` — the detected repos as checkboxes (all ticked after a check), plus an input
   "Add a repo folder name" + Add; the value sent stays `repos: string[]`.
6. **Runs folder** under a collapsed "Advanced" with the auto value shown.
7. Save → `router.push('/settings/projects/connect/?id=<id>')`; Back as today.
The Profile field and its stale hint are removed (D12). Every field has the hint of §6.7.

**Connect page**: title "Connect {name}"; three numbered cards, each with **Copy**: (1) "Add this block to
`.orchestra/orchestrator.json`" + `reportBlock` + "Or give this prompt to your agent:" + `agentPrompt`; (2) "Add
these keys to `.orchestra/orchestrator.local.json` yourself (they hold the token; keep `enginePath`)" +
`localKeys`; (3) status: `lastEventAt` null → "Waiting for the first event — start any run of this project"
(refetch every 10 s); else "Connected — last event {time}" + a link to Runs board. A link "Back to projects".

**Start page**: five steps, each a card with a done/not-done mark computed live:
1. *Run the dashboard* — done when hosting is direct, or container and mapped. Body: the hosting line; for Docker
   the `.env` line and the compose command; for IIS the Hosting Bundle and app-pool note.
2. *Add a project* — done when `GET api/projects` has at least one item. Button → New project.
3. *Connect it* — done when any project has `lastEventAt`. Lists projects not yet connected, each → its connect page.
4. *Run a plan* — done when any run exists (`GET api/runs` non-empty; after 0010, the table's total). Body: Library →
   open a plan → **Copy prompt** → paste into your agent opened in the project folder.
5. *Review and close* — text only: review the diff, commit yourself, set the plan's Status to Done, `run.ps1 close <id>`.

### 6.7 Field hints (en / fa)

| Field | en | fa |
|---|---|---|
| Project folder | The folder as you see it in Windows, e.g. F:\Projects\MyApp. The rest fills itself. | مسیر پوشه همان‌طور که در ویندوز می‌بینید، مثلاً F:\Projects\MyApp. بقیه خودش پر می‌شود. |
| Name | Shown in menus and filters. | در منوها و فیلترها نمایش داده می‌شود. |
| Id | Lowercase letters, digits and dashes. The engine's report block uses it; it cannot change later. | حروف کوچک انگلیسی، عدد و خط تیره. بلوک report موتور از آن استفاده می‌کند و بعداً عوض نمی‌شود. |
| Repos | The git repositories inside the folder. Found ones are ticked; add one by its folder name. | ریپوهای git داخل پوشه. پیداشده‌ها تیک خورده‌اند؛ برای اضافه کردن، اسم پوشه‌اش را بنویسید. |
| Runs folder | Where the orchestrator keeps its runs. Usually .orchestra\runs inside the project. | جایی که ارکستر runها را نگه می‌دارد. معمولاً .orchestra\runs داخل پروژه. |

### 6.8 Connect, start and hosting copy (en / fa)

Every visible string of the new screens; `{x}` are placeholders. The executor adds no visible string beyond this
table and §6.7.

| Key | en | fa |
|---|---|---|
| `hosting.docker` | Running in Docker. It can see: {roots} | در داکر اجرا می‌شود. این پوشه‌ها را می‌بیند: {roots} |
| `hosting.dockerNone` | Running in Docker, but no folder is shared with it. Add ORCHESTRA_HOST_ROOT to deploy/.env (the folder that holds your projects, e.g. F:/Projects) and restart. | در داکر اجرا می‌شود، اما هیچ پوشه‌ای به آن وصل نیست. در deploy/.env خط ORCHESTRA_HOST_ROOT را با پوشه‌ای که پروژه‌هایتان در آن است (مثلاً F:/Projects) اضافه کنید و دوباره اجرا کنید. |
| `hosting.direct` | Running directly on this computer; any folder you can open works. | مستقیم روی همین کامپیوتر اجرا می‌شود؛ هر پوشه‌ای که بتوانید باز کنید قابل استفاده است. |
| `form.notVisible` | The dashboard cannot see this folder. | داشبورد این پوشه را نمی‌بیند. |
| `form.ready` | Ready for the orchestrator — no setup needed. | آماده برای ارکستر؛ راه‌اندازی لازم نیست. |
| `form.browse` · `form.choose` · `form.up` | Browse · Choose this folder · Up | انتخاب · همین پوشه را انتخاب کن · بالاتر |
| `form.badgeReady` · `form.badgeRepo` | ready · repo | آماده · ریپو |
| `form.addRepo` · `form.add` | Add a repo folder name · Add | اسم پوشه‌ی یک ریپو · افزودن |
| `form.inEachRepo` | in each repo | در هر ریپو |
| `form.advanced` | Advanced | پیشرفته |
| `connect.title` | Connect {name} | اتصال {name} |
| `connect.block` | 1. Add this block to .orchestra/orchestrator.json | ۱. این بلوک را به .orchestra/orchestrator.json اضافه کنید |
| `connect.orPrompt` | Or give this prompt to your agent: | یا این پرامپت را به ایجنت خود بدهید: |
| `connect.local` | 2. Add these keys to .orchestra/orchestrator.local.json yourself. They hold the token, so they never go to an agent. Keep enginePath as it is. | ۲. این کلیدها را خودتان به .orchestra/orchestrator.local.json اضافه کنید. توکن داخل آن‌هاست، پس به ایجنت داده نمی‌شوند. enginePath را دست نزنید. |
| `connect.noProfilesPath` | profilesPath is not included: the dashboard cannot tell where its profiles folder is on your computer. Keep yours; it is needed only when the project names a profile. | profilesPath اضافه نشده، چون داشبورد نمی‌داند پوشه‌ی پروفایل‌ها روی کامپیوتر شما کجاست. اگر دارید همان را نگه دارید؛ فقط وقتی لازم است که پروژه یک پروفایل نام ببرد. |
| `connect.waiting` | 3. Waiting for the first event — start any run of this project. | ۳. منتظر اولین رویداد — یک run از این پروژه شروع کنید. |
| `connect.connected` | 3. Connected — last event {time}. | ۳. وصل است — آخرین رویداد {time}. |
| `connect.board` · `connect.back` · `connect.copy` · `connect.copied` | Open Runs board · Back to projects · Copy · Copied | باز کردن Runs board · بازگشت به پروژه‌ها · کپی · کپی شد |
| `start.title` | Getting started | شروع کار |
| `start.done` · `start.todo` | Done · To do | انجام شد · مانده |
| `start.s1.title` | Run the dashboard | داشبورد را اجرا کنید |
| `start.s1.docker` | Docker: put ORCHESTRA_HOST_ROOT=<the folder that holds your projects> in deploy/.env, then run: docker compose -f deploy/docker-compose.yml up -d | داکر: در deploy/.env خط ORCHESTRA_HOST_ROOT=<پوشه‌ای که پروژه‌هایتان در آن است> را بگذارید و اجرا کنید: docker compose -f deploy/docker-compose.yml up -d |
| `start.s1.iis` | IIS: install the ASP.NET Core 10 Hosting Bundle, give the app pool read access to your projects folder, and bind the site to localhost only. | IIS: بسته‌ی ASP.NET Core 10 Hosting Bundle را نصب کنید، به Application Pool اجازه‌ی خواندن پوشه‌ی پروژه‌ها را بدهید، و سایت را فقط روی localhost باز کنید. |
| `start.s2.title` · `start.s2.body` · `start.s2.action` | Add a project · Give the folder as you see it in Windows; the rest fills itself. · New project | پروژه اضافه کنید · مسیر پوشه را همان‌طور که در ویندوز می‌بینید بدهید؛ بقیه خودش پر می‌شود. · پروژه‌ی تازه |
| `start.s3.title` · `start.s3.body` | Connect it · Paste the two pieces from the project's Connect page; this step turns green with the first event. | وصلش کنید · دو تکه‌ی صفحه‌ی «اتصال» پروژه را paste کنید؛ با اولین رویداد این قدم سبز می‌شود. |
| `start.s4.title` · `start.s4.body` | Run a plan · Library → open a plan → Copy prompt → paste it into your agent, opened in the project folder. | یک پلن اجرا کنید · Library ← یک پلن را باز کنید ← Copy prompt ← آن را به ایجنتی بدهید که در پوشه‌ی پروژه باز است. |
| `start.s5.title` · `start.s5.body` | Review and close · Review the diff and commit it yourself, set the plan's Status to Done, then run: run.ps1 close <task id> | بررسی و بستن · diff را ببینید و خودتان commit کنید، Status پلن را Done کنید، بعد اجرا کنید: run.ps1 close <task id> |
| `runs.noProjects` | No projects yet — Getting started | هنوز پروژه‌ای نیست — شروع کار |

## 7. Tests

- `ProjectStore.IsValid` checks only non-empty fields and `Profile == "default"` (no rooted-path check) and the create/update handlers never touch the disk, so a stored `F:\…` stays valid in the container; D12 keeps `profile` as `default`.
- **API:** `PathMapperTests` (longest prefix; case-insensitive host; whole segment only — `F:/Proj` does not
  match `F:/Projects2`; `\` and `/`; drive root; no match → unchanged; `ToHost` round-trip). `HostingHandler` (mode
  from the env var through an injectable reader). `FoldersHandler` (inside a temp root; outside → 400; files not
  listed; dot-folders skipped; ready/repo badges). Check (per-repo `.ai/` counts as present; `.orchestra/orchestrator.json`
  → `isReady`; host form out). Validation (`F:\x` with a map → valid; without → `Projects.Path.NotVisible`).
  `ReportTokenStoreTests` (§6.4); the events endpoint accepts the generated token, 401 on a wrong one, never 404.
  Scaffold prompt (creates `orchestrator.json` with the D7 shape; a folder with only `.orchestra/` and no config is not ready and still gets the prompt). Validation (a `profile` other than `default` no longer fails). `ListProjects` returns the stored host paths for a dashboard project, not `/host/…`. Connect (block has the id and `PublicUrl` when set, else the request's host; token not in `agentPrompt`; `profilesPath` only when mapped).
  `lastEventAt` on the list.
- **Engine:** §5 row 10.
- **UI:** form — a check result fills empty name/id/runs/repos and keeps a typed name; repo picker add/untick; the
  Profile field is gone; save navigates to connect. Connect page — three copies, waiting vs connected. Start page —
  each step's mark from mocked queries. `architecture.test.ts` stays green.

## 8. Stop and ask

- Docker Hub still refuses the build (403): the Docker owner-checks wait; phases are verified with `dotnet run` meanwhile.
- Anything in §6.6 that cannot be built without choosing a layout the plan does not describe → make the preview first.

## 9. Phases

**A — API and Docker.** Files 1–9. **Gate:** `dotnet build` (0 warnings) + `dotnet test` in `api/`. **Owner
checks:** with the new `.env` line, `GET api/system/hosting` shows the root; check Project A by its Windows path.
Stop here — show the owner the diff.

**B — Engine.** File 10. **Gate:** the offline suite on both configs, count up by the new case(s). **Owner
checks:** remove the Windows variable, put `reportToken` in Project A's local file, a `-DryRun` posts an event. In a temp folder with the D7 `orchestrator.json` (two empty repo folders) and an `orchestrator.local.json` holding `enginePath` and `profilesPath`, `run.ps1 agents` passes — if the engine rejects the shape, STOP and ask (the D7 shape changes, not the engine).
Stop here — show the owner the diff.

**C — UI.** Files 11–14. **Gate:** `typecheck`, `lint`, `test`, `build` in `ui/`. **Owner checks:** the
checklist under «نتیجه». Stop here — show the owner the diff.

**D — Docs.** Files 15–16; re-read the rules each one touches and merge or delete what D6 and D13 make obsolete;
the hand-over names what was removed. **Gate:** links resolve. Stop here — show the owner the diff.

## 10. Definition of done

All four phases' gates green with counts at or above the baseline; the owner's checklist passes under Docker and
under `dotnet run`; ADR 0008 written; `Status: Done`, the `Done:` date, the commit and the merge in the header.
