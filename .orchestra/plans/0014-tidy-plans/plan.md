# Plan 0014 — One plan shape in Orchestra, two board fixes, a header that stays

**Status:** Draft
**Created:** 2026-11-25 · **Updated:** 2026-11-25 · **Done:** —
**Preview:** none — no new screen; the header keeps its buttons and only moves two of them
**Run:** `0014-tidy-plans`
**Planned by:** Claude Opus 5.5, 2026-11-25. **Executor:** another agent, ideally a cheaper model.
**Plan review:** Freebuff (Buffy), relayed by the owner, 2026-11-25: changes requested — 1 blocker (the §9 grep
could never fail), 4 important, 4 minor. v2 applies all nine: §9 (the command out of the table, `engine/docs/history/`
excluded), D2 (all three call sites), §6.2.10 (template `evidence/` row, `skills/` row), D6 (the review prompt is
kept, made generic), `.ai/ci.md` (§6.1.4), §7 (no header tests exist), D3 (the sidebar sticks too), §6.2.5 (no
retrofit). A re-review of v2 by the same reviewer, 2026-11-25: **approve**, with three minor wording points
(M1–M3); v3 applies all three (§6.2.10 `skills/` row in the template README, §9 phase 1 = 1–4a, §6.2.3 no
repeated rule).
**Decided by:** Alireza, 2026-11-25, in the chat. Every choice below is already decided — **decide nothing new.** If
reality contradicts this plan, STOP and ask.
**Branch:** `alireza/tidy-plans` — **already exists** (made by the planner from `main` @ `361658b`); it carries the
planner's uncommitted files, which belong to this plan's diff: this plan, its row in `plans/README.md`, and
`.orchestra/ideas/plan-page-and-checklist.md` with its row in `ideas/README.md`. Switch to it; do not create another.
**Base:** `main` @ `361658b`
**Companion:** None
**Steps changed:** no preview (no new screen)

---

## فهم من از درخواست

- پلن 0013 تمام شد، ولی دو مورد از چک‌لیستش روی برد رد شد. هر دو باگ واقعی‌اند و runهای تازه را هم می‌گیرند:
  - کارت run بسته‌شده در ستون Done برچسب «منتظر تأیید شما» دارد. وقتی اپ بسته باشد و run جلو برود، رویدادهای
    وضعیت به داشبورد نمی‌رسند و کارت آخرین وضعیتی را نشان می‌دهد که رسیده بود.
  - عنوان کارت «claude-opus · …» است، چون موقع ساختن run از رویدادها، نام خانواده‌ی مدل به‌جای شماره‌ی پلن
    نوشته می‌شود.
- دکمه‌های «بازگشت» و «تازه‌سازی» به ابتدای هدر می‌روند (در انگلیسی سمت چپ؛ در فارسی صفحه آینه می‌شود). هدر
  موقع اسکرول بالای صفحه ثابت می‌ماند تا برای برگشتن لازم نباشد تا بالا اسکرول کنید.
- همه‌ی پلن‌های Orchestra یک شکل می‌شوند. پلن‌های قدیمی تخت (0001 تا 0012) ارزش بازنویسی ندارند و پاک می‌شوند؛
  git آن‌ها را نگه می‌دارد. 0010 که هنوز باز است به پوشه‌ی خودش می‌رود و برای اجرا با ارکستر آماده می‌شود.
  **پلن‌های شکل تازه (از 0013 به بعد) بعد از Done هم سر جایشان می‌مانند** و وضعیتشان دیده می‌شود.
- شواهد هر پلن (`evidence/`) هم بعد از بستن پلن می‌ماند؛ گاهی برمی‌گردید و نتیجه را دوباره می‌بینید. git آن را
  دنبال نمی‌کند.
- لاگ‌های session در `.ai/context/` دست نمی‌خورند؛ یک خط توضیح می‌گوید پلن‌های قدیمی را از git بگیرید.

