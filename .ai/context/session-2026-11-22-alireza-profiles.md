# Session 2026-11-22 — plan 0006 (profiles and per-project overrides), phases A–C

**Repository state:** branch `alireza/profiles` from `main` (`3007a72`); nothing committed, nothing pushed.

## What changed

### A — storage, API, export (dashboard)

- `Modules/Profiles/`: `Profile`/`ProfileVersion` entities, configurations, migration `20261122174524_Profiles`
  (both tables **and** `Run.EffectiveConfigJson`), `ConfigMerge` (D2 merge, sources, suggested file, diff),
  `ProfileSchema` (the engine's three transport checks + dashboard-only early checks + locked-step rule),
  `ProfileExporter` (atomic `{profile, version, exportedAt, config}`), `ProfileSeedService` (embedded
  `default-profile.json`, exported at start), the five features, `ProfilesController`, `ProfilesModuleInstaller`
  (order 80).
- `GET api/projects/{id}/effective` on `ProjectsController`; unreadable project file → 422
  `Profiles.Project.Unreadable`, unknown project → 404 `Projects.Project.NotFound`.
- Import side of D10: `RunEntity.EffectiveConfigJson`, `GetRunDto.EffectiveConfig`, `ArchiveReader`
  (`effectiveConfig`), `ImportService.ReadEffectiveConfig` (bad JSON = one import error), `RunAssembler`.
- `OrchestraOptions.ProfilesPath` + `ResolveProfilesPath` — a relative path resolves against the **database
  file's folder**, so `/data/orchestra.db` + mount `/data/profiles` works inside the image.
- 29 tests in `api/tests/Orchestra.Api.Tests/Profiles/`.

### B — engine

- `agents.ps1`: `Merge-ConfigObject`, `Get-ProfilesPath`, `Test-LockedStepOff`; `Import-OrchConfig` merges
  the project file over the profile's `config`, validates the effective config, keeps `ProfileId`/`ProfileVersion`.
- `bus.ps1`: `New-TaskRun` writes `config.effective.json` once.
- `run.ps1`: the one-line version mismatch note, `profile: <id> v<n>` in `agents`/`help`, the `preview` line in
  the planner prompt, the rates note names the profile, `Write-RunArchive` adds `effectiveConfig`.
- `engine/README.md`, `engine/examples/orchestrator.local.json` (`profilesPath`); six offline tests `P1`–`P6`
  with fixtures in `engine/tests/offline/profile-fixtures/`.

### C — screens

- `ui/src/features/profiles/**` (list, editor with tabs, workflow switches with locked ones disabled, reason
  dialog, history + diff), pages `settings/profiles` and `settings/profiles/edit`, feature registry and the
  `profiles` message catalogue (fa/en).
- Project effective page (`settings/projects/effective`, `?id=`) with the `path · value · from` table and the
  suggested `orchestrator.json` + prompt; linked from the projects table.
- `RunDetail.effectiveConfig` shown collapsed in the run page, hidden when null.

### Docs and deployment

- `.ai/architecture/{data-model,overview}.md`, `.ai/engine/conventions.md`, ADR
  `0007-profiles-exported-to-a-file.md` + its index row, plan 0006 header/row, `deploy/docker-compose.yml`
  (bind mount, uid 10001 note), `deploy/.env.example` (`ORCHESTRA_PROFILES_DIR`).

## Verified

- `dotnet build Orchestra.slnx`: 0 warnings, 0 errors. `dotnet test`: **246** passed (was 217).
- `ui`: `npm run typecheck`, `npm run lint`, `npm run test` (**64** passed, was 56), `npm run build` (static
  export, both locales, every route prerendered).
- Offline suite: **133/133** on `number` (`evidence/0006-profiles/suite-number.log`) and **133/133** on `slug`
  (`evidence/0006-profiles/suite-slug.log`).
- Live API smoke on a temp database and a temp project: seed created `default` v1 and exported it; `PUT` without
  a reason → 400 `Profiles.Reason.Required`; a locked step off → 400 `Profiles.Step.Locked`; a real save → v2 and a
  rewritten export; revert to the current version → 400; revert to v1 → v3 with reason `revert to v1`;
  `versions?diff=1,2` returned 103 changes; `GET api/projects/demo/effective` merged the project's chain and
  `workflow.gatesPath` over the profile and reported `sources` per path, with `workflow.steps` left out of the
  suggested file.

## Open

- **The plan file was reset.** The working tree's `.orchestra/plans/0006-profiles.md` had gone back to the
  pre-review copy (the `project.json` version, `B1–B8` only) while only the `**Base:**` line survived. The
  reviewed text (D5 `profilesPath`, D6 locked steps, D7 backward compatibility, D10 `Run.EffectiveConfigJson`,
  §6.6) was restored from the session and the header set to `Running`. **Check it against your own copy.**
- **The owner's checks of §7 of the plan are still open:** a real run in each project with `profile: default`,
  a project override that wins over the profile, a dashboard-off run, a deleted export file, and a revert seen
  from the engine. Also: the Docker profiles folder must be writable by uid 10001, and `PROJECT*_ROOT` stays
  commented today, so the effective page answers 422 in the container until those lines are uncommented.
- The project's own `orchestrator.json` still carries the D1 keys it wants to keep overriding — that is the
  in-place override rule, not a bug.
- Next: plan 0007 (move projects to `.orchestra/`).
