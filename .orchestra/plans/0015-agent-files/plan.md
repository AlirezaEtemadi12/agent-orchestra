# Plan 0015 — Agent files stay out of git; setup and board fixes found by adding Orchestra

**Status:** Draft
**Created:** 2026-11-25 · **Updated:** 2026-11-25 · **Done:** —
**Preview:** none — the New project check keeps its look; it lists fewer rows and gains one "Also found" group
**Run:** `0015-agent-files`
**Planned by:** Claude Opus 5.5, 2026-11-25. **Executor:** another agent, ideally a cheaper model.
**Plan review:** <the reviewing model, or the owner> — a different model family than the planner's.
**Decided by:** Alireza, 2026-11-25, in the chat. Every choice below is already decided — **decide nothing new.** If
reality contradicts this plan, STOP and ask.
**Branch:** `alireza/agent-files` — the implementer creates it.
**Base:** `main` after plan 0010 is closed — record the hash here. **Not before:** plan 0010 runs in this same
working tree, and D7 renames the repo key its run uses.
**Companion:** None
**Steps changed:** no preview (no new screen)

---

## فهم من از درخواست

- صفحه‌ی New project برای `.cursor/`، `.cursorrules`، `CLAUDE.md` و copilot برچسب «نیست» نشان می‌دهد، انگار چیزی کم است.
  این‌ها فایل‌هایی‌اند که خود عامل‌های هوش مصنوعی برای خودشان می‌سازند. ارکستر به آن‌ها نیاز ندارد و هر کس با
  عامل خودش کار می‌کند. پس:
  - فقط چیزهایی که ارکستر لازم دارد (`AGENTS.md`، `.ai/`، `.orchestra/`) وضعیت «پیدا شد / نیست» می‌گیرند؛
  - فایل‌های عامل‌ها و پوشه‌ی قدیمی `orchestrator/` فقط **اگر باشند** زیر عنوان «این‌ها هم پیدا شد» می‌آیند، با این
    توضیح که prompt راه‌اندازی قاعده‌هایشان را به `AGENTS.md` می‌برد و آن‌ها را به `.gitignore` اضافه می‌کند.
- قاعده: فایل‌هایی که یک عامل برای خودش می‌سازد در git نمی‌آیند. **یک استثنا:** `.claude/skills/`، سه فایل
  سه‌خطی که setup برای skillها می‌سازد. حجمی ندارند و در git می‌مانند تا کسی که ریپو را clone می‌کند skillها را پیدا
  کند.
- `.gitignore` همین ریپو این قاعده را اجرا می‌کند.
- چهار ایراد که موقع اضافه کردن خود Orchestra پیدا شد:
  1. وقتی پوشه‌ی `.orchestra/runs/` نیست (پروژه‌ای که هنوز هیچ run ای نداشته)، import کل کار را رها می‌کند و
     Library خالی می‌ماند. از این به بعد «run ای نیست» خطا نیست و Library خوانده می‌شود.
  2. setup برای پروژه‌ی تک‌ریپو، نام پروژه را با فاصله نام ریپو گذاشت (`Project Orchestra`). نام ریپو بی‌فاصله
     می‌شود (`Project-Orchestra`)، و در `orchestrator.json` همین ریپو هم عوض می‌شود.
  3. setup فرمان‌های وریفای Orchestra را پیدا نکرد، چون `Orchestra.slnx` و `package.json` یک پوشه پایین‌ترند
     (`api/`، `ui/`). از این به بعد یک پوشه پایین‌تر را هم نگاه می‌کند.
  4. زیر فهرست بررسی فقط یک «.» نوشته می‌شود. به‌جایش نوشته می‌شود «خود این پوشه ریپو است».
  5. روی برد یک کارت «health» می‌آید که یک روز در Running می‌ماند؛ health یک run نیست و کارتی نمی‌گیرد.
  6. کارت run تازه تا import بعدی با نام پوشه (`gridify-tables`) نشان داده می‌شود؛ عنوان پلن از همان اول نشان داده می‌شود.

**کارهایی که این پلن انجام نمی‌دهد:**
- `.gitignore` پروژه‌های دیگر (Project A، Project B) را عوض نمی‌کند؛ prompt راه‌اندازی از عامل آن پروژه می‌خواهد.
- هیچ فایل عاملی را پاک نمی‌کند.
- `.claude/skills/` را از git بیرون نمی‌برد.

