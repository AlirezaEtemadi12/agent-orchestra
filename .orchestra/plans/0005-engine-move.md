# Plan 0005 — One engine: the orchestrator moves into `engine/`

**Status:** Done (2026-11-22). A+B 2026-11-20; C 2026-11-22 in both projects (Project B `d7da51c`,
Project A `58d1806`); `health` green in both. Phase C found three Project A behaviours the engine had lost
(the plan's `**Branch:**` line, the frontend contract doc → config `contractDoc`, the planner phase name);
fixed in `fe8a77f`, offline suite 127/127 on both configs
**Created:** 2026-11-19 · **Updated:** 2026-11-22 · **Done:** 2026-11-22 — A+B commit `ad2f99f`, merge `4c183de`; gaps commit `fe8a77f` on `alireza/engine-project-a-gaps`, merge `f3e8f58`; all into `alireza/skeleton-and-import`
**Preview:** none of its own — the roadmap's phase 2 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** pre-gate review 2026-11-20 (B1–B7 + 5 smaller points, checked against both orchestrator repos read-only); fixes in this version
**Baseline counts:** Project B offline suite 114/114 (HEAD `b327225`) · Project A offline suite 116/116 (HEAD `4c0d570`) — measured 2026-11-20, logs in `.orchestra/evidence/0005-engine-move/baseline-*.log`
**Decided by:** owner's §8 answers 2026-11-20; D10 proposed by the planner, D11 and "dirty means code" decided by Alireza 2026-11-20
**Branch:** `alireza/engine-move` — the implementer creates it.
**Base:** `alireza/skeleton-and-import` @ `89dbee1` (plan 0003 closed; there is no `main` yet). This plan comes **before** 0004 (owner, 2026-11-20)
**Companion:** **shim** — in `Project ProjectA/orchestrator` and `Project ProjectB/orchestrator`:
the engine files are removed and `run.ps1` becomes a 10-line shim (§6.4), written and approved in
those repos from the brief in `.orchestra/prompts/engine-shim-companion.md`. The shim finds the engine through
`enginePath` in a git-ignored `orchestrator.local.json` (§8.2).
**Steps changed:** no preview of its own (owner, 2026-11-19)

---

## فهم من از درخواست

ارکستر الان دو نسخه دارد، یکی در Project A و یکی در ProjectB. هر تغییری باید دو بار زده شود، و همین حالا هم
دو نسخه کمی با هم فرق دارند. این پلن **کد ارکستر را یک‌جا در `engine/` همین repo** می‌آورد:
- `run.ps1` و `lib/`،
- تست آفلاین،
- `refresh-graph.ps1`،
- راهنماهای conductor.

بعد از آن، هر پروژه فقط این‌ها را نگه می‌دارد:
- `orchestrator.json` خودش،
- پوشه‌ی `runs/`،
- یک `run.ps1` کوچک که موتور مشترک را با تنظیمات همان پروژه صدا می‌زند.

دستورهایی که امروز می‌زنید (`powershell -File orchestrator/run.ps1 new …`) **همان‌طور می‌مانند**.

برداشت من از تفاوت‌های دو نسخه، بعد از مقایسه‌ی خط به خط:
- **(۱)** Project A پلن‌ها را با اسم (slug) بایگانی می‌کند و Project B با شماره. این فرق یک تنظیم می‌شود: `planFiling: "number" | "slug"`.
- **(۲)** Project A دو تابعی را که دیگر استفاده نمی‌شوند comment کرده است. این پلن آن‌ها را حذف می‌کند.
- **(۳)** توضیح‌های مخصوص هر پروژه (اسم پروژه، فهرست repoها) از تنظیمات خوانده می‌شود.

پایه، نسخه‌ی Project B است، چون جلوتر است.

این پلن کارهای زیر را **نمی‌کند**:
- پروفایل‌ها را نمی‌سازد (پلن 0006).
- پروژه‌ها را به `.orchestra/` منتقل نمی‌کند (پلن 0007).
- رفتار هیچ دستوری را عوض نمی‌کند.

## جریان کار

1. پیش از شروع، هر دو repo ارکستر باید تمیز باشند و هیچ تغییر commit‌نشده‌ای نداشته باشند (§8 سؤال ۱).
2. فایل‌های موتور Project B به `engine/` کپی می‌شوند. سه فرق بالا اعمال می‌شود.
3. تست آفلاین در `engine/` با تنظیمات هر دو پروژه اجرا می‌شود و باید سبز باشد.
4. در هر repo ارکستر، companion فایل‌های موتور را حذف می‌کند و `run.ps1` را با shim عوض می‌کند. shim مسیر
   Orchestra را از `enginePath` در فایل `orchestrator.local.json` همان پروژه می‌خواند (این فایل در git نیست).
   این کار فقط وقتی انجام می‌شود که آن پروژه run باز نداشته باشد.
5. شما یک run کوچک در هر پروژه می‌زنید. خروجی و ledger مثل قبل است.

فازها:
- **A:** انتقال موتور و یکی کردن دو نسخه.
- **B:** تست آفلاین برای هر دو پروژه.
- **C:** companion یعنی shim در دو repo.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- `powershell -NoProfile -File engine/tests/offline/run-offline.ps1` در این repo سبز است، با تعداد تست‌ها
  دست‌کم برابر هر نسخه‌ی قبلی.
- در Project B، `powershell -File orchestrator/run.ps1 agents` همان خروجی قبل را دارد، به‌اضافه‌ی یک خط
  `engine: <enginePath>\engine`.
- در Project A، `close` یک run که پلنش با اسم بایگانی شده، مثل قبل پلن را پیدا می‌کند.
- اگر `orchestrator.local.json` یا `enginePath` نباشد، shim یک خطای یک‌خطی می‌دهد که می‌گوید چه چیزی را بنویسید.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md).
2. Phase A–B read the two orchestrator repos **read-only** and write only under `engine/` here.
   Phase C is the companion, done in those repos, not from here.
