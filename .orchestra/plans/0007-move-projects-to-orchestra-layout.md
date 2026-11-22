# Plan 0007 — Move Project A and Project B to `.ai/` + `.orchestra/`

**Status:** Draft — waiting for the owner's answers in §8 (question 1 decides the shape), then the plan review (gate 2)
**Created:** 2026-11-19 · **Updated:** 2026-11-20 · **Done:** —
**Preview:** none of its own — preview 0001 §2 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** — (a different model family than the planner's)
**Decided by:** — (gate 2)
**Branch:** `alireza/orchestra-layout-support` here; each companion names its own
**Base:** `main` after plan 0006 is done
**Companion:** **two** — `Project B` and `Project A`, each written and approved in a
session opened in that workspace, from the brief in `.orchestra/prompts/move-to-orchestra-layout.md`.
**Steps changed:** no preview of its own (owner, 2026-11-19)

---

## فهم من از درخواست

دو پروژه‌ی فعلی باید همان ساختاری را بگیرند که Orchestra دارد:
- `.ai/` فقط دانش پروژه باشد.
- هر چیزی که مال ارکستر است در `.orchestra/` باشد: workflow، نقش‌ها، پلن‌ها، پیش‌نمایش‌ها، ایده‌ها، تحقیق‌ها،
  شواهد، runها و `project.json`.

هر دو در git خود پروژه می‌مانند.

یک واقعیت مهم: هر دو پروژه **چند repo زیر یک پوشه‌ی workspace** دارند، و خود workspace repo نیست:
- Project B: Agent، Ui و Web.
- Project A: Ui و Web.

پوشه‌ی `orchestrator/` هر کدام هم خودش یک repo جدا است. پلن‌ها امروز داخل هر repo (`<repo>/.ai/plans/`) و
کنار کد آن repo هستند.

برداشت من، که در §8 سؤال ۱ از شما می‌پرسم:
- `orchestrator/` (که بعد از پلن 0005 فقط تنظیمات و runها را دارد) به **`.orchestra/` در ریشه‌ی workspace** تغییر نام می‌دهد.
- همان repo git خودش را نگه می‌دارد.
- پلن‌ها، پیش‌نمایش‌ها، ایده‌ها و تحقیق‌های **همه‌ی repoهای آن پروژه** در آن جمع می‌شوند، به تفکیک repo
  (`plans/web/0032-…`).
- هر repo فقط `.ai/` دانش خودش را نگه می‌دارد.

هزینه‌ی این روش: پلن دیگر در تاریخچه‌ی git همان repo کد نیست و در PR آن دیده نمی‌شود.

این پلن کارهای زیر را **نمی‌کند**:
- محتوای هیچ قاعده یا پلنی را تغییر نمی‌دهد. فقط جابه‌جا می‌کند و لینک‌ها را درست می‌کند.
- تاریخچه‌ی git را بازنویسی نمی‌کند.
- چیزی را push نمی‌کند.

## جریان کار

1. سمت Orchestra (همین repo):
   - مسیرهای پیش‌فرض کتابخانه برای ساختار چند repo (`.orchestra/plans/*/*.md`).
   - مسیرهای workflow در تنظیمات موتور (`.orchestra/workflow.md`، `.orchestra/roles/`، `.orchestra/prompts/`).
   - و یک بررسی `move-check` که می‌گوید یک پروژه آماده‌ی انتقال هست یا نه.
2. برای هر پروژه، در یک session داخل همان workspace، یک companion plan از روی brief نوشته و تأیید می‌شود.
   آن session این کارها را می‌کند:
   - تغییر نام `orchestrator/` به `.orchestra/`،
   - انتقال `<repo>/.ai/agents/`، `<repo>/.ai/prompts/`، `<repo>/.ai/plans/` (و `done/`) و فایل‌های
     پیش‌نمایش و ایده به جای تازه‌شان در `.orchestra/`،
   - درست کردن لینک‌ها، skillها و `AGENTS.md`،
   - و یک commit در هر repo، **فقط وقتی شما بگویید**.
3. تنظیمات Orchestra برای آن پروژه به مسیر تازه عوض می‌شود (`RunsPath` = `<root>/.orchestra/runs`) و
   پروژه دوباره خوانده می‌شود. تعداد runها و پلن‌ها باید با قبل یکی باشد.

فازها:
- **A:** پشتیبانی Orchestra.
- **B:** companion در ProjectB.
- **C:** companion در ProjectA.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- در هر دو workspace، `orchestrator/` دیگر نیست و `.orchestra/` با همه‌ی دسته‌ها هست.
- `powershell -File .orchestra/run.ps1 agents` کار می‌کند.
- در هر repo، `.ai/` دیگر `plans/`، `agents/` یا `prompts/` ندارد.
- در داشبورد، تعداد runها و پلن‌های هر پروژه قبل و بعد از انتقال یکی است، و پلن 0032 هنوز به `T-WEB-002`
  لینک است.
- skill `project-b-orchestrate` هنوز `CONDUCTOR.md` را پیدا می‌کند.
- `git log` در هر repo: یک commit جابه‌جایی، بدون تغییر محتوا (`git diff -M` همه را rename نشان می‌دهد).

## 0. Ground rules for the executor

1. Phase A changes only this repo. Phases B–C are companions in their workspaces.
2. **Moves only.** `git mv` for tracked files; content edits only for links that a move broke. The
   companion's diff must show every moved file as a rename (`git diff -M --stat`).
3. Every repo of the project must be clean before its companion starts; no run may be in flight
   (`run.ps1 queue` empty, no `runs/T-*` folder in `Implementing`).

### Baseline

Plans 0001–0006 done; dashboard counts per project noted before the move (runs, library items).

## 1. Goal

Both projects follow ADR 0005's layout; the engine, the skills and the dashboard keep working with no
change in behaviour or counts.

## 2. Out of scope

Rewriting rules; merging per-repo `.ai/` docs; changing plan numbering; pushing; deleting history.

## 3. Rules that apply

[ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md); each project's own
`workflow.md` gates for its companion.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | `.orchestra/` holds `project.json` (replaces `orchestrator.json`, with `profile: "default"` — plan 0006) |
| D2 | The engine's `workflow` paths in `project.json` point at `.orchestra/` (`gatesPath: ".orchestra/workflow.md"`, `rolesPath: ".orchestra/roles"`, `promptsPath: ".orchestra/prompts"`, `planTemplate: ".orchestra/plans/TEMPLATE.md"`); `agentsDoc` and `indexDoc` stay `{repo}/AGENTS.md` and `{repo}/.ai/README.md` |
| D3 | `cleanTree.ignore` drops `.ai/plans/` (plans are no longer in the repos) and keeps `.ai/context/` |
| D4 | Plan files keep their names; finished plans lose the `done/` folder and get the plan 0002 header fields only if they lack a `Status` line (`**Status:** Done`) — no other content change |
| D5 | The dashboard's per-project `Library` setting is removed after the move (defaults apply) |

## 5. File manifest (this repo)

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Library/LibraryDefaults.cs` — multi-repo defaults `.orchestra/plans/*.md` **and** `.orchestra/plans/*/*.md` (repo = first segment under `plans/`) | EDIT (plan 0002 creates it) | A |
| 2 | `api/src/Orchestra.Api/Modules/Projects/Features/MoveCheck/*` — `GET api/projects/{id}/move-check`: clean repos? runs in flight? `orchestrator/` present? `.orchestra/` absent? (read-only, existence + `git status --porcelain` via `git -C <repo>` with a 10 s timeout) | NEW | A |
| 3 | `engine/examples/project.json` — the D1–D3 shape for a multi-repo project | NEW | A |
| 4 | `engine/docs/CONDUCTOR.md` — paths to `.orchestra/` | EDIT | A |
| 5 | `.orchestra/prompts/move-to-orchestra-layout.md` — the companion brief (§6.2) | NEW | A |
| 6 | `api/tests/Orchestra.Api.Tests/Library/MultiRepoDefaultsTests.cs`, `Projects/MoveCheckTests.cs` | NEW | A |
| 7 | `ui/src/features/projects/components/move-check-card.tsx` (on the project page) | NEW | A |

## 6. Per-file specification

### 6.1 Move check

`MoveCheck` → `{ ready, items: { check, ok, detail }[] }` with checks: `orchestratorFolder` exists,
`orchestraFolder` absent, each repo clean (`git status --porcelain` empty), `noRunInFlight` (no
`runs/T-*/task.json` with status `Implementing` or a `progress.json` touched in the last 10 min). It
runs `git` read-only and never writes; when `git` is not on the path, the repo checks say
`unknown` (not ready).

### 6.2 Companion brief (per workspace)

1. Preconditions: the move check is ready; the owner said go in that session.
2. In the orchestrator repo: `git mv` its tracked files so that the folder becomes `<root>/.orchestra/`
   (rename the folder itself after the git moves); `orchestrator.json` → `project.json` with `profile`
   and the D2/D3 path changes; add `workflow.md` (moved from the repo that owns the gates today —
   `Web/.ai/agents/workflow.md` in Project B, after comparing the three copies: identical copies → one
   file; differing → STOP and show the owner the diff), `roles/` (from `<repo>/.ai/agents/*.md` except
   `workflow.md` and `README.md`), `prompts/` (from `<repo>/.ai/prompts/`), `plans/<repo-lowercase>/`
   (from `<repo>/.ai/plans/` and `done/`), `previews/` (root `*.html` and `runs/_drafts/*.html`),
   `ideas/` (root `ideas.md` as `ideas/README.md` until it is split), `research/` (root `research/`),
   `evidence/` (root `screenshots/`, git-ignored).
3. In each product repo: `git rm` the moved files (they now live in `.orchestra/`), fix links in
   `AGENTS.md` and `.ai/README.md` to the new places (relative `../.orchestra/…`).
4. Skills under `<root>/.claude/skills/`: their one-line pointers move to `.orchestra/…`.
5. Commit only on the owner's word, one commit per repo, message `Move orchestrator docs to .orchestra/
   (plan 0007, no content change)`.
6. Then in Orchestra: set the project's `RunsPath` to `<root>/.orchestra/runs`, drop its `Library`
   globs, re-import, and compare the counts with the baseline.

## 7. Tests

| File | Facts |
|---|---|
| `Library/MultiRepoDefaultsTests.cs` | `.orchestra/plans/web/0032-x.md` → repo `web`, number `0032`; single-repo `.orchestra/plans/0001-x.md` still indexed |
| `Projects/MoveCheckTests.cs` | each check true/false on temp trees with `git init`; `git` missing → unknown; the check writes nothing |
| companions | their own: the rename-only diff, the engine's `agents` and `health`, the dashboard counts |

## 8. Stop and ask — open questions for the owner

1. **Where do a multi-repo project's plans live?**
   - (a) **Recommended:** in the workspace's `.orchestra/plans/<repo>/`, one git (the former
     `orchestrator` repo) for all orchestrated work of the project.
   - (b) In each repo's own `<repo>/.orchestra/plans/`, versioned with that repo's code, with the
     workspace's `.orchestra/` holding only `project.json`, `workflow.md`, roles and runs.

   (a) gives one place and one history. (b) keeps a plan in the same PR as its code. ADR 0005 holds
   either way; the brief and D2 change with the answer.
   — **answered 2026-11-20:** (a), the workspace's `.orchestra/plans/<repo>/`.
2. **The three `workflow.md` copies in Project B** (Agent, Ui, Web) — if they differ, merge them into
   one (a rule change, reviewed as such), or keep one per repo under `.orchestra/workflow/<repo>.md`?
   — **answered 2026-11-20:** merge into one, reviewed as a rule change.
3. **Project A is a client project.** Does its `.orchestra/` stay in a private repo only? It must never be
   linked from Orchestra's public repo.
   — **answered 2026-11-20:** private only; Orchestra's public repo never names its paths or content.

## 9. Phases

- **A — Orchestra support.** Files 1–7. **Gate** (plan 0001 §9). Stop.
- **B — Project B companion.** From file 5, in that workspace. **Owner checks:** the items of «نتیجه»
  for ProjectB.
- **C — Project A companion.** The same. **Owner checks:** the same for ProjectA.

## 10. Definition of done

Both workspaces on the new layout; counts equal before and after; skills and engine working; session
logs written here and there; this header `Status: Done` with the date and the commits.
