# Plan 0003 — New project: the form, the folder check, the scaffold prompt

**Status:** Done — phases A–C implemented and verified 2026-11-20; commit `f7251e6` on `alireza/new-project` ("Plan 0003: new project — store, folder check, scaffold prompt, screens"), merge `5774f87` into `alireza/skeleton-and-import`. Owner browser checks of «نتیجه» still open
**Created:** 2026-11-19 · **Updated:** 2026-11-20 · **Done:** 2026-11-20
**Preview:** [`../previews/0001-skeleton-and-import/`](../previews/0001-skeleton-and-import/index.html) §5 and the "New project" mock — approved 2026-11-19 (v3)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** pre-gate review 2026-11-20 (14 points against the code at `a0235f4`); its fixes are
in this version — D8–D13, §5 rows 15–20, §8. The gate-2 review proper is still to come.
**Decided by:** Alireza, 2026-11-19 (preview v3; «حله» to the prompt-not-write approach); D8–D13
proposed by the planner 2026-11-20, **owner to confirm at gate 2**. Every choice below is decided —
**decide nothing new.** If reality contradicts this plan, STOP and ask.
**Branch:** `alireza/new-project` — the implementer creates it from the base.
**Base:** `alireza/skeleton-and-import` @ `a0235f4` (plan 0002 merged at `861dd2a`)
**Companion:** None
**Steps changed:** no preview of its own — preview 0001 covers it (owner, 2026-11-19)

---

## فهم من از درخواست

می‌خواهید پروژه را از داشبورد اضافه کنید، نه با ویرایش دستی فایل تنظیمات. داشبورد این‌ها را می‌پرسد:
- اسم،
- مسیر پروژه،
- مسیر runها،
- repoها،
- پروفایل.

بعد پوشه را نگاه می‌کند و برای هر چیزی که کم است، یک پرامپت آماده نشان می‌دهد که آن را به ایجنت خودتان
(Claude، Cursor یا هر ایجنت دیگری) بدهید:
- `.ai/`، درخت دانش مستقل از ایجنت. اگر `.claude/`، `.cursor/`، `CLAUDE.md` یا `AGENTS.md` هست، از روی آن‌ها ساخته می‌شود.
- `.orchestra/`.

ایجنت فایل‌ها را در git خود پروژه می‌سازد. **داشبورد هیچ‌وقت در پوشه‌ی پروژه نمی‌نویسد** (ADR 0005). فقط
فهرست پروژه‌های خودش را ذخیره می‌کند.

برداشت من:
- پروژه‌هایی که امروز در `appsettings.Local.json` هستند می‌مانند و در داشبورد با برچسب «از تنظیمات» و
  غیرقابل ویرایش نشان داده می‌شوند.
- پروژه‌هایی که از داشبورد اضافه می‌شوند در فایل خود داشبورد (`data/projects.json`) ذخیره می‌شوند و
  قابل ویرایش و حذف هستند.

این پلن کارهای زیر را **نمی‌کند**:
- پروفایل‌ها را نمی‌سازد (فاز ۲، پلن 0006). در این پلن فهرست پروفایل فقط «default» است و غیرفعال.
- `project.json` را نمی‌خواند و اعمال نمی‌کند. فقط متنش را برای پرامپت می‌سازد.
- پروژه‌های فعلی را به ساختار تازه منتقل نمی‌کند (پلن 0007).

## جریان کار

1. در «تنظیمات › پروژه‌ها»، فهرست پروژه‌ها را می‌بینید و «پروژه‌ی جدید» را می‌زنید.
2. فرم را پر می‌کنید: اسم، شناسه (خودکار از اسم ساخته می‌شود و قابل تغییر است)، و مسیر ریشه.
3. «بررسی پوشه» را می‌زنید. داشبورد فقط **وجود** این چیزها را نگاه می‌کند:
   - `AGENTS.md`، `CLAUDE.md`، `.claude/`، `.cursor/`، `.cursorrules`،
     `.github/copilot-instructions.md`، `.ai/`، `.orchestra/`، `orchestrator/`،
   - و repoهای زیرپوشه (پوشه‌هایی که `.git` دارند).

   مسیر runها را هم پیشنهاد می‌کند.