3. **Behaviour-preserving.** Except the three differences of D3–D5, every line of Project B's engine
   is copied as it is. No refactor, no rename, no "while I am here".

### Baseline

Before the first edit, in each orchestrator repo (read-only): no uncommitted **code** — `*.ps1`,
`*.json`, `tests/**` (uncommitted Markdown does not count: [workflow § Dirty means code](../workflow.md#gate-3--branch-before-you-edit));
note the HEAD hash. Code is copied **from that commit** (`git show <hash>:<path>`), never from the working
tree; the history docs of D7 are copied as they are on disk. Then run
`powershell -NoProfile -File tests/offline/run-offline.ps1` in the background with its output in a log
(`.orchestra/evidence/0005-engine-move/baseline-<project>.log`) and wait for the tally — it takes 15+ minutes; never
stop it on a timeout. Record both counts on the header's **Baseline counts** line. If either repo has
uncommitted code, STOP and tell the owner.

**Checked 2026-11-20 by the executor — not met, so phase A was not started:**

- Project B's `orchestrator/` is a git repo on `main` at `c4b9ca6`, **dirty**: `run.ps1`,
  `orchestrator.json`, `tests/offline/run-offline.ps1` and `CLOSE-PLAN.md` are modified, and
  `SUITE-SPEED-PLAN.md` is untracked. The uncommitted work renames the archive file to
  `runs/_archive/<id>.<yyyyMMddHHmm>.json` (a task id is reused after close).
- Project A's `orchestrator/` is a git repo on `main` at `a947857`, **dirty**: the same four files plus
  `ARCHIVE-ON-CLOSE-PLAN.md`, with the same uncommitted archive-name change.
- Neither repo has a remote. §8 question 1's answer ("the archive work is committed in both") is not
  what the working trees show, so the copy in D1 has no commit to name.
- (Superseded by the new D11, 2026-11-20.) Run folders exist in both projects — 28 in Project B,
  3 in Project A; they do not block phase C any more.
- **Owner's action:** commit the archive-name change (`run.ps1`, `orchestrator.json`,
  `tests/offline/run-offline.ps1`) in both repos. The `.md` files may stay uncommitted.
- The offline suite runs but is long: Project B's suite was still running after 13 minutes here and was
  stopped by the executor, so its baseline count is still unmeasured.

## 1. Goal

One engine in `engine/`, both projects driving it through a shim, every command and the offline suite
behaving exactly as before.

## 2. Out of scope

Profiles and the effective-config merge (0006); `.orchestra/` layout (0007); the reporter (0004);
changing any command, prompt text, verdict rule or ledger format; moving `runs/`.

## 3. Rules that apply

[ADR 0001](../../.ai/decisions/0001-one-repo-dotnet-nextjs-sqlite.md) (`engine/` in this repo),
[ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md) (the engine never needs
the dashboard), [ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md).

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | Base: Project B's orchestrator at its HEAD when phase A starts (recorded in the header) |
| D2 | The engine already separates its own folder (`$PSScriptRoot`: `lib/`, `refresh-graph.ps1`) from the config's folder (`root`, `runsDir` relative to `orchestrator.json`) — kept as it is; the shim passes `-Config` |
| D3 | `planFiling: "number" \| "slug"` in `orchestrator.json` (default `number`). `Cmd-Close`'s filed-copy check uses Project B's code for `number` and Project A's code for `slug`, both copied verbatim |
| D4 | `Get-Sentinel`, `Get-PrimarySentinel` (`lib/bus.ps1`) and `Get-RepoGateDoc` (`lib/gates.ps1`) are deleted (dead since 0.3.0 / 0.5.x, per Project A's comment); `rules.gateCommandsFromRepoDoc` is removed from both configs |
| D5 | The project description in `Show-Usage` (two lines) comes from `orchestrator.json` → `description` |
| D6 | The conductor docs (`CONDUCTOR.md`, `REVIEW-BRIEF.md`, `WORKING-MODEL.md`) move to `engine/docs/`; the project-specific `CLOSE-PLAN.md` stays in each project (its skill points at it) |
| D7 | Historical design notes and finished plans (`DESIGN.md`, `*-PLAN.md` including the untracked `SUITE-SPEED-PLAN.md`, `PROJECTA-PORT-PROMPT.md`) go to **`engine/docs/history/`**, unchanged, with a README saying they are history. Not under `.orchestra/research/` (its files are flat `YYYY-MM-DD-<topic>.md`, and the library would index them as research) |
| D8 | The engine is Windows PowerShell 5.1 compatible (as today); no `pwsh`-only syntax is added |
| D9 | `engine/VERSION` = `1.0.0` — a marker only; nothing reads it until 0006. Each project's `orchestrator.json` `version` stays its config version |
| D10 | **Engine path:** the shim's one function `Get-EnginePath` reads `enginePath` from `orchestrator.local.json` beside it (git-ignored). The owner may change this mechanism later; only that function changes |
| D11 | **Projects in flight:** open runs (folders under `runs/`) do **not** block phase C — the engine is behaviour-identical and reads the same `runs/` through the same `orchestrator.json`, so a run continues under the shim. Phase C waits only while a `run.ps1` of that project is **executing** (a PowerShell process whose command line has that project's `orchestrator` path); the executor checks, and the owner confirms before the swap |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `engine/run.ps1` (copy + D3, D5) | NEW | A |
| 2 | `engine/lib/{agents,bus,gates,pool}.ps1` (copy + D4) | NEW | A |
| 3 | `engine/refresh-graph.ps1` (copy) | NEW | A |
| 4 | `engine/tests/offline/{run-offline.ps1,mock-agent.ps1}` (copy + a slug-filing case + a run per project config) | NEW | B |
| 5 | `engine/tests/offline/configs/{number,slug}.orchestrator.json` (synthetic, both styles) | NEW | B |
| 6 | `engine/docs/{CONDUCTOR,REVIEW-BRIEF,WORKING-MODEL}.md` (copy; paths updated to `engine/`) | NEW | A |
| 7 | `engine/README.md` (what the engine is, how a project calls it, `orchestrator.local.json`) | NEW | A |
| 8 | `engine/VERSION` | NEW | A |
| 9 | `engine/examples/orchestrator.json` (Project B's, with placeholders for repos, verify commands, `description`, `planFiling`) | NEW | A |
| 10 | `engine/docs/history/**` + `README.md` (D7) | NEW | A |
| 11 | `.orchestra/prompts/engine-shim-companion.md` (§6.4) | NEW | C |
| 12 | `.ai/architecture/overview.md` — `engine/` exists; `.ai/ci.md` — a separate "Engine (Windows only, owner-run)" section with the offline-suite command, its count and its run time (15+ min: run in the background with a log, never stop it on a timeout), **outside** the green table (needs Windows PowerShell 5.1) | EDIT | A |
| 13 | `.ai/engine/conventions.md` — PowerShell 5.1 rules, the hooks, "behaviour-preserving" | NEW | A |
| 14 | `.ai/README.md` — `engine/` in the folder map; a "Doing X → read Y" row: changing the engine → `.ai/engine/conventions.md` | EDIT | A |
| 15 | `AGENTS.md` — Shape row: `engine/` exists (drop "later") | EDIT | A |
| 16 | `engine/examples/orchestrator.local.json` (`{ "enginePath": "C:\\path\\to\\Project Orchestra" }`) | NEW | A |

## 6. Per-file specification

### 6.1 Copy and differences

Copy with line endings normalized to CRLF (as the sources). Then exactly:

Find code by the anchors below, never by line number. If an anchor is not found exactly, STOP.

- `run.ps1` `function Show-Usage`: in its here-string, the first line
  `orchestrator / run.ps1 - multi-agent, four-gate driver for Project B (Agent, Ui, Web)` and the two
  lines starting `  - Repos: Agent (.NET 10 WPF), Ui (Next.js 16), Web (.NET 10 API)` become
  `$Ctx.Config.description` (format the here-string with `-f`). `Show-Usage` gets `-Ctx`; for `help`
  without a loadable config it prints `(no project config loaded)`.
- `function Cmd-Close`, inside `if (-not $Force) {`, from the line
  `$num = Get-PlanNumber -Branch $branch -PlanText $planText` through
  `Write-Host "plan $num is filed: …"`: wrap in `switch ($planFiling) { 'slug' { … } default { … } }`
  with `$planFiling = Get-CfgValue $Ctx 'planFiling' $null 'number'`. `default` = those Project B lines
  unchanged. `'slug'` = Project A's block from its comment `# Project A plans are filed by slug` through
  `Write-Host "plan '$slug' is filed: …"`, unchanged.
- `function Get-PlanSlug` is copied from Project A's **`lib/gates.ps1`** (not `run.ps1`) into
  `engine/lib/gates.ps1`, right after `Get-PlanNumber`, unchanged.
- `refresh-graph.ps1` is copied unchanged: it finds `lib/` by `$PSScriptRoot` and `run.ps1` always calls
  it with `-Config $Ctx.ConfigPath`, so the move does not change what it reads.
- `lib/bus.ps1`: delete `Get-Sentinel` and `Get-PrimarySentinel`. `lib/gates.ps1`: delete
  `Get-RepoGateDoc`. `grep` proves no caller remains.
- `run.ps1` `Cmd-Agents`: after the `config:` line print `engine: <$OrchDir>`.

### 6.2 Tests

`run-offline.ps1` runs its whole suite once per config in `configs/` (number and slug filing), in a temp
folder, with `mock-agent.ps1`, as today. New cases: `close` with `planFiling: slug` finds `done/<slug>.md`,
then `*-<slug>.md`, then a file naming the branch (Project A's three steps); `agents` prints the engine
line; a config without `description` prints the fallback.

### 6.3 Docs

`engine/README.md` explains: the engine has no state of its own; a project calls it through its shim
with its own `orchestrator.json`; `orchestrator.local.json` (git-ignored, one per project) holds
`enginePath`, the Orchestra checkout; updating the engine is a
`git pull` of Orchestra; a project can pin an engine version later (0006).

### 6.4 The shim — companion brief

For a session opened in each orchestrator repo (`.orchestra/prompts/engine-shim-companion.md`):

- First check D11 (no `run.ps1` of this project executing); if one is, stop and tell the owner.
- Delete `lib/`, `refresh-graph.ps1`, `tests/`, and the docs moved by D6/D7. Keep `orchestrator.json`
  (add `description` and `planFiling` — `number` for Project B, `slug` for Project A; remove
  `rules.gateCommandsFromRepoDoc`), `CLOSE-PLAN.md`, `.gitignore`, `runs/`.
- Add `orchestrator.local.json` to `.gitignore`; create the file with the `enginePath` the owner gives
  (never guessed). It is never committed.
- Skills: in every `.claude/skills/*/SKILL.md` that names `orchestrator/CONDUCTOR.md`,
  `REVIEW-BRIEF.md` or `WORKING-MODEL.md`, replace the path with
  "`<enginePath from orchestrator/orchestrator.local.json>/engine/docs/<file>`". `CLOSE-PLAN.md` paths stay.
- New `run.ps1`:

```powershell
# Calls the shared engine in Orchestra with this project's orchestrator.json.
function Get-EnginePath {
    # The one place that knows where the engine is (plan 0005 D10).
    $local = Join-Path $PSScriptRoot 'orchestrator.local.json'
    if (-not (Test-Path -LiteralPath $local)) { return $null }
    $path = (Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json).enginePath
    if ($path -and (Test-Path -LiteralPath (Join-Path $path 'engine\run.ps1'))) { return $path }
    return $null
}
$engine = Get-EnginePath
if (-not $engine) {
    Write-Host 'Write the Orchestra checkout as "enginePath" in orchestrator\orchestrator.local.json (it must contain engine\run.ps1).' -ForegroundColor Red
    exit 1
}
if ($args -contains '-Config') {
    Write-Host 'Do not pass -Config: the shim always uses this project''s orchestrator.json.' -ForegroundColor Red
    exit 1
}
& (Join-Path $engine 'engine\run.ps1') -Config (Join-Path $PSScriptRoot 'orchestrator.json') @args
exit $LASTEXITCODE
```

- Owner checks in that repo: `agents`, `health`, a `new` on a one-line task with `-DryRun`, and the
  close of a filed plan.

## 7. Tests

The offline suite (§6.2) is the test. Its count is recorded in `.ai/ci.md` and must be ≥ the larger of
the two baselines.

## 8. Stop and ask — open questions for the owner

1. **Dirty orchestrator repos** — the archive-name change was found **uncommitted** in both (2026-11-20); the owner commits the code files, Markdown may stay (workflow § Dirty means code). Left: Project B's
   untracked `SUITE-SPEED-PLAN.md` — commit it there, or move it with the history docs (D7)?
   — **answered 2026-11-20:** move it with the history docs into `engine/docs/history/` (D7).
2. **`ORCHESTRA_HOME`** — a user environment variable on Windows, or a path written in each project's
   `orchestrator.json` (`enginePath`)? The variable keeps machine paths out of the projects' git.
   — **answered 2026-11-20:** neither: `enginePath` in a **git-ignored `orchestrator.local.json`** per project (modular, no machine path in git); the shim stops with a clear message when the file or path is missing. The owner may revisit this choice later — keep the lookup in one function of the shim so it is a one-place change.
3. **Skills** (`.claude/skills/project-b-orchestrate` etc. point at `orchestrator/CONDUCTOR.md`): update
   them in the companion to `%ORCHESTRA_HOME%/engine/docs/CONDUCTOR.md`, or keep a copy of `CONDUCTOR.md`
   in each project?
   — **answered 2026-11-20:** point the skills at the engine's `CONDUCTOR.md`, through the `enginePath` of `orchestrator.local.json`; no copies.
4. **Projects in flight** (added 2026-11-20; changed the same day by the owner): open runs do not block; phase C waits only while that project's `run.ps1` is executing (D11). Phases A–B never touch the projects.

## 9. Phases

- **A — move and reconcile.** Files 1–3, 6–10, 12–16. **Gate:** the offline suite with the number
  config. Stop.
- **B — both configs.** Files 4–5. **Gate:** the suite with both configs. Stop.
- **C — companion.** File 11; then a session in each orchestrator repo. **Owner checks:** the items of
  «نتیجه».

## 10. Definition of done

Offline suite green for both configs; both projects run through the shim; no engine file left in either
project; session log written; this header `Status: Done` with the date, commit and merge.
