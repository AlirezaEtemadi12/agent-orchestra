# Session 2026-11-19 — plan 0002 Library, phases A–C

**Repository state:** branch `alireza/library`, cut from `alireza/skeleton-and-import` @ `7a09d6e`
(plan 0001 is committed there but not merged — there is no `main` in this clone yet). All three
phases are **committed** as `1022f5e` on `alireza/library` and **merged** into
`alireza/skeleton-and-import` as `861dd2a` (`--no-ff`); the current branch is the base, tree clean.
`alireza/library` still exists — deleting it is the owner's third close question. Nothing pushed.

## What changed

Plan: [`../plans/0002-library.md`](../../.orchestra/plans/0002-library.md) §5 files 1–23.

### Phase A — configuration and indexing

- `Common/OrchestraOptions.cs` — `ProjectOptions.RootPath` (optional), `LibraryOptions` (five glob
  lists), `ResolveRootPath()` and `DeriveRootPath()`. A root is derived **only** from
  `<x>/orchestrator/runs` or `<x>/.orchestra/runs`; `/projects/p1` (the Docker mount) and
  `<root>/runs` derive nothing, which is the guard that keeps the library from reading `/`.
- `Modules/Library/LibraryItemEntity.cs`, `LibraryCategory.cs`, `LibraryDefaults.cs` — metadata only
  (D2) and the optional `.orchestra/` layout (plan 0007 edits `LibraryDefaults` alone).
- `Modules/Library/Indexing/` — `GlobMatcher` (`*` one segment, `**` any depth, README/TEMPLATE never
  items, the first whole-segment `*` is the repo, a matched `done` never is),
  `SafePath` (text under the root **and** no symlink/junction below it), `PlanHeaderParser` (the three
  real header styles, tolerant field regexes, preview resolution with a folder → `index.html`),
  `IdeaHeaderParser` (the one-line `·` header), `MarkdownTitle`, `PlanRunLinker` (D5) and
  `LibraryIndexer` (per-glob prefix walk, skips `.git`/`node_modules`/`bin`/`obj`/`.next`/`out`/
  `graphify-out`, 5000-item cap, evidence extensions and groups, preview only when indexed).
- `Data/Configurations/LibraryItemConfiguration.cs`, `OrchestraDbContext.LibraryItems`,
  `ImportLogEntity.LibraryItems` and the single migration `Library`.
- `ImportService` rebuilds the library inside the same `SaveChanges` as the project's runs; its errors
  join the import log; `ImportResultDto`/`ImportLogDto` carry `libraryItems`.
- `appsettings.Local.example.json`, `deploy/.env.example`, `deploy/docker-compose.yml` — the optional
  `RootPath`, `Library` globs for today's layout, and the commented read-only root mount.
- `ui/src/features/imports/**` — the fifth KPI and the table column, `imports.libraryItems` in fa + en.

### Phase B — the API

- `Modules/Library/Features/ListLibrary/` — `LibraryDtos` (item, run, detail, `previewUrl`) and
  `ListLibraryHandler`: every project or one, category, a status filter that only plans and ideas
  obey, every sort key of §6.3 (nulls last in both directions, `run` by task id then `n` as a number).
- `Features/GetLibraryItem/` — the item, `previewUrl` only when the preview was indexed, every D5 run
  newest first, the plan's evidence (group prefixed by the plan's number or slug).
- `Features/ReadLibraryFile/` — rules 1–5 of §6.4: indexed item only, `SafePath` again at request
  time, 20 MB → 413, the exact sandbox CSP for `.html`, `default-src 'none'` for markdown and images,
  octet-stream + attachment for anything else.
- `Features/PlanPrompt/` — the mock's text filled server-side (§6.5); `previewFolder` strips
  `index.html` and keeps the slash, null → `none`.
- `LibraryController` + `LibraryModuleInstaller` (Order 60); `GET api/library`, `…/{project}/{category}/item`,
  `…/{project}/{category}/file`, `…/{project}/plan/prompt`.
- `RunDetailDto.plan` (`{ relPath, number, title, status } | null`): the plan whose `RunKey` is the
  run, else a number match before a slug match, then the lowest path — the same answer every call.
- `Error.cs` / `ErrorMessages.cs` — `Library.Item.NotFound` (404) and `Library.File.TooLarge` (413),
  en + fa.
- Tests: `LibraryApiFixture`, `ListLibraryTests`, `ReadLibraryFileTests`, `PlanPromptTests`,
  `RunDetailPlanTests` (including a file replaced by a symlink after indexing, skipped where the
  account may not create links).

### Phase C — the screens

- `ui/package.json` — `react-markdown` 10.1.0 and `remark-gfm` 4.0.1, exact.
- `components/app/markdown.tsx` — the shared renderer, GFM tables, no raw HTML. The run detail's stage
  texts now render through it (closes plan 0001 D23).
- `features/library/**` — `library-params` (tab/sort/dir parsing, detail URLs that carry the
  category), `api`, `queries`, fa + en messages, the list with tabs and filters, the evidence
  gallery with its dialogs, the copy-prompt button with its selection fallback, the sandboxed
  preview frame, and the item page.
- Routes `/[locale]/library/` and `/[locale]/library/view/`; registry, `FEATURE_KEYS`, the `work`
  menu category and `common.menu.work` (en + fa).
- Run detail: a "Plan" link from `RunDetailDto.plan` and `Markdown` for the stage texts.
- `.ai/ui/design.md` — the sandboxed frame, the gallery, the one renderer.

## Verified

- `dotnet build Orchestra.slnx` — 0 warnings, 0 errors.
- `dotnet test Orchestra.slnx` — **147/147 passed**, 0 failed, 0 skipped (68 → 123 → 147).
- `npm run typecheck`, `npm run lint`, `npm run test` (**35/35**, was 22), `npm run build` — clean;
  the export lists `/fa/library`, `/en/library`, `/fa/library/view` and `/en/library/view`.

## Not verified

- The owner checks of phases A, B and C (§9): the import-log library count, `GET api/library` on a
  real project and its run's `plan`, and the screens themselves. The agent does not run the app.
- The migration was generated but **not applied**: `Database.Migrate()` runs at the API's next start.

## Open

- **The owner's `dotnet run` (PID 61140) holds `api/src/Orchestra.Api/bin/Debug`.** Building normally
  fails with MSB3027/MSB3021. The gates above ran with
  `-p:ArtifactsPath="<repo>/artifacts/agent"`, and the migration was generated with
  `dotnet ef migrations add ... --configuration Release`. Do not stop their process.
- Two existing test expectations changed with phase A, both on purpose: a project whose `RunsPath`
  is not an `orchestrator/runs` shape now logs one `library root not set` error
  (`ImportServiceTests.A_corrupt_block_is_logged_and_the_rest_is_imported`: 2 → 3), and the
  `Fixtures/**` copy needed `EnableDefaultNoneItems=false` because the SDK's default globs skip dot
  folders — `.orchestra/` and `.ai/` are exactly what a library fixture is.
- The three-step close (commit, merge, delete the branch) waits for the owner's separate permission.