## جریان کار

1. **فاز ۱، API.** بررسی پوشه، prompt راه‌اندازی، import بدون پوشه‌ی runs، نام ریپو و تشخیص فرمان‌های وریفای.
2. **فاز ۲، UI.** فهرست بررسی با گروه «این‌ها هم پیدا شد» و متن «خود این پوشه ریپو است».
3. **فاز ۳، قاعده و این ریپو.** ADR 0005، `AGENTS.md`، `.gitignore` و نام ریپو در `orchestrator.json`.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

صفحه‌ی New project فقط چیزهایی را «نیست» می‌گوید که واقعاً لازم‌اند. پروژه‌ی تازه بدون هیچ run ای هم در Library دیده
می‌شود.

**چک‌لیست شما در اپ:**
- [ ] **New project، پوشه‌ی Orchestra:** سه ردیف `AGENTS.md`، `.ai/`، `.orchestra/` با «پیدا شد». زیرش «این‌ها هم پیدا
      شد» با `.claude/` و توضیحش. هیچ «نیست» ای برای `.cursor/` و مانند آن نیست. زیر فهرست: «خود این پوشه ریپو است».
- [ ] **New project، یک پوشه‌ی خالی:** همان سه ردیف با «نیست»، و گروه «این‌ها هم پیدا شد» اصلاً نیست.
- [ ] **Library:** Re-import را بزنید؛ در Import log برای Orchestra خطای «runs folder» نیست.
- [ ] **setup یک پروژه‌ی آزمایشی تک‌ریپو با فاصله در نام:** در `orchestrator.json`، نام ریپو خط تیره دارد، نه فاصله.

## 0. Ground rules for the executor

1. Follow [`../../workflow.md`](../../workflow.md): branch before the first edit; never commit/push/merge unless
   asked in that message; one phase = one uncommitted diff.
2. **Do not start while a run is open in this repo**: `.orchestra/runs/` must hold no run folder (only `_`-prefixed
   files). D7 renames the repo key a live run's `task.json` holds.
3. The only new Persian strings are the ones in D2, given there verbatim; copy them byte for byte.

### Baseline

```
cd api; dotnet build Orchestra.slnx -warnaserror; dotnet test Orchestra.slnx      # 313 on 2026-11-25
cd ui; npm run typecheck; npm run lint; npm run test; npm run build                # 85 on 2026-11-25
```

Plan 0010 lands first and raises both; record the real counts before the first edit. They never drop, except the
tests §8 names, which change on purpose.

## 1. Goal

The New project check asks only for what Orchestra needs; agent-specific files are named when found and kept out of
git by rule; and setup and import work for a single-repo project shaped like Orchestra.

## 2. Out of scope

- Writing any project's `.gitignore` from the app (the prompt asks the project's own agent).
- Deleting any agent file, here or elsewhere; taking `.claude/skills/` out of git.
- Creating `.orchestra/runs/` from setup (D6 makes it unnecessary).
- The engine; Project A; Project B.

## 3. Rules that apply

