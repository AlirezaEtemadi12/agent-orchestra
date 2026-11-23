# Session 2026-11-19 — alireza — plan 0001 questions and preview

## Repository state

- `main`, no commit, nothing staged. Only this log was added to the repo; no other file changed.
- Preview (not in the repo yet): https://claude.ai/artifact/TvWCeEtmg57vypeZeFBTez. Once the layout in
  it is approved, it moves to `.ai/previews/0001-skeleton-and-import/index.html`.

## Owner's answers (plan 0001 §8 and follow-ups)

| Question | Answer |
|---|---|
| `.ai/` layout | One `.ai/` at the root, with separate subfolders (`.ai/api/`, `.ai/ui/`). No docs under `api/` or `ui/` |
| Name | Orchestra stays |
| Port | 3500 |
| Theme | Light + dark (+ system) |
| Companion `close` archive | **Dropped.** The importer reads only `_ledger.md` and open runs; from phase 1, events go straight to SQLite. Remove the companion from the roadmap and plan 0001 |
| Git remote | Created later by the owner, **public**, so others can use it |
| Languages | **fa + en from day one** (new) |
| Default language | The browser's, otherwise `en` |
| Dates | fa: Jalali with Latin digits. en: Gregorian. Time zone: the browser's |
| Planner workflow (new rule) | Ask «فهم از درخواست» first, then build an HTML preview, and write the plan only after approval |
| New storage areas | Previews in one categorized place; agent test evidence (screenshots); research; raw ideas that become plans. The owner asked whether plans could live in the DB so agents can use them |

## Proposed in the preview, waiting for approval

- Files stay the source of truth and the DB indexes them. Keeping plans only in the DB would break
  ADR 0002 (agents would depend on the dashboard) and would take plans out of git.
- New folders: `.ai/api/`, `.ai/ui/`, `ideas/` (one file per idea, replacing `vision/ideas.md`),
  `research/`, `previews/`, `.orchestra/evidence/` (git-ignored). `plans/done/` is dropped; the `Status:` line
  decides.
- Plan 0001 = skeleton + runs import + four screens + i18n. Plan 0002 = Library (indexes
  ideas/research/previews/plans/evidence). The `.ai/` restructure is a separate docs-only change.
- Decisions D10–D17, including a new i18n ADR and a reworded `AGENTS.md` rule 6.

## Round 2 (preview v2, same URL)

- Owner: split each project into `.ai/` (agent-neutral knowledge) and `.orchestra/` (workflow, roles,
  plans, previews, ideas, research, evidence, runs, `project.json` overrides), both kept in the
  project's own git. Profiles in Orchestra hold the defaults (e.g. the reviewer); a project overrides them.
- Owner: the planner workflow is adjustable per profile/project. Default: understanding → HTML preview
  (schematic / flow / mockups) → plan. The agent writes «فهم از درخواست» in its own words and flags
  ambiguities; the owner corrects it.
- Owner: `plans/done/` can go. The plan header gets `Created / Updated / Done / Preview / Run`; the
  dashboard tracks status and offers a "copy prompt for the conductor" button (it does not start runs).
- Owner: runs are grouped per project and show session and call counts; a run is named after its plan.
  Session name = task name stays an idea (needs adapter support).
- Owner agreed: files are the source of truth and the DB only indexes them; a deleted file disappears.
- Owner agreed (D21): the dashboard never writes into a project. It shows a prompt and the file
  contents, and the owner's agent writes `.ai/` and `.orchestra/`.
- Owner agreed: the `.ai/` / `.orchestra/` split, profiles and migrating the two projects stay ideas
  for phase 2. Plan 0001 reads today's `orchestrator/runs` layout. Order: 0001 skeleton+import →
  0002 library/plan tracking → 0003 new-project wizard + scaffold prompt → phase 1–2.
- New idea: a skill library in Orchestra, reused across projects. Needs an agent-neutral format
  exported per agent. Idea only.

## Seen in real data (read-only)

- The ledger has several `verify` blocks per run (T-AGENT-027: four in one hour). A later block
  replaces the session numbers; adding them up would double-count.
