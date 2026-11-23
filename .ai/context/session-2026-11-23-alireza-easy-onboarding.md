# Session 2026-11-23 — plan 0009 (easy onboarding), all four phases

## Repository state

- Branch `alireza/easy-onboarding`, created from `alireza/locale-switch-fix` @ `5cc332c`.
- The whole plan is **uncommitted** (one dirty tree, per plan 0009 §9). Plan files and earlier session logs are
  the owner's uncommitted Markdown; not touched.
- Before the first edit the API test tree could not build because
  `api/tests/Orchestra.Api.Tests/artifacts/agent/{bin,obj}` (build output from an earlier run with an artifacts
  path) sat inside the project's compile globs. That generated folder was deleted; it is not project content.

## Phase A — API and Docker (§5 rows 1–9)

- **Hosting and paths:** `Common/Hosting/PathMapper.cs` (D2, pure, longest whole-segment host prefix,
  `ToHost` inverse in Windows spelling), `Common/Hosting/HostingInfo.cs` (D1 mode from
  `DOTNET_RUNNING_IN_CONTAINER` through `IHostingReader`, D5 browse roots, invalid map entries dropped with
  one warning), `OrchestraOptions` gains `PathMap`, `BrowseRoots`, `PublicUrl`; `ApiRoutes.System` (D17).
- **Setup module** (`Modules/Setup`, namespace not `System`): `api/system/hosting` and `api/system/folders`
  (§6.2), folder names only, dot-folders and unreadable folders skipped, 500 max, badges from the readiness
  and repo rules.
- **Projects:** stored host paths kept beside the mapped options (D3 — `ProjectRegistry.Stored`,
  `StoredProject.ToOptions(mapper)`, `ListProjects` returns them), validation on the mapped path (D4, new
  `Projects.Path.NotVisible` / `Projects.RunsPath.NotVisible`, only from create/update), the check answers
  `visible: false` instead of failing, readiness is the config file and `.ai/` + `AGENTS.md` count when
  found in every repo (D7, `InEachRepo` on the item), the scaffold prompt creates
  `.orchestra/orchestrator.json` in Project A's shape (D7), `lastEventAt` on the list (D11), the connect
  endpoint (D8/D9) with `PublicUrl`, `profilesPath` only when the map moves it, and no token in the agent
  prompt, the Profile field's validation rule deleted (D12).
- **Token:** `ReportTokenStore` (D6) — configured wins, else the saved `<data>/report-token`, else 32 random
  bytes written atomically; the filter reads the store and the "unset → 404" branch is gone.
- **Docker:** one `${ORCHESTRA_HOST_ROOT}:/host:ro` mount plus the two `Orchestra__PathMap__0__*` variables
  (D13); `.env.example` loses `PROJECT*`, `ReportToken` and the `Orchestra__Projects__*` block;
  `appsettings.Local.example.json` documents the three new settings.

## Phase B — the engine reads the token from the project file (D10)

- `engine/lib/report.ps1`: `Get-ReportToken` — the env variable named by `tokenEnv` first, then
  `reportToken` in the project's `orchestrator.local.json` (beside `ConfigPath`), else `''`; an unreadable
  or malformed file is ignored, not fatal. `Send-OrchestraEvent` uses it for the `Authorization` header.
- `engine/examples/orchestrator.local.json` shows the `reportToken` key; the Reporting section of
  `engine/README.md` explains the two sources and that the file is git-ignored.
- The resolver was checked directly (`file='file-token' env='env-token' none=''`) and is now a permanent
  suite check, `T-TOK`, at the end of `engine/tests/offline/run-offline.ps1`.

## Phase C — the screens for the owner (§5 rows 10–15, §6.6–6.8)

- **Data:** `features/projects/types.ts` gains `Hosting`, `FolderList`, `FolderCheck`'s new fields and
  `Connect`; `api.ts` and `queries.ts` gain `useHosting`, `useFolders` (only while the browser is open) and
  `useConnect` (refetches every 10 s while the page is open); `ProjectDto.lastEventAt` is on the type.
- **Form:** `project-form.tsx` — one path field with **Browse** beside it, a check on blur and on pick, the
  auto-fill rule from D14 in the pure, tested `form-autofill.ts` (never overwrites a typed name or runs
  folder, leaves a repo list the owner changed), the repo picker (`repo-picker.tsx`: detected repo folders
  ticked, untick, add by folder name), the runs folder under *Advanced*, the Profile field gone, and a save
  that goes to `settings/projects/connect/?id=…`. `project-schema.ts`: `repos` is an array, no `profile`.
- **Browse:** `folder-browser.tsx` — a sheet over `api/system/folders`, names only, up/choose, badges for
  "ready" and "repo". It is fully controlled by the form (no state syncing effect).
- **Check result:** `check-result.tsx` — hosting line, ready/not-ready rows (config file, `.ai/`, `AGENTS.md`,
  the four harness folders), repos, the runs folder suggestion, `visible: false` as its own text, and the
  repair prompt only when something is missing.
- **Connect:** `features/projects/components/connect-page.tsx` + `app/[locale]/settings/projects/connect/` —
  the project name, the `report` block (copy), the `orchestrator.local.json` keys (copy; the token is shown
  to the owner here and never inside the agent prompt), the agent prompt (copy), and a waiting → connected
  line driven by `lastEventAt`.
