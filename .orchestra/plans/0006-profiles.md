# Plan 0006 — Profiles and per-project overrides

**Status:** Running — approved 2026-11-22 after the third gate-2 review; phases A–C implemented, verified 2026-11-22 (API 246, UI 64, engine suite 133/133 on both configs); waiting only for the owner's real run in each project
**Created:** 2026-11-19 · **Updated:** 2026-11-22 · **Done:** —
**Preview:** [`../previews/0001-skeleton-and-import/`](../previews/0001-skeleton-and-import/index.html) §2 (the three layers), §3 (adjustable workflow) and the "Profiles" mock — approved 2026-11-19 (v3)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (Freebuff), 2026-11-22: first pass B1–B8 + 9 smaller points; second pass B1–B2 + 16 smaller points; third pass no blockers, 7 clarifications — all applied in this version
**Decided by:** owner's §8 answers 2026-11-20 and 2026-11-22 (questions 4–11)
**Branch:** `alireza/profiles` — the implementer creates it.
**Base:** `main` (the public initial commit `3007a72`; the earlier `alireza/skeleton-and-import` history is local only)
**Companion:** None — the engine is in this repo after 0005. A project opts in by adding `"profile"`
to its own `orchestrator.json` (the owner, by hand); plan 0007 moves that file to `.orchestra/`.
**Steps changed:** no preview of its own — preview 0001 covers it (owner, 2026-11-19)

---

## فهم من از درخواست

هر تنظیمی که امروز در `orchestrator.json` هر پروژه تکرار شده، در یک **پروفایل** در Orchestra جمع می‌شود:
- زنجیره‌ی مدل‌ها برای هر نقش،
- tierها و poolها، adapterها، سهمیه، sessionها، و تعداد دورهای خودکار،
- منبع قیمت‌ها،
- و مراحل workflow (کدام مرحله روشن است و کدام قفل).

هر پروژه همان `orchestrator.json` خودش را نگه می‌دارد و یک کلید `profile` به آن اضافه می‌کند. هر مقداری
که با پروفایل فرق دارد، سر جای خودش در همین فایل می‌ماند، مثلاً «مرورکننده: deepseek». این فایل در git همان
پروژه است، پس پروژه بدون Orchestra هم تنظیماتش را دارد. موقع شروع هر run، موتور پروفایل و `orchestrator.json`
را ادغام می‌کند. اولویت با پروژه است، و نتیجه برای همان run ذخیره می‌شود.

پروفایل را در داشبورد ویرایش می‌کنید. هر تغییر یک نسخه‌ی تازه با دلیل می‌سازد. داشبورد پروفایل را به یک
**فایل** export می‌کند و موتور فقط همان فایل را می‌خواند. پس داشبورد خاموش، run را متوقف نمی‌کند (ADR 0002).

برداشت من: چون داشبورد در پوشه‌ی پروژه نمی‌نویسد (ADR 0005)، تغییر `orchestrator.json` از داشبورد **مستقیم
نیست**. صفحه‌ی پروژه مقدارهای نهایی را نشان می‌دهد، کنار هر مقدار می‌گوید از پروفایل آمده یا از پروژه، و
برای تغییر، متن آماده‌ی `orchestrator.json` و یک پرامپت برای ایجنت شما می‌دهد.

این پلن کارهای زیر را **نمی‌کند**:
- پروژه‌ها را به `.orchestra/` منتقل نمی‌کند (پلن 0007). تا آن موقع فایل تنظیمات پروژه همان
  `orchestrator/orchestrator.json` است.
- قیمت‌ها را دوباره حساب نمی‌کند.
- روی سرور کار نمی‌کند (پلن 0008).

## جریان کار

1. اولین بار، داشبورد یک پروفایل `default` را از روی بخش‌های مشترک `orchestrator.json` امروز Project B
   می‌سازد.