- `_ledger.md` and `task.json` are UTF-8 with a BOM, and `task.json` is written by PowerShell
  `ConvertTo-Json`. Artifact paths are absolute Windows paths.

## Round 3 — approved, docs restructured, plan written

Owner approved preview v3 («Approved, go ahead»). On branch `alireza/orchestra-layout` (unborn — no
commit yet; everything is untracked):

- **Moved to `.orchestra/`:** `workflow.md` (was `.ai/agents/`), `plans/` (was `.ai/plans/`),
  `prompts/` (was `.ai/prompts/`). New: `.orchestra/README.md`, `ideas/` (13 files + index),
  `research/2026-11-19-open-source-orchestrators.md`, `previews/0001-skeleton-and-import/index.html`
  (the approved preview with a doctype added).
- **Rules reviewed (what was removed or merged):**
  - `workflow.md` gate 1 is now understanding → preview → plan. Gate 2's "owner first reads
    «فهم»" was removed, because step 1 does that now.
  - `plans/done/` was dropped; the plan header gained `Created / Updated / Done / Preview / Run / Steps
    changed`.
  - `vision/ideas.md` was deleted: its phase-tagged items went into `roadmap.md` and the rest into
    `ideas/`. "Run archive" was dropped (D13).
  - The roadmap's companion `close` paragraph was removed.
  - `overview.md` rule 3 now says "never" and links ADR 0005, instead of "phases 0–1".
  - `AGENTS.md` rules 1, 3, 5 and 6 were reworded, not added to.
  - `start-session.md` lost its plan-0001-specific task block.
- **New ADRs:** 0004 (fa + en), 0005 (`.ai/` = knowledge, `.orchestra/` = work, files are the truth,
  the dashboard never writes into a project). 0001–0003 stay `Proposed` until plan 0001's gate 2.
- **`data-model.md` rewritten from the real files.** Two facts changed the design:
  - **Task ids are reused after `close`.** Project A `T-WEB-001` is three runs on three branches, so
    `(ProjectId, TaskId)` is not a key. Runs are split by branch after a close, and `RunKey` is
    `<taskId>.<n>`.
  - **The ledger keeps only the final review round.** Reviewer quality for closed runs is "rounds
    needed" plus the final counts. The preview's "blockers per review" is not computable, and the
    plan says so (D22).
  - The import is now a **rebuild per project in one transaction**, replacing keyed upserts.
- **Plan 0001** has §5–§7 written and the rest refreshed (D1–D22). Status: Draft, waiting for a
  plan review by another model family.

## Round 4 — committed, then the remaining plans

- Docs restructure committed: `ea33554` on `alireza/orchestra-layout` (owner's request). Plan 0001's
  `Base:` now names it.
- Owner: "write only the plans for the rest, each separately; v3 is enough as their preview". Written,
  all `Draft`, each with `Steps changed: no preview of its own`:
  - **0002** Library.
  - **0003** New project.
  - **0004** Live events, with a reporter companion.
  - **0005** Engine move into `engine/`, with a shim companion.
  - **0006** Profiles.
  - **0007** Move the two projects to the new layout, with two companions.
  - **0008** Server mode.
- Plans 0004–0008 have open questions in §8. Plans 0001–0003 have none.
- Roadmap and plans index updated (uncommitted).
- **Read-only findings in the orchestrators:**
  - The engine already takes `-Config` and resolves `root`/`runsDir` against the config's folder, so
    the shim in 0005 is ~10 lines.
  - The two copies differ only in three ways: plan filing by number vs slug, dead code that Project A
    commented out, and the project text.
  - `Add-History` is the single place where events can be hooked. The reporter can send per-round
    counts from the state, which closes the D22 gap for future runs.
  - Both workspaces hold several repos under a root that is not a git repo. This drives 0007 §8
    question 1.
- **Conflict found:** both orchestrator repos have **uncommitted "archive on close"** work
  (`ARCHIVE-ON-CLOSE-PLAN.md` in Project A; `Write-RunArchive` and an `archive` config block in Project B).
  Another session is doing what the owner said today is not needed. Not touched; asked in 0004 §8 Q1
  and 0005 §8 Q1.

## Round 5 — the close archive is back

- Owner relayed another session's decision: question 4 changed. `close` now writes
  `runs/_archive/<id>.json`, and the importer must read it. Verified read-only: committed as `c4b9ca6`
  (Project B) and `a947857` (Project A). Project A already has `T-UI-001.json` and `T-WEB-001.json`.
  Schema 1 holds `task` (the full `task.json`), `reviews`/`readbacks`/`verify` texts per round, and
  `usage` with exact tokens but **no cost**.
- Updated:
  - `roadmap.md`: the "no archive change" sentence is gone.
  - `data-model.md`: a new source row, matching step 4a (match by branch), the `StageText` table,
    `Source = Archive`.
  - Plan 0001: فهم, D13, a new D23, `ArchiveReader`, fixtures, `texts` in the run detail, messages,
    tests, an owner check.
  - 0004 §8 Q1 is answered. Its new question: turn the archive off after phase 1? ADR 0006's text
    was adjusted.
  - 0005 §8 Q1 is answered. What is left there: Project B's untracked `SUITE-SPEED-PLAN.md`.
- **Flaw reported to the owner:** the archive is named by task id only. A reused task id therefore
  overwrites the previous run's archive (Project A `T-WEB-001` had three runs; only the last archive
  survives). The importer matches archives by branch and also accepts `<id>.<suffix>.json`.
  Suggested fix for the orchestrator session: a unique name, e.g. `<id>.<yyyyMMddHHmm>.json`.

