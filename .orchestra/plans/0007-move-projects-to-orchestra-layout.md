# Plan 0007 — Move Project A and Project B to `.ai/` + `.orchestra/`

**Status:** Running — phases A–C implemented 2026-11-22/23; the two companions are done in their workspaces, uncommitted, and their dashboard switch (D9) is the owner's
**Created:** 2026-11-19 · **Updated:** 2026-11-23 · **Done:** —
**Preview:** none of its own — preview 0001 §2 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19; revised 2026-11-22. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (Freebuff), 2026-11-22: first pass B1–B3 + 12 smaller points; second pass no blockers, 8 smaller points — all applied in this version
**Decided by:** — (gate 2)
**Branch:** `alireza/orchestra-layout` here; each companion names its own
**Base:** the commit that merges plan 0006 into `main` (phase A does not start before that merge)
**Companion:** **two** — `Project B` and `Project A`, each run in a session opened in that workspace,
from the brief in `.orchestra/prompts/move-to-orchestra-layout.md` (file 6).
**Steps changed:** no preview of its own (owner, 2026-11-19)

---

## فهم من از درخواست

دو پروژه‌ی فعلی باید همان ساختاری را بگیرند که Orchestra دارد:
- `.ai/` فقط دانش پروژه باشد.
- هر چیزی که مال ارکستر است در `.orchestra/` باشد: workflow، نقش‌ها، promptها، پلن‌ها، پیش‌نمایش‌ها،
  ایده‌ها، تحقیق‌ها، شواهد، runها و تنظیمات پروژه (`orchestrator.json`).

یک واقعیت مهم: هر دو پروژه **چند repo زیر یک پوشه‌ی workspace** دارند، و خود workspace repo نیست:
- Project B: Agent، Ui و Web.
- Project A: Ui و Web.

پوشه‌ی `orchestrator/` هر کدام خودش یک repo جدا است. بعد از پلن 0005 فقط این‌ها را دارد: shim
(`run.ps1`)، `orchestrator.json`، فایل محلی git-ignored، چک‌لیست بستن پلن و `runs/`.

تصمیم‌ها (جواب‌های §8، 2026-11-20):
- `orchestrator/` به **`.orchestra/` در ریشه‌ی workspace** تغییر نام می‌دهد و همان repo git خودش را نگه می‌دارد.
- پلن‌ها، پیش‌نمایش‌ها، ایده‌ها و تحقیق‌های **همه‌ی repoهای آن پروژه** در آن جمع می‌شوند، به تفکیک repo
  (`plans/Web/0032-…`).
- هر repo فقط `.ai/` دانش خودش را نگه می‌دارد.
- سه نسخه‌ی `workflow.md` در Project B یکی می‌شوند، و این یک تغییر قاعده است که بررسی می‌شود.
- Project A خصوصی می‌ماند و repo عمومی Orchestra هیچ مسیر یا محتوایی از آن را نام نمی‌برد.

هزینه‌ی این روش: پلن دیگر در تاریخچه‌ی git همان repo کد نیست و در PR آن دیده نمی‌شود.

این پلن کارهای زیر را **نمی‌کند**:
- محتوای هیچ قاعده یا پلنی را تغییر نمی‌دهد (جز یکی کردن workflowها)؛ فقط جابه‌جا می‌کند و لینک‌ها را درست می‌کند.
- تاریخچه‌ی git را بازنویسی نمی‌کند و چیزی را push نمی‌کند.
- پروفایل `default` را عوض نمی‌کند: مسیرهای تازه در `orchestrator.json` خود هر پروژه می‌نشینند، چون
  دو پروژه در دو زمان جابه‌جا می‌شوند و داده‌ی پروژه پیش خود پروژه می‌ماند.

## جریان کار

