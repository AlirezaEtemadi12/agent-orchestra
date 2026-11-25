# Plan 0013 — One plan per change, one folder per plan, a board by plan

**Status:** Done
**Created:** 2026-11-24 · **Updated:** 2026-11-25 · **Done:** 2026-11-25
**Preview:** [`preview.html`](preview.html) — the two schematics the owner approved in the chat on 2026-11-24, kept as a page
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-24. **Executor:** another agent, ideally a cheaper model.
**Plan review:** an owner-run review, 2026-11-24 (by hand, not through the orchestrator, so no read-back). v2 applies its B1–B4, G1–G4, G6, N1–N8; G5 was half right (README.md and TEMPLATE.md are already never items, by file name) and is settled by D18. A second review of v2 found R1–R5; v3 applies all five. A third review of v3 found R6 (the `PlanName` call sites and `RunAssembler`); v4 applies it.
**Decided by:** Alireza, 2026-11-24, in the chat. Every choice below is already decided — **decide nothing new.** If reality
contradicts this plan, STOP and ask.
**Branch:** `alireza/workspace-plans`. **Commit:** `abee4a4` on that branch. **Merge:** `d806659` into `main`, 2026-11-25 (not pushed)
**Base:** `main` after plan 0012 is committed and merged. **Prerequisite, checked before the first edit:** `git ls-tree main api/src/Orchestra.Api/Modules/Projects/Features/Setup/` and `git ls-tree main engine/templates/project/` both list files. If either is empty, 0012 is not on `main` yet — STOP; do not branch from `alireza/local-app`
**Companion:** Project A — phase 6 writes its companion plan into Project A's own `.orchestra/`; nothing here edits that repo
**Steps changed:** none
**Implementation note (2026-11-25):** D6's `Reviewing` engine state is not written: the offline suite asserts `Planning` for the whole review (S9, T-M1, T-M7, T-W1), and adding the state broke four of them. The label is read off the hop instead — `BoardHandler` shows "In review" while the last progress event belongs to a `plan-reviewer` or `readback` hop, and `AwaitingApproval` still reads "Reviewed · needs you". The rest of D6 is unchanged.

---

## فهم من از درخواست

- محور کار «فضای کاری» است، نه ریپو. هر تغییری که هم بک‌اند و هم فرانت را عوض می‌کند **یک پلن** است، با یک پیش‌نمایش
  و یک جریان کار. در خود پلن برای هر ریپو یک بخش جدا هست.
- هر پلن یک پوشه دارد (vertical slice) و هر چه به آن پلن مربوط است در همان پوشه است: خود پلن، پیش‌نمایش، شواهد و
  یادداشت‌های تحقیق. پوشه‌های سراسری `previews/` و `evidence/` دیگر استفاده نمی‌شوند.
- skillها هر کدام پوشه‌ی خودشان را دارند. `CLOSE-PLAN.md` به skill «بستن پلن» تبدیل می‌شود که هیچ سؤالی نمی‌پرسد:
  commit می‌کند، merge می‌کند و شاخه را پاک می‌کند. push فقط وقتی انجام می‌شود که مالک خودش بگوید.
- skill تازه‌ای هم اضافه می‌شود: پلن‌های انجام‌نشده را فهرست می‌کند، مالک یکی را انتخاب می‌کند، و عامل ارکستر کار آن
  پلن را از همان مرحله‌ای که هست ادامه می‌دهد.
- شناسه‌ی هر run همان شناسه‌ی پلن است (`0013-workspace-plans`)، بدون اسم ریپو. ریپوهای یک پلن **پشت سر هم** پیاده
  می‌شوند، نه همزمان. اولویت با خوانایی است و سرعت در درجه‌ی دوم.
- کارت برد این‌طور خوانده می‌شود: «شماره · عنوان پلن».
- وضعیت‌ها روشن‌تر می‌شوند: پیش‌نویس، در بازبینی، بازبینی‌شده (منتظر شما)، تأییدشده، و بقیه. اگر مالک دستور
  پیاده‌سازی پلنی را بدهد که بازبینی‌شده است، همین دستور تأیید حساب می‌شود و با زمانش ثبت می‌شود. این تأیید ضمنی فقط از
  دستور خود مالک می‌آید و هیچ زنجیره‌ی خودکاری نمی‌تواند آن را بدهد.
- در اپ دسکتاپ دکمه‌ی «بازگشت» و «تازه‌سازی» برمی‌گردد.
- پلن‌های تمام‌شده‌ی قبلی نگه داشته نمی‌شوند. git بایگانی آن‌هاست.

**کارهایی که این پلن انجام نمی‌دهد:**
- اجرای همزمان چند ریپوی یک پلن؛
- run ای که چند پلن را با هم پیش ببرد؛
- push خودکار؛
- تأیید خودکار بدون دستور مالک؛
- ویرایش مستقیم ریپوی Project A از اینجا. مهاجرت Project A یک پلن همراه است که در خود Project A اجرا می‌شود.

## جریان کار

**ساختن پلن.** مالک کاری را می‌خواهد. عامل:
1. شماره‌ی آزاد بعدی را برمی‌دارد.
2. پوشه‌ی `plans/NNNN-<slug>/` را می‌سازد.
3. پیش‌نمایش را در همان پوشه می‌گذارد.
4. بعد از تأیید پیش‌نمایش، `plan.md` را می‌نویسد. سربرگ پلن می‌گوید کدام ریپوها عوض می‌شوند، و هر مرحله‌ی پلن
   می‌گوید در کدام ریپو انجام می‌شود.