## Round 6 — plan 0002 review round 1

- Committed `7296504` (plans 0002–0008 and the archive changes).
- The owner relayed review round 1 of plan 0002 (6 blockers, 10 important, 4 minor). Each point was
  checked and holds. All are applied, and the plan's §11 maps every point to its fix. Main changes:
  - `SafePath` rejects any link or junction segment below the root, at indexing and on every request.
  - The preview CSP has no network (`connect-src 'none'`, `form-action 'none'`). Only Google Fonts and
    cdnjs/jsDelivr/unpkg scripts may load.
  - `PreviewRelPath` is set only for an indexed preview.
  - New: `RunDetailDto.plan`, `ImportLog.LibraryItems`, sorting by every column, enumeration from
    each glob's fixed prefix, and the D11 repo rule (one rule that also covers plan 0007).
  - When several runs match a plan, the newest is linked.
  - Ideas get a status and a linked plan.
  - `react-markdown` 10.1.0 and `remark-gfm` 4.0.1 are pinned.
- The reviewer left the new fa strings to the owner. As the planner, I wrote them: `previewSandboxed`,
  `empty`, `ideaStatus.*`, `copyPromptHint`, `runs`, `linkedPlan`, `imports.libraryItems`,
  `runs.detail.plan`. They are original text, not translations.
- Not committed; waiting for review round 2.

## Round 7 — plan 0002 review round 2

- The owner relayed round 2: changes requested, with 4 blockers, 6 important and 3 minor points. I
  checked each one against the files and they all hold:
  - Run branches are `plan-NNNN-…`.
  - Ideas carry `**Added:**` inline on the `**Status:**` line.
  - The mock's prompt and columns differ from the plan.
- **Owner decided (blocker 1):** keep the plan's screens (the No./Title split, the Done column and the
  detail page), recorded as D12, but use the mock's prompt text.
- Main fixes:
  - D5 matches by number plus repo, else by slug after stripping `plan-` and `NNNN-` from the branch.
    `RunDetailDto.plan` is deterministic.
  - One shared `Markdown` component also renders `StageText`. This closes plan 0001 D23.
  - `OrchestraDbContext` was added to the manifest (eight tables).
  - Every route carries `{category}`, and `Library.Prompt.NotAPlan` was dropped.
  - New: `IdeaHeaderParser`, `report.md` cards, the missing-`RootPath` rule, cascade delete, and
    `Repo` for plans only.
- New fa string written by the planner: `library.evidenceReport` = «گزارش».
- Not committed; waiting for review round 3.

## Round 8 — plan 0002 review round 3

