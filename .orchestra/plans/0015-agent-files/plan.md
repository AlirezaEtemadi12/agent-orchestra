# Plan 0015 — Agent files stay out of git; setup, board, import and cost fixes found by adding Orchestra

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
  7. صفحه‌ی Runs فقط با شروع برنامه، setup و دکمه‌ی Re-import پر می‌شود؛ run ای که در این بین ساخته یا بسته شود
     آن‌جا نمی‌آید. از این به بعد هر رویدادی که وضعیت یک run را عوض می‌کند (`run.created`، `status`، `history`،
     `ledger.written`، `run.closed`) چند ثانیه بعد import **همان پروژه** را راه می‌اندازد؛ رویدادهای پشت‌سرهم یک
     import می‌شوند. `progress` و `pool.*` import راه نمی‌اندازند. engine فرمانی نمی‌دهد و منتظر چیزی نیست.
  8. health در جدول Runs هم ردیف `health.1` می‌سازد (import از همان رویدادهای `pool.*` یک run می‌سازد). همان قاعده‌ی
     مورد ۵ در import هم اجرا می‌شود.
  9. وقتی هزینه‌ی هیچ‌کدام از runها معلوم نیست، صفحه‌ی Cost، جدول Projects و خلاصه‌ی Runs به‌جای «—» عدد `$0.000`
     نشان می‌دهند (جمع چند مقدار خالی صفر می‌شود). از این به بعد جمع مقدارهای خالی، خالی است و «—» نشان داده می‌شود؛
     اگر حتی یک run هزینه داشته باشد، جمع همان‌هاست.
  10. لجر برای run بدون شاخه `branch -` می‌نویسد و import همین «-» را نام شاخه می‌گیرد. برای همین run بسته‌شده‌ی بدون
      شاخه با archive خودش یکی نمی‌شود و دو ردیف می‌سازد (مثلاً T-AGENT-015 در Project B). «-» یعنی شاخه‌ای
      نیست.
  11. وقتی یک run از چند منبع ساخته می‌شود، زمان شروعش زمان اولین منبعی است که خوانده شده، نه زودترین زمان. برای همین
      runهایی که امروز بسته شدند زمان شروعشان امروز است و زمان آخرین تغییرشان اکتبر. از این به بعد زمان شروع، زودترین
      زمانی است که یکی از منبع‌ها می‌گوید.
  12. قالب پلن بعد از هر فاز می‌گوید «این‌جا بایست و diff را به مالک نشان بده». این با قاعده‌ی کانداکتر (همه‌ی فازها
      پشت‌سرهم، بدون پرسیدن) نمی‌خواند و اجرای 0010 را بین فازها منتظر نگه داشت. پلن مرور و read-back شده است؛
      فازها پشت‌سرهم اجرا می‌شوند و فقط روی چیزهایی که بخش «Stop and ask» پلن می‌گوید، یا BLOCKED و شکست gate، می‌ایستند.
      بررسی‌های مالک یک بار در آخر انجام می‌شود. این پلن هم از همین حالا همین‌طور اجرا می‌شود. (روی main انجام شد، پیش از این پلن.)
  13. skill `orchestrate` می‌گوید «هر بار یک فاز، بعد verify». برای همین 0010 بعد از هر فاز verify شد، در حالی که
      قاعده‌ی کانداکتر یک verify در آخر است. skill هم همین را می‌گوید: همه‌ی فازها پشت‌سرهم، بعد یک verify.
      (توقف بین فازها هم از همین جمله‌ی skill می‌آمد.) این هم روی main و در همه‌ی پروژه‌های ثبت‌شده انجام شد، پیش از این پلن.
  14. import لجر هیچ run ای را که شناسه‌اش نام پوشه‌ی پلن است (`0010-gridify-tables`) نمی‌خواند: الگوی عنوان بلوک
      فقط شناسه‌هایی مثل `T-AGENT-015` را می‌پذیرد و بقیه را بی‌صدا رد می‌کند. برای همین 0010 بعد از Re-import هم
      Sessions و Cost ندارد. از این به بعد هر شناسه‌ای خوانده می‌شود؛ ریپوی فاصله‌دار در خط `repo` و چند فاز در یک
      خط (`phase 1 2 3`) هم خوانده می‌شوند؛ و عنوانی که خوانده نشود خطا در Import log است، نه سکوت.
  15. جدول Runs و صفحه‌ی یک run، شناسه‌ی engine را با «.1» عنوان می‌کنند (`0010-gridify-tables.1`، `T-AGENT-015.1`)
      و شماره‌ی پلن را با «v» می‌نویسند (`v0010`) که شبیه نسخه‌ی پلن است. از این به بعد عنوان مثل board است:
      `0010 · Every growing table …`؛ شناسه‌ی run کوچک زیرش می‌آید. run ای که شماره‌ی پلن ندارد همان شناسه را عنوان دارد.