4. نتیجه‌ی بررسی، کنار یک پرامپت آماده می‌آید. پرامپت را کپی می‌کنید و به ایجنت خودتان می‌دهید.
5. «ذخیره» پروژه را به فهرست اضافه می‌کند و آن را می‌خواند. «بررسی دوباره» بعد از کار ایجنت، تیک‌ها را
   به‌روز می‌کند.
6. پروژه‌ای را که از داشبورد اضافه کرده‌اید می‌توانید ویرایش یا حذف کنید. حذف فقط آن را از داشبورد برمی‌دارد
   و به پوشه دست نمی‌زند.

فازها:
- **A:** ذخیره‌ی پروژه‌ها در فایل داشبورد، و API.
- **B:** بررسی پوشه و ساختن پرامپت.
- **C:** صفحه‌ها.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- «تنظیمات › پروژه‌ها»: Project A و Project B با برچسب «از تنظیمات» دیده می‌شوند و دکمه‌ی حذف ندارند.
- یک پوشه‌ی آزمایشی خالی با یک `CLAUDE.md` بسازید و آن را به‌عنوان پروژه‌ی جدید اضافه کنید:
  - بررسی، `CLAUDE.md` را «پیدا شد» و `.ai/` و `.orchestra/` را «نیست» نشان می‌دهد.
  - پرامپت، ساختن هر دو را می‌خواهد و می‌گوید `CLAUDE.md` را بخوان.
- پرامپت را به ایجنت بدهید. بعد از کار ایجنت، «بررسی دوباره» همه را «پیدا شد» نشان می‌دهد.
- پروژه را ذخیره کنید. در صفحه‌ی runها با صفر run دیده می‌شود.
- پروژه را حذف کنید. از فهرست می‌رود و پوشه‌اش دست‌نخورده می‌ماند.
- مسیری را بدهید که وجود ندارد. خطای روشن در فرم نشان داده می‌شود.
- همه را در فارسی و انگلیسی، و در تم روشن و تاریک، چک کنید.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md) and plan 0001's ground rules 2–6.
2. **The folder check only tests existence** (`File.Exists`, `Directory.Exists`, one level of
   `Directory.EnumerateDirectories` for repos). It never reads a file's content and never writes.
3. The only file this plan writes is the dashboard's own `projects.json` (§6.1).

### Baseline

Plan 0002's gates green; counts as recorded in `.ai/ci.md`.

## 1. Goal

Add, edit and remove projects from the dashboard, check a project folder for the knowledge and
orchestrator structure, and generate the prompt that makes the owner's agent create what is missing.

## 2. Out of scope

Profiles (0006); reading or applying `project.json`; moving existing projects (0007); writing into any
project folder; creating git repositories; browsing folders in a picker (the path is typed or pasted).

## 3. Rules that apply

