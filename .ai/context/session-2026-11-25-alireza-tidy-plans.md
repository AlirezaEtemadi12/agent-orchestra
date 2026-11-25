# Session 2026-11-25 — plan 0014 (one plan shape, two board fixes, a header that stays), both phases

## Repository state

- Branch `alireza/tidy-plans`, made by the planner from `main` @ `361658b`.
- The whole plan is **one uncommitted tree**, as plan 0014 §0 asks: the owner commits and merges. It
  carries the planner's own files too (`.orchestra/plans/0014-tidy-plans/`, `ideas/plan-page-and-checklist.md`
  and its row, the 0014 row of `plans/README.md`).
- The plan itself was reviewed at gate 2 in this session before any edit: v1 changes requested (1 blocker,
  4 important, 4 minor), v2 approve with 3 minor wording points, v3 applies all three — approved.

## Phase 1 — board and header

- `BoardHandler.ToCard` (D1): the `run.closed` event is kept, not only the fact that one exists; on a closed
  card the status is that event's `status`, else the run row's, and `WaitingFor(closed, …)` answers `null`
  at its top. `LaneOf` is unchanged.
- `RunAssembler` (D2): `run.PlanNumber ??= data.Text("planFamily")` deleted from the `RunCreated` case; the
  three two-argument `PlanName.From` calls (live task, archive, events) now pass the task id, so a run is
  named after its plan even with no plan text.
- Tests: three in `BoardTests` (close-event status, close through the run row, closed but still
  `AwaitingApproval`), three in `RunAssemblerEventsTests` (number from the task, from the task id, none for
  `T-WEB-007`).
- `app-shell.tsx`: `<HistoryButtons />` moved into the left group after `AppSidebarTrigger`; the header is
  `sticky top-0 z-30 … bg-background/95 backdrop-blur`. `app-sidebar.tsx`: the `<aside>` is
  `sticky top-0 h-svh overflow-y-auto`.
- `.ai/ci.md`: a “Baseline after plan 0014” section above plan 0012's (api 313, ui 85, engine 146 untouched).

## Phase 2 — plans and rules

- `git rm` of the eleven flat plans (0001–0009, 0011, 0012), of `.orchestra/previews/` and of the two
  companion prompts; `git mv` of `prompts/review-plan-0001.md` → `prompts/review-plan.md` and of
  0010 into `0010-gridify-tables/plan.md` (Updated 2026-11-25, `Run`, new `Base`, `../../workflow.md`).
- `plans/README.md`: the first bullet now describes folders only and names the deletion (no repeated
  “stays where it is” rule); the table keeps 0010, 0013, 0014.
- `workflow.md`: the “Plans 0001-0012 keep their flat file name” sentence is gone; gate 2 names
  `prompts/review-plan.md`.
- Both `plans/TEMPLATE.md` files link to `../../workflow.md`; this repo's header is now the engine's
  (Preview `preview.html`, a `**Repos:**` line) with `alireza/<topic>` kept.
- `AGENTS.md` Status row is one sentence with no flat-plan links; `roadmap.md` has the eight links as plain
  text (0010 repointed) and a new “Later — Project B on plan folders” section; `.ai/context/README.md`
  explains how to read old logs.
- One line each: `data-model.md`, `overview.md`, ADRs 0007/0008/0009, the three ideas, `start-session.md`
  (path only, no Persian word changed).
- The retired `previews/` folder: `.orchestra/README.md` (plans row, evidence row, a `skills/` row, the
  `previews/` row gone), `engine/templates/project/README.md` (same, `skills/` row), `engine/README.md`
  tree, `CONDUCTOR.md` line 84 (`.orchestra/plans/NNNN-topic/preview.html`).
- `prompts/review-plan.md` made generic: title, “the plan's **Planned by:** line names it”, the 0002
  paragraph and the “no code yet” paragraph deleted, `<the plan's path>` twice, the read list and the
  preview sentence generic, “plan 0001” → “the plan”.

## Verified

- `dotnet build api/Orchestra.slnx -warnaserror`: 0 warnings, 0 errors.
- `dotnet test api/Orchestra.slnx`: **313 passed** (307 + 6 new), 0 failed — again after phase 2.
- `ui`: `npm run typecheck` clean, `npm run lint` clean, `npm test` **85 passed**, `npm run build` static
  export succeeded; `npm test` again after phase 2, 85.
- Phase-2 check 1: the §9 stale-path grep prints nothing (before phase 2 it printed 40 lines in 22 files).
- Phase-2 check 2: a script pass over `.orchestra/`, `.ai/` (not `context/`), `engine/` (not
  `docs/history/`) and `AGENTS.md` — 104 files, 185 links. Seven “failures”, all accounted for: four are
  links quoted inside plan 0014's own spec text (they resolve in the file they describe), three were
  already broken on `main` (below).

## Not verified

- The owner's checklist in the browser: the sticky header and the two buttons at the start of it (both
  languages, both themes), and the board items, which need a real run (0010's first).
- The engine offline suite was not re-run: phase 2 touched only `engine/README.md` and
  `engine/docs/CONDUCTOR.md`, both prose.

## Open

- Plan 0014's own header still says `Status: Draft` (the plan row 13 says to leave the plan as the planner
  wrote it); `Done` and the commit/merge are the owner's close step.
- Three relative links were already broken on `main` and are left for a later tidy-up, since neither this
  plan nor its manifest names them: `engine/README.md` lines 90 and 94 use `../../` where `../` is right
  (`../../README.md`, `../../.ai/decisions/0002-…`), and
  `engine/templates/project/knowledge/AGENTS.md:16` links `.orchestra/workflow.md`, which is written for a
  copied project, not for the template's own folder.