**کارهایی که این پلن انجام نمی‌دهد:**
- صفحه‌ی Runs خودش را مثل board هر چند ثانیه تازه نمی‌کند؛ داده‌ی دیتابیس به‌روز می‌شود و صفحه با باز شدن دوباره آن را نشان می‌دهد.
- پوشه‌ی runs را با FileSystemWatcher زیر نظر نمی‌گیرد؛ اگر engine نتواند رویداد بفرستد، import شروع برنامه و Re-import مثل امروز می‌مانند.
- خط آخر فاز قبل (مثلاً «VERDICT: BLOCKED») روی کارتی که در verify است، عوض نمی‌شود.
- `.gitignore` پروژه‌های دیگر (Project A، Project B) را عوض نمی‌کند؛ prompt راه‌اندازی از عامل آن پروژه می‌خواهد.
- هیچ فایل عاملی را پاک نمی‌کند.
- `.claude/skills/` را از git بیرون نمی‌برد.

## جریان کار

1. **فاز ۱، API.** بررسی پوشه، prompt راه‌اندازی، import بدون پوشه‌ی runs، نام ریپو و تشخیص فرمان‌های وریفای،
   import خودکار بعد از رویدادها، health نه روی board و نه در جدول Runs، هزینه‌ی نامعلوم «—»، شاخه‌ی «-»، زمان شروع run و خواندن لجر runهای با پوشه‌ی پلن.
2. **فاز ۲، UI.** فهرست بررسی با گروه «این‌ها هم پیدا شد» و متن «خود این پوشه ریپو است»؛ خلاصه‌ی Runs هزینه‌ی خالی را می‌پذیرد؛ عنوان run در جدول Runs و صفحه‌ی run مثل board.
3. **فاز ۳، قاعده و این ریپو.** ADR 0005، `AGENTS.md`، `.gitignore`، نام ریپو در `orchestrator.json`.

فازها پشت‌سرهم اجرا می‌شوند؛ بررسی‌های مالک بعد از فاز آخر است.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

صفحه‌ی New project فقط چیزهایی را «نیست» می‌گوید که واقعاً لازم‌اند. پروژه‌ی تازه بدون هیچ run ای هم در Library دیده
می‌شود.

**چک‌لیست شما در اپ:**
- [ ] **New project، پوشه‌ی Orchestra:** سه ردیف `AGENTS.md`، `.ai/`، `.orchestra/` با «پیدا شد». زیرش «این‌ها هم پیدا
      شد» با `.claude/` و توضیحش. هیچ «نیست» ای برای `.cursor/` و مانند آن نیست. زیر فهرست: «خود این پوشه ریپو است».
- [ ] **New project، یک پوشه‌ی خالی:** همان سه ردیف با «نیست»، و گروه «این‌ها هم پیدا شد» اصلاً نیست.
- [ ] **Library:** Re-import را بزنید؛ در Import log برای Orchestra خطای «runs folder» نیست.
- [ ] **setup یک پروژه‌ی آزمایشی تک‌ریپو با فاصله در نام:** در `orchestrator.json`، نام ریپو خط تیره دارد، نه فاصله.
- [ ] **import خودکار:** یک پلن کوچک را با `run.ps1` شروع کنید و بدون زدن Re-import، چند ثانیه بعد صفحه‌ی Runs را باز
      کنید: run آن‌جاست. وقتی run بسته شد، Sessions و Cost همان‌جا پر شده‌اند. Import log برای هر کدام یک ردیف دارد.
- [ ] **health:** `run.ps1 health` را اجرا کنید؛ نه روی board کارتی می‌آید، نه در صفحه‌ی Runs ردیفی.
- [ ] **هزینه‌ی نامعلوم:** در Cost، پروژه‌ای که هیچ run اش هزینه ندارد (مثلاً یک پروژه‌ی تازه) «—» نشان
      می‌دهد، نه `$0.000`. Project B همان `$5.406` را نشان می‌دهد.