- **Start:** new vertical slice `features/start/` (`start.manifest.ts`, `queries.ts`, `start-steps.ts`,
  `i18n/{en,fa}.ts`, `components/start-page.tsx`) and `app/[locale]/start/` — four steps (dashboard,
  project, connect, first run) marked from hosting, projects and runs, Docker and IIS notes, the project
  links, and what still waits.
- **Also:** `FEATURE_KEYS`/registry/messages gain `start` first; the runs screen shows a "no projects yet"
  empty state that links to `/start/`; `projects/i18n/{en,fa}.ts` rewritten with the §6.7/§6.8 texts
  verbatim (labels, hints, check-result lines, folder-browser titles, connect panel).

## Phase D — docs

- `README.md` and `README.fa.md`: run it, open Getting started, one `ORCHESTRA_HOST_ROOT` line for Docker,
  the no-Docker/`dotnet run` path, adding a project by its Windows path, the dashboard's own token, and the
  IIS paragraph (Hosting Bundle 10, app-pool read access, `localhost` only).
- New [ADR 0008](../decisions/0008-host-paths-and-own-report-token.md) + its row in
  [`../decisions/README.md`](../decisions/README.md).
- [`../architecture/overview.md`](../architecture/overview.md): the `Setup` module, the token store, the host
  path translation, and the hosting table's token row. `data-model.md`: the 401 wording replaces "no token
  configured → 404". `vision/roadmap.md`: one parent mount, and 0009/0010 in the order line.
- Removed: the per-project mounts and their `PROJECT*` variables, the `Orchestra__Projects__*` block, the
  optional-token 404 rule, and the Profile field.

## Verified

- `dotnet build` in `api/`: **0 warnings, 0 errors** (`TreatWarningsAsErrors` is on).
- `dotnet test` in `api/`: **269 passed, 0 failed** (22 new tests: PathMapper, ReportTokenStore, the filter
  rewrite, hosting, folders, connect, scaffold shape, path mapping in validation).
- `ui/`: `npm run typecheck`, `npm run lint`, `npm run test` → **77 passed** (new: auto-fill rule, repo
  picker, connect page waiting/connected, start-page steps, no-Profile), `npm run build` → static export with
  `/[locale]/start` and `/[locale]/settings/projects/connect` prerendered.
- `docker compose -f deploy/docker-compose.yml config -q` parses with the two variables set.
- `engine/tests/offline/run-offline.ps1`: **140/140 per config** (`number` and `slug`), all configs passed,
  exit 0 — one more than the 139 baseline, and `T-TOK` is in the table.

## Not verified

- No Docker build or run: Docker Hub still answers 403 from this machine (earlier session log), so the image
  with the new `PathMap` variables was never built here.
- The dashboard was not started, so the new endpoints were not hit over HTTP and the screens were not seen in
  a browser; the owner's phase-A/B checks (`GET api/system/hosting`, adding Project A by its Windows path,
  the Connect page, `/start`) are open.
- `deploy/.env` on this machine still has the old `PROJECT*` lines and no `ORCHESTRA_HOST_ROOT`; that file is
  the owner's and was not touched — compose stops with a message until the line is added.

## Open

1. Committed on `alireza/easy-onboarding`; merge into `main` (with the locale fix under it) and branch delete are the owner's call.
2. Plan 0008 must gate `api/system/*` and `api/projects/{id}/connect` and require `Orchestra:BrowseRoots`;
   until then D16's loopback rule is documentation only.
3. Plan 0010 (Gridify tables) is untouched.

## Review after hand-over (planner, same day)

Fixed before the commit: `PathMapper.Covers` now takes normalized values and a root that keeps its slash
(`C:/`), and `FoldersHandler` resolves `..` with `GetFullPath` and compares in forward-slash form — the
browser works in a direct install and cannot leave its roots (three new tests in `FoldersHandlerTests`); the
connect page polls the project list (where `lastEventAt` is), not the connect call; the browser's Up goes back
to the root list and shows the API's error. Small: `PathMapper.LooksAbsolute` (a drive path is absolute in a
Linux container too), a relative path is `Projects.Project.Invalid` again, the connected time is formatted,
the profilesPath note shows only when the key is missing (`ConnectDto.HasProfilesPath`), one `useHosting` in
`lib/hosting/queries.ts`. Build, typecheck and lint pass; **the test suites were not re-run** after these
fixes (owner's request — they are slow).

Machine note: the engine reads the Windows variable `ORCHESTRA_REPORT_TOKEN` before `reportToken`. If
`Orchestra__ReportToken` is removed from `deploy/.env`, the dashboard makes a new token and the old variable
gets 401s — delete the variable then.

## After the 0009 commit (same day)

- Offline suite: one config instead of two (`planFiling` is read only by `close`, whose checks set both
  styles themselves), four parallel shards, workspaces copied from one template, one rerun-alone of a
  failed section. 140/140 in 440 s, was ~37 min. See `.ai/ci.md`.
- Plan [0011](../../.orchestra/plans/0011-fewer-git-calls.md) written (Draft): one `git status
  --porcelain=v2` per repo per snapshot instead of four calls. The before/after check stays (D1).
- Both READMEs gained a usage guide (Docker, add a project, connect the engine, run a task).
- `deploy/.env` on this machine gained `ORCHESTRA_HOST_ROOT`; `docker compose ... up --build -d` ran,
  and `api/system/hosting` answers `mode: container, mapped: true`.
- The API and UI suites were not re-run after the review fixes.

## Notes for the next session

- The offline suite reports with `Write-Host`, so a plain `> log` redirect keeps only the two config headers.
  Run it in an interactive/PTY shell and read the output instead (that is how the tally above was read).