**کارهایی که این پلن انجام نمی‌دهد:**
- Project B: بعد از Orchestra و Project A نوبت آن است؛ فقط یک خط در roadmap ثبت می‌شود.
- کدی که شکل تخت پلن را می‌خواند می‌ماند، چون Project B هنوز آن شکل را دارد.
- صفحه‌ی هر پلن و چک‌لیست در اپ: فقط ایده است (`ideas/plan-page-and-checklist.md`).
- اجرای 0010: بعد از merge همین پلن، Orchestra در اپ ثبت می‌شود و 0010 با ارکستر اجرا می‌شود.
- پاک کردن داده‌های قدیمی داشبورد و Project A: انجام شده است (planner، 2026-11-25)، جزو این پلن نیست.

## جریان کار

1. **فاز ۱، برد و هدر.** کارت run بسته‌شده وضعیت پایانی خودش را نشان می‌دهد و هیچ‌وقت منتظر شما نیست. عنوان کارت از
   پلن می‌آید، نه از نام مدل. دکمه‌های بازگشت و تازه‌سازی به ابتدای هدر می‌روند و هدر ثابت می‌شود.
2. **فاز ۲، پلن‌ها و قاعده‌ها.** پلن‌های قدیمی پاک می‌شوند، 0010 به پوشه می‌رود، لینک‌ها به متن ساده تبدیل می‌شوند،
   قاعده‌ی «پلن‌های قدیمی تخت می‌مانند» حذف می‌شود و قالب پلن درست می‌شود.

بعد از merge، کار شما: ساختن نسخه‌ی تازه‌ی اپ، و بعد ثبت Orchestra در اپ (Projects، بعد setup). setup فایل‌های
`orchestrator.json` و `run.ps1` را برای Orchestra می‌سازد و بعد 0010 با ارکستر اجرا می‌شود.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

برد وضعیت واقعی هر run را نشان می‌دهد، حتی اگر اپ وسط کار بسته بوده باشد. Orchestra فقط پلن‌های شکل تازه دارد.

**چک‌لیست شما در اپ:**
- [ ] **برد:** هیچ کارتی در ستون Done برچسب «منتظر تأیید شما» ندارد. (برای دیدنش: اپ را ببندید، یک run را تأیید و
      پیاده‌سازی کنید، اپ را باز کنید و run را ببندید.)
- [ ] **عنوان کارت:** اولین run تازه، مثلاً 0010، با `0010 · …` نشان داده می‌شود، نه `claude-opus · …`.
- [ ] **هدر:** بازگشت و تازه‌سازی در ابتدای هدر هستند؛ صفحه‌ی بلندی را پایین بروید، هدر سر جایش می‌ماند.
- [ ] **Library:** بعد از ثبت Orchestra در اپ، تب Plans فقط 0010، 0013 و 0014 را برای Orchestra نشان می‌دهد.

## 0. Ground rules for the executor

1. Follow [`../../workflow.md`](../../workflow.md): never commit/push/merge unless asked in that message; one
   phase = one uncommitted diff. The branch already exists (header).
2. **Deleting is `git rm`, moving is `git mv`** — so git keeps the history and shows renames.
3. Persian text is never retyped or edited. §6.2.9 changes one ASCII path inside a Persian block of
   `start-session.md`; no Persian word changes.
4. Nothing outside this repository is touched.
5. Plan 0013's text is history: it is **not** edited, even where it describes the rule this plan removes (D16).

### Baseline

```
cd api; dotnet build Orchestra.slnx -warnaserror; dotnet test Orchestra.slnx      # 307 on 2026-11-25
cd ui; npm run typecheck; npm run lint; npm run test; npm run build                # 85 on 2026-11-25
```

The two numbers are plan 0013's own Definition of done; `.ai/ci.md` stops at plan 0012 (300 / 83) because 0013
added no row. Record the real counts before the first edit; they never drop. §6.1.4 writes the row.

## 1. Goal

A closed run reads as closed on the board and every card is titled after its plan; the header's back and reload
sit first and stay on screen; and every plan in this repository is a folder.

## 2. Out of scope

- An **open** run whose status events were lost still shows its last reported status until the next event
  arrives; only closed runs are fixed here.