- [ ] **Project B بعد از Re-import:** در صفحه‌ی Runs، T-AGENT-015، 020، 023 تا 026 و T-AGENT-002 هر کدام یک ردیف دارند
      (منبع Both)، و هیچ ردیفی زمان شروعش بعد از زمان آخرین تغییرش نیست.
- [ ] **لجر Orchestra:** Re-import را بزنید؛ ردیف 0010 در Runs، Sessions را ۳ و Cost را `$1.714` نشان می‌دهد و Import log
      برای Orchestra بلوک‌های خوانده‌شده را بیشتر از صفر می‌گوید.
- [ ] **عنوان run:** در Runs، ردیف 0010 عنوانش `0010 · Every growing table is a server-side Gridify table` است و زیرش
      `0010-gridify-tables.1`؛ هیچ‌جا «v0010» نیست. صفحه‌ی همان run هم همین عنوان را دارد.

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
git by rule; setup and import work for a single-repo project shaped like Orchestra; and the Runs page follows the runs without a manual Re-import.

## 2. Out of scope

- Writing any project's `.gitignore` from the app (the prompt asks the project's own agent).
- Deleting any agent file, here or elsewhere; taking `.claude/skills/` out of git.
- Creating `.orchestra/runs/` from setup (D6 makes it unnecessary).
- The engine's code; Project A; Project B.

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
| D9 | **`health` is not a run.** `run.ps1 health` takes pool slots with `taskId = 'health'` (`engine/lib/agents.ps1:1112`), and the board makes a "health" card of those `pool.*` events that sits in Running for a day. `BoardHandler` skips a `(project, taskId)` group whose events are all `pool.*` and that matches no run row. The engine is unchanged. The test "all `pool.*`" is one helper, `EventKinds.IsPool(string kind)` (`kind` starts with `pool.`), used here and in D12 |
| D11 | **An event that moves a run imports its project.** New singleton `ImportQueue` (`Modules/Import/ImportQueue.cs`) with `void Request(string projectId)`, and a hosted service `ImportQueueService` beside it. `ReceiveEventHandler` calls `queue.Request(envelope.Project)` after it **stores a new** event (not for a duplicate, not for a refused one) whose kind is `run.created`, `status`, `history`, `ledger.written` or `run.closed`; `progress` and `pool.*` never do. The service waits for a request, then waits until `ImportAfterEventSeconds` pass with no new request (a new request restarts the wait; never longer than 30 s from the first request), takes every pending project id at once and calls `ImportService.ImportOneAsync` for each one `ProjectRegistry.Find` still knows (it waits for the gate, so a running import delays it and nothing is lost). A failed import is logged and the loop goes on, as `ImportHostedService` does. New option `OrchestraOptions.ImportAfterEventSeconds`, `int`, default `3`; `0` turns it off (`Request` does nothing). The engine is unchanged; ADR 0002 holds: the dashboard reacts to what it was told, the engine waits for nothing |
| D12 | **`health` is not a run in the import either.** `RunAssembler.MergeEvents` skips a `(TaskId, Branch)` group whose events are all `EventKinds.IsPool` and that matches no run from the files — the same rule as D9 — so the Runs table shows no `health.1` row. A group with any other kind, or one that matches a run, is merged as today |
| D10 | **A card's title from the plan before the import.** When the card's run row is missing or has no `Task`, `BoardHandler` passes the `task` field of the instance's `run.created` event to `PlanName.From` instead of `run?.Task`. So the 0010 card reads `0010 · Every growing table …`, not the slug, before the next import |
| D8 | **Verify commands one folder down.** `VerifyCommandDetector.Detect(folder)`: when the folder itself has neither a solution nor a `package.json`, look at its direct subfolders, sorted by name, skipping names that start with `.` and `node_modules`, `bin`, `obj`. Each subfolder with a solution adds `dotnet build <sub>/<file>` and `dotnet test <sub>/<file>` (its first `*.slnx` by name, else its first `*.sln`); then each subfolder with a `package.json` adds `npm --prefix <sub> run <script>` for the scripts today's rule picks. .NET subfolders first, then npm (today's order). Paths use `/`. A folder that has its own solution or `package.json` behaves exactly as today |
| D13 | **An unknown cost stays unknown.** New `Orchestra.Api.Common.Money` (static) with `long? SumMicros(IEnumerable<long?> values)`: `null` when no value has one, otherwise the sum of those that do. Used in `CostSummaryHandler.GroupByRuns` and `ByModel`, and `ListProjectsHandler` (`Cost(runs.Sum(run => run.CostMicros))`), instead of LINQ `Sum`, which turns all-null into `0`. `RunsSummaryDto.CostUsd` becomes `decimal?`: `RunsSummaryHandler` drops `?? 0` from `costMicros`, and `SumAsync` stays (SQL `SUM` over all-null is already `NULL`). UI: `RunsSummary.costUsd` becomes `number \| null`; `MoneyText` already renders `null` |
| D14 | **`branch -` is no branch.** `LedgerParser`: when the repo line's branch is `-`, `block.Branch` is `null`. So a closed ledger run without a branch merges with its archive (whose branch is empty) instead of making a second row |
| D15 | **A run starts at the earliest time any source gives.** `RunAssembler.MergeLiveRuns` and `MergeArchives` (the `else` branch, where the target already exists): `target.CreatedAt` becomes the earlier of itself and the source's `CreatedAt` (when the source has one). `UpdatedAt` rules are unchanged, so the Cost page still dates a run as today |
| D16 | **No stop between phases.** Already done on `main` (2026-11-25, before this plan): both `TEMPLATE.md` copies say phases run back to back and owner checks come once, after the last phase. Nothing to do here; this plan follows it (§9) |
| D17 | **One verify, at the end.** Already done on `main` (2026-11-25, before this plan): the `orchestrate` skill's step 4, in this repo, in `engine/templates/` and in every registered project, says every phase back to back, then one `verify`. Nothing to do here |
| D18 | **The ledger reads every run id.** `LedgerParser`'s heading accepts any run id without spaces (`0010-gridify-tables` as well as `T-AGENT-015`); its repo line accepts a repo name with spaces; its phase line accepts a list of numbers (`phase 1 2 3`, phases run together), which gives **one** row whose `Phase` is the first number. A trimmed line that starts with `## ` and does not match the heading is a `LedgerError` ("Unrecognised ledger heading.") and ends the current block, so nothing after it is read into the wrong block. A leading BOM on the file does not stop its first heading from matching. The exact patterns are in §6.1 item 12 |
| D19 | **A run is titled like its board card.** In `ui/src/features/runs/components/runs-list.tsx` (the `runKey` column) and `run-detail.tsx` (`PageHeader`): when `planNumber` is set, the title is `` `${planNumber} · ${planTitle ?? task ?? ''}` `` and the run key is a muted line under it (`data-ltr`); otherwise the title is the run key and the muted line is `task` when there is one. No `v` before the plan number anywhere: the detail description becomes the project name alone. The run key in URLs and the column id stay as they are |
## 5. File manifest