1. سمت Orchestra (همین repo، فاز A):
   - موتور یاد می‌گیرد پلن‌ها را در `.orchestra/plans/{repo}/` پیدا کند و یک پلن را با سطر
     `**Status:** Done` «بسته» بداند، نه با پوشه‌ی `done/`. پروژه‌ای که هنوز جابه‌جا نشده مثل امروز کار می‌کند.
   - متن‌هایی که اسم `orchestrator/run.ps1` را می‌برند، اسم `.orchestra/run.ps1` را هم می‌برند.
   - داشبورد `orchestrator.json` پروژه را اول در `.orchestra/` و بعد در `orchestrator/` می‌جوید.
   - نقش‌ها و promptها برای هر repo جدا می‌مانند (`roles/Web/`)، چون نسخه‌های repoها با هم فرق دارند.
   - brief همراه (prompt) نوشته می‌شود.
2. برای هر پروژه، در یک session داخل همان workspace، کار از روی brief انجام می‌شود:
   - تغییر نام پوشه‌ی `orchestrator/` به `.orchestra/` (خود پوشه ریشه‌ی repo است، پس git چیزی نمی‌بیند)،
   - انتقال workflow، نقش‌ها، promptها، پلن‌ها، پیش‌نمایش‌ها و ایده‌ها از repoها به `.orchestra/`،
   - درست کردن مسیرها در `orchestrator.json`، لینک‌ها، skillها و `AGENTS.md`،
   - و یک commit در هر repo، **فقط وقتی شما بگویید**.
3. شما تنظیمات Orchestra را برای آن پروژه عوض می‌کنید (`RunsPath` = `<root>/.orchestra/runs`) و پروژه
   دوباره خوانده می‌شود. تعداد runها و پلن‌ها باید با قبل یکی باشد.

فازها:
- **A:** پشتیبانی موتور و brief.
- **B:** companion در Project B.
- **C:** companion در Project A.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- در هر دو workspace، `orchestrator/` دیگر نیست و `.orchestra/` با همه‌ی دسته‌ها هست.
- `powershell -File .orchestra/run.ps1 agents` و `health` کار می‌کنند.
- در هر repo، `.ai/` دیگر `plans/`، `agents/` یا `prompts/` ندارد.
- در داشبورد، تعداد runها و پلن‌های هر پروژه قبل و بعد از انتقال یکی است، و پلنی که شما انتخاب می‌کنید
  هنوز به run خودش لینک است.
- صفحه‌ی effective هر پروژه در داشبورد هنوز باز می‌شود (۴۲۲ نمی‌دهد).
- skill هماهنگ‌کننده‌ی هر پروژه هنوز `CONDUCTOR.md` و چک‌لیست بستن پلن را پیدا می‌کند.
- `close` یک run که پلنش `Status: Done` دارد کار می‌کند.
- `git log` در هر repo: یک commit جابه‌جایی؛ `git diff -M --stat` همه را rename نشان می‌دهد.

## 0. Ground rules for the executor

1. Phase A changes only this repo. Phases B–C are companions in their workspaces.
2. **Moves only.** Files cross repos, so a move is copy + `git add` in `.orchestra/` and `git rm` in
   the product repo (§6.2); content edits only for links and paths a move broke,
   the one workflow merge (D7), and the `**Status:** Done` line D4 adds.
3. Every repo of the project must be clean of code (workflow.md, "Dirty means code") and no run may be
   in flight (`run.ps1 queue` empty, no `runs/T-*/task.json` in `Implementing`) before its companion starts.

### Baseline

Plans 0001–0006 merged; offline suite 133/133 on both configs; dashboard counts per project noted
before the move (runs, plans, previews, ideas).

## 1. Goal

Both projects follow ADR 0005's layout; the engine, the skills and the dashboard keep working with no
change in behaviour or counts.

## 2. Out of scope

Rewriting rules (except the D7 merge); merging per-repo `.ai/` knowledge docs; changing plan numbering;
changing the `default` profile; pushing; deleting history; a dashboard "move check" screen (dropped
2026-11-22, D8).

## 3. Rules that apply

[ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md); plan 0006 (a project's
settings live in its own `orchestrator.json`); each project's own `workflow.md` gates for its companion.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | The folder `orchestrator/` is renamed on disk to `.orchestra/`. It is the root of its own repo, so the rename changes nothing inside git; `orchestrator.json`, the git-ignored `orchestrator.local.json` and `runs/` move with it unchanged in name. The engine finds the workspace through `root` (relative to the config file), so it needs no change for the rename. There is **no** `project.json` (plan 0006) |
| D2 | The new paths are overrides in the project's own `orchestrator.json` `workflow` block. **Roles and prompts stay per repo** (review B2, 2026-11-22: the repos' copies differ, and a merge would be a rule change nobody asked for): `rolesPath: ".orchestra/roles/{repo}"`, `rolesIndex: ".orchestra/roles/{repo}/README.md"`, `promptsPath: ".orchestra/prompts/{repo}"`. The gates are one file: `gatesPath: ".orchestra/workflow.md"` (D7). `planTemplate: ".orchestra/plans/TEMPLATE.md"`; `agentsDoc` and `indexDoc` stay `{repo}/AGENTS.md` and `{repo}/.ai/README.md`. The profile is not touched |
| D3 | New **top-level** config key `plansDir` (next to `planFiling`; a project key, not a profile key — plan 0006 D1), workspace-relative with the `{repo}` token: `.orchestra/plans/{repo}`. Folder names are the repo names as configured (`plans/Web/`), no lowercasing. When `plansDir` is set, `close` looks for the plan there (by number, or by slug for `planFiling: slug`) and accepts only a file whose `**Status:**` line starts with `Done`. When it is not set, today's `plansDoneDir` behaviour is unchanged |
| D3a | **Where a plan is written** (review B3): with `plansDir` set, gate 1 writes the plan to `<plansDir>/NNNN-topic.md` (a copy of `runs/_drafts/<id>.plan.md`; slug filing: `<slug>.md`) and the preview to `.orchestra/previews/NNNN-topic/index.html`. The plan stays there for its whole life; closing it changes only its `Status:` line. CONDUCTOR.md and each project's close-plan checklist say this |
| D4 | Plan files keep their names. `done/` disappears: finished plans move next to open ones; a finished plan that has no `**Status:**` line gets `**Status:** Done` as its second line, and a Status line in another form (`Status: **Done**`, unbolded `Status: Done`) is rewritten to `**Status:** Done …` keeping the rest of the line — no other content change. `close` accepts the same forms anyway: `(?i)^\s*\*{0,2}Status:?\*{0,2}\s*\*{0,2}Done` |
| D4a | The dashboard reads the project file from `<root>/.orchestra/orchestrator.json`, falling back to `<root>/orchestrator/orchestrator.json` (review B1: `EffectiveConfigHandler` hardcodes the old path). `EffectiveConfigDto` gains `configPath`, the root-relative path it read (`.orchestra/orchestrator.json`), and the effective page's copy text uses it instead of the literal |
| D4b | `LibraryDefaults.Previews` gains `.orchestra/runs/_drafts/*.html`, so in-flight preview drafts stay indexed once the projects' `Library` globs are removed |
| D5 | `cleanTree.ignore` in each project drops `.ai/plans/` and keeps `.ai/context/` |
| D6 | The worker guard (`agents.ps1:887`, "never run orchestrator/run.ps1") and the `close` help (`run.ps1:72-73`) name both `orchestrator/run.ps1` and `.orchestra/run.ps1`. The footer (`run.ps1:109`) points at the engine's own docs, `engine/docs/CONDUCTOR.md` and `engine/docs/WORKING-MODEL.md` (stale since plan 0005); the help line for `close` says "filed (Status: Done in plansDir, or in plansDoneDir)" |
| D7 | Project B's three `workflow.md` copies (owner, 2026-11-20: one file): identical → one file; different → the companion STOPS, shows the owner the diff, and the merged text is a rule change the owner approves in that session. Roles and prompts are **not** merged (D2) |
| D8 | No dashboard move-check endpoint or card: a one-time move of two projects does not earn an API, a UI card and tests. The companion's preconditions (rule 3) are checked by the session itself with `git status` and `run.ps1 queue` |
| D9 | The dashboard needs two code changes (D4a, D4b); the rest already works: `LibraryDefaults` indexes `.orchestra/plans/*.md` and `.orchestra/plans/*/*.md`, and `DeriveRootPath` accepts `<root>/.orchestra/runs`. Per project the owner changes `RunsPath` to `<root>/.orchestra/runs` and removes any `Library` globs (in `appsettings.Local.json` or `deploy/.env`) |