**اجرا.** مالک می‌گوید «با ارکستر»:
1. موتور یک run با همان نام پوشه می‌سازد.
2. پلن بازبینی می‌شود. برد اول «در بازبینی» نشان می‌دهد و بعد «بازبینی‌شده، منتظر شما».
3. مالک یا تأیید می‌کند، یا مستقیماً می‌گوید پیاده کن، که آن هم تأیید حساب می‌شود.
4. موتور مرحله به مرحله پیش می‌رود. شاخه‌ی هر ریپو وقتی ساخته می‌شود که اولین مرحله‌ی آن ریپو شروع شود. وریفای
   دستورهای همه‌ی ریپوهایی را که تا آن لحظه دست خورده‌اند اجرا می‌کند.
5. وقتی همه سبز شد، مالک می‌گوید «پلن را ببند». skill بستن پلن بدون هیچ سؤالی، در هر ریپو commit و merge می‌کند و
   شاخه را پاک می‌کند. بعد گزارش می‌دهد.
6. اگر merge به تعارض بخورد یا تست‌ها قرمز باشند، کار متوقف می‌شود و گزارش داده می‌شود. این توقف سؤال نیست؛ گزارش
   خطاست.

**انتخاب پلن.** مالک می‌گوید «پلن‌ها». skill انتخاب پلن فهرست پلن‌های باز را با شماره، عنوان، وضعیت و ریپوها نشان
می‌دهد. مالک یک شماره را انتخاب می‌کند و عامل از همان مرحله ادامه می‌دهد. این skill هیچ‌وقت چیزی را تأیید نمی‌کند.

**مرحله‌ها:**
1. رفع‌های سریع: دو باگ برد، دکمه‌های بازگشت و تازه‌سازی، و setup که promptهای تخت را برای Project A نمی‌سازد.
2. موتور: شناسه از پلن، چند ریپو در یک run، وضعیت «در بازبینی»، و تأیید ضمنی.
3. چیدمان و قالب‌ها: پوشه‌ی پلن، skillها، و setup که اشاره‌گرهای skill را می‌سازد.
4. داشبورد: برد بر اساس پلن با عنوان و برچسب ریپوها، نام‌های تازه‌ی وضعیت، و Library در چیدمان تازه.
5. به‌روزرسانی قاعده‌ها، انتقال همین پلن به شکل پوشه، و بستن پلن 0007. پلن‌های قدیمی Orchestra همان‌جا که
   هستند می‌مانند.
6. نوشتن پلن همراه برای Project A.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

یک پلن یعنی یک پوشه، یک run و یک کارت روی برد، هر چند ریپو را که عوض کند. بستن پلن یک کلمه است و هیچ سؤالی پشتش
نیست.

**چک‌لیست شما در اپ:**
1. **Runs board:** هیچ کارتی با وضعیت AwaitingApproval در ستون Done نیست، و هیچ run ای دو کارت ندارد.
2. **عنوان کارت‌ها:** با `NNNN · عنوان` شروع می‌شوند.
3. **Library:** پلن‌ها، پیش‌نمایش‌ها و شواهد Orchestra را از پوشه‌های تازه‌شان نشان می‌دهد.
4. **دکمه‌ها:** بالای صفحه دکمه‌ی «بازگشت» یک صفحه به عقب برمی‌گردد و «تازه‌سازی» صفحه را دوباره بار می‌کند.
5. **تأیید ضمنی:** در یک پروژه‌ی آزمایشی، `run.ps1 new -PlanFile .orchestra/plans/0001-x/plan.md` یک run با شناسه‌ی
   `0001-x` می‌سازد. بعد از بازبینی PASS، دستور `implement 0001-x` بدون `approve` اجرا می‌شود، و در `task.json` این
   ثبت می‌شود: `approved.by = "implement"`.
6. **skill انتخاب پلن:** در Orchestra، «پلن‌ها» فهرست پلن‌های باز را نشان می‌دهد.

---

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../../workflow.md): branch before the first edit; never commit/push/merge unless asked in
   that message; one phase = one uncommitted diff.
2. The engine is PowerShell 5.1: no `??`, no ternary, `-Encoding UTF8` on every write, CRLF kept where a file has it.
3. **Old shapes keep working.** A run whose `task.json` has `repo` (one string) and no `repos`, a plan filed flat
   (`plans/NNNN-x.md`) and a project whose `plansDir` has `{repo}` all behave exactly as today. Every engine test
   that passes now passes unchanged.
4. Persian text is never retyped from another file. The only new Persian strings in this plan are the UI
   labels in §6.3.1 and §6.3.4, given there verbatim; copy them byte for byte.
5. Nothing under `<Project A>` (or any other project) is edited by this plan. Phase 5 writes a Markdown
   companion plan into it; that single new file is the only write.

### Baseline

```
cd api; dotnet build Orchestra.slnx -warnaserror; dotnet test Orchestra.slnx      # 300 passed
cd ui; npm run typecheck; npm run lint; npm run test; npm run build                # 84 passed
powershell -NoProfile -File engine/tests/offline/run-offline.ps1                   # 146/146 on 2026-11-24; record the real count before phase 1
```

The counts never drop.

## 1. Goal

A change is one plan in one folder, run as one orchestrator run named after it, across as many repos of the
workspace as it touches, one repo after another; its state reads plainly on a board grouped by plan; closing it
asks nothing.

## 2. Out of scope