- Round 3 found 2 blockers, 5 important and 6 minor points, and every one is applied. Main changes:
  - `RootPath` is derived only from `<x>/runs`. A root that is unset, missing or a filesystem root is
    an import error for that project, and the project gets no library items. In Docker the library
    stays empty until the owner sets `RootPath` and adds its mount.
  - `Markdown` moved to `components/app/`.
  - Evidence keeps images and `report.md` only.
  - There is one migration, `Library`.
  - New file: `LibraryDefaults.cs` holds the default globs, and plan 0007 edits it.
  - The note about multi-file previews is written.
- The B1 choice (limited derivation instead of a required `RootPath`) was made by the planner as the
  safe default, not by the owner.
- No new Persian strings in this round.
- The reviewer pointed out that the fa strings of rounds 1–2 were written by the planner, not the
  owner, which conflicts with AGENTS.md rule 6. They are waiting for the owner's check.
- Not committed.

## Round 9 — plan 0002 review round 4

- Round 4 found 1 blocker, 4 important and 4 minor points, and every one is applied:
  - `RootPath` is derived only from `orchestrator/runs` or `.orchestra/runs`, with either path
    separator.
  - A `done` folder is never a repo.
  - An evidence file directly under the prefix has a null group.
  - `RunKey` sorts by its number.
  - There is a separate "not set" error text.
  - Plan 0007 file 1 is now `EDIT`.
- One «نتیجه» bullet was removed: Orchestra's own plan in the library. Orchestra is not a configured
  project until plan 0007. No new Persian text was added.
- Owner: the reviewer is DeepSeek. Plan 0002's review round 5 was clean, so 0002 now waits for gate 2
  and for plan 0001 to be done.

## Round 10 — plan 0001 review round 1 (DeepSeek)

- Verdict: changes requested, with 3 blockers, 5 important and 7 minor points. All are applied, and the
  new §11 maps each point to its fix.
- The planner made these choices:
  - `PlanFamily` is dropped, because it has no source.
  - Every money column is `CostMicros` (`data-model.md` fixed).
  - `Run.Status` is the raw word from the file.
  - ADRs 0001–0003 are accepted at gate 2 (new file 57).
- Plan 0002 is corrected to nine tables.
- **Open:** pin the exact UI versions. The versions are in Project B's `Ui/package.json`. A read of
  that project was refused by the tool's permission check, so the owner's leave is needed.
- Rounds 2–3 (DeepSeek) are applied:
  - `Base` is the coming commit that holds the review fixes, because `7296504` lacks the round 1
    `data-model.md` fixes.
  - With the owner's leave, the exact UI versions were read from Project B's `Ui/package-lock.json`
    (read only) and written into §6.10.
- Earlier entry, still open for the owner: name the reviewing model in 0002's header. It must be a different model family
  than Claude, and gate 2 depends on it.
- Not committed.

## Not verified

- Nothing built or run. The plan's Next.js 16 / next-intl static-export design (§6.11) is not proven.
  §8 tells the implementer to stop if it does not build.

## Open

- Owner: commit the docs restructure on `alireza/orchestra-layout` (or say otherwise). Plan 0001's
  `Base:` waits for that hash.
- Plan 0001 review by a non-Claude model, then gate 2.
- Before the repo goes public, scrub the docs. `.ai/` and `.orchestra/` mention the owner's folder
  paths, Project A and Project B, and the preview's mock data comes from real ledger blocks.
- D11 deviates from the preview: CORS in development instead of a `/api` proxy, because a static export
  cannot use rewrites.

## Phase A — skeleton (2026-11-19)

- Branch `alireza/skeleton-and-import` from `7f49400`. Files 1–22, 38, 41–51, 51a, 53–55 and 57 of the
  plan; ADRs 0001–0003 are now `Accepted — 2026-11-19 (plan 0001's gate 2)`.
- Gates: `dotnet build` 0 warnings 0 errors, `dotnet test` 3 passed; `npm run typecheck`, `npm run
  lint`, `npm run test` (22 passed) and `npm run build` (11 static pages) all green.