## 5. File manifest (this repo)

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `engine/run.ps1` — `close`: D3 (`plansDir` + Status check, both filing modes, one shared `$planDir` variable instead of the two `$doneDir` lines); help (`:72-73`) and footer (`:109`) text (D6) | EDIT | A |
| 2 | `engine/lib/agents.ps1` — worker guard text at `:887` (D6). Nothing else: the engine has no known-keys check | EDIT | A |
| 3 | `engine/tests/offline/run-offline.ps1` — T-PD1–6 (§7) | EDIT | A |
| 4 | `engine/README.md` — the project layout drawn as `.orchestra/` (old name noted), `plansDir` documented; `engine/examples/orchestrator.json` lines 3 (copy destination) and 11–17 (`gatesPath`/`rolesPath`/`rolesIndex`/`promptsPath`/`planTemplate` to the D2 values), plus `"plansDir": ".orchestra/plans/{repo}"` | EDIT | A |
| 5 | `engine/docs/CONDUCTOR.md` — paths to `.orchestra/`; D3a (gate 1 writes the plan into `plansDir` and the preview into `previews/`; close = set `Status: Done`), with the `plansDoneDir` text kept for a project without `plansDir` | EDIT | A |
| 6 | `.orchestra/prompts/move-to-orchestra-layout.md` — the companion brief (§6.2), with no project path, run id or plan number | NEW | A |
| 7 | `api/src/Orchestra.Api/Modules/Projects/Features/EffectiveConfig/EffectiveConfigHandler.cs` — D4a | EDIT | A |
| 8 | `api/tests/Orchestra.Api.Tests/Profiles/EffectiveConfigTests.cs` — one test per location (new, old, neither → 422 as today), each asserting `configPath` | EDIT | A |
| 8a | `ui/src/features/projects/components/project-effective-config.tsx` (`:68`) and `ui/src/features/projects/types.ts` — the copy text names `configPath` (D4a) | EDIT | A |
| 8b | `api/src/Orchestra.Api/Modules/Library/LibraryDefaults.cs` and `api/tests/Orchestra.Api.Tests/Library/` — D4b, one test: a `.orchestra/runs/_drafts/T-X.html` is indexed as a preview | EDIT | A |
| 9 | `.orchestra/plans/README.md` — row 0007 | EDIT | A |

## 6. Per-file specification

### 6.1 `close` with `plansDir`

```
$pd = top-level plansDir from $Ctx.Config, else $null
if ($pd) {
  if (-not $state.repo) { throw "run '<id>' has no repo in task.json, so its plan folder is unknown. close -Force to delete it anyway." }
  $planDir = Resolve-WorkflowPath $Ctx $pd $state.repo
  <collect every candidate by today's matching (number; or slug exact, -slug suffix, branch text)>
  <keep only candidates whose first 15 lines match the D4 Status pattern; accept the first>
} else { $planDir = <today's plansDoneDir>; <today's code, unchanged> }
```

The Done filter runs **before** a candidate is chosen, so the run's own open plan (which also contains
its branch) never hides a Done copy (review point 4). Messages in `plansDir` mode:
- candidates found, none Done → `plan <n> is in <dir> but its Status is not Done yet. Close the plan
  first (the close-plan checklist), or close -Force to delete the run anyway.`
- no candidate → `plan <n> is not in <dir>. Write it there first (gate 1), or close -Force to delete
  the run anyway.`

### 6.2 Companion brief (per workspace)