| # | File | Change | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Projects/Features/CheckFolder/FolderCheck.cs` | EDIT — D1 | 1 |
| 1a | `api/src/Orchestra.Api/Modules/Runs/Features/Board/BoardHandler.cs`, `api/tests/Orchestra.Api.Tests/Runs/BoardTests.cs` | EDIT — D9, D10 | 1 |
| 2 | `api/src/Orchestra.Api/Modules/Projects/Features/ScaffoldPrompt/ScaffoldPromptBuilder.cs` | EDIT — D3 | 1 |
| 3 | `api/src/Orchestra.Api/Modules/Import/ImportService.cs` | EDIT — D6 | 1 |
| 4 | `api/src/Orchestra.Api/Modules/Projects/Features/Setup/{SetupPlanner,SetupApplier,VerifyCommandDetector}.cs` | EDIT — D7, D8 | 1 |
| 5 | `api/tests/Orchestra.Api.Tests/Projects/{FolderCheckTests,ScaffoldPromptBuilderTests}.cs`, `Import/ImportServiceTests.cs`, the setup tests that cover D7/D8 (new file `Projects/SetupKeysTests.cs` if none fits) | EDIT/NEW — §7 | 1 |
| 5a | `api/src/Orchestra.Api/Modules/Import/{ImportQueue,ImportQueueService}.cs` (NEW), `ImportModuleInstaller.cs`, `api/src/Orchestra.Api/Common/OrchestraOptions.cs`, `api/src/Orchestra.Api/Modules/Events/{EventKinds,Features/ReceiveEvent/ReceiveEventHandler}.cs`, `api/src/Orchestra.Api/Modules/Import/RunAssembler.cs` | EDIT/NEW — D9 helper, D11, D12 | 1 |
| 5b | `api/tests/Orchestra.Api.Tests/Events/{EventsFixture,ReceiveEventTests}.cs`, `Import/ImportQueueTests.cs` (NEW), `Import/RunAssemblerEventsTests.cs` | EDIT/NEW — §7 | 1 |
| 5c | `api/src/Orchestra.Api/Common/Money.cs` (NEW), `Modules/Usage/Features/CostSummary/CostSummaryHandler.cs`, `Modules/Projects/Features/ListProjects/ListProjectsHandler.cs`, `Modules/Runs/Features/RunsTable/RunsSummaryHandler.cs`, `Modules/Import/Ledger/LedgerParser.cs`, `Modules/Import/RunAssembler.cs` | EDIT/NEW — D13, D14, D15, D18 | 1 |
| 5d | `api/tests/Orchestra.Api.Tests/Import/{LedgerParserTests,RunAssemblerTests}.cs`, the cost-summary, project-list and runs-summary tests (grep for `CostUsd`), `MoneyTests.cs` (NEW, beside them) | EDIT/NEW — §7 | 1 |
| 6 | `ui/src/features/projects/components/check-result.tsx`, `ui/src/features/projects/types.ts` (`required: boolean`), `ui/src/features/projects/i18n/{en,fa}.ts`, `ui/src/features/projects/projects.test.tsx` | EDIT — D2 | 2 |
| 6a | `ui/src/features/runs/types.ts` (`RunsSummary.costUsd: number \| null`) and any test fixture that types it | EDIT — D13 | 2 |
| 6b | `ui/src/features/runs/components/{runs-list,run-detail}.tsx`, `ui/src/features/runs/runs.test.tsx` | EDIT — D19 | 2 |
| 7 | `.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md`, `AGENTS.md`, `.gitignore`, `.orchestra/orchestrator.json` | EDIT — D4, D5, D7 | 3 |
| 8 | `.ai/architecture/data-model.md` or `overview.md` — only if one of them lists the folder-check items or the single-repo key; grep first | EDIT | 3 |
| 9 | `.ai/architecture/overview.md` lines 36–37 (the importer runs "on start and on demand") | EDIT — D11 | 3 |

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
6. **`ImportQueue` / `ImportQueueService`** (D11): the queue holds a lock-protected `HashSet<string>` of pending
   project ids, the time of the first and of the last request, and a `SemaphoreSlim(0)` it releases on each
   `Request`. It takes `IOptions<OrchestraOptions>` and `TimeProvider`; the service waits with
   `Task.Delay(…, TimeProvider, …)` so a test can drive it with a fake clock. The service makes one DI scope per
   batch for `ImportService` and `ProjectRegistry`. `ReceiveEventHandler` gains the `ImportQueue` constructor
   parameter; `EventsFixture` passes a real queue built from a fixed option value. Register both in
   `ImportModuleInstaller` (`AddSingleton<ImportQueue>()`, `AddHostedService<ImportQueueService>()`).
