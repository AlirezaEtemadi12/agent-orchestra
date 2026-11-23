# Session 2026-11-22 — plan 0007 phase A (engine and dashboard read `.orchestra/`)

**Repository state:** branch `alireza/orchestra-layout` from `main` (`9360e26`); nothing committed,
nothing pushed. Phases B and C are companions in the two project workspaces and were **not** run.

## What changed

- **`engine/run.ps1` — `close`** (plan 0007 D3). A project that sets a top-level `plansDir` (D2's
  `{repo}` token allowed) now keeps every plan of a repo in one folder for its whole life: `close`
  collects every match (number; or, with `planFiling: slug`, the exact `<slug>.md`, `*-<slug>.md` and
  any file naming the branch), then keeps only the ones whose header says `Done`, so a run's own open
  plan never hides a filed copy. `Test-PlanDone` reads the first 15 lines and accepts `**Status:** Done`,
  `Status: **Done**` and an unbolded `Status: Done`. Two messages, one per case: "is in <dir> but its
  Status is not Done yet" and "is not in <dir>. Write it there first (gate 1)". A run whose `task.json`
  has no `repo` stops on its own line. Without `plansDir` the old `plansDoneDir` code runs unchanged.
- **`engine/run.ps1` — help and footer.** `close` says "filed (Status: Done in the project's plansDir,
  or in <repo>/<plansDoneDir>)", and the footer points at `engine/docs/CONDUCTOR.md` and
  `engine/docs/WORKING-MODEL.md` instead of the stale `orchestrator/` copies.
- **`engine/lib/agents.ps1`** — the worker guard names both `orchestrator/run.ps1` and
  `.orchestra/run.ps1`.
- **`engine/tests/offline/run-offline.ps1`** — T-PD1–T-PD6 (both filing modes, the open-plan-vs-Done
  case, the unchanged done/ path, "no plan", "no repo in task.json").
- **`engine/README.md`, `engine/examples/orchestrator.json`, `engine/docs/CONDUCTOR.md`** — the layout
  drawn as `.orchestra/` (old name noted), `plansDir` documented, the example config carries D2's paths
  and `plansDir`, and the conductor procedure says where gate 1 writes a plan and a preview and that
  closing a plan is its `Status: Done` line.
- **`api` — `EffectiveConfigHandler`** (D4a): reads `<root>/.orchestra/orchestrator.json` and falls back
  to `<root>/orchestrator/orchestrator.json`, and `EffectiveConfigDto` gained `configPath`, the
  root-relative file it read. Two tests (new layout wins over the old one; the old path is reported), and
  the 422 detail now names both paths.
- **`api` — `LibraryDefaults.Previews`** (D4b): `.orchestra/runs/_drafts/*.html` is indexed, so an
  in-flight preview stays visible after a project drops its own globs. One indexer test.
- **`ui`** — `EffectiveConfigDto.configPath` and the effective page's copy prompt use it instead of a
  hardcoded `orchestrator/orchestrator.json`.
- **`.orchestra/prompts/move-to-orchestra-layout.md`** — the companion brief (§6.2), with no project
  path, run id or plan number.

## Verified

- `dotnet build Orchestra.slnx`: 0 warnings, 0 errors (with `-p:ArtifactsPath=artifacts/agent`, because
  the owner's `Orchestra.Api` process holds `bin/Debug`).
- `dotnet test Orchestra.slnx`: **248** passed (was 246).
- `ui`: `npm run typecheck`, `npm run lint`, `npm test` (**64** passed, 13 files), `npm run build`.
- Offline suite: **139/139** on `number` (`.orchestra/evidence/0007-orchestra-layout/suite-number.log`) and
  **139/139** on `slug` (`.orchestra/evidence/0007-orchestra-layout/suite-slug.log`), T-PD1–T-PD6 listed.
- The Status matcher was smoke-tested on its own: `**Status:** Done — x`, `Status: **Done**`,
  `Status: Done` and a Done line on line 3 all accept; `Running` and no Status line refuse.

## Open

- **Two suite runs found two defects in the new tests, not in the engine.** (1) An em dash inside a
  PowerShell string literal split the argument list: Windows PowerShell 5.1 reads a script without a BOM
  in the ANSI code page, so non-ASCII belongs in comments and docs, never in a string the suite passes on.
  (2) T-PD1/T-PD2/T-PD4 assumed number filing and failed under the `slug` config, whose `planFiling` is
  `slug` (its slug is everything after the first `/`, so `alireza/plan-0042-done` is a slug, not number
  0042). Each case now sets `planFiling` explicitly instead of inheriting the config's style.
  Both suites are green on the final code (139/139 each).
- **`.orchestra/plans/0007-move-projects-to-orchestra-layout.md`'s Persian is double-encoded on disk**
  (it was before this session — `git show HEAD:…` has the same mojibake). Only the `Status:` line was
  changed here, byte for byte otherwise. It needs the owner's original file back.
- **Phases B and C are the owner's:** rename each workspace's `orchestrator/` to `.orchestra/`, run the
  brief, then switch that project's `RunsPath` to `<root>/.orchestra/runs` and remove its `Library` globs.
  Until then nothing changes for a project: the engine reads the old layout through its own config.
