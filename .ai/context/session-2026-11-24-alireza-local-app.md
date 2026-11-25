# Session 2026-11-24 — plan 0012 (a local app that sets a project up), all six phases

## Repository state

- Branch `alireza/local-app`, created from `bdc8840` (the plan's own commit on `main`).
- The whole plan is **one uncommitted tree**, as plan 0012 §9 asks: the owner commits and merges.
- `deploy/` is gone; the data it held was copied to `%LOCALAPPDATA%\Orchestra\` by the owner's step §6.1
  (`orchestra.db`, `projects.json`, `report-token`, `profiles\`).

## Phase A — decisions, data, deploy

- ADR [0009](../decisions/0009-local-app-sets-projects-up.md) (new); 0001, 0003, 0005 and 0008 amended;
  `decisions/README.md`, `vision/roadmap.md` (phase 3 removed, 0012 in the order), `AGENTS.md`,
  `plans/README.md`; plan 0008 `Dropped`.
- `deploy/` deleted (11 files) after the data move.

## Phase B — templates and API

- `engine/templates/project/**` (17 files): the shim, `workflow.md`, the plans/ideas READMEs and template,
  four prompts, four role sections, and the two knowledge files.
- No hosting: `PathMapper`, `HostingInfo`, `PathMap`/`PublicUrl`, `GET api/system/hosting`, the connect
  endpoint, `ScaffoldPromptHandler` and the `NotVisible` error codes are deleted; `LocalPaths` keeps
  `LooksAbsolute`/`Normalize`/`Covers` for the folders route and the setup guard.
- `DatabasePath` empty by default → `%LOCALAPPDATA%\Orchestra\orchestra.db`; `Orchestra:EnginePath` added;
  `AllowedHosts` is the three loopback names.
- The setup feature: `SetupPlanner`, `SetupApplier`, `SetupGuard`, `EngineLocator`, `TemplateSet`,
  `VerifyCommandDetector`, `SetupDtos`, and `POST api/projects/setup/plan|apply`.
- Tests: deleted `PathMapperTests`, `ConnectHandlerTests`, `HostingHandlerTests`; added `LocalPathsTests`,
  `SetupPlannerTests`, `SetupApplierTests`, `SetupGuardTests`, `VerifyCommandDetectorTests`,
  `EngineLocatorTests`.

## Phase C — the engine's new offline case

- `engine/tests/offline/run-offline.ps1` gained `T-SU1..T-SU4`: per shape (single repo, two repos) a temp
  workspace whose `.orchestra/` is the template, the §6.3.4 config, a profile folder and a local json;
  `run.ps1 agents` exits 0, and `run.ps1 new … -DryRun` writes a planner prompt holding the template's
  workflow text and its role section. The artifact is `runs/<id>/artifacts/preview.plan.prompt.md` — the
  `.md` suffix is what the first attempt missed.
- `engine/examples/orchestrator.json` and `.ai/ci.md` updated.

## Phase D — the screens

- `setup-step.tsx` and `setup-result.tsx` (new), the route `settings/projects/setup`, `project-form.tsx`
  (no hosting line, no scaffold box, "Continue" goes to setup), `projects-table.tsx` ("Check setup"),
  `api.ts`/`queries.ts`/`types.ts`/`index.ts`/i18n/manifest.
- Deleted: `connect-page.tsx`, `hosting-line.tsx`, `connect.test.tsx`, the connect route,
  `lib/hosting/queries.ts`. `start/**` is two steps (D16).
- `setup.test.tsx` (new) and `projects.test.tsx` ("Check setup" on every row with a folder).

## Phase E — the window and publish

- `Extensions/OrchestraHost.cs` (new) builds the host; `Program.cs` runs it.
- `api/src/Orchestra.Desktop` (new): health check (2 s), the host on the exe's folder, a Photino window
  1280×860 on `http://localhost:3500`, the `pickFolder` message answered from `ShowOpenFolder`, and a 5 s
  stop after the window closes. Photino.NET 3.2.3 in `Directory.Packages.props`.
- `ui/src/lib/desktop/bridge.ts` + `bridge.test.ts`; the form's Browse uses it when the window exists.
- `scripts/publish.ps1`; `dist/` in `.gitignore`; `OrchestraHostTests`.

## Verified

- `dotnet build api/Orchestra.slnx`: 0 warnings, 0 errors.
- `dotnet test api/Orchestra.slnx`: **300 passed**, 0 failed.
- `ui`: `npm run typecheck` clean, `npm run lint` clean, `npm test` **83 passed**, `npm run build` static
  export of 34 pages including `settings/projects/setup`.
- Engine offline suite (`number` config): **146/146 checks**, 289 s, `all configs passed`, exit 0.
- `scripts/publish.ps1`: see *Open* — the run and its probe are reported in the chat, not here.

## Not verified

- The owner's browser and window checks (both languages, both themes, the Windows folder dialog, two
  projects reporting events from the local app).
- The published exe's behaviour on a machine without .NET: the publish is `--self-contained`, but only
  this machine ran it.

## Open

- The commit and the merge are the owner's.
- A `.gitignore` file exists in `.orchestra/` now; the engine's templates carry their own.

## Verification (after the hand-over)

- Gates again: api 300 passed, 0 warnings (`--no-incremental`); ui 83 passed, typecheck/lint/build clean;
  engine 146/146, 326 s.
- Published exe: health, HTML, one instance, Origin 403, refused roots, closing stops the app.
- **Bug found and fixed:** `EngineLocator.TemplatesRoot` left out `engine/`, so the real app planned no
  template rows and answered `engineFound: false`; the unit tests built `TemplateSet` from a fixed folder
  and `EngineLocatorTests` asserted the wrong path. After the fix, setup through the exe works for a single
  repo and for a two-repo workspace, and a `-DryRun` event reaches the dashboard.
- Minor, not fixed: an empty or malformed body to `setup/apply` answers 400 (model validation) before the
  Origin 403; nothing is written either way. The health check counts any HTTP answer on 3500 as
  "already open".
- Owner's "Check setup" on Project A found three more bugs, fixed the same day (see plan 0012's header):
  commands sent as text, not lists (400 on every re-plan and apply); `.orchestra` taken for a repo;
  the runs board's stale "Live events are off" banner (reporter field removed). api 300, ui 84.
- Open: for a project whose config uses per-repo prompts and roles (`{repo}` in `promptsPath`/`rolesPath`,
  as Project A), setup still plans the flat `prompts/*.md` and `roles/README.md`, which that config never
  reads.

## Plan 0013 written (draft)

- The owner wants the workspace first: one plan per change across repos, one folder per plan, a board by
  plan (`NNNN · title`), clearer stages, implicit approval by their own "implement", a close that asks
  nothing, a pick-plan skill, and back/reload buttons in the desktop app. Written as
  [plan 0013](../../.orchestra/plans/0013-workspace-plans/plan.md) with a preview; not reviewed yet.
- Board bugs seen on Project A and planned in 0013 D12: events are grouped by branch, so a run's pre-branch
  events form a second card; an old closed run with a reused id pulls a new run into Done.
- Project A: its 166 done plans were removed with `git rm` in its `.orchestra/` repo (staged, not committed);
  five open plans and the two READMEs stay. The open per-repo prompts item above is 0013 D15.