2. در «تنظیمات › پروفایل‌ها»: فهرست پروفایل‌ها؛ ویرایش هر بخش در یک فرم (نقش‌ها و زنجیره‌ها، tierها،
   adapterها، مراحل workflow، و بقیه)؛ «ذخیره» یک دلیل می‌خواهد و نسخه‌ی تازه می‌سازد. تاریخچه‌ی نسخه‌ها
   را با diff می‌بینید و می‌توانید نسخه‌ی قبلی را برگردانید (که خودش یک نسخه‌ی تازه است).
3. هر ذخیره، پروفایل را در `<data>/profiles/<id>.json` می‌نویسد. این فایل مال داشبورد است.
4. موتور موقع شروع هر دستور:
   - پروفایلی را که `orchestrator.json` نام برده از `<profilesPath>/<id>.json` می‌خواند. `profilesPath` در
     فایل `orchestrator.local.json` همان پروژه است (در git نیست)، کنار `enginePath`،
   - `orchestrator.json` را رویش ادغام می‌کند،
   - و وقتی run تازه ساخته می‌شود، نتیجه را یک بار در `runs/<id>/config.effective.json` می‌نویسد.

   اگر فایل پروفایل نباشد، با خطای روشن متوقف می‌شود. موتور هیچ‌وقت سراغ API نمی‌رود.
5. در «تنظیمات › پروژه‌ها › <پروژه>»: مقدارهای نهایی، منبع هر مقدار، و متن پیشنهادی `orchestrator.json` با
   پرامپت کپی.
6. در جزئیات هر run، تنظیماتی که آن run با آن اجرا شد دیده می‌شود.

فازها:
- **A:** ذخیره‌ی پروفایل، نسخه‌ها و export در داشبورد.
- **B:** ادغام در موتور.
- **C:** صفحه‌ها.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- پروفایل `default` با همان زنجیره‌هایی که امروز Project B دارد ساخته شده است.
- مرورکننده‌ی اول را در پروفایل عوض کنید و با دلیل ذخیره کنید. نسخه‌ی ۲ در تاریخچه با diff دیده می‌شود.
- در یک `orchestrator.json` آزمایشی با `profile` مرورکننده را جدا بنویسید و یک `new -DryRun` بزنید.
  `config.effective.json` مرورکننده‌ی پروژه را دارد و بقیه را از پروفایل.
- داشبورد را خاموش کنید و یک run بزنید. با فایل export‌شده کار می‌کند.
- فایل پروفایل را پاک کنید. دستور با یک خطای یک‌خطی متوقف می‌شود که می‌گوید کدام فایل نیست.
- نسخه‌ی ۱ را برگردانید. نسخه‌ی ۳ ساخته می‌شود که برابر نسخه‌ی ۱ است.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md).
2. The dashboard writes only under its data folder. It never writes a project's `orchestrator.json` (ADR 0005).
3. The engine never calls the API (ADR 0002).

### Baseline

Plan 0005's offline suite (127 per config) and 0001–0004's gates green; counts recorded.

## 1. Goal

Versioned profiles edited in the dashboard and exported to a file; per-project values in
the project's own `orchestrator.json`; the engine merges the two at run start and records the result per run.

## 2. Out of scope

Moving projects to `.orchestra/` (0007); editing a project's `orchestrator.json` from the dashboard; profiles on a
server (0008); a model catalogue with live prices beyond what `modelRatesSource` does today; a profile
picker in the project form (only `default` exists; `ProjectStore.IsValid` keeps `Profile == "default"`).

## 3. Rules that apply