7. **`RunAssembler`** (D12): the check sits at the top of the `foreach (var group …)` loop in `MergeEvents`,
   before `NewRun`: when no run matched and `group.All(item => EventKinds.IsPool(item.Kind))`, `continue`.
8. **`BoardHandler`** (D9) uses the same `EventKinds.IsPool`.
9. **`Money.SumMicros`** (D13): one method, no other change to the handlers' shapes. `CostRowDto.CostUsd` and
   `ProjectDto.CostUsd` are already `decimal?`.
10. **`LedgerParser`** (D14): `block.Branch = value == "-" ? null : value;` at the repo line. Nothing else reads `-`.
11. **`RunAssembler`** (D15): in `MergeLiveRuns`, replace `target.CreatedAt = target.CreatedAt == default ? created : target.CreatedAt;`
    with "default → `created`; otherwise the earlier of the two". In `MergeArchives`' `else` branch, add the same
    with `archive.Task.CreatedAt` when it is not null. `ApplyLedgerBlock` and `ApplyEvent` stay as they are.
12. **`LedgerParser`** (D18): the three patterns become exactly

    ```
    Header:    ^## (\d{4}-\d{2}-\d{2} \d{2}:\d{2}) - (\S+) - (verify|close)\s*$
    RepoLine:  ^repo (.+?) \| status (\S+) \| plan v(\d+) \| branch (\S+)$
    PhaseLine: ^- phase (\d+(?: \d+)*): (\w+) by (\S+) \| changed (\d+) \| not in plan (\d+) \| violations (\d+)$
    ```

    The phase arm takes the first number of `Groups[1]` (`Split(' ')[0]`). The unmatched-heading check sits right
    after the heading match: `line.StartsWith("## ", StringComparison.Ordinal)` → `Finish(...)` the current block,
    add the error with the line number, set `block = null`, `continue`. Strip a leading BOM (U+FEFF) before `Trim()`.