- Three deviations, each small and named here:
  - `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 pulls `SQLitePCLRaw` 2.1.11, which NuGet audit
    reports as a high-severity advisory and `TreatWarningsAsErrors` turns into a restore error.
    Central transitive pinning raises it to 2.1.14 (`Directory.Build.props` + `Directory.Packages.props`).
  - The locale path helper lives in `lib/i18n/locale-path.ts`: importing the switcher in a test pulls
    next-intl, which needs the Next runtime to resolve `next/navigation`.
  - The API fallback serves a directory's `index.html` itself. `UseDefaultFiles` did not rewrite
    `/fa/…` for a `PhysicalFileProvider` outside `wwwroot`, so `/` and every locale page fell through
    to `404.html`.
- Smoke test on `http://localhost:3500` (Development): `/` 200, `/fa/` and `/en/` 200 with `lang`/`dir`,
  `/fa/cost/` 200, `/api/health` 200 `{"status":"ok"}`, `/api/nope` 404 `ProblemDetails` in en and fa,
  `/xyz` 404 `404.html`.
- The browser checks (theme and language persistence, both widths) are still the owner's.
- Not committed: the whole phase is one uncommitted diff, as the workflow asks.

## Phases B–E — data, import, four screens, deploy (2026-11-19)

- Files 23–37 and 39–40: entities, configurations, migration `Initial`, ledger/archive/task readers,
  `RunAssembler`, `ImportService`, `ImportHostedService`, the import log and its trim.
- Files 45–50: `ProjectsController`/`ListProjects`, `RunsController`/`ListRuns`/`GetRun`, `UsageController`
  with `cost` and `quality`; the four screens, the shared `components/app/` pieces and the re-import
  button in the shell.
- Files 52 and 56: `deploy/Dockerfile`, `deploy/docker-compose.yml`, `deploy/.env.example`, and the
  "Run it" section of `README.md`.
- Gates: `dotnet build` 0 warnings 0 errors, `dotnet test` 68 passed; `npm run typecheck`, `npm run lint`,
  `npm run test` (22 passed) and `npm run build` (12 static pages) all green.
- Four deviations, each small and named here:
  - `RunDetailDto` names the row arrays `sessionRows` and `roundRows`: the plan's list fields already
    carry `sessions` and `rounds` as counts/summaries.
  - The projects query lives in `lib/projects/queries.ts`, because the architecture test forbids a
    feature importing another feature and the cost and quality screens both need the project list.
  - `vitest.config.mts` inlines `next-intl` and aliases `next/navigation` to its real file: next-intl's
    ESM imports the subpath without an extension, which Node cannot resolve.
  - `ImportServiceTests` writes `ledger.lifecycle.md` into its temporary runs folder as `_ledger.md`, so
    the import path is tested against a ledger that actually exists.
- Smoke test on `http://localhost:3500` (Development, fixtures as the runs folder): `POST /api/import`
  built 5 runs from 6 files and 7 blocks with 1 error (the schema-2 archive); `/api/projects`,
  `/api/runs`, `/api/runs/demo/T-DEMO-001.2`, `/api/usage/cost`, `/api/usage/quality` and
  `/api/import/logs` all answer with data; `?groupBy=week` is 400 and an unknown project is a 404
  `ProblemDetails`.
- The browser checks (both languages, both themes, the detail links) are still the owner's.
- Docker: the image was not built here; `deploy/.env` has to point at real runs folders first.
- Not committed: the whole plan is still one uncommitted diff.

## Review and close (2026-11-19)

- Reviewed by the planner (Claude Opus 5.5). Gates re-run: `dotnet test` 68 passed (built into a
  separate output folder, because a running `Orchestra.Api` held the exe), and `npm run typecheck`,
  `lint`, `test` (22 passed) and `build` (12 pages) green. The running instance answered `/`, `/fa/`,
  `/api/health`, `/api/runs` with 200 and `/api/nope` with 404.
- One fix: `UiHosting.ResolveInside` compared the root prefix without a trailing separator, so a
  sibling folder such as `ui/out-x` passed the check. It now compares with the separator.
- No secret or personal path in source; the runs folders are only opened with `FileAccess.Read`.
- Plan 0001 is `Done` and committed; not merged into `main` and not pushed. The browser checks (both
  languages and themes, the detail links) and a Docker build against real runs folders are still the
  owner's.
