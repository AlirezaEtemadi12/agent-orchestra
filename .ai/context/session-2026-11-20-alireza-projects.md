# Session 2026-11-20 — plan 0003 New project, phases A–C

**Repository state:** branch `alireza/skeleton-and-import` @ `a0235f4` (plan 0002 merged as
`861dd2a`). The plan file and the work are **uncommitted**: the owner has not asked for a commit or a
branch cut, so nothing is staged, committed or pushed. `alireza/new-project` does not exist yet.

## What changed

Plan: [`../plans/0003-new-project.md`](../../.orchestra/plans/0003-new-project.md) §5 files 1–20.

### Phase A — store and CRUD

- `Modules/Projects/ProjectStore.cs` — `projects.json` beside the database (`<database folder>`),
  schema 1, atomic write (temp file, then `File.Move`), an unparsable or unknown-schema file is
  reported and never overwritten; `StoredProject.ToOptions()`.
- `ProjectOrigin.cs` (`Config` · `Dashboard`), `ProjectRegistry` rewritten: one immutable snapshot
  (config first, then store entries whose id is not taken), `OriginOf`, and `Create/Update/Remove`
  that write the store and only then swap the snapshot with `Volatile.Write` under a lock.
- `ProjectInput` + `ProjectValidation` (id, name, absolute root, runs path, 0–20 repos, profile
  `default`) and `Features/{CreateProject,UpdateProject,DeleteProject}`; create/update save, then
  `ImportService.ImportOneAsync` (waits for the gate), delete waits for the gate, deletes
  `ImportLog`s (their errors by cascade) and the `Project` row (runs and library by cascade), then
  removes the entry.
- `ImportService` — `ImportOneAsync` and the public `EnterAsync`; `TryEnter`/`Leave` unchanged.
- `ProjectOptions.Repos`; `ListProjects` DTO gains `origin`, `rootPath`, `runsPath`, `repos`.
- Errors `Projects.Project.IdTaken`, `.ReadOnly`, `.Invalid`, `Projects.Store.Unreadable` (en + fa).
- `.ai/architecture/data-model.md` — the `Project` row, the import paragraph and a new "The
  dashboard's own data" section; `.ai/api/conventions.md` — atomic writes and `Library 60`.

### Phase B — folder check and prompt

- `Features/CheckFolder/{FolderCheck,CheckFolderHandler}` — existence only, the nine known items in
  order, repos from the root `.git` or direct subfolders (max 20, sorted), suggested runs path
  (`orchestrator/runs` when it exists, else `.orchestra/runs`), per-repo `.ai/`.
- `Features/ScaffoldPrompt/{ScaffoldPromptBuilder,ScaffoldPromptHandler}` — the §6.4 text verbatim,
  sections included by condition, `project.json` embedded, the one line when nothing is missing.

### Phase C — the screens

- `ui/package.json` — exact `react-hook-form` 7.88.0, `@hookform/resolvers` 5.9.1, `zod` 4.6.5.
- `lib/api/client.ts` — `put` and `delete`; a 204 or an empty body resolves to `undefined`.
- `lib/projects/queries.ts` — the one shared `ProjectDto` gains `origin`, `rootPath`, `runsPath`,
  `repos`; `features/projects` defines no second one.
- `features/projects/**` — manifest, api, queries, fa + en catalogues, the list (origin badge, last
  import, remove only for dashboard projects, unreadable-store alert), the create/edit form
  (React Hook Form + Zod from `t`, id built from the name until typed, check folder, check result
  badges, suggested runs path filling only an empty field, the prompt with Copy and Check again,
  `IdTaken` on the id field), the remove dialog.
- Routes `/[locale]/settings/projects/` and `…/edit/?id=`; `FEATURE_KEYS` and the registry gain
  `projects`; `MenuCategory` and the sidebar gain `settings` last; `common.menu.settings`;
  `common.states.noProjects` reworded.

## Verified

- `dotnet build Orchestra.slnx` — 0 warnings, 0 errors.
- `dotnet test Orchestra.slnx` — **176/176 passed**, 0 failed, 0 skipped (147 → 176).
- `npm run typecheck`, `npm run lint`, `npm run test` (**49/49**, was 35), `npm run build` — clean;
  the export lists `/fa|en/settings/projects` and `/fa|en/settings/projects/edit`.
- Gates ran with `-p:ArtifactsPath="<repo>/artifacts/agent"`; the owner's runtime was not touched.

## Not verified

- The owner checks of §9 (both real roots through `POST api/projects/check`, the Project B prompt,
  and the screens in fa + en and both themes). The agent did not start the app.
- `ProjectDto` carries `runsPath` as well as the plan's `origin`, `rootPath`, `repos`: the edit form
  and the table's "runs" column need it and the plan's DTO row did not list it.

## Open

- The plan's phase gates said "Stop" after each phase; the owner asked for all three in one pass, so
  they ran in order without stopping.
- A dashboard project whose import is running at delete time waits for the gate (D11); nothing has
  exercised that on a real import yet.

## Review and close (2026-11-20, Claude Opus 5.5)

- Re-ran every gate: api build 0/0, **176/176**; ui typecheck, lint, **49/49**, build (the four
  `settings/projects` routes prerendered).
- One fix: `DeleteProjectHandler` released the import gate **before** removing the entry from the
  registry, so a full import starting in that gap rebuilt the deleted rows and left them orphaned.
  The entry is now removed inside the gate. Tests still 176/176.
- Accepted deviation: `ProjectDto` also carries `runsPath` (the form and the table need it).
- Still the owner's: the browser checks of «نتیجه» and phase B's checks on the two real roots.
- Closed: commit `f7251e6` on `alireza/new-project`, merge `5774f87` into `alireza/skeleton-and-import`.
- Next: plan 0004 (live events, phase 1) — its §8 is open, so it needs the owner's answers and a review before gate 2.