ADRs 0002, 0003 (the dashboard may edit **its own** settings), 0005, 0006.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | A **profile** holds exactly: `roles`, `tiers`, `adapters`, `modelRatesSource`, `modelRates`, `quota`, `sessions`, `revise`, `readback`, `preflight`, `defaults`, `rules`, `workflow.steps`. **Every other key stays in the project's file**, untouched by the merge — today that is `version`, `note`, `name`, `description`, `repos`, `root`, `runsDir`, the `workflow` paths, `verifyCommands`, `cleanTree`, `planFiling`, `contractDoc`, `archive`, `report`, `adapterTransport` (a prose note no code reads), plus `profile`. A key the engine adds later is a project key unless a plan moves it into this list |
| D2 | **Merge = the profile's `config` as the base, the whole project file on top**, key by key, recursively (so `workflow` merges too: the project's paths and the profile's `steps` both survive; no `steps` anywhere = all on). **Arrays replace** (a project that sets a role's `chain` replaces the whole chain); `null` removes a key. **Overrides are in place** (owner, 2026-11-22): a D1 key the project file still has wins over the profile; there is no `overrides` block. So a project that adopts a profile deletes the D1 keys it wants from the profile |
| D3 | Profiles live in the database tables `Profile` and `ProfileVersion` (the dashboard's own data, like events — ADR 0006 applies) and are exported on every save to `<data>/profiles/<id>.json` (atomic write) |
| D4 | A save needs a reason (1–300 chars); a revert is a new version with the reason `revert to v<n>`; a revert to the **current** version is refused (owner, 2026-11-22) |
| D5 | **Where the export lives** (owner, 2026-11-22): the dashboard writes to `Orchestra:ProfilesPath` (default `<data>/profiles`). The engine reads `profilesPath` from the project's git-ignored `orchestrator.local.json` (beside `enginePath`); a missing key or folder → `config: set "profilesPath" in orchestrator.local.json to the dashboard's profiles folder`. In Docker, `deploy/docker-compose.yml` bind-mounts a host folder (`ORCHESTRA_PROFILES_DIR` in `deploy/.env`) at `/data/profiles`; the database stays on the named volume. No path is ever written in a project's `orchestrator.json` (no `profilesDir`), and there is no `ORCHESTRA_HOME` |
| D6 | `workflow.steps` = `understanding`, `preview`, `plan`, `review`, `approve`, `implement`, `verify`, each `{ enabled }`. **Locked** (always on; the editor shows them disabled; the engine rejects a config that turns one off): `understanding`, `plan`, `review` (owner, 2026-11-22 — `Cmd-Approve` needs a review and its family check, so it is unchanged), `approve`, `implement`, `verify`. Only `preview` is adjustable; the engine passes it to the planner prompt as one line. The six locked steps change no engine behaviour today; they are shown (disabled, "Always on") so the profile records the whole workflow (owner, 2026-11-22). **Read-back has one switch only:** the existing `readback.enabled`; it is not in `workflow.steps`, and the editor shows it among the steps but writes `readback.enabled` |
| D7 | Backward compatible: an `orchestrator.json` with no `profile` key works exactly as today (no merge) |
| D8 | Validation: the engine's existing `Import-OrchConfig` checks run on the **effective** config, so every rule that holds today still holds |
| D9 | **Where the merge and the record happen:** `Import-OrchConfig` merges and validates (no run exists yet) and keeps `ProfileId`/`ProfileVersion` on `$Ctx`. The command that **creates** the run (`new`, through `New-TaskRun`) writes `runs/<id>/config.effective.json` once, shaped `{ "profile": id | null, "profileVersion": n | null, "writtenAt": …, "config": { …effective… } }`. Later commands (`revise`, `readback`, `approve`, `verify`, `close`) never rewrite it; when the profile version on `$Ctx` differs from that file's `profileVersion` they print one line `profile <id> is v<n> now; this run started on v<m>` and go on with the current one |
| D10 | **The run's effective config reaches the dashboard** through the existing import: `Write-RunArchive` adds `effectiveConfig` to the archive; the importer reads it from the archive and from a live run's `config.effective.json` into a new nullable column `Run.EffectiveConfigJson`; `GET api/runs/{id}` returns it |
| D11 | **Seed source:** `api/src/Orchestra.Api/Modules/Profiles/default-profile.json` (an embedded resource), made once from the D1 keys of `engine/examples/orchestrator.json` with `workflow.steps` all enabled. A test in `api/tests` checks that its keys equal D1's list, and that its `roles`/`tiers` equal the engine example's, **ignoring `note`/`description` keys** (read from the repo: the test finds the root by walking up from `AppContext.BaseDirectory` to the first folder that contains `engine/examples/orchestrator.json`, so drift fails a test, not Docker) |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Modules/Profiles/{ProfileEntity,ProfileVersionEntity}.cs` + configurations + migration `Profiles` | NEW | A |
| 2 | `api/src/Orchestra.Api/Modules/Profiles/{ProfileSeed,ProfileExporter,ProfileSchema}.cs`; `default-profile.json` (embedded resource, D11) + its `EmbeddedResource` line in `Orchestra.Api.csproj` | NEW/EDIT | A |
| 3 | `api/src/Orchestra.Api/Modules/Profiles/Features/{ListProfiles,GetProfile,SaveProfile,RevertProfile,GetVersions}/*`; `api/src/Orchestra.Api/Modules/Projects/Features/EffectiveConfig/*` + the route on the existing `ProjectsController` | NEW/EDIT | A |
| 4 | `api/src/Orchestra.Api/Modules/Profiles/{ProfilesController,ProfilesModuleInstaller}.cs` (Order 80) | NEW | A |
| 5 | `api/tests/Orchestra.Api.Tests/Profiles/**` | NEW | A |
| 5a | `Modules/Runs` — `Run.EffectiveConfigJson` (entity, configuration, migration `RunEffectiveConfig`), the `GetRun` DTO; `Modules/Import` — `ArchiveReader`/`ArchiveSnapshot` (the archive's `effectiveConfig`), the live-run read in `ImportService`, `RunAssembler` (D10) | EDIT | A |
| 5b | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `ProfilesPath` with a `ResolveProfilesPath` like `ResolveDatabasePath` (relative → under the data folder; absolute kept); the bind mount must be writable by the image's uid 10001 (say so in `deploy/README` or the compose comment); `deploy/docker-compose.yml` (bind mount at `/data/profiles`), `deploy/.env.example` (`ORCHESTRA_PROFILES_DIR=`), `appsettings.Local.example.json` (`"ProfilesPath": ""`, and fix the missing comma after `"ReportToken": ""`) (D5) | EDIT | A |
| 5c | `engine/README.md` and `engine/examples/orchestrator.local.json` — the `profilesPath` key | EDIT | B |
| 6 | `engine/lib/agents.ps1` — `Merge-ConfigObject`, `Get-ProfilesPath` next to `Import-OrchConfig` (no new lib file, so no dot-source changes in `run.ps1`, `refresh-graph.ps1` or the suite) | EDIT | B |
| 7 | `engine/lib/agents.ps1` — `Import-OrchConfig` loads the profile when the config has `profile` and merges by D2, validates the effective config, rejects a locked step turned off, keeps `ProfileId`/`ProfileVersion` on `$Ctx` (D9) | EDIT | B |
| 8 | `engine/lib/bus.ps1` — `New-TaskRun` writes `config.effective.json` (D9); `engine/run.ps1` — the version line in later commands; `Write-RunArchive` adds `effectiveConfig` (D10); the planner prompt gets the `preview` line; `agents`/`help` print `profile: <id> v<n>`; the rates fallback note (around `run.ps1:705`, asserted by `T-L2`) names the profile **only when one is loaded** (without a profile the text stays `orchestrator.json modelRates`, so `T-L2` stays green) | EDIT | B |
| 9 | `engine/tests/offline/run-offline.ps1` + fixtures in **`engine/tests/offline/profile-fixtures/`** (`profile.json`, `project.orchestrator.json` — not matched by the `configs/` glob either) — not in `configs/`, whose `*.orchestrator.json` glob would add a third full pass | EDIT/NEW | B |
| 10 | `ui/src/features/profiles/**` (manifest, api, queries, types, `i18n/{fa,en}.ts`, components, `index.ts`); pages `ui/src/app/[locale]/settings/profiles/page.tsx` and `…/settings/profiles/edit/page.tsx` (`?id=`) | NEW | C |
| 10a | `ui/src/lib/features/define-feature.ts` (`profiles` in `FEATURE_KEYS`); `ui/src/features/registry.ts` (`FEATURES`, menu category `settings`); `ui/src/i18n/messages.ts` (the `profiles` catalogue). `ui/src/architecture.test.ts` must stay green: imports across features only through `index.ts` | EDIT | C |
| 11 | `ui/src/features/projects/{api,queries,types}.ts` (the effective call); `ui/src/features/projects/components/project-effective-config.tsx`, exported from `ui/src/features/projects/index.ts`; page `ui/src/app/[locale]/settings/projects/effective/page.tsx` (`?id=`), linked from the projects list | NEW/EDIT | C |
| 12 | `ui/src/features/runs/types.ts` (`RunDetail.effectiveConfig`); `ui/src/features/runs/components/run-detail.tsx` — the run's effective config (collapsed JSON) from the `GetRun` field of D10; hidden when null | EDIT | C |
| 13 | `.ai/architecture/data-model.md` (the two profile tables, `Run.EffectiveConfigJson`, the table count, the "one exception" sentence now naming events **and** profiles, `<data>/profiles/` under the dashboard's own data), `.ai/architecture/overview.md`, `.ai/engine/conventions.md` | EDIT | A–B |
| 14 | `.ai/decisions/0007-profiles-exported-to-a-file.md` | NEW | A |

## 6. Per-file specification

### 6.1 Storage and export

`Profile { Id (slug), Name, CurrentVersion }`, `ProfileVersion { ProfileId, Version, Json, Reason,
CreatedAt }` (unique `(ProfileId, Version)`). `ProfileSeed` runs at start when no profile exists and
creates `default` v1 from the embedded `default-profile.json` (D11) — never from `engine/`, which is not
in the Docker image. `ProfileExporter` writes `{ "profile": id, "version": n, "exportedAt": …, "config": { … } }`
to `<ProfilesPath>/<id>.json` after every save and at start (so the file always equals the current
version).

### 6.2 API

| Endpoint | Does |
|---|---|
| `GET api/profiles` | id, name, current version, updated |
| `GET api/profiles/{id}?version=` | the version's JSON (current by default) |
| `PUT api/profiles/{id}` `{ json, reason }` | validates (§6.3) → new version → export |
| `POST api/profiles/{id}/revert` `{ version }` | new version equal to that one; the current version → 400 `Profiles.Revert.Current` |
| `GET api/profiles/{id}/versions` | the list with reasons; `?diff=a,b` → a JSON-path list of changes |
| `GET api/projects/{id}/effective` (Projects module, file 3) | reads the project's `orchestrator.json` (read-only, `RootPath/orchestrator/orchestrator.json`), merges with its profile by D2, returns `{ effective, sources: { path, from: profile\|project } [] }` |

| Code | Status | en | fa |
|---|---|---|---|
| `Profiles.Profile.NotFound` | 404 | Profile not found. | پروفایل پیدا نشد. |
| `Profiles.Profile.Invalid` | 400 | The profile is not valid: {detail} | پروفایل معتبر نیست: {detail} |
| `Profiles.Reason.Required` | 400 | Say why you change it. | دلیل تغییر را بنویسید. |
| `Profiles.Step.Locked` | 400 | The step {step} is always on and cannot be turned off. | مرحله‌ی {step} همیشه روشن است و نمی‌شود خاموشش کرد. |
| `Profiles.Revert.Current` | 400 | This is already the current version. | این همین الان نسخه‌ی فعلی است. |
| `Profiles.Project.Unreadable` | 422 | The project's orchestrator.json cannot be read here: {detail} | فایل orchestrator.json این پروژه اینجا خوانده نمی‌شود: {detail} |

### 6.3 Validation (`ProfileSchema`)

The engine's own checks today are three (`agents.ps1`: no `inbox`-transport hop in `planner`; an inbox
hop has a `family`; each adapter's `transport` is `cli` or `inbox`); `ProfileSchema` runs those **by transport, as the engine
does** (not by the adapter name `manual`), plus **new, dashboard-only** early checks: every role has a
non-empty `chain`; each hop has `adapter`, `model`, `family`; a hop's adapter exists in `adapters`; tier
keys are known; locked steps on. Every check **skips `note` keys** (they sit inside `tiers`, `modelRates`,
`modelRatesSource` and each adapter in the example). The engine re-validates the effective config (D8) and is the guarantee.

### 6.4 Engine

`Merge-ConfigObject($base, $over)` implements D2 on `PSCustomObject`s (PowerShell 5.1). `Import-OrchConfig`:
if the loaded `orchestrator.json` has `profile`: load `<profilesPath>/<profile>.json`
(`Get-ProfilesPath` reads `orchestrator.local.json` beside the config, D5; missing file →
`config: profile '<id>' not found at <path>; open the dashboard once or copy the export there`), take
its `config`, merge the whole project file on top (D2), then run today's
validations on the result and reject a locked step turned off. The run record follows D9; the archive
follows D10. `Show-Usage`/`agents` print `profile: <id> v<n>`.

### 6.5 UI

Profiles editor: one tab per section of D1; roles as an ordered list of hops per role (add, remove,
move up/down; fields adapter/model/family/pool/tier); tiers and adapters as key/value forms; workflow
steps as switches with locked ones disabled, and read-back shown among them but saved to
`readback.enabled` (D6); a raw JSON tab for the rest (validated on save). Save opens
a dialog asking for the reason. Versions: list + side-by-side diff of two versions. Project effective
config: a table `path · value · from` and the suggested `orchestrator.json` — the non-D1 keys plus only the D1 values that differ from
the profile — with **Copy** and a prompt "Replace orchestrator/orchestrator.json in {project} with this;
commit nothing." In Docker the page needs the project folder mounted (`PROJECT*_ROOT` in
`deploy/docker-compose.yml`, commented today); without it the page shows `Profiles.Project.Unreadable`. Persian/English strings follow
the pattern of plans 0001–0004; the implementer writes the en text and **stops to ask the owner for the
fa text** of any key not listed here, rather than writing Persian itself.

| Key | en | fa |
|---|---|---|
| `profiles.title` | Profiles | پروفایل‌ها |
| `profiles.version` | Version {n} | نسخه‌ی {n} |
| `profiles.save` | Save as a new version | ذخیره به‌عنوان نسخه‌ی تازه |
| `profiles.reason` | Why this change? | چرا این تغییر؟ |
| `profiles.revert` | Restore this version | برگرداندن این نسخه |
| `profiles.history` | History | تاریخچه |
| `profiles.locked` | Always on | همیشه روشن |
| `profiles.exported` | Saved and exported for the engine | ذخیره و برای موتور export شد |
| `profiles.from.profile` | profile | پروفایل |
| `profiles.from.project` | project | پروژه |

### 6.6 Notes from the implementation (2026-11-22)

- The plan file on disk had been reset to the pre-review copy; this text is the reviewed version the
  implementation followed.
- `ResolveProfilesPath` resolves a relative `ProfilesPath` against the **database file's folder**, not the
  content root: the image keeps the database at `/data/orchestra.db` and its content root is `/app`, so
  content-root resolution would write the export inside the image and the `/data/profiles` mount would
  stay empty.
- One migration (`Profiles`) carries both profile tables and `Run.EffectiveConfigJson`.
- The effective endpoint resolves the project through `ProjectRegistry.Find(id)` (`ProjectOptions.ResolveRootPath`)
  and answers an unknown id with the existing `Projects.Project.NotFound` (404).
- A live `config.effective.json` that does not parse is one import error (like an unreadable `task.json`)
  and the run's column stays null.
- `workflow.steps` is read only when a profile is loaded (D7 keeps a project without `profile` exactly as today).

## 7. Tests

| File | Facts |
|---|---|
| `Profiles/ProfileSeedTests.cs` | seed keeps only D1 profile keys; runs once; `default-profile.json` keys equal D1 and its `roles`/`tiers` equal `engine/examples/orchestrator.json` (D11) |
| `Runs/EffectiveConfigImportTests.cs` | archive with `effectiveConfig` → `Run.EffectiveConfigJson`; live `config.effective.json` read; neither → null (D10) |
| `Profiles/SaveRevertTests.cs` | save needs a reason; version increments; revert makes a new equal version; revert to the current version → 400; export written atomically and equal to the current version |
| `Profiles/ProfileSchemaTests.cs` | each rule of §6.3 rejects with its detail |
| `Profiles/EffectiveConfigTests.cs` | D2: objects merge, arrays replace, null removes, `workflow` paths and `steps` both kept; sources reported per path; an unreadable project file → 422 |
| engine offline: `profile-merge` cases (fixtures in `profile-fixtures/`) | `config.effective.json` written once at `new`, not rewritten by `revise`; a newer profile version prints the one line; project chain replaces profile chain; `report`/`archive`/`contractDoc` in the project survive the merge (D1); `review` turned off is rejected; a missing `profilesPath` or profile file → the one-line error; the archive has `effectiveConfig`; a config without `profile` behaves as today; `T-L2`'s note updated |
| `ui: features/profiles/**` | locked switches disabled; reason dialog required; diff view; copy of the suggested `orchestrator.json` |

## 8. Stop and ask — open questions for the owner

1. **One profile or several from the start?** The preview mentioned "cheap" and "accurate"; this plan
   seeds only `default` and lets you add more. Enough?
   — **answered 2026-11-20:** only `default`.
2. **Model prices** (`modelRates`, `modelRatesSource`) — in the profile (shared) as D1 says, or per
   project?
   — **answered 2026-11-20:** in the profile.
3. **The planner's chain** is Claude (the conductor) in both projects today. Keep it in the profile, or
   per project?
   — **answered 2026-11-20:** in the profile; a project may override it.
4. **How the engine finds the export** — **answered 2026-11-22:** `profilesPath` in the git-ignored
   `orchestrator.local.json`; Docker bind-mounts that folder; no `profilesDir`, no `ORCHESTRA_HOME` (D5).
5. **`review` off?** — **answered 2026-11-22:** locked, like approve and verify (D6).
6. **`archive` and `report`** — **answered 2026-11-22 (planner's default, owner did not object):** they
   stay in the project's file. They are not a name clash: the question was only *which file holds them*.
   `report` carries that project's own id and token variable, and `archive` belongs to its import (D1).
7. **Profile picker in the project form?** — **answered 2026-11-22:** no; only `default` (§2).
8. **Which file holds a project's settings?** — **answered 2026-11-22:** its `orchestrator.json`, with a
   `profile` key; no separate `project.json`; the shims do not change (D2, §6.4).
9. **`overrides` block?** — **answered 2026-11-22:** no; values stay in place (D2).
10. **Revert to the current version?** — **answered 2026-11-22:** refused, 400 (D4).
11. **Locked steps with no engine effect?** — **answered 2026-11-22:** shown disabled (D6).

## 9. Phases

- **A — dashboard storage, API, export.** Files 1–5b, 13 (data model), 14. **Gate** (`.ai/ci.md`). Stop.
- **B — engine merge.** Files 6–9, 13 (engine). **Gate:** the offline suite (background, log in
  `evidence/0006-profiles/`, ~18 min per config). Stop.
- **C — screens.** Files 10–12. **Gate.** **Owner checks:** the items of «نتیجه». Stop.

## 10. Definition of done

All gates and the offline suite green; a real run in each project with `profile: default` behaves as
before; session log written; this header `Status: Done` with the date, commit and merge.