### 6.2 Phase 2 — UI

1. **`types.ts`**: the folder-check item type gains `required: boolean`. `ui/src/features/runs/types.ts`:
   `RunsSummary.costUsd: number | null` (D13); the Runs page already passes it to `MoneyText`.
2. **`runs-list.tsx` / `run-detail.tsx`** (D19): one small helper in `ui/src/features/runs/` —
   `runTitle(run: { runKey: string; planNumber: string | null; planTitle: string | null; task: string | null })`
   returning `{ title, subtitle }` — used by both, so they cannot drift.
3. **`check-result.tsx`**: render `check.items.filter((item) => item.required)` as today. Then, when
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
- **Import after events (D11):** storing a `status` event requests its project; a `progress`, a `pool.acquired`, a
  duplicate and a refused event request nothing. The queue with `ImportAfterEventSeconds = 0` keeps nothing. Three
  requests for the same project within the wait give one import; requests for two projects give one import each;
  a project removed before the wait ends is skipped without error.
- **Import skips health (D12):** events `pool.acquired`/`pool.released` for `health` and no files → no run; the
  same events for a task that has a `task.json` → that run, unchanged.
- **Unknown cost (D13):** `Money.SumMicros([null, null])` = `null`, `([])` = `null`, `([null, 5])` = `5`; the cost
  summary by project and by model, and the project list, give `CostUsd = null` for a project whose runs all have no
  cost and the sum of the known ones otherwise; the runs summary over runs with no cost gives `CostUsd = null`.
- **Ledger branch (D14):** a block with `branch -` parses to `Branch = null`; a closed ledger run with `branch -` and
  an archive of the same task with no branch assemble into one run, source `Both`.
- **Start time (D15):** a ledger run whose first block is later than the archive's (or `task.json`'s) `createdAt`
  gets the earlier `CreatedAt`; a source with a later `createdAt` does not move it.
- **Ledger ids (D18):** a block headed `## 2026-11-25 14:53 - 0010-gridify-tables - verify` parses with that task
  id and its sessions and cost; `repo Project Orchestra | status Verified | plan v4 | branch x` gives repo
  `Project Orchestra`; `- phase 1 2 3: PASS by …` gives one row with `Phase = 1`; a `## something else` line gives
  one error and the lines after it join no block; a file that starts with a BOM reads its first block. The existing
  `T-AGENT-…` cases still pass.
- **Run title (D19, `runs.test.tsx`):** a run with `planNumber '0010'` and a title shows `0010 · <title>` and the
  run key under it, and no `v0010`; a run without a plan number shows its run key as the title.
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
| 1 API | 1–5 (with 1a, 5a–5d) | api gates |
| 2 UI | 6, 6a, 6b | ui gates |
| 3 Rule and this repo | 7–9 | all gates; §6.3.2 and §6.3.3; `Get-Content .orchestra/orchestrator.json -Raw \| ConvertFrom-Json` succeeds |

Phases run back to back with no stop between them (D16); a run stops only on what §8 names, BLOCKED, a violation, a
file outside the plan or a failed gate.

**Owner checks:** once, after phase 3: the whole checklist (browser build and the running app).

## 10. Definition of done

- [ ] All gates green; test counts ≥ baseline plus the new tests, less the ones §8 names as changed.
- [ ] The owner's checklist passed.
- [ ] `Status: Done`, the `Done:` date, the commit and the merge in the header; the plans README row.