- Reading the flat plan shape (engine, API, Library, fixtures) — unchanged; Project B still uses it.
- Plan 0013's text, the session logs in `.ai/context/`, the test fixtures under `api/tests/**/Fixtures/`.
- Removing evidence at close (the owner keeps it).
- Anything in Project A or Project B.

## 3. Rules that apply

- [`../../workflow.md`](../../workflow.md) — gates; **this plan edits gate 1 step 3** (§6.2.4).
- [`../README.md`](../README.md) — plan shape; **this plan edits it** (§6.2.3).
- [ADR 0004](../../../.ai/decisions/0004-persian-and-english.md) — Persian text.
- `.ai/api/conventions.md`, `.ai/ui/conventions.md`.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | **A closed card shows how the run closed.** In `BoardHandler.ToCard`, when `closed` is true: the status is the `status` field of the `run.closed` event that made it closed, else (closed through the run row) `run.Status`; and `WaitingFor` is `null`. The lane rule is unchanged (`closed` → done). A closed run that never left `AwaitingApproval` therefore shows `AwaitingApproval` in Done, but does not say "Waiting for your approval" |
| D2 | **The plan family is never a plan number, and the task id names a plan everywhere.** Delete `run.PlanNumber ??= data.Text("planFamily");` from `RunAssembler.ApplyEvent` (the `RunCreated` case). The three `PlanName.From` calls that pass two arguments get the task id as the third: the live `task.json` path (`PlanName.From(task.Task, task.Branch, target.TaskId)`), the archive path (`PlanName.From(archive.Task.Task, archive.Task.Branch, target.TaskId)`) and the events path (`PlanName.From(run.Task, run.Branch, run.TaskId)`). The ledger path already passes it. So a D4 run is named after its plan even when no event reached the dashboard. Nothing else in import changes. The owner's database was emptied on 2026-11-25, so no existing row needs repair |
| D3 | **History buttons first; header and sidebar stay on screen.** `HistoryButtons` moves from the right-hand group into the left-hand group of `app-shell.tsx`, right after `AppSidebarTrigger` and before the app name. The header gets `sticky top-0 z-30 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80`. The sidebar's `<aside>` in `app-sidebar.tsx` gets `sticky top-0 h-svh overflow-y-auto`, so the menu stays beside a long page too. Logical order only: in `fa` (rtl) the group mirrors to the right like the rest of the page — no `dir`-specific code |
| D4 | **Old plans go; open and new plans stay.** `git rm` the flat plans 0001–0009, 0011, 0012 and the whole `.orchestra/previews/` folder. `git mv` 0010 into `0010-gridify-tables/plan.md`. 0013 stays. A plan in the new shape is never deleted at Done |
| D5 | **A link to a deleted plan becomes plain text**: `[0009](../../.orchestra/plans/0009-easy-onboarding.md)` → `0009`, `[plan 0008](…)` → `plan 0008`, keeping the visible words and the sentence. A link to 0010 is repointed to its folder |
| D6 | **The two companion prompts of done plans go** (`git rm`): `prompts/engine-shim-companion.md` (plan 0005's companion), `prompts/move-to-orchestra-layout.md` (plan 0007's companion). **The review prompt stays, made generic**: `git mv prompts/review-plan-0001.md prompts/review-plan.md`, because it is the only statement of how a gate-2 review is done and reported (§6.2.11). `prompts/start-session.md` stays |
| D7 | **The plan template links from its folder.** A plan is now `plans/NNNN-x/plan.md`, one level deeper than the template, so both templates (this repo's and `engine/templates/project/`) link to `../../workflow.md`. This repo's template header becomes the engine template's (Preview `preview.html` in the folder, a `Repos:` line) |

## 5. File manifest

| # | File | Change | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Runs/Features/Board/BoardHandler.cs` | EDIT — D1 | 1 |
| 2 | `api/src/Orchestra.Api/Modules/Import/RunAssembler.cs` | EDIT — D2 | 1 |
| 3 | `api/tests/Orchestra.Api.Tests/Runs/BoardTests.cs`, `api/tests/Orchestra.Api.Tests/Import/RunAssemblerEventsTests.cs` | EDIT — §7 | 1 |
| 4 | `ui/src/components/layout/app-shell.tsx`, `ui/src/components/layout/app-sidebar.tsx` | EDIT — D3 | 1 |
| 4a | `.ai/ci.md` | EDIT — §6.1.4 | 1 |
| 5 | `.orchestra/plans/0001-skeleton-and-import.md` … `0009-easy-onboarding.md`, `0011-fewer-git-calls.md`, `0012-local-app-and-project-setup.md` (11 files) | DELETE — D4 | 2 |
| 6 | `.orchestra/previews/` (its one file, `0001-skeleton-and-import/index.html`) | DELETE — D4 | 2 |
| 7 | `.orchestra/plans/0010-gridify-tables.md` → `.orchestra/plans/0010-gridify-tables/plan.md` | MOVE + EDIT — §6.2.2 | 2 |
| 8 | `.orchestra/prompts/engine-shim-companion.md`, `move-to-orchestra-layout.md` | DELETE — D6 | 2 |
| 8a | `.orchestra/prompts/review-plan-0001.md` → `.orchestra/prompts/review-plan.md` | MOVE + EDIT — D6, §6.2.11 | 2 |
| 9 | `.orchestra/plans/README.md`, `.orchestra/workflow.md`, `.orchestra/plans/TEMPLATE.md`, `engine/templates/project/plans/TEMPLATE.md` | EDIT — §6.2.3–6.2.5 | 2 |
| 10 | `AGENTS.md`, `.ai/vision/roadmap.md`, `.ai/context/README.md` | EDIT — §6.2.6–6.2.8 | 2 |
| 11 | `.ai/architecture/data-model.md`, `.ai/architecture/overview.md`, `.ai/decisions/0007-…`, `0008-…`, `0009-…`, `.orchestra/ideas/desktop-window.md`, `hosting.md`, `mobile-view.md`, `.orchestra/prompts/start-session.md` | EDIT — D5, one line each | 2 |
| 12 | `.orchestra/README.md`, `engine/README.md`, `engine/templates/project/README.md`, `engine/docs/CONDUCTOR.md` | EDIT — the `previews/` folder 0013 retired, §6.2.10 | 2 |
| 13 | `.orchestra/ideas/plan-page-and-checklist.md`, `.orchestra/ideas/README.md`, the 0014 row of `plans/README.md` | already written by the planner — leave as is | — |

## 6. Per-file specification

### 6.1 Phase 1 — board and header

1. **`BoardHandler.ToCard`** (D1). Keep the existing `closedByEvent` logic but remember the event it found: the
   last `run.closed` whose index is after `created` (or any, when `created < 0`). After the `foreach` over
   events, and before `label` is computed:
   - if that event exists, `status = EventData.Read(it.DataJson).Text("status") ?? status`;
   - else if `closed` (through the run row), `status = run!.Status`;
   - pass `closed` to `WaitingFor` (new first parameter `bool closed`) and return `null` at its top when it is
     true.
   The `Reviewing` label rule after it is unchanged.
2. **`RunAssembler`** (D2): the line `run.PlanNumber ??= data.Text("planFamily");` is deleted. The three
   two-argument calls gain the task id, exactly as D2 writes them (on `main`: lines 214, 264 and 314; line 104
   already passes it).
3. **`app-shell.tsx`** (D3): the `<header>` class becomes
   `sticky top-0 z-30 flex h-14 items-center justify-between gap-2 border-b bg-background/95 px-3 backdrop-blur supports-[backdrop-filter]:bg-background/80`.
   `<HistoryButtons />` moves out of the right-hand `div` and is placed right after `<AppSidebarTrigger />`.
   The keyboard handler is unchanged. **`app-sidebar.tsx`**: the `<aside>` class gains `sticky top-0 h-svh
   overflow-y-auto`; nothing else in it changes.
4. **`.ai/ci.md`**: add a section above `## Baseline after plan 0012 …`, in the same shape:
   `## Baseline after plan 0014 — one plan shape in Orchestra, two board fixes, a header that stays (2026-11-25)` with the real `api` and `ui` counts
   from phase 1's gate output and the engine's `146` (the engine is not touched). Plan 0013 added no row; this
   one is the first after it.

### 6.2 Phase 2 — plans and rules

1. **Deletions** (D4, D6) — exactly:
   ```bash
   git rm .orchestra/plans/0001-skeleton-and-import.md .orchestra/plans/0002-library.md \
     .orchestra/plans/0003-new-project.md .orchestra/plans/0004-live-events.md .orchestra/plans/0005-engine-move.md \
     .orchestra/plans/0006-profiles.md .orchestra/plans/0007-move-projects-to-orchestra-layout.md \
     .orchestra/plans/0008-hosting.md .orchestra/plans/0009-easy-onboarding.md \
     .orchestra/plans/0011-fewer-git-calls.md .orchestra/plans/0012-local-app-and-project-setup.md
   git rm -r .orchestra/previews
   git rm .orchestra/prompts/engine-shim-companion.md .orchestra/prompts/move-to-orchestra-layout.md
   git mv .orchestra/prompts/review-plan-0001.md .orchestra/prompts/review-plan.md
   ```
2. **0010.** `git mv .orchestra/plans/0010-gridify-tables.md .orchestra/plans/0010-gridify-tables/plan.md`, then
   only these header and link edits:
   - `**Updated:** 2026-11-23` → `**Updated:** 2026-11-25`;
   - `**Run:** —` → ``**Run:** `0010-gridify-tables` ``;
   - the `**Base:**` line → ``**Base:** `main` after plan 0014 is merged — the implementer records the hash``;
   - `**Branch:**` line: unchanged;
   - every `../workflow.md` → `../../workflow.md` (the link moved one level down).
   Nothing in its body changes. After the move, every relative link in it must resolve (check in §9).
3. **`plans/README.md`.**
   - The first bullet becomes: "One plan is one folder: `NNNN-<topic>/plan.md`, with its page at
     `NNNN-<topic>/preview.html` and its evidence at `NNNN-<topic>/evidence/` (git-ignored). The number is
     reserved when the folder is made ([ADR 0010](../../.ai/decisions/0010-plan-is-a-folder.md)). The flat plans
     0001–0012 were deleted on 2026-11-25 (0010 moved into its folder); git keeps them:
     `git show 361658b:.orchestra/plans/<file>`." (That a plan stays after `Done` is already the next bullet's
     rule, "stays where it is"; it is not repeated here.)
   - The table keeps only three rows: 0010 (link `0010-gridify-tables/plan.md`, Updated 2026-11-25), 0013
     (unchanged) and 0014 (the planner added it; set its Status to the plan's own).
4. **`workflow.md`** gate 1 step 3: delete the sentence `Plans 0001-0012 keep their flat file name (plan 0013 D16).`
   Nothing else.
5. **Templates** (D7).
   - `.orchestra/plans/TEMPLATE.md`: lines 3–14 (the header) become exactly lines 3–15 of
     `engine/templates/project/plans/TEMPLATE.md`, except the Branch line keeps `` `alireza/<topic>` ``.
   - Both templates: every `../workflow.md` → `../../workflow.md`.
   - Plans already written (0010, 0013, this one) are **not** given a `**Repos:**` line: a single-repo project
     may omit it (plan 0013 D3).
6. **`AGENTS.md`** — the `**Status**` row's second cell becomes exactly:
   `Phases 0–2 done. Plans before 0013 were flat files and were deleted on 2026-11-25 (0010 moved into its folder); git keeps them. Open and recent plans with their state: [`.orchestra/plans/README.md`](.orchestra/plans/README.md); phases: [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md)`
7. **`.ai/vision/roadmap.md`.**
   - D5 on every link to a deleted plan (eight lines: 14–17, 21, 31–33 on `main`, and any other the grep in §9
     finds); the 0010 link → `../../.orchestra/plans/0010-gridify-tables/plan.md`.
   - Append one section at the end:
     ```markdown
     ## Later — Project B on plan folders

     Owner, 2026-11-25: after Orchestra and Project A run on plan folders, Project B moves the same way —
     `NNNN-<topic>/` folders numbered per workspace, the three skills, `plansDir` without `{repo}`. Its numbers
     are per repo today and collide under one numbering, so its open plans are renumbered then. Nothing is
     done there before.
     ```
8. **`.ai/context/README.md`** — append one paragraph:
   "Session logs written before 2026-11-25 link to flat plans (`.orchestra/plans/00NN-<topic>.md`) and
   `.orchestra/previews/`, deleted that day. The logs are not rewritten; read those files from git:
   `git show 361658b:<path>`."
9. **One line each** (D5): `.ai/architecture/data-model.md:110`, `.ai/architecture/overview.md:58`,
   `.ai/decisions/0007-profiles-exported-to-a-file.md:35`, `.ai/decisions/0008-host-paths-and-own-report-token.md:7`,
   `.ai/decisions/0009-local-app-sets-projects-up.md:46`, `.orchestra/ideas/desktop-window.md:3`,
   `.orchestra/ideas/hosting.md:3`, `.orchestra/ideas/mobile-view.md:8`. And in
   `.orchestra/prompts/start-session.md` line 14, inside the Persian block, only the path
   `.orchestra/plans/0001-skeleton-and-import.md` → `.orchestra/plans/README.md`.
10. **The retired `previews/` folder** (missed by plan 0013; plan folders hold the preview since then):
    - `.orchestra/README.md`: delete the `previews/` row; the `plans/` row becomes
      ``| [`plans/`](plans/README.md) | `NNNN-<topic>/`: `plan.md`, its `preview.html`, and its git-ignored `evidence/` | yes |``;
      the `evidence/` row becomes ``| `plans/NNNN-<topic>/evidence/` | `phase-<X>/`: an agent's screenshots and check reports | **no** |``;
      add after the `prompts/` row ``| [`skills/`](skills/) | `<name>/SKILL.md`: orchestrate, close-plan, pick-plan (pointers in `.claude/skills/`) | yes |``.
    - `engine/templates/project/README.md`: delete the `previews/` row and the `evidence/` row; the `plans/` row
      becomes
      ``| `plans/` | One folder per plan, `NNNN-topic/`: `plan.md` with its status and dates, its `preview.html`, its git-ignored `evidence/`. `TEMPLATE.md` is the shape. |``;
      add after the `roles/` row
      ``| `skills/` | One folder per skill, `<name>/SKILL.md`: orchestrate, close-plan, pick-plan. Setup also writes a three-line pointer to each in the project's `.claude/skills/<name>/SKILL.md`, from the template's `claude-skills/` folder. |``.
    - `engine/README.md`, the tree: `plans/<Repo>/` line → `plans/NNNN-<topic>/   ← one folder per plan (plan.md, preview.html, evidence/); README.md and TEMPLATE.md at the root`;
      `previews/  ideas/  research/  evidence/` → `ideas/  research/`; the `CLOSE-PLAN.md` line →
      `skills/<name>/SKILL.md     ← orchestrate, close-plan, pick-plan (pointers in .claude/skills/)`.
    - `engine/docs/CONDUCTOR.md` line 84: `copy it to .orchestra/previews/NNNN-topic/index.html — the dashboard
      reads previews from there` → `copy it to .orchestra/plans/NNNN-topic/preview.html — the dashboard reads
      previews from there`. Nothing else in that file.
11. **`prompts/review-plan.md`** (D6), after the `git mv`. Only these edits; the checks, the report shape and
    the rules stay word for word:
    - title: `# Review a plan (gate 2)`;
    - first paragraph: `(plan 0001 says `Claude Opus 5.5`)` → `(the plan's **Planned by:** line names it)`;
    - delete the "Why: plan 0002 is reviewed and waiting…" paragraph;
    - in the text block, `.orchestra/plans/0001-skeleton-and-import.md` (twice) → `<the plan's path>`, and its
      read list's last item becomes `6. <the plan's path> — the whole file, every section, not excerpts`;
    - the "Then only the docs you need" paragraph: replace
      `.orchestra/previews/0001-skeleton-and-import/index.html, and plans 0002-0008 only where 0001 states
      something about them` with `the plan's own preview.html, and other plans only where this one states
      something about them`;
    - delete the paragraph that starts "This repository has no code yet";
    - every other "plan 0001" / "0001" in the block → "the plan".
    Then add one line to `workflow.md` gate 2, after its first paragraph: `A reviewer outside the orchestrator
    starts from [`prompts/review-plan.md`](prompts/review-plan.md).`

## 7. Tests

- **Board (`BoardTests`):**
  - a run with `run.created`, then `status` → `AwaitingApproval`, then `run.closed` with `{"status":"Verified"}`:
    one card, lane `done`, status `Verified`, `WaitingFor` null;
  - the same events without `run.closed`, plus a run row with `Status = "Verified"` and a `ClosedAt`, created
    after the first event: lane `done`, status `Verified`, `WaitingFor` null;
  - a closed run whose last status is `AwaitingApproval` and whose `run.closed` has no `status`: lane `done`,
    `WaitingFor` null;
  - the existing open-run test (`AwaitingApproval`, not closed) still says `approval`.
- **Import (`RunAssemblerEventsTests`):**
  - a `run.created` with `planFamily = "claude-opus"` and task `Plan 0010: Gridify tables` → `PlanNumber`
    `0010`, `PlanTitle` `Gridify tables`;
  - a `run.created` with `planFamily` and task id `0010-gridify-tables`, no plan text → `PlanNumber` `0010`;
  - a `run.created` with `planFamily` and an old id (`T-WEB-007`), no plan text → `PlanNumber` null.
- **UI:** none new. There is no test for the header or the sidebar today (`components/layout/` has only
  `locale-switcher.test.tsx`); D3 is covered by the build and the owner's check in the browser.

## 8. Stop and ask

- An existing test must change to pass (other than one that asserted the plan family as a number).
- A link to a deleted plan in a file not named in §6.2 (outside `.ai/context/`, plan 0013 and test fixtures).
- A relative link that resolves on `main` stops resolving after phase 2, other than in the files §2 leaves alone.

## 9. Phases

| Phase | Files (§5) | Gate |
|---|---|---|
| 1 Board and header | 1–4a | api gates + ui gates |
| 2 Plans and rules | 5–12 | all gates; then the stale-path grep and the link check below |

**Phase 2's two checks.**

1. Stale paths — this must print nothing:

   ```bash
   grep -rnE 'plans/00(0[0-9]|1[12])-[a-z-]+\.md|previews/0001|\]\(\.?\.?/?previews/|\.orchestra/previews/|review-plan-0001|engine-shim-companion|move-to-orchestra-layout' --include=*.md . \
     | grep -vE '\.ai/context/|plans/0013-|plans/0014-|Fixtures/|node_modules|dist/|engine/docs/history/|\.ai/decisions/0010-plan-is-a-folder\.md'
   ```

   Before phase 2 (checked by the planner on this branch, 2026-11-25) the same command prints 40 lines in 22
   files; that proves it can fail. The excluded
   files are history that stays as written: session logs, plans 0013 and 0014, test fixtures, the engine's design
   history, and ADR 0010, which describes the old three-path layout as the reason for the change.
2. Every relative Markdown link in `.orchestra/`, `.ai/` (except `context/`), `engine/` (except `docs/history/`)
   and `AGENTS.md` resolves — one script pass; report the count. **Exempt by name:** the two `TEMPLATE.md`
   files, whose `../../workflow.md` is written for the copy at `plans/NNNN-x/plan.md`, not for the template's own
   place.

**Owner checks after phase 1:** the header item of the checklist, in the browser build (`npm run dev`). The board
items need a run, so they wait for 0010. Stop after every phase — show the owner the diff.

## 10. Definition of done

- [x] All gates green; test counts ≥ baseline plus the new tests: api 313 (0 warnings), ui 85, engine 146/146 — run by the closing session, 2026-11-25.
- [x] `plans/` holds `README.md`, `TEMPLATE.md`, `0010-gridify-tables/`, `0013-workspace-plans/`,
      `0014-tidy-plans/` and nothing else; `.orchestra/previews/` is gone.
- [ ] The owner's checklist passed (the board items after 0010's first run).
- [ ] `Status: Done`, the `Done:` date, the commit and the merge in the header; the plans README row.