Plan 0001 §3, plus [ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) (the
dashboard never writes into a project) and [ADR 0003](../../.ai/decisions/0003-read-only-dashboard-first.md)
(it may edit its own settings).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | Two origins: `config` (from `Orchestra:Projects`, read-only in the UI) and `dashboard` (stored in `<data folder>/projects.json`, editable). An id present in both → the config one wins and a warning is logged |
| D2 | `projects.json` is the dashboard's own data, **not** the database (the database is rebuildable; this file is not). Written atomically (temp file + replace). Its folder is the database's folder |
| D3 | The folder check tests existence only (§0 rule 2) |
| D4 | The prompt is English (for an agent), built from the check result; the page around it is localized |
| D5 | The `project.json` shown in the prompt is the phase-2 shape, with `profile: "default"` and empty `overrides` — the engine does not read it yet |
| D6 | Removing a project deletes its rows (runs, library, its import logs) and its entry; nothing on disk outside the dashboard's data |
| D7 | In Docker the server sees only mounted folders: the form says so, and a path that does not exist **inside the container** is reported as missing, not as an error of the dashboard |
| D8 | **Registry refresh.** `ProjectRegistry` stays a singleton. It takes `IOptions<OrchestraOptions>` and `ProjectStore`, builds one immutable snapshot (config projects, then store projects whose id is not taken) at construction, and exposes `All`, `Find(id)` and `OriginOf(id)` (`ProjectOrigin.Config` · `ProjectOrigin.Dashboard`). `Create/Update/Delete` go through the registry: it writes the store, and only after the write succeeds swaps the snapshot (`Volatile.Write` of a new dictionary under a lock). Nothing re-reads `projects.json` per request |
| D9 | **One project shape.** A store entry becomes an in-memory `ProjectOptions` (`Id`, `Name`, `RunsPath`, `RootPath`, `Repos`, `Library = new()` → the library defaults). `ProjectOptions` gains `Repos` (`List<string>`, default empty) so a config project may list them too; a config project without it has `repos: []`. Custom library globs stay config-only, so `projects.json` has **no `library` field** |
| D10 | **`rootPath` in the DTO** is `ProjectOptions.ResolveRootPath()` and may be `null` for a config project (the UI shows "—"). A dashboard project always has it (required, §6.1) |
| D11 | **Import and delete hold the import gate.** `ImportService` gains `ImportOneAsync(ProjectOptions, ct)`, which **waits** for the gate (`Gate.WaitAsync`) instead of refusing. Create/update save, then call it. Delete waits for the gate too, then in one transaction: `ImportLogs.Where(ProjectId == id).ExecuteDeleteAsync` (their `ImportError`s go by the existing cascade) and the `Project` row (its `Run` and `LibraryItem` rows go by cascade); no `Project` row (never imported) is fine. Then the store entry is removed. If the store write fails after the rows went, the next import rebuilds them — the database is rebuildable (D2) |
| D12 | **No field-error list from the server.** Field messages come from the form's Zod schema only (§6.5). The server re-validates and answers `Projects.Project.Invalid` (400, `code` only, as `ProblemResults.From` does today); the form maps `Projects.Project.IdTaken` to the id field. `ProblemResults`, `Error` and `ApiError` are not changed |
| D13 | **Menu category `settings`.** `MenuCategory` and the sidebar's `MENU_CATEGORIES` gain `settings`, shown last; the projects feature is its only item. Route `/settings/projects/`, the edit page `/settings/projects/edit/?id=` — the same query-string pattern as plan 0002's `/library/view/` |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Projects/ProjectStore.cs` (`projects.json` read/write; path = `Path.Combine(Path.GetDirectoryName(options.ResolveDatabasePath(env.ContentRootPath)), "projects.json")`, D2) and `ProjectOrigin.cs` (enum `Config` · `Dashboard`, D8) | NEW | A |
| 2 | `api/src/Orchestra.Api/Modules/Projects/ProjectRegistry.cs` — merge config + store, `OriginOf`, snapshot swap (D8); `ProjectsModuleInstaller.cs` registers `ProjectStore` | EDIT | A |
| 3 | `api/src/Orchestra.Api/Modules/Projects/Features/{CreateProject,UpdateProject,DeleteProject}/*` | NEW | A |
| 4 | `api/src/Orchestra.Api/Modules/Projects/Features/CheckFolder/*` (`FolderCheck`, `FolderCheckHandler`) | NEW | B |
| 5 | `api/src/Orchestra.Api/Modules/Projects/Features/ScaffoldPrompt/*` (`ScaffoldPromptBuilder`) | NEW | B |
| 6 | `api/src/Orchestra.Api/Modules/Projects/ProjectsController.cs` — the endpoints of §6.3 | EDIT | A–B |
| 7 | `api/src/Orchestra.Api/Common/Errors/ErrorMessages.cs` — §6.3 codes (`Projects.Project.NotFound` exists already) | EDIT | A |
| 8 | `api/tests/Orchestra.Api.Tests/Projects/**` | NEW | A–B |
| 9 | `ui/src/features/projects/**` (manifest, api, queries, types, i18n fa/en, components) | NEW | C |
| 10 | `ui/src/app/[locale]/settings/projects/page.tsx`, `…/settings/projects/edit/page.tsx` | NEW | C |
| 11 | `ui/src/features/registry.ts` (add `projectsFeature`); `ui/src/lib/i18n/messages/common.{fa,en}.ts` — `menu.settings`, and `states.noProjects` reworded (§6.5) | EDIT | C |
| 12 | `ui/package.json` — exact pins `react-hook-form` `7.88.0`, `@hookform/resolvers` `5.9.1`, `zod` `4.6.5` | EDIT | C |
| 13 | `.ai/architecture/data-model.md` — the `Project` table row says "registered project (from configuration or the dashboard's `projects.json`)"; "Import = rebuild per project" names the two origins and that delete removes the rows and import logs (D11); a new "The dashboard's own data" section (`projects.json`, import logs) | EDIT | A |
| 14 | `.ai/api/conventions.md` — atomic writes of the dashboard's own files; the installer `Order` list gains `Library 60` (drift since plan 0002) | EDIT | A |
| 15 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `ProjectOptions.Repos` (D9) | EDIT | A |
| 16 | `api/src/Orchestra.Api/Modules/Import/ImportService.cs` — `ImportOneAsync`, which waits for the gate (D11); `EnterAsync(ct)` (`Gate.WaitAsync`) next to `TryEnter`/`Leave`, which the delete handler calls before deleting and leaves in a `finally`. `ImportOneAsync` takes the gate itself; callers that already hold it (none today) must not call it | EDIT | A |
| 17 | `api/src/Orchestra.Api/Modules/Projects/Features/ListProjects/{ProjectDto,ListProjectsHandler}.cs` — `origin`, `rootPath` (nullable, D10), `repos` | EDIT | A |
| 18 | `ui/src/lib/features/define-feature.ts` — `'projects'` in `FEATURE_KEYS`, `'settings'` in `MenuCategory`; `ui/src/components/layout/app-sidebar.tsx` — `'settings'` last in `MENU_CATEGORIES` (D13); `ui/src/i18n/messages.ts` — `projects: projectsMessages.{en,fa}` | EDIT | C |
| 19 | `ui/src/lib/api/client.ts` — `put` and `delete`; `request()` returns `undefined` for a 204 or an empty body instead of calling `response.json()`; its test `ui/src/lib/api/client.test.ts` (NEW, §7) | EDIT | C |
| 20 | `ui/src/lib/projects/queries.ts` — the one shared `ProjectDto` gains `origin`, `rootPath: string \| null`, `repos: string[]`; `features/projects` imports it and defines no second `ProjectDto` | EDIT | C |

## 6. Per-file specification

### 6.1 `projects.json`

```json
{ "schema": 1, "projects": [ { "id": "myshop", "name": "MyShop", "rootPath": "D:\\work\\MyShop",
  "runsPath": "D:\\work\\MyShop\\.orchestra\\runs", "repos": ["api", "web"], "profile": "default",
  "createdAt": "2026-11-20T08:00:00Z" } ] }
```

Validation as plan 0001 §6.4 for `id`, `name`, `runsPath`; `rootPath` required, absolute; `repos` 0–20
names `^[A-Za-z0-9._-]{1,60}$`; `profile` must be `default` for now. A missing file = no dashboard
projects. A file that does not parse → start continues, the config projects load, one error is logged and
shown on the projects page (the file is never overwritten while it does not parse: create/update/delete
return 409 `Projects.Store.Unreadable`).

### 6.2 The folder check

`FolderCheck(rootPath)` → `{ exists, isAbsolute, items: { name, kind (file|folder), found }[], repos:
string[], suggestedRunsPath }`. Items, in this order: `AGENTS.md`, `CLAUDE.md`, `.claude/`, `.cursor/`,
`.cursorrules`, `.github/copilot-instructions.md`, `.ai/`, `.orchestra/`, `orchestrator/`. Repos: `.git`
in the root → `["."]`; else every direct subfolder holding `.git` (sorted, max 20). Suggested runs path:
`orchestrator/runs` if that folder exists, else `.orchestra/runs`. Also, for each repo, whether
`<repo>/.ai/` exists (reported as `repoKnowledge: { repo, found }[]`).

### 6.3 API

| Endpoint | Does |
|---|---|
| `GET api/projects` | (plan 0001) + `origin` (`config` · `dashboard`), `rootPath` (nullable, D10), `repos`. The response stays a plain list (five pages read it through `lib/projects/queries.ts`) |
| `GET api/projects/store` | `{ readable: bool, error: string \| null }` — feeds the unreadable-file alert of §6.5 |
| `POST api/projects` | create (dashboard origin) → saves, then `ImportOneAsync` (D11) → 201 + the project |
| `PUT api/projects/{id}` | update a dashboard project (id is immutable) → saves, then re-imports; unknown id → 404 `Projects.Project.NotFound`; config project → 409 `ReadOnly` |
| `DELETE api/projects/{id}` | D6 / D11 → 204; unknown id → 404; config project → 409 `ReadOnly` |
| `POST api/projects/check` `{ rootPath }` | §6.2 |
| `POST api/projects/scaffold-prompt` `{ name, rootPath, repos, check }` | `{ text }` (§6.4) |

| Code | Status | en | fa |
|---|---|---|---|
| `Projects.Project.IdTaken` | 409 | A project with this id already exists. | پروژه‌ای با این شناسه وجود دارد. |
| `Projects.Project.ReadOnly` | 409 | This project comes from the configuration file; change it there. | این پروژه از فایل تنظیمات می‌آید. آن را همان‌جا تغییر دهید. |
| `Projects.Project.Invalid` | 400 | Some fields are not valid. | بعضی فیلدها معتبر نیستند. |
| `Projects.Store.Unreadable` | 409 | projects.json cannot be read; fix or remove it first. | فایل projects.json خوانده نمی‌شود. اول آن را درست یا حذف کنید. |

There is no per-field error list (D12): the form validates with Zod, and the server's 400 is a
backstop with `code` only.

### 6.4 The scaffold prompt

`ScaffoldPromptBuilder` assembles numbered sections; a section is included only when its condition
holds. Text (English, copy exactly; `{…}` filled):

```text
This folder is being added to Orchestra, a dashboard for a gated multi-agent orchestrator.
Project: {name}. Root: {rootPath}. Repos: {repos|none}.
Work only inside this root. Show me the file tree you will create and wait for my yes before writing.

[if any of CLAUDE.md, .claude/, .cursor/, .cursorrules, .github/copilot-instructions.md, AGENTS.md exists]
1. Read what already instructs agents here: {found list}. Keep every rule; move each into the place below
   that owns it; never keep the same rule in two files.

[if .ai/ is missing]
2. Create .ai/ — the project's knowledge tree, for any agent:
   - .ai/README.md: an index "doing X -> read Y".
   - .ai/architecture/overview.md: the pieces and how they talk, read from the code.
   - .ai/decisions/README.md: an empty ADR index (one decision per file, never rewritten).
   - one folder per area that has its own rules (for example .ai/api/, .ai/ui/), each rule in one doc.
   - .ai/context/README.md: one session log per session, newest read first.
   Write only what you verified in the code; mark anything else as a question for me.
[if repos > 1 and some repo lacks .ai/] Each repo without .ai/ gets its own: {repos without .ai}.

[if AGENTS.md is missing]
3. Create AGENTS.md: a pointer, not a rule book — what the project is, where to start (.ai/README.md,
   the newest session log, .orchestra/workflow.md), and the few rules that bite first, each linked.
[if CLAUDE.md exists] Make CLAUDE.md one line that points to AGENTS.md.

[if .orchestra/ is missing]
4. Create .orchestra/ — the orchestrator's working folder, in git:
   - README.md (what each folder is), workflow.md (the gates: understanding -> HTML preview -> plan ->
     review by another model family -> my approval -> branch -> implement -> verify -> hand over
     uncommitted),
   - ideas/README.md, research/, previews/, plans/README.md, plans/TEMPLATE.md, prompts/,
   - project.json:
{projectJson}
   - add .orchestra/evidence/ and .orchestra/runs/ to .gitignore.

5. Commit nothing. Tell me what you created and what you could not verify.
```

`{projectJson}` = indented JSON `{ "name", "repos", "profile": "default", "overrides": {} }`. When
nothing is missing, the text is one line: `Nothing to create: .ai/, AGENTS.md and .orchestra/ already exist.`

### 6.5 UI

`features/projects` — `/{locale}/settings/projects/` (table: name, id, root, runs, origin badge,
last import, actions edit/remove for dashboard projects; a warning alert when `projects.json` is
unreadable) and `/{locale}/settings/projects/edit/?id=` (no id = new). The edit page: the form
(React Hook Form + Zod built from `t`), **Check folder** (button), the check result list (found →
success badge, missing → warning badge), the repos, the suggested runs path (fills the field when it is
empty), the prompt in a read-only code block with **Copy** and **Check again**, and **Save**. Remove asks
in a confirm dialog that names the project and says the folder is not touched.

| Key | en | fa |
|---|---|---|
| `common.menu.settings` | Settings | تنظیمات |
| `common.states.noProjects` (reworded; replaces the appsettings text) | No project yet. Add one in Settings › Projects. | هنوز پروژه‌ای نیست. یکی را در تنظیمات › پروژه‌ها اضافه کنید. |
| `projects.title` | Projects | پروژه‌ها |
| `projects.new` | New project | پروژه‌ی جدید |
| `projects.edit` | Edit project | ویرایش پروژه |
| `projects.origin.config` | From the configuration file | از فایل تنظیمات |
| `projects.origin.dashboard` | Added here | اضافه‌شده در داشبورد |
| `projects.fields.name` | Name | اسم |
| `projects.fields.id` | Id | شناسه |
| `projects.fields.rootPath` | Project folder | پوشه‌ی پروژه |
| `projects.fields.runsPath` | Runs folder | پوشه‌ی runها |
| `projects.fields.repos` | Repos | repoها |
| `projects.fields.profile` | Profile | پروفایل |
| `projects.fields.profileLater` | Profiles arrive in phase 2; every project uses the default for now. | پروفایل‌ها در فاز ۲ می‌آیند. فعلاً همه‌ی پروژه‌ها پیش‌فرض را دارند. |
| `projects.dockerHint` | In Docker, only folders mounted into the container can be checked. | در Docker فقط پوشه‌هایی که به container وصل شده‌اند بررسی می‌شوند. |
| `projects.check` | Check folder | بررسی پوشه |
| `projects.checkAgain` | Check again | بررسی دوباره |
| `projects.found` | found | پیدا شد |
| `projects.missing` | missing | نیست |
| `projects.folderMissing` | This folder does not exist. | این پوشه وجود ندارد. |
| `projects.notAbsolute` | Use a full path. | مسیر کامل را بنویسید. |
| `projects.promptTitle` | Prompt for your agent | پرامپت برای ایجنت شما |
| `projects.promptHint` | Give this to the agent you work with in this folder. It creates the files; the dashboard never writes into your project. | این را به ایجنتی بدهید که با آن در این پوشه کار می‌کنید. فایل‌ها را ایجنت می‌سازد و داشبورد هیچ‌وقت در پروژه‌ی شما نمی‌نویسد. |
| `projects.copy` | Copy | کپی |
| `projects.copied` | Copied | کپی شد |
| `projects.save` | Save | ذخیره |
| `projects.saved` | Project saved | پروژه ذخیره شد |
| `projects.remove` | Remove | حذف |
| `projects.removeTitle` | Remove {name}? | {name} حذف شود؟ |
| `projects.removeBody` | It leaves the dashboard with its runs and library. Its folder is not touched. | پروژه با runها و کتابخانه‌اش از داشبورد حذف می‌شود. به پوشه‌اش دست زده نمی‌شود. |
| `projects.removed` | Project removed | پروژه حذف شد |
| `projects.storeUnreadable` | projects.json cannot be read. Projects added here are hidden until it is fixed or removed. | فایل projects.json خوانده نمی‌شود. تا وقتی درست یا حذف نشود، پروژه‌های اضافه‌شده در داشبورد دیده نمی‌شوند. |
| `projects.errors.IdTaken` | This id is taken. | این شناسه گرفته شده است. |
| `projects.errors.required` | Required. | لازم است. |
| `projects.errors.idFormat` | Lowercase letters, digits and dashes, up to 40. | حروف کوچک انگلیسی، رقم و خط تیره، تا ۴۰ کاراکتر. |

## 7. Tests

| File | Facts |
|---|---|
| `Projects/ProjectStoreTests.cs` | missing file → empty; round trip; atomic write (temp then replace); an unparsable file is reported and never overwritten |
| `Projects/ProjectRegistryTests.cs` | config + store merged; duplicate id → config wins, warning; origin set; after create the new project is in `All` without a restart; a failed store write leaves the snapshot unchanged |
| `Projects/CreateUpdateDeleteTests.cs` | create validates and imports; id taken → 409; update of a config project → 409 ReadOnly; unknown id → 404; delete removes runs, library items, import logs **and their import errors**, works for a never-imported project, and leaves the temp project folder byte-for-byte unchanged |
| `Projects/FolderCheckTests.cs` | each item found/missing in a temp tree; repos from subfolder `.git`; root `.git` → `["."]`; suggested runs path both ways; missing folder; relative path |
| `Projects/FolderCheckReadOnlyTests.cs` | after a check, the temp tree's file list and last-write times are unchanged |
| `Projects/ScaffoldPromptBuilderTests.cs` | nothing missing → the one line; only `.ai/` missing → sections 2 and 5; `CLAUDE.md` present → sections 1 and the pointer line; multi-repo without `.ai/` lists them; `project.json` embedded and valid JSON |
| `ui: features/projects/**/*.test.tsx` | config projects have no remove; a config project without a root shows "—"; the form's validation messages; `IdTaken` lands on the id field; check result badges; the suggested runs path fills an empty field only; copy; remove confirm names the project |
| `ui: lib/api/client.test.ts` | a 204 `delete` resolves to `undefined`; `put` sends the JSON body |
| existing `ui/src/architecture.test.ts`, `i18n/messages.test.ts` | stay green: `projects` registered once, fa/en keys equal |

## 8. Stop and ask

- A test needs the import gate in a way D11 does not cover (for example, a request that must not wait).
- `ExecuteDeleteAsync` does not cascade to `ImportError` on the test database (the cascade of D11 is a
  database rule, not an EF one).

## 9. Phases

Gates as plan 0001 §9.

- **A — store and CRUD.** Files 1–3, 6 (CRUD), 7, 8 (store/registry/CRUD), 13–17. **Gate.** Stop.
- **B — check and prompt.** Files 4, 5, 6 (check/prompt), 8 (check/prompt). **Gate.** **Owner
  checks:** `POST api/projects/check` on both real roots lists what exists; the prompt for Project B's
  root mentions `orchestrator/` found and `.orchestra/` missing. Stop.
- **C — screens.** Files 9–12, 18–20. **Gate.** **Owner checks:** the items of «نتیجه». Stop.

## 10. Definition of done

All gates green and counts recorded; the read-only tests pass; session log written; this header
`Status: Done` with the date, commit and merge.