- Running two repos of one plan at the same time. Two **different** plans may still run at once, as today.
- A run spanning plans; renaming old run ids; rewriting `_ledger.md` history.
- Any push. Any approval that does not come from the owner's own `implement` or `approve`.
- Moving the knowledge folders (`.ai/`, `AGENTS.md`) — unchanged.
- Gridify tables (plan 0010) — unchanged, still Draft.

## 3. Rules that apply

- [`../workflow.md`](../../workflow.md) — gates; **this plan edits gate 1, gate 4 and the plans README** (§6.4).
- [ADR 0002](../../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md) — the engine never depends on the dashboard.
- [ADR 0003](../../../.ai/decisions/0003-read-only-dashboard-first.md), [ADR 0009](../../../.ai/decisions/0009-local-app-sets-projects-up.md) — the dashboard writes into a project only through setup.
- [ADR 0004](../../../.ai/decisions/0004-persian-and-english.md) — Persian text.
- [ADR 0005](../../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md) — what `.orchestra/` holds; **this plan amends it** (§6.4).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | **A plan is a folder** `.orchestra/plans/NNNN-<slug>/` holding `plan.md`, and optionally `preview.html` (or `preview/index.html` with assets), `evidence/` (git-ignored), `research.md`, `notes.md`. The slug is lowercase kebab-case. Nothing else of a plan lives anywhere else |
| D2 | **Numbers are per workspace**, four digits, the next free one = highest folder or flat-file number in `plans/` + 1. Project A's first is `0001` |
| D3 | **One plan, many repos.** `plan.md`'s header has `**Repos:** Web, Ui` (names = keys of `orchestrator.json` `repos`), in execution order. A single-repo project omits it. Every phase heading names its repo: `### Phase 2 — Ui` (read with `^#{2,3}\s*Phase\s+(\d+[a-z]?)\s*[—–-]\s*([A-Za-z0-9_.-]+)`, group 1 the phase, group 2 the repo). **Only a run with more than one repo reads it**; a single-repo run always uses `repos[0]`, whatever its headings say (this repo's plans head phases `### Phase 1 — measure`). One plan, one preview, one review, one approval |
| D4 | **Run id = the plan folder name** (`0013-tenant-filter`). `run.ps1 new -PlanFile <folder>/plan.md` takes the id, the repos and the task text (the plan's `# Plan NNNN — <title>` line) from the plan; the positional `<taskId> <repo> "<task>"` stay for old use |
| D5 | **Repos run one after another**, never at once: a phase runs in the repo its heading names. One branch name for every repo — the one `approve` sets today (`Get-PlanBranch`: the plan's `**Branch:**` line, else `<git user>/<task id>`, so `alireza/0013-tenant-filter`) — is created in a repo when its first phase starts, from that repo's current HEAD, with today's clean-tree gate per repo. Verify runs the verify commands of every repo the run has touched so far. The order is the plan's (API before UI by convention) |
| D6 | **Statuses**: engine adds `Reviewing` (set while gate 2 review or read-back runs; back to `AwaitingApproval` or `Planning` after). Everything else stays: `Planning`, `NeedsInput`, `AwaitingApproval`, `Approved`, `Implemented`, `Verified`, `VerifyFailed`, `Violation`. The board names them: Draft, In review, Reviewed · needs you, Approved, Implementing, Implemented, Verifying, Verified, Failed, Closed (§6.3.1). Every place that lists the review-time states learns `Reviewing`: `Test-ManualKeyStale` in `lib/bus.ps1` (else a review request in flight counts as stale and `queue` hides it), and `Write-RunSummary`'s `next:` line (`Reviewing` → `wait; status <id>`) |
| D7 | **Implicit approval**: `Cmd-Implement` gains a switch `-AllowImplicitApproval`, passed **only** by the command-line dispatch of `run.ps1 implement`; `Cmd-Watch`'s resume path and every other caller do not pass it. With the switch and status `AwaitingApproval`, it first runs exactly `approve`'s checks with no notes and no `-Force`; if they pass it records `approved` as `approve` would with `approved.by = "implement"`, then implements; if they fail it refuses with `approve`'s message. Without the switch, `AwaitingApproval` is refused as today. `approve` itself records `approved.by = "owner"`. The conductor runs `implement` only on the owner's words (CONDUCTOR.md, §6.4) |
| D8 | **The plan file's Status follows the run**: on approval (explicit or implicit) the engine sets `plan.md`'s `**Status:**` to `Approved`, on the first implement to `Running`. `Done` is written only by the close-plan skill. The engine writes only that one line, keeping BOM and line endings. Where `plan.md` lives inside a repo of the run (a single-repo project like this one), its path relative to that repo is added to `preexistingDirty` **before** gate 3's clean-tree check, so the Status write never blocks the run, never counts as the implementer's change and is never shown to the verifier as part of the diff |
| D9 | **Skills**: each is `.orchestra/skills/<name>/SKILL.md`, the versioned text. Claude Code finds skills in `.claude/skills/`, so each has a three-line pointer `.claude/skills/<name>/SKILL.md` (frontmatter + "Read `.orchestra/skills/<name>/SKILL.md` and follow it"). Three skills: `orchestrate` (today's conductor pointer), `close-plan` (§6.2.2), `pick-plan` (§6.2.3). Setup creates the six files when missing (ADR 0009 allows it) |
| D10 | **close-plan asks nothing.** "Close the plan" is the owner's yes to commit, merge (`--no-ff` into the base branch) and `branch -d`, for every repo of the plan, in plan order. It never pushes, never forces, never `-D`, never stashes. It **stops and reports** (no question) only on: a merge conflict (after `merge --abort`), red gates it ran itself, or a run that is not `Verified` after it ran verify once. Foreign files are left unstaged and named in the report |
| D11 | **pick-plan** lists every plan whose header Status is not `Done` or `Dropped`, as a numbered table (number, title, Status, repos, run status from `runs/<id>/task.json` if any). The owner answers with a number; the agent continues as the conductor from that plan's state (§6.2.3). It never approves |
| D12 | **Board = one card per run = one plan.** Title `NNNN · <plan title>`; one chip per repo of the run with that repo's last phase state. Events are grouped by `(project, taskId)` and split into run instances at each `run.created` event — **the branch is not part of the key**. `closed` counts only a `run.closed` after the instance's `run.created`, or the matched run's `ClosedAt` when that run was created at or after the instance started. This fixes both bugs, checked in the owner's database on 2026-11-24: `T-UI-004` has `run.created`, `status` and `progress` events with a null branch (planning) and later events on `alireza/violation-photos-ui`, so it formed two cards; the null-branch card matched no branchless run, fell back to the newest run, which has a `ClosedAt`, and so sat in Done with status `AwaitingApproval`. `T-UI-008` shows the same split. For `MatchRun`, an instance's branch is its first non-empty event branch (none before the branch exists); a run row is matched to at most one instance — the newest instance whose start is at or before the run's `CreatedAt` |
| D13 | **Library globs** follow D1: plans `.orchestra/plans/*/plan.md` and the flat `.orchestra/plans/*.md`; previews `.orchestra/plans/*/preview.html`, `.orchestra/plans/*/preview/index.html` plus today's previews globs; research `.orchestra/plans/*/research.md` plus `.orchestra/research/**/*.md`; evidence `.orchestra/plans/*/evidence/**/*` plus today's. The old `.orchestra/plans/*/*.md` glob is removed (it would list `research.md` and `notes.md` as plans). `README.md` and `TEMPLATE.md` stay out by `GlobMatcher.NeverItems`, unchanged; the template stays a flat `TEMPLATE.md` (D18), so no folder template is ever listed. A project's own globs still override |
| D14 | **Back and reload** are two icon buttons at the start of the header's right-hand group, in every build (browser too): back = `router.back()`, reload = `window.location.reload()`. Keys: `Alt+←` back, `F5` and `Ctrl+R` reload, handled in the app shell. Checked by hand in the published exe as well as in the browser (WebView2 may handle these keys itself) |
| D15 | **Setup's prompts and roles** (plan 0012 D10): the planner skips the template rows under `prompts/` and `roles/` when the project's existing `orchestrator.json` sets `workflow.promptsPath` or `workflow.rolesPath` to anything other than `.orchestra/prompts` / `.orchestra/roles` |
| D16 | **Old plans stay where they are.** Every reader accepts both shapes (rule 0.3), so this repo's flat plans 0001–0012, `previews/0001-…` and `evidence/` are not moved and no link is rewritten; only plans from 0013 on are folders. Done plans are never archived — git is the archive; deleting them is the owner's call, per project |
| D17 | Plan 0007 closes as **Done** in phase 5: its moves happened; its last open item ("Check setup" for both projects) is replaced by this plan's companion plans |
| D18 | **The plan template stays `plans/TEMPLATE.md`**, flat; a new plan is a copy of it saved as `plans/NNNN-<slug>/plan.md`. So `workflow.planTemplate` keeps its value `.orchestra/plans/TEMPLATE.md` everywhere and the Library never lists the template |
| D19 | **Setup writes the D1 layout**: `SetupApplier.RenderConfig` writes `plansDir = ".orchestra/plans"` for both shapes (no `{repo}`); `SetupPlanner` drops the per-repo `.orchestra/plans/<repo>/.gitkeep` rows; `engine/examples/orchestrator.json` gets the same `plansDir` |

## 5. File manifest

| # | File | NEW/EDIT |
|---|---|---|
| 1 | `engine/run.ps1` | EDIT — `new -PlanFile` (D4), `approve` and `implement` calling `Set-PlanStatus` (D8), `implement` (D5, D7), `Reviewing` (D6), `verify` per touched repo, `close` for many repos, usage text |
| 2 | `engine/lib/gates.ps1`, `engine/lib/bus.ps1` | EDIT — `Get-RunRepos`, `Get-PhaseRepo`, per-repo branch helper; `Reviewing` in `Test-ManualKeyStale` (D6) |
| 3 | `engine/lib/plans.ps1` | NEW — read a plan header (number, title, repos, status), set its Status line (D8), next free number (D2) |
| 4 | `engine/lib/report.ps1` | EDIT — progress/status events carry `repo` |
| 5 | `engine/tests/offline/*` | EDIT/NEW — §7 |
| 6 | `engine/docs/CONDUCTOR.md`, `engine/docs/REVIEW-BRIEF.md` | EDIT — ids, many repos, implicit approval, skills |
| 7 | `engine/templates/project/plans/README.md`, `plans/TEMPLATE.md` | EDIT (D18) |
| 8 | `engine/templates/project/skills/{orchestrate,close-plan,pick-plan}/SKILL.md` | NEW |
| 9 | `engine/templates/project/claude-skills/{orchestrate,close-plan,pick-plan}/SKILL.md` | NEW — the pointers |
| 10 | `engine/templates/project/workflow.md`, `README.md`, `.gitignore`; this repo's root `.gitignore`; `engine/examples/orchestrator.json` | EDIT — D1 paths; the template `.gitignore` is unchanged (its `evidence/` already matches at any depth); the root `.gitignore` gains `.orchestra/plans/*/evidence/`; D19 |
| 11 | `engine/templates/project/previews/.gitkeep` | DELETE (the folder's only file) |
| 12 | `api/.../Projects/Features/Setup/SetupPlanner.cs`, `SetupApplier.cs` | EDIT — D9 pointer target paths (§6.2.5), D15, D19 |
| 13 | `api/.../Runs/Features/Board/BoardHandler.cs`, `BoardDtos.cs` | EDIT — D12 |
| 14 | `api/.../Import/PlanName.cs`, `Import/Tasks/TaskJsonReader.cs`, `Import/RunAssembler.cs` | EDIT — `NNNN-slug` ids, `repos`, and the four `PlanName.From` calls giving the task id (D12) |
| 15 | `api/.../Library/LibraryDefaults.cs` | EDIT — D13 |
| 16 | `api/tests/...` | EDIT/NEW — §7 |
| 17 | `ui/src/components/layout/app-shell.tsx`, `ui/src/components/layout/history-buttons.tsx` | EDIT/NEW — D14 |
| 18 | `ui/src/features/board/components/board.tsx`, `ui/src/features/board/types.ts`, `board.test.tsx` | EDIT — D12 title and chips, D6 labels |
| 19 | `ui/src/lib/i18n/messages/common.{en,fa}.ts`, runs catalogues | EDIT |
| 20 | `.orchestra/` of this repo | MOVE — phase 5, this plan only (D16) |
| 21 | `.orchestra/workflow.md`, `plans/README.md`, `plans/TEMPLATE.md`, `.ai/decisions/0005-…`, `.ai/decisions/0010-plan-is-a-folder.md`, `.ai/decisions/README.md`, `AGENTS.md`, `.ai/README.md` | EDIT/NEW — §6.4 |
| 22 | `.orchestra/plans/0007-…`, `.orchestra/plans/0012-…` | EDIT — 0007 Done (D17); 0012 D10 note points at D15 |

## 6. Per-file specification

### 6.1 Engine (phase 2)

1. **`lib/plans.ps1`.**
   - `Read-PlanHeader <path>` returns `{ Number, Slug, Title, Repos[], Status }`:
     - Number and Slug come from the folder name `NNNN-slug`, or from the file name for a flat plan.
     - Title comes from the first `# Plan NNNN — <title>` line.
     - Repos come from `**Repos:**`: comma-separated and trimmed. It is empty when the line is absent.
     - Status is the first word after `**Status:**`.
   - `Set-PlanStatus <path> <word>` replaces only the first word after `**Status:**` on that one line. It keeps
     the BOM and line endings, and does nothing when the line is absent.
   - `Get-NextPlanNumber <plansDir>` implements D2.
2. **`new`.**
   - With `-PlanFile` and no `<taskId>`, the header supplies everything:
     - the id is the folder name (or the file name without `.md`);
     - the repos are `Repos`, or the single key of `repos` when the plan names none and the config has one;
     - the task text is `Plan NNNN: <title>`, so `PlanName` keeps parsing it.
   - An unknown repo name throws, listing the known ones, as today.
   - The new state carries both fields:
     - `repos` (the array);
     - `repo` = `repos[0]`, so old readers keep working.
   - It also carries `planFile` (the absolute path).
3. **`Reviewing`.** Status is set to `Reviewing` right before:
   - the gate 2 reviewer call;
   - the read-back call.

   The existing code then sets the result as today. A crash leaves `Reviewing`. `review` and `readback` accept
   `Reviewing` as a start state, so a rerun recovers.
4. **`implement <id> [phase]`.**
   - **AwaitingApproval.** `okStates` gains `AwaitingApproval` only when `-AllowImplicitApproval` is set (D7). Then call the body of `Cmd-Approve` (extract it as
     `Invoke-Approve -Implicit`) with no notes and no Force. On success, set `approved.by = 'implement'`, report
     `status` → `Approved`, then continue. On failure, throw `Cmd-Approve`'s message prefixed with
     `implement could not approve: `.
   - **Which repo.** `Get-PhaseRepo` reads the phase heading (D3's pattern) from the approved plan text:
     - a single-repo run never reads the heading: the repo is `repos[0]` (rule 0.3 — plans 0001–0012 name
       topics, not repos, after the dash);
     - in a run with more than one repo, when the heading is absent, the repo is `repos[0]` and a yellow warning
       names the phase (no throw, no silence);
     - in a run with more than one repo, when it names a repo not in `repos`, throw.
   - **Branch and clean tree.** Branch creation and the gate 3 clean-tree check run **for that repo** the first
     time it is touched. They are recorded per repo in `state.repoState.<Repo> = { baseSha, baseBranch }`. The
     old top-level `baseSha` and `baseBranch` stay, mirroring `repos[0]`.
   - **Branch name.** The branch is the one `approve` set. `Get-PlanBranch` is unchanged; with the D4 id its
     fallback gives `<user>/<id>`.
   - **Prompt.** The implementer prompt gets:
     - `Repo root:` for that repo;
     - the list of all repo roots of the run.
   - **The other repos of the run.** The post-implement checks (branch switched, commits made, files outside the
     plan) run on the phase's repo, as today on `state.repo`. A change seen in any other repo — another repo of
     this run included — stays what it is today: a note from `Compare-WorkspaceFingerprint`, never a violation.
     A stray edit there is caught when that repo's own phase starts, by its clean-tree gate. This rule is not
     widened.
   - **Plan file.** After a successful implement on a run with `planFile`, call
     `Set-PlanStatus planFile Running` (D8). On approval, call `Set-PlanStatus planFile Approved`.
5. **`verify`.** Runs the verify commands of every repo in `state.repoState` (D5). The verifier prompt lists each
   repo's diff against its own `baseSha`. A run with only the old fields behaves as today. For a run with more
   than one repo in `repoState`:
   - `Invoke-Role` gets `-Cwd $Ctx.Root` (the workspace root) and `-Repo` = the last repo in `repoState` (the
     one whose phase ran last); the prompt names every repo root;
   - the manual key is `verifier.<hash>` where the hash covers every repo's `baseSha..HEAD` diff, joined in
     plan order;
   - `Write-LedgerEntry` writes one entry with `repo` = the repos joined by `+` (`Web+Ui`).
6. **`close`.**
   - With a `planFile`, the "filed and Done" check reads `Read-PlanHeader(planFile).Status -eq 'Done'`.
   - Without one, today's `plansDir` / `planFiling` code runs unchanged.
7. **Events.** `progress` and `status` events add `repo` (the phase's repo). The event schema version stays,
   since the field is additive.

### 6.2 Layout, templates, skills (phase 3; item 6 in phase 1)

1. **Templates.**
   - **Plan template.** `templates/project/plans/TEMPLATE.md` (D18) gets these changes:
     - `**Preview:** [preview.html](preview.html)`;
     - `**Run:** <the folder name>`;
     - a new `**Repos:**` line;
     - `**Companion:**` kept, for another *workspace* only;
     - phase headings `### Phase N — <Repo>` (a single-repo project writes `### Phase N — <topic>`: D3 has the
       engine read `repos[0]` there, so the word after the dash is a topic, not a repo name).
   - **`plans/README.md`.** States D1–D3 and the index table: number, title, Status, Repos, Created, Updated.
   - **`.gitignore`.** Unchanged: its existing `evidence/` line has no slash before the end, so it already ignores
     `plans/NNNN-x/evidence/` at any depth. (A `plans/*/evidence/` line would anchor to `.orchestra/` and is not
     needed.)
   - **`workflow.md`.** Its gate 1 paths change to D1.
2. **`skills/close-plan/SKILL.md`.** Generic. It reads repo names, verify commands, the base branch and the
   branch prefix from `orchestrator.json`, and does D10 in this order:
   1. **Find.** Find the plan by number or slug, or the only `Verified` run.
   2. **Check the run.** Read `runs/<id>/task.json`. If it is not `Verified`, run `run.ps1 verify <id>` once.
      If it is still not `Verified`, stop and report.
   3. **Commit, per repo of `repoState`, in plan order:**
      - stage the exact paths; where `plan.md` sits in that repo (a single-repo project), it is staged too, with
        the engine's `Approved`/`Running` Status, so the tree is clean for the switch in step 4;
      - commit, with the message shape of today's Project A CLOSE-PLAN §1 (run id, verdicts, gate counts from the
        logs).
   4. **Merge, per repo:** `git switch <baseBranch>`, then `git merge --no-ff <branch>`. On a conflict:
      - run `merge --abort`;
      - stop and report which repos merged and which did not.
   5. **Mark the plan Done.**
      - Set the `plan.md` Status line to
        `Done <date> — run <id>, merged as <hash per repo> (not pushed)`, and update the plans README row.
      - This happens on the base branch, after the merge, so `Done` lands on the base and not on the plan branch.
      - Commit in the repo that holds `.orchestra/`: its own repo in a workspace (where `plan.md`'s earlier
        Status edits were never on a plan branch and are committed here), the project repo otherwise.
   6. **Delete branches.**
      - Run `git branch -d <branch>` per repo.
      - Also delete any other branch `--merged` lists whose plan is Done.
      - Never delete `-D`, a remote branch, the current branch or the base branch.
   7. **Close the run.** Run `run.ps1 close <id>`.
   8. **Report.** One table per repo: commit, merge, close hash, branches deleted, foreign files. Then the
      ledger usage. Then one line: nothing pushed; say "push" to push.

   Never push.
3. **`skills/pick-plan/SKILL.md`.** It implements D11.
   - **Triggers:** "plans", «پلن‌ها», "pick a plan", "what's open".
   - **The table.** One numbered table. It is sorted by number, newest first.
   - **After the owner answers**, act by state:

     | State | What the skill does |
     |---|---|
     | no run, plan `Draft` | `run.ps1 new -PlanFile <folder>/plan.md` |
     | `Planning` / `Reviewing` | `run.ps1 status <id>` and wait |
     | `NeedsInput` | show the question |
     | `AwaitingApproval` | show the review verdicts and say: approve, or say "implement" |
     | `Approved` / `Implemented` | `implement <id> <next phase>` (only after the owner says implement, per D7) |
     | `Implemented` with all phases done | `verify <id>` |
     | `VerifyFailed` | show the failing gate |
     | `Verified` | say "close the plan" |

   - It reads `engine/docs/CONDUCTOR.md` for every command, as `orchestrate` does.
4. **`skills/orchestrate/SKILL.md`.** Read (only read) `<Project A>\.claude\skills\<project>-orchestrate\SKILL.md` and write its body here, made generic: no project name, `name: orchestrate`.
5. **Pointers.**
   - They live in `templates/project/claude-skills/<name>/SKILL.md`, frontmatter `name` and `description`
     included.
   - `TemplateSet` is unchanged: it lists and reads template files by their relative path.
   - The target path is decided in `SetupPlanner.Plan`, where today every template row gets
     `Path = ".orchestra/" + file`. A file under `claude-skills/` gets `Path = ".claude/skills/" + <the rest>`
     instead, at the project root (the workspace root in a workspace).
   - `item.Id` stays the template-relative path (`claude-skills/close-plan/SKILL.md`), because
     `SetupApplier.Execute` reads the content with `templates.Read(item.Id)` and writes it to `item.Path`.
     Only `Path` changes.
6. **`SetupPlanner`.** It implements D15. A test covers a config with `{repo}` paths, and asserts that no
   `prompts/` or `roles/` rows are planned.

### 6.3 Dashboard (phase 4; D12's grouping and the history buttons in phase 1)

1. **Status labels.** These go in the board catalogue, `en.ts` and `fa.ts`, exactly as given. The fa column is
   copied byte for byte, never retyped (rule 0.4).

   | Engine status, condition | en | fa |
   |---|---|---|
   | `Planning` | Draft | پیش‌نویس |
   | `Reviewing` | In review | در بازبینی |
   | `AwaitingApproval` | Reviewed · needs you | بازبینی‌شده · منتظر شما |
   | `Approved` | Approved | تأییدشده |
   | progress `phase` = implement | Implementing | در حال پیاده‌سازی |
   | `Implemented` | Implemented | پیاده‌شده |
   | progress `phase` = verify | Verifying | در حال وریفای |
   | `Verified` | Verified | وریفای‌شده |
   | `VerifyFailed`, `Violation` | Failed | ناموفق |
   | closed | Closed | بسته‌شده |

2. **`BoardHandler`** implements D12 exactly.
   - `BoardCardDto` gains `Repos: IReadOnlyList<BoardRepoDto>`, where `BoardRepoDto(string Name, string? Status)`
     is filled from the last `status`/`progress` event that carries that `repo`. Old events without `repo` fill
     nothing, and the card then shows no chips.
   - The lane is computed from the run's status, as today.
3. **`PlanName.From`** gains a third parameter `taskId`, parsed with `^(\d{4})-(.+)$`: the signature is
   `From(string? task, string? branch, string? taskId = null)`, because four other call sites in
   `Import/RunAssembler.cs` and the existing `PlanNameTests` pass two arguments and must compile unchanged.
   `BoardHandler` passes the card's task id; `RunAssembler`'s four calls pass `run.TaskId`, `task.TaskId` and
   so on, so the runs list names a D4 run too. The second parameter stays the branch. The title still comes
   from the task text `Plan NNNN: <title>` when present, which every D4 run has. Only a run with no such text
   falls back to the slug as its title (`0013 · tenant-filter`).
4. **History buttons.** `history-buttons.tsx` holds two ghost icon buttons (`ArrowRight` in rtl and `ArrowLeft` in
   ltr for back; `RotateCw` for reload) with these aria-labels:
   - en: `Back`, `Reload`;
   - fa: `بازگشت`, `تازه‌سازی`.

   One `keydown` listener in the app shell does `Alt+ArrowLeft` → back and `F5`/`Ctrl+R` → reload, with
   `preventDefault`.
5. **`LibraryDefaults`** implements D13.

### 6.4 Rules and this plan's own move (phase 5)

1. **Only this plan moves** (D16): `git mv .orchestra/plans/0013-workspace-plans.md
   .orchestra/plans/0013-workspace-plans/plan.md` and `.orchestra/previews/0013-workspace-plans/index.html` →
   `.orchestra/plans/0013-workspace-plans/preview.html`. Fix the links to those two paths: the plans README,
   `AGENTS.md`, the session log, and this plan's own `**Preview:**` line (it becomes `[preview.html](preview.html)`).
   Nothing else moves.
2. **Skills.** `.orchestra/skills/{orchestrate,close-plan,pick-plan}/` copies the templates, with this repo's
   one-repo shape. `.claude/skills/` gets the pointers. `.orchestra/prompts/` keeps its four files; they are
   prompts for people, not skills.
3. **`workflow.md`.**
   - Gate 1 step 2: the preview goes to `plans/NNNN-<topic>/preview.html`, and the number is reserved by creating
     the folder.
   - Gate 1 step 3: the plan is `plans/NNNN-<topic>/plan.md`.
   - Gate 4: replace "The close is three separate questions" with "Saying «close the plan» is the yes to all
     three — the close-plan skill"; the other gate 4 lines stay.
   - Evidence goes to `plans/NNNN-<topic>/evidence/phase-<X>/`.
4. **`plans/README.md`.**
   - New plans are folders (D1); the flat `NNNN-<topic>.md` rule stays only as "plans 0001–0012 keep that shape" (D16). The previews path rule goes.
   - State D1–D3; keep the "stays where it is" rule.
   - Rewrite the companion line: another repo of the same workspace is a `Repos:` entry; another workspace is a
     companion plan.
5. **`plans/TEMPLATE.md`.** Stays where it is (D18), with the same edits as §6.2.1.
6. **ADR 0005.** Amend the `.orchestra/` contents list: `skills/` added; `previews/` and `evidence/` become
   per-plan.
7. **ADR 0010 `plan-is-a-folder`.** It records D1, D3, D5 and D7, with the reasons: readability, one review per
   change, no concurrency.
8. **`AGENTS.md` Status.** Lists 0013.
9. **Plan 0007.** Status `Done`, with the note from D17.
10. **Plan 0012.** D10 gets the line "Narrowed by plan 0013 D15".
11. **The hand-over** lists what was removed:
    - the previews and evidence folders for new plans (old ones stay, D16);
    - the flat file-name rule for new plans;
    - "three separate questions";
    - the `plans/*/*.md` glob.

### 6.5 Companion for Project A (phase 6)

Write `<Project A>\.orchestra\plans\0001-orchestra-layout\plan.md`. This is the only write outside this
repo, and it is a Markdown file. The plan, for an agent working in Project A:

1. **Plans into folders.** Move the five open plans into folders, keeping their text byte for byte:
   - `0001-bank-transactions/`, holding `web.md` and `ui.md` plus a short `plan.md` header with `Repos: Web, Ui`.
     This is a migration exception: they were written as a pair before D3.
   - `0002-audit-log/`, holding `web.md` and `ui.md` the same way.
   - `0003-external-api-log/plan.md`.

   Runs already started (`T-WEB-007`, `T-UI-009` for bank-transactions) finish under their old ids and are
   closed with today's `CLOSE-PLAN.md`. A plan that is `Done` by the time the companion runs is deleted
   (`git rm`) instead of moved.
2. **Previews.** Move the previews of those plans into their folders. Delete the other previews with `git rm`,
   since git keeps them.
3. **Evidence.** Leave untracked evidence and list it.
4. **Skills.**
   - `CLOSE-PLAN.md` is removed, replaced by `.orchestra/skills/close-plan/SKILL.md` from the template, plus the
     Project A specifics: `main` base; Web before Ui; the gate commands.
   - `.claude/skills/<project>-close-plan` and `<project>-orchestrate` are replaced by the three pointers.
5. **Config.** In `orchestrator.json`:
   - `plansDir` becomes `.orchestra/plans`;
   - `planFiling` is removed, since a `planFile` run does not use it.
6. **Plan folder READMEs.** `plans/Ui/README.md` and `plans/Web/README.md` are folded into one
   `plans/README.md`. Their rule text moves verbatim where it is still true, and the rest is deleted and listed.
7. **Commit.** Commit in `.orchestra/` only when the owner says so.

## 7. Tests

- **Engine:**
  - `new -PlanFile` on a folder plan gives the id, repos and task text (D4).
  - An unknown repo in `Repos:` throws.
  - `implement` on `AwaitingApproval` with review PASS approves with `by = implement` (D7).
  - The same with review FINDINGS and no notes refuses, and the status is unchanged.
  - Phase 2 → Ui creates the Ui branch only then, and Web's branch already exists (D5).
  - `verify` runs both repos' commands.
  - `Set-PlanStatus` keeps BOM and CRLF.
  - A `Reviewing` crash state reruns, and `queue` still lists its pending review request (D6).
  - `watch` resuming on `AwaitingApproval` never writes `approved`; only `run.ps1 implement` does (D7).
  - `approve` records `by = owner`.
  - A multi-repo run whose phase heading is missing prints a warning and uses `repos[0]`.
  - A single-repo run whose heading is `### Phase 1 — measure` implements in `repos[0]` and does not throw (R1).
  - Every old test passes unchanged (rule 0.3).
- **API:**
  - Board: a run whose first events have no branch and later ones do is **one** card (bug 2).
  - Board: a closed run and a new run with the same id give one Done card and one live card (bug 1).
  - Board: repo chips come from `repo` on events.
  - `PlanName` parses `0013-tenant-filter`.
  - The Library lists `plans/0013-x/plan.md` as a plan and `research.md` as research, not as a plan.
  - Setup: D15; the pointers land in `.claude/skills/` while their `Id` stays the template path; a new
    workspace config has `plansDir = .orchestra/plans` and no per-repo `.gitkeep` rows (D19).
- **UI:**
  - The board card title `NNNN · title` and the chips.
  - The history buttons call `router.back` and `location.reload`.
  - `Alt+←` and `F5` do the same.

## 8. Stop and ask

- A plan in this repo whose number has two files or two folders.
- A link fix in phase 5 that would change Persian text.
- `Cmd-Approve` cannot be extracted without changing its messages.
- Any engine test that must change to pass.

## 9. Phases

| Phase | Files (§5) | Gate |
|---|---|---|
| 1 Quick fixes | D12's grouping and `closed` rule only (no chips, no titles) in 13 + its tests; D14 (17, the two labels in 19); D15 (`SetupPlanner` in 12 + its test) | api gates + ui gates |
| 2 Engine | 1–6 | engine tests |
| 3 Templates, skills, setup pointers | 7–12 | engine tests + api gates |
| 4 Board by plan, labels, Library | the rest of 13–19 | api gates + ui gates |
| 5 Rules and this plan's own move | 20–22 | all gates; every relative link in `.orchestra/`, `.ai/`, `AGENTS.md` resolves |
| 6 Project A companion | — (one new file there) | none; the owner reads it |

**Owner checks after phase 1:** items 1 and 4 of the checklist above. **After phase 4:** items 2, 3 and 5. **After
phase 5:** item 6. Stop after every phase — show the owner the diff.

## 10. Definition of done

- [x] All gates green; test counts ≥ baseline plus the new tests: engine 146/146 (146 at the baseline), api 307 tests with 0 warnings (300 at the baseline), ui 85 tests (84 at the baseline), 2026-11-25.
- [ ] The owner's checklist passed — the six items are the owner's, in the app.
- [ ] Verified by someone other than the author: <who, counts> — not yet; the author ran the gates above.
- [x] `Status: Done`, the `Done:` date, the commit and the merge in the header; the plans README row; 0007 Done.