1. Preconditions: ground rule 3; the owner said go in that session; dashboard counts written down.
2. Close the terminals/sessions that use `orchestrator/`; rename the folder to `.orchestra/` (D1).
3. Inside `.orchestra/` (its own repo), with `git mv` from the product repos impossible across repos,
   **copy then `git add`** here and **`git rm`** there:
   - `workflow.md` (D7);
   - `roles/<Repo>/` from `<repo>/.ai/agents/*.md` except `workflow.md` (its `README.md` stays
     `roles/<Repo>/README.md`); `prompts/<Repo>/` from `<repo>/.ai/prompts/` (D2). `rolesIndex` is
     set only if some repo had `.ai/agents/README.md`; it points at that file's new place and is
     otherwise left out (a missing index is a silent no-op in the engine);
   - `plans/<Repo>/` from `<repo>/.ai/plans/*.md` and `done/*.md` except `TEMPLATE.md` (D4); each
     repo's `.ai/plans/README.md` is kept as `plans/<Repo>/README.md` (the library never indexes a
     `README.md`, so counts do not change);
   - one `plans/TEMPLATE.md` at the root, by the D7 rule: identical copies → one file; different →
     STOP and ask the owner which one;
   - `previews/` (root `*.html` and `runs/_drafts/*.html`), `ideas/` (root `ideas.md` as
     `ideas/README.md`), `research/`, `evidence/` (root `screenshots/`).
   - `.orchestra/.gitignore` keeps what it has and lists `runs/` and `evidence/` (ADR 0005).
4. `orchestrator.json`: D2 overrides, top-level `plansDir` (D3), D5.
5. In each product repo: fix links in `AGENTS.md` and `.ai/README.md` (relative `../.orchestra/…`).
6. Skills under `<root>/.claude/skills/` and the close-plan checklist: paths to `.orchestra/…`, and D3a
   (where gate 1 writes a plan and a preview; close = `Status: Done`).
7. `powershell -File .orchestra/run.ps1 agents` and `health` pass.
8. Commit only on the owner's word, one commit per repo: `Move orchestrator docs to .orchestra/ (plan
   0007, no content change)`.
9. The owner switches the dashboard setting (D9), re-imports and compares counts with step 1.

"Moves only" is checked per region, and the session log records each:
- **tracked product-repo files** → the product repos show only deletions plus link edits;
  `.orchestra/` shows additions whose content equals the deleted files (a hash list), except the D4
  `Status` lines;
- **untracked workspace-root files** (root `*.html`, `ideas.md`, `research/`, `screenshots/`) → a hash
  list before and after; no deletion side exists;
- **`<root>/.claude/skills/`** is outside every repo → a local edit, listed in the session log for the
  owner to accept; no commit.

## 7. Tests

| Test | Facts |
|---|---|
| T-PD1 | `plansDir` set, plan `0042-x.md` in `plans/Web/` with `**Status:** Done` → close succeeds |
| T-PD2 | same plan with `**Status:** Running` → close refuses with the "not Done" line |
| T-PD3 | `planFiling: slug`, folder holds the run's own open plan (Running, names the branch) **and** a Done copy → close succeeds on the Done one |
| T-PD4 | no `plansDir` → today's `plansDoneDir` cases still pass (the existing tests unchanged) |
| T-PD5 | `plansDir` set, no plan file in the folder → the "not in <dir>. Write it there first (gate 1)" line |
| T-PD6 | `plansDir` set, run whose `task.json` has no `repo` → the "no repo" line, nothing read from `.orchestra/plans/` |
| companions | their own: `agents`, `health`, the per-side diff check, the dashboard counts |

## 8. Stop and ask — open questions for the owner

1. Where do a multi-repo project's plans live? — **answered 2026-11-20:** the workspace's `.orchestra/plans/<repo>/`.
2. The three `workflow.md` copies in Project B — **answered 2026-11-20:** merge into one, reviewed as a rule change (D7).
3. Project A is a client project — **answered 2026-11-20:** private only; Orchestra's public repo never names its paths or content.

## 9. Phases

- **A — engine and dashboard support, and the brief.** Files 1–9. **Gate:** `dotnet test` and the
  offline suite green on both configs; the suite count is the base's count (133 at 0006) + 6 per config,
  with T-PD1–6 listed in the log; UI typecheck, lint, test and build. Stop.
- **B — Project B companion.** From file 6, in that workspace. **Owner checks:** the items of «نتیجه» for Project B.
- **C — Project A companion.** The same. **Owner checks:** the same for Project A.

## 10. Definition of done

Both workspaces on the new layout; counts equal before and after; skills and engine working; session
logs written here and there; this header `Status: Done` with the date and the commits.