- [`../../workflow.md`](../../workflow.md) — gates; "a rule change reviews the old rules" (§6.3).
- [ADR 0005](../../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) — **this plan amends it** (D4).
- [ADR 0009](../../../.ai/decisions/0009-local-app-sets-projects-up.md) — setup is the only writer into a project.
- [ADR 0004](../../../.ai/decisions/0004-persian-and-english.md) — Persian text.
- `.ai/api/conventions.md`, `.ai/ui/conventions.md`.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | **Required and optional items.** `FolderCheckItemDto` gains `bool Required`. Required, always in `Items`: `AGENTS.md`, `.ai/`, `.orchestra/` (in that order, with today's found/`InEachRepo` rules). Optional, in `Items` **only when found**: `CLAUDE.md`, `.claude/`, `.cursor/`, `.cursorrules`, `.github/copilot-instructions.md`, `orchestrator/`. `IsReady` and everything else in `FolderCheckDto` are unchanged. The folder that does not exist (and `Invisible`) lists the three required items only |
| D2 | **The check in the UI.** `check-result.tsx` lists required items as today (badge found/missing). Optional items that came back go under a heading, one line each with a `found` badge, `orchestrator/` with a suffix. Below the list, when `repos` is exactly `["."]`, a sentence replaces the bare "."; otherwise the comma list stays. New keys in `ui/src/features/projects/i18n/{en,fa}.ts` under `form`: `alsoFound` en `Also found` · fa `این‌ها هم پیدا شد`; `alsoFoundNote` en `Orchestra does not need these. The setup prompt asks your agent to move their rules into AGENTS.md and add them to .gitignore.` · fa `ارکستر به این‌ها نیاز ندارد. prompt راه‌اندازی از عامل شما می‌خواهد قاعده‌هایشان را به AGENTS.md ببرد و آن‌ها را به .gitignore اضافه کند.`; `oldLayout` en `old layout` · fa `چیدمان قدیمی`; `rootIsRepo` en `The folder itself is the repo.` · fa `خود این پوشه ریپو است.` |
| D3 | **The scaffold prompt names the files to ignore.** In `ScaffoldPromptBuilder.Build`, when any of `CLAUDE.md`, `.cursor/`, `.cursorrules`, `.github/copilot-instructions.md`, `.claude/` is found, one line is added right after section 1: `   Then add them to the project's .gitignore: <the found ones, comma-separated>. For .claude/ write two lines, ".claude/*" and "!.claude/skills/": Orchestra's skill pointers stay in git.` (The `.claude/` sentence only when `.claude/` is among them.) Nothing else in the prompt changes |
| D4 | **The rule** (one place, ADR 0005): a new `**Amended:** 2026-11-25` line, and one bullet in Decision after the `.orchestra/` bullet: `- **Agent-specific files are not committed.** What an AI agent writes for itself — `.cursor/`, `.cursorrules`, `CLAUDE.md`, `.github/copilot-instructions.md`, `.claude/` — stays out of git: each person works with the agent they choose, and `AGENTS.md` with `.ai/` are the only agent instructions in git. One exception: `.claude/skills/`, the three-line pointers setup writes to the skills in `.orchestra/skills/`, so a clone finds them.` `AGENTS.md`'s "**Never create a `CLAUDE.md`.**" becomes "**Never create a `CLAUDE.md`** or any other agent-specific file ([ADR 0005](.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md))." |
| D5 | **This repo's `.gitignore`** gains, after the "Graph output" block: a comment `# Agent-specific files: each person uses their own agent (ADR 0005). Orchestra's skill pointers stay.` and the lines `CLAUDE.md`, `.cursor/`, `.cursorrules`, `.github/copilot-instructions.md`, `.claude/*`, `!.claude/skills/` |
| D6 | **No runs folder is not an error.** In `ImportService.ImportProjectAsync`, when `RunsPath` is set but the folder does not exist, the file sources under it (ledger, `task.json` files, archives) are read as empty — events from the database are read as today — and the import goes on: the Library is indexed and the log is saved **with no error**. An empty `RunsPath` keeps today's error ("The runs folder does not exist.") — that is a broken config, not a new project |
| D7 | **A single-repo key has no spaces.** `SetupPlanner` gets `public static string RepoKey(string name)`: every run of characters outside `[A-Za-z0-9_.-]` becomes one `-`, leading and trailing `-` are trimmed, and an empty result is `repo`. `SetupPlanner.VerifyCommands` and `SetupApplier.RenderConfig` use `RepoKey(request.Name)` wherever they use `request.Name` as a key today. Reason: plan 0013 D3 reads a repo name in a phase heading with `[A-Za-z0-9_.-]+`. This repo's `.orchestra/orchestrator.json` renames the key `Project Orchestra` → `Project-Orchestra` in `repos` and in `verifyCommands` (phase 3) |
| D9 | **`health` is not a run.** `run.ps1 health` takes pool slots with `taskId = 'health'` (`engine/lib/agents.ps1:1112`), and the board makes a "health" card of those `pool.*` events that sits in Running for a day. `BoardHandler` skips a `(project, taskId)` group whose events are all `pool.*` and that matches no run row. The engine is unchanged |
| D10 | **A card's title from the plan before the import.** When the card's run row is missing or has no `Task`, `BoardHandler` passes the `task` field of the instance's `run.created` event to `PlanName.From` instead of `run?.Task`. So the 0010 card reads `0010 · Every growing table …`, not the slug, before the next import |
| D8 | **Verify commands one folder down.** `VerifyCommandDetector.Detect(folder)`: when the folder itself has neither a solution nor a `package.json`, look at its direct subfolders, sorted by name, skipping names that start with `.` and `node_modules`, `bin`, `obj`. Each subfolder with a solution adds `dotnet build <sub>/<file>` and `dotnet test <sub>/<file>` (its first `*.slnx` by name, else its first `*.sln`); then each subfolder with a `package.json` adds `npm --prefix <sub> run <script>` for the scripts today's rule picks. .NET subfolders first, then npm (today's order). Paths use `/`. A folder that has its own solution or `package.json` behaves exactly as today |

## 5. File manifest

| # | File | Change | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Projects/Features/CheckFolder/FolderCheck.cs` | EDIT — D1 | 1 |
| 1a | `api/src/Orchestra.Api/Modules/Runs/Features/Board/BoardHandler.cs`, `api/tests/Orchestra.Api.Tests/Runs/BoardTests.cs` | EDIT — D9, D10 | 1 |
| 2 | `api/src/Orchestra.Api/Modules/Projects/Features/ScaffoldPrompt/ScaffoldPromptBuilder.cs` | EDIT — D3 | 1 |
| 3 | `api/src/Orchestra.Api/Modules/Import/ImportService.cs` | EDIT — D6 | 1 |
| 4 | `api/src/Orchestra.Api/Modules/Projects/Features/Setup/{SetupPlanner,SetupApplier,VerifyCommandDetector}.cs` | EDIT — D7, D8 | 1 |
| 5 | `api/tests/Orchestra.Api.Tests/Projects/{FolderCheckTests,ScaffoldPromptBuilderTests}.cs`, `Import/ImportServiceTests.cs`, the setup tests that cover D7/D8 (new file `Projects/SetupKeysTests.cs` if none fits) | EDIT/NEW — §7 | 1 |
| 6 | `ui/src/features/projects/components/check-result.tsx`, `ui/src/features/projects/types.ts` (`required: boolean`), `ui/src/features/projects/i18n/{en,fa}.ts`, `ui/src/features/projects/projects.test.tsx` | EDIT — D2 | 2 |
| 7 | `.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md`, `AGENTS.md`, `.gitignore`, `.orchestra/orchestrator.json` | EDIT — D4, D5, D7 | 3 |
| 8 | `.ai/architecture/data-model.md` or `overview.md` — only if one of them lists the folder-check items or the single-repo key; grep first | EDIT | 3 |

## 6. Per-file specification

### 6.1 Phase 1 — API

1. **`FolderCheck`** (D1): replace `KnownItems` with two arrays, `RequiredItems` and `OptionalItems`, each of
   `(Name, Kind)`. `Run` builds the required ones exactly as today (the `.ai/` and `AGENTS.md` switch arms stay)
   with `Required: true`, then appends each optional one that exists with `Required: false, Found: true`. The two
   not-a-folder paths (`!Directory.Exists`, `Invisible`) return the required ones with `Found: false`.
2. **`ScaffoldPromptBuilder`** (D3): the line goes after the lines `Section1(...)` adds. `InstructionFiles` is
   unchanged.
3. **`ImportService`** (D6): split today's guard. `string.IsNullOrWhiteSpace(runsPath)` keeps the early return with
   the error. `!Directory.Exists(runsPath)` sets a local `hasRuns = false`; every read under `runsPath` (the
   ledger, `EnumerateFiles(runsPath, "task.json", …)`, the archive folder) is skipped when it is false, and the
   method continues to the run assembly, the library build and `SaveLogAsync` as today.
4. **`SetupPlanner` / `SetupApplier`** (D7): add `RepoKey`; use it at `SetupPlanner.cs` line 284
   (`detected[single ? request.Name : repo]`) and `SetupApplier.cs` line 366 (`repoObject[single ? request.Name : repo]`).
   Grep both files for any other `request.Name` used as a key and change those too; a `request.Name` used as the
   project's display name stays.
5. **`VerifyCommandDetector`** (D8): as written there.

### 6.2 Phase 2 — UI

1. **`types.ts`**: the folder-check item type gains `required: boolean`.
2. **`check-result.tsx`**: render `check.items.filter((item) => item.required)` as today. Then, when
   `check.items.some((item) => !item.required)`, a block with `t('form.alsoFound')` as a small heading, the items
   with a `secondary` badge `t('found')` (and `t('form.oldLayout')` after `orchestrator/` in the same muted style
   as `form.inEachRepo`), and `t('form.alsoFoundNote')` as a muted paragraph. The repos line: when
   `check.repos.length === 1 && check.repos[0] === '.'`, `t('form.rootIsRepo')` (not `data-ltr`); otherwise as today.

### 6.3 Phase 3 — rule and this repo

1. D4 in ADR 0005 and `AGENTS.md`; D5 in `.gitignore`; D7's rename in `.orchestra/orchestrator.json` (keep its
   indentation; it must still parse).
2. **Rules about rules:** `grep -rn "CLAUDE.md\|\.cursor" .ai AGENTS.md .orchestra/workflow.md engine/docs engine/templates --include=*.md`,
   leaving out `.ai/context/` and `engine/docs/history/`. On `main` the only hits are `AGENTS.md:7` (D4 changes it)
   and the 0003 row of `.ai/vision/roadmap.md` (history of what plan 0003 built; leave it). Any other hit: STOP and
   ask.
3. After the `.gitignore` edit, `git status --short` shows no new untracked or deleted file from `.claude/skills/`,
   and `git check-ignore -v .claude/settings.local.json` names the new `.claude/*` line.

## 7. Tests

- **`FolderCheckTests`:** an empty folder → exactly the three required items, all `Found = false`; a folder with
  `.cursor/` and `orchestrator/` → three required items plus those two, `Required = false`, `Found = true`; a
  folder with no agent files → no optional item.
- **`ScaffoldPromptBuilderTests`:** a check with `.cursor/` and `.claude/` found → the `.gitignore` line names both
  and carries the `.claude/*` / `!.claude/skills/` sentence; with only `CLAUDE.md` → the line without that sentence;
  with none → no `.gitignore` line.
- **`ImportServiceTests`:** a project whose `RunsPath` does not exist but whose `.orchestra/plans/0001-x/plan.md`
  does → the log has no error and the library holds that plan; an empty `RunsPath` → today's error.
- **Setup:** `RepoKey("Project Orchestra")` = `Project-Orchestra`, `RepoKey("  a  b ")` = `a-b`, `RepoKey("!!")` =
  `repo`; a single-repo setup named `My App` writes `repos` and `verifyCommands` keyed `My-App`. The detector: a
  folder with `api/X.slnx` and `ui/package.json` (scripts `lint`, `test`) → `dotnet build api/X.slnx`,
  `dotnet test api/X.slnx`, `npm --prefix ui run lint`, `npm --prefix ui run test`; a folder with its own
  `package.json` → today's commands only.
- **Board:** a group with only `pool.acquired`/`pool.released` events and no run row → no card; a run with
  `run.created` `{task: "Plan 0010: Gridify tables"}` and no run row → title `Gridify tables`, number `0010`.
- **UI (`projects.test.tsx`):** the check renders no "missing" badge for `.cursor/`; an optional item shows under
  "Also found" with the note; `repos: ['.']` renders "The folder itself is the repo."

## 8. Stop and ask

- A run folder exists in `.orchestra/runs/` (rule 0.2).
- A test outside these must change to pass. **These change on purpose:** the `FolderCheckTests` cases that assert
  the old nine-item list, and `ImportServiceTests` line 118 (the missing-runs-folder error, which D6 removes for a
  set `RunsPath`).
- Another caller reads an optional item from `FolderCheckDto.Items` expecting it to be present when not found.
- A grep hit in §6.3.2 not named there.

## 9. Phases

| Phase | Files (§5) | Gate |
|---|---|---|
| 1 API | 1–5 (with 1a) | api gates |
| 2 UI | 6 | ui gates |
| 3 Rule and this repo | 7–8 | all gates; §6.3.2 and §6.3.3; `Get-Content .orchestra/orchestrator.json -Raw \| ConvertFrom-Json` succeeds |

**Owner checks:** after phase 2, the first two checklist items (browser build); after phase 3, the last two.
Stop after every phase — show the owner the diff.

## 10. Definition of done

- [ ] All gates green; test counts ≥ baseline plus the new tests, less the ones §8 names as changed.
- [ ] The owner's checklist passed.
- [ ] `Status: Done`, the `Done:` date, the commit and the merge in the header; the plans README row.
