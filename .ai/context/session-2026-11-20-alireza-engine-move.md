# Session 2026-11-20 — plan 0005 One engine, phases A–B

**Repository state:** branch `alireza/engine-move`, commit `ad2f99f`, merged into
`alireza/skeleton-and-import` as `4c183de`. Phase C is **not** done from here (it happens in the two
projects) and is the only open item.

## What changed

Plan: [`../plans/0005-engine-move.md`](../../.orchestra/plans/0005-engine-move.md) §5 files 1–10, 12–16.

- **Baseline first.** Both orchestrator repos were clean of uncommitted code. The offline suites ran
  from their own HEADs, read-only: Project B `b327225` (main) **114/114**, Project A `4c0d570` (main)
  **116/116**; logs in [`evidence/0005-engine-move/`](../../evidence/0005-engine-move/).
- **The copy.** `run.ps1`, `lib/{agents,bus,gates,pool}.ps1`, `refresh-graph.ps1`,
  `tests/offline/{run-offline,mock-agent}.ps1`, the conductor docs and the history docs were taken with
  `git show <hash>:<path>` — byte-identical to the commit, never from a working tree.
- **D3** `planFiling`: `Cmd-Close`'s filed-copy check is a `switch`; `default` is Project B's number
  block unchanged, `'slug'` is Project A's block unchanged (three lookups), and `Get-PlanSlug` moved
  verbatim from Project A's `lib/gates.ps1` into `engine/lib/gates.ps1`.
- **D4** the dead code is gone: `Get-Sentinel`, `Get-PrimarySentinel`, `Get-RepoGateDoc`, and
  `rules.gateCommandsFromRepoDoc` from the configs (`grep` proves no caller remained).
- **D5** `Show-Usage` takes `-Ctx` and formats its title and repos lines from
  `orchestrator.json → description { title, repos }`; with no config it prints
  `(no project config loaded)`, and `help` no longer fails when the config does not load.
- `Cmd-Agents` prints `engine: <engine folder>` after the `config:` line.
- Docs: `engine/README.md`, `engine/VERSION` (1.0.0), `engine/examples/{orchestrator,orchestrator.local}.json`,
  `engine/docs/history/README.md`, [`.ai/engine/conventions.md`](../engine/conventions.md),
  `.ai/ci.md` (its own "Engine (Windows only, owner-run)" section, outside the green table),
  `.ai/architecture/overview.md`, `.ai/README.md`, `AGENTS.md`.
- **Phase B.** `engine/tests/offline/configs/{number,slug}.orchestrator.json` hold the suite's synthetic
  config; the suite runs its whole set once per config in a child process and prints a tally per config.
  Each workspace copies the pass's config and only fills the mock-agent path. New cases: `T-C5`–`T-C8`
  (slug filing: `done/<slug>.md`, then `*-<slug>.md`, then a done file naming the branch, then the
  refusal), `T-A1` (the engine line), `T-D1` (the description fallback), `T-D2` (help with an
  unloadable config). The real-config checks (S15, T5, T-L1, T-L2, T17) now read
  `engine/examples/orchestrator.json`, the engine's own copy of a project config.

## Verified

- `powershell -NoProfile -File engine/tests/offline/run-offline.ps1` — **121/121 with
  `number.orchestrator.json` and 121/121 with `slug.orchestrator.json`**, exit 0, ~18 min per config.
  Log: [`evidence/0005-engine-move/suite-phase-a-b.log`](../../evidence/0005-engine-move/suite-phase-a-b.log).
  Run 1 is kept too: it failed 3 checks in the slug pass because `T-C1`–`T-C3` assumed number filing;
  they now set `planFiling: number` themselves, so the case means the same thing in both passes.
- `engine/run.ps1 help` with the example config prints its description in both places; with no config
  it prints `(no project config loaded)` and still prints the usage; `agents` prints the engine line.
- Every edited PowerShell file parses (Parser API); every new JSON file parses.

## Not verified

- **Phase C** — the shim in either project. The brief is
  [`../prompts/engine-shim-companion.md`](../../.orchestra/prompts/engine-shim-companion.md); D11 needs
  the owner to confirm that no `run.ps1` of that project is executing before the swap.
- The owner's checks of «نتیجه» in either project (`agents` with the engine line, `close` of a filed
  plan, the red line when `orchestrator.local.json` is missing).

## Deviations

- The plan writes the filing style read as `Get-CfgValue $Ctx 'planFiling' $null 'number'`, which
  returns the default even when the key is set (the helper's `$Name` is `[string]`, so `$null` becomes
  `''`). The engine reads the top-level property directly, in three lines, and says why.
- `description` is an object `{ title, repos }` rather than one string: `Show-Usage` needs the two
  lines in two different places of a fixed here-string.
- The engine files keep the source's line endings (a mix of LF and CRLF; the source tree sets
  `* -text`), not "normalized to CRLF" as §6.1 says: normalizing would rewrite every file of a
  behaviour-preserving copy.
