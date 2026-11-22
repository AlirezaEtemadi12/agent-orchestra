# Plan 0007 — Move Project A and Project B to `.ai/` + `.orchestra/`

**Status:** Draft — revised 2026-11-22 after plans 0005 and 0006; waiting for the plan review (gate 2)
**Created:** 2026-11-19 · **Updated:** 2026-11-22 · **Done:** —
**Preview:** none of its own — preview 0001 §2 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19; revised 2026-11-22. **Executor:** another agent, ideally a cheaper model.
**Plan review:** — (a different model family than the planner's)
**Decided by:** — (gate 2)
**Branch:** `alireza/orchestra-layout` here; each companion names its own
**Base:** `main` after plan 0006 is merged
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
- در داشبورد، تعداد runها و پلن‌های هر پروژه قبل و بعد از انتقال یکی است، و پلن 0032 هنوز به `T-WEB-002`
  لینک است.
- skill هماهنگ‌کننده‌ی هر پروژه هنوز `CONDUCTOR.md` و چک‌لیست بستن پلن را پیدا می‌کند.
- `close` یک run که پلنش `Status: Done` دارد کار می‌کند.
- `git log` در هر repo: یک commit جابه‌جایی؛ `git diff -M --stat` همه را rename نشان می‌دهد.

## 0. Ground rules for the executor

1. Phase A changes only this repo. Phases B–C are companions in their workspaces.
2. **Moves only.** Files cross repos, so a move is copy + `git add` in `.orchestra/` and `git rm` in
   the product repo (§6.2); content edits only for links and paths a move broke,
   and the one workflow merge (D7).
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
| D2 | The new paths are overrides in the project's own `orchestrator.json` `workflow` block: `gatesPath: ".orchestra/workflow.md"`, `rolesPath: ".orchestra/roles"`, `rolesIndex: ".orchestra/roles/README.md"`, `promptsPath: ".orchestra/prompts"`, `planTemplate: ".orchestra/plans/TEMPLATE.md"`; `agentsDoc` and `indexDoc` stay `{repo}/AGENTS.md` and `{repo}/.ai/README.md`. The profile is not touched |
| D3 | New config key `defaults.plansDir`, workspace-relative with the `{repo}` token (like the workflow paths): `.orchestra/plans/{repo}`. Folder names are the repo names as configured (`plans/Web/`), no lowercasing. When `plansDir` is set, `close` looks for the plan there (by number, or by slug for `planFiling: slug`, same matching as today) and accepts it only when its `**Status:**` line starts with `Done`. When it is not set, today's `plansDoneDir` behaviour is unchanged |
| D4 | Plan files keep their names. `done/` disappears: finished plans move next to open ones; a finished plan that has no `**Status:**` line gets `**Status:** Done` as its second line — no other content change |
| D5 | `cleanTree.ignore` in each project drops `.ai/plans/` and keeps `.ai/context/` |
| D6 | The worker guard (`agents.ps1`, "never run orchestrator/run.ps1") and `run.ps1` help/footer texts name both `orchestrator/run.ps1` and `.orchestra/run.ps1`; the help line for `close` says "filed (Status: Done in plansDir, or in plansDoneDir)" |
| D7 | Project B's three `workflow.md` copies: identical → one file; different → the companion STOPS, shows the owner the diff, and the merged text is a rule change the owner approves in that session |
| D8 | No dashboard move-check endpoint or card: a one-time move of two projects does not earn an API, a UI card and tests. The companion's preconditions (rule 3) are checked by the session itself with `git status` and `run.ps1 queue` |
| D9 | The dashboard needs no code change: `LibraryDefaults` already indexes `.orchestra/plans/*.md` and `.orchestra/plans/*/*.md`. Per project the owner changes `RunsPath` to `<root>/.orchestra/runs` and removes any `Library` globs (in `appsettings.Local.json` or `deploy/.env`) |

## 5. File manifest (this repo)

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `engine/run.ps1` — `close`: D3 (`plansDir` + Status check, both filing modes); help and footer text (D6) | EDIT | A |
| 2 | `engine/lib/agents.ps1` — worker guard text (D6); `plansDir` recognised by the known-keys check if one exists | EDIT | A |
| 3 | `engine/tests/offline/run-offline.ps1` — T-PD1–4 (§7) | EDIT | A |
| 4 | `engine/README.md` — the project layout drawn as `.orchestra/` (old name noted), `plansDir` documented | EDIT | A |
| 5 | `engine/docs/CONDUCTOR.md` — paths to `.orchestra/` and `plans/{repo}/`; "file the plan" = set `Status: Done` | EDIT | A |
| 6 | `.orchestra/prompts/move-to-orchestra-layout.md` — the companion brief (§6.2), with no project path or name beyond "Project A/B" | NEW | A |
| 7 | `.orchestra/plans/README.md` — row 0007 | EDIT | A |

## 6. Per-file specification

### 6.1 `close` with `plansDir`

```
$pd = Get-CfgValue $Ctx 'defaults' 'plansDir' $null
if ($pd) { $dir = Resolve-WorkflowPath $Ctx $pd $state.repo; <find by number or slug in $dir>;
           <read the first 15 lines; require a line matching '^\*\*Status:\*\*\s*Done'> }
else     { <today's plansDoneDir code, unchanged> }
```

Found but not Done → `plan <n> is in <dir> but its Status is not Done yet. Close the plan first (the
close-plan checklist), or close -Force to delete the run anyway.` Not found → the existing "not filed"
message with `<dir>`.

### 6.2 Companion brief (per workspace)

1. Preconditions: ground rule 3; the owner said go in that session; dashboard counts written down.
2. Close the terminals/sessions that use `orchestrator/`; rename the folder to `.orchestra/` (D1).
3. Inside `.orchestra/` (its own repo), with `git mv` from the product repos impossible across repos,
   **copy then `git add`** here and **`git rm`** there:
   `workflow.md` (D7), `roles/` (from `<repo>/.ai/agents/*.md` except `workflow.md`; `README.md` kept as
   `roles/README.md`), `prompts/` (from `<repo>/.ai/prompts/`), `plans/<Repo>/` (from
   `<repo>/.ai/plans/` and `done/`, D4), `plans/TEMPLATE.md`, `previews/` (root `*.html` and
   `runs/_drafts/*.html`), `ideas/` (root `ideas.md` as `ideas/README.md`), `research/`, `evidence/`
   (root `screenshots/`, git-ignored).
4. `orchestrator.json`: D2 overrides, `defaults.plansDir` (D3), D5.
5. In each product repo: fix links in `AGENTS.md` and `.ai/README.md` (relative `../.orchestra/…`).
6. Skills under `<root>/.claude/skills/` and the close-plan checklist: paths to `.orchestra/…`.
7. `powershell -File .orchestra/run.ps1 agents` and `health` pass.
8. Commit only on the owner's word, one commit per repo: `Move orchestrator docs to .orchestra/ (plan
   0007, no content change)`.
9. The owner switches the dashboard setting (D9), re-imports and compares counts with step 1.

Because files cross repos, "rename only" is checked per side: the product repos show only deletions
plus link edits; `.orchestra/` shows only additions whose content equals the deleted files
(`git diff --no-index` or a hash list in the session log).

## 7. Tests

| Test | Facts |
|---|---|
| T-PD1 | `plansDir` set, plan `0042-x.md` in `plans/Web/` with `**Status:** Done` → close succeeds |
| T-PD2 | same plan with `**Status:** Running` → close refuses with the "not Done" line |
| T-PD3 | `planFiling: slug`, `plans/Web/<slug>.md` Done → close succeeds |
| T-PD4 | no `plansDir` → today's `plansDoneDir` cases still pass (the existing tests unchanged) |
| companions | their own: `agents`, `health`, the per-side diff check, the dashboard counts |

## 8. Stop and ask — open questions for the owner

1. Where do a multi-repo project's plans live? — **answered 2026-11-20:** the workspace's `.orchestra/plans/<repo>/`.
2. The three `workflow.md` copies in Project B — **answered 2026-11-20:** merge into one, reviewed as a rule change (D7).
3. Project A is a client project — **answered 2026-11-20:** private only; Orchestra's public repo never names its paths or content.

## 9. Phases

- **A — engine support and brief.** Files 1–7. **Gate:** offline suite green on both configs (the
  existing count + 4). Stop.
- **B — Project B companion.** From file 6, in that workspace. **Owner checks:** the items of «نتیجه» for Project B.
- **C — Project A companion.** The same. **Owner checks:** the same for Project A.

## 10. Definition of done

Both workspaces on the new layout; counts equal before and after; skills and engine working; session
logs written here and there; this header `Status: Done` with the date and the commits.
