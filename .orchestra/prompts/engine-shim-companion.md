# Companion — a project drives the shared engine (plan 0005, phase C)

Paste this into a session **opened in the project's orchestrator repo**
(`Project ProjectB/orchestrator` or `Project ProjectA/orchestrator`). It is not run from Orchestra:
this change is made in that repo, in that repo's own history.

Plan 0005 moved the orchestrator's code into `Project Orchestra/engine/` — once for every project. What
is left here is this project's settings and a ten-line shim.

## Stop first

1. **Dirty code:** if `git status --porcelain` shows a `*.ps1`, `*.json` or `tests/**` change, stop and
   tell the owner. Markdown may be uncommitted; it does not block.
2. **A run in flight:** phase C waits while a `run.ps1` of this project is **executing** — a PowerShell
   process whose command line contains this project's `orchestrator` path. Check:

   ```powershell
   Get-CimInstance Win32_Process -Filter "Name = 'powershell.exe'" | Where-Object { $_.CommandLine -like '*<this project>\orchestrator*' } | Select-Object ProcessId, CommandLine
   ```

   If one is there, stop and tell the owner; ask before continuing. Open run folders under `runs/` do
   **not** block (D11): the engine is behaviour-identical and reads the same `runs/` through the same
   `orchestrator.json`.

## 1. Ask the owner for `enginePath`

The path to the Orchestra checkout — the folder that contains `engine\run.ps1`. Never guess it, never
copy it from another project. If `orchestrator.local.json` already exists, use its value.

## 2. Delete what moved

- `lib/` (all of it)
- `refresh-graph.ps1`
- `tests/` (all of it — the suite lives in `engine/tests/offline/` now)
- the docs moved by D6: `CONDUCTOR.md`, `REVIEW-BRIEF.md`, `WORKING-MODEL.md`
- the history moved by D7: `DESIGN.md`, every `*-PLAN.md` (`ARCHIVE-ON-CLOSE-PLAN.md`,
  `PROJECTA-PORT-PROMPT.md`, `CLOSE-PLAN`-unrelated plans, `SUITE-SPEED-PLAN.md`, …)

**Keep:** `orchestrator.json`, `CLOSE-PLAN.md` (its skill points at it), `.gitignore`, `runs/`,
`orchestrator.manual.json` if this project has one.

## 3. `orchestrator.json`

- add `description` — the two lines `run.ps1 help` prints, taken from this project's current usage
  header (the `orchestrator / run.ps1 - … for <project> …` line, and the `- Repos: …` line with its
  continuation):

  ```json
  "description": {
      "title": "orchestrator / run.ps1 - multi-agent, four-gate driver for <project> (<repos>)",
      "repos": "  - Repos: <A>, <B> - separate git repos under one workspace root. The root is not a git repo."
  },
  ```

- add `planFiling`: **`number`** for Project B, **`slug`** for Project A (D3).
- Project A only: add `"contractDoc": "Web\\.ai\\context\\frontend-handoff.md"` (the engine's default is Project B's
  `Web\.ai\architecture\api-surface-v3.md`). Nothing else changes.
- remove `rules.gateCommandsFromRepoDoc` (D4: the function it gated is gone).

## 4. `orchestrator.local.json`

Add it to `.gitignore` (it holds a machine path and is never committed), then create it:

```json
{
    "enginePath": "<the path the owner gave>"
}
```

## 5. The new `run.ps1`

Replace the whole file with exactly this:

```powershell
# Calls the shared engine in Orchestra with this project's orchestrator.json.
function Get-EnginePath {
    # The one place that knows where the engine is (plan 0005 D10).
    $local = Join-Path $PSScriptRoot 'orchestrator.local.json'
    if (-not (Test-Path -LiteralPath $local)) { return $null }
    $path = (Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json).enginePath
    if ($path -and (Test-Path -LiteralPath (Join-Path $path 'engine\run.ps1'))) { return $path }
    return $null
}
$engine = Get-EnginePath
if (-not $engine) {
    Write-Host 'Write the Orchestra checkout as "enginePath" in orchestrator\orchestrator.local.json (it must contain engine\run.ps1).' -ForegroundColor Red
    exit 1
}
if ($args -contains '-Config') {
    Write-Host 'Do not pass -Config: the shim always uses this project''s orchestrator.json.' -ForegroundColor Red
    exit 1
}
& (Join-Path $engine 'engine\run.ps1') -Config (Join-Path $PSScriptRoot 'orchestrator.json') @args
exit $LASTEXITCODE
```

## 6. Skills

In every `.claude/skills/*/SKILL.md` that names `orchestrator/CONDUCTOR.md`, `REVIEW-BRIEF.md` or
`WORKING-MODEL.md`, replace the path with
`<enginePath from orchestrator/orchestrator.local.json>/engine/docs/<file>`. Paths to `CLOSE-PLAN.md`
stay as they are.

## 7. Hand back

Do not commit, merge or push. Leave the tree uncommitted and tell the owner what changed. The owner
checks in this repo:

1. `powershell -File orchestrator/run.ps1 agents` — the same table as before, plus a line
   `engine: <enginePath>\engine`;
2. `powershell -File orchestrator/run.ps1 health` — unchanged;
3. `powershell -File orchestrator/run.ps1 new <taskId> <repo> "<one line>" -DryRun` — prompts built, no
   model call, nothing written into a repo;
4. `close` of a run whose plan is filed — it finds the filed plan (by number here, by slug in Project A).

If `agents` prints the red `Write the Orchestra checkout as "enginePath" …` line, the local file is
missing or its path has no `engine\run.ps1`; that is the designed message, not a bug.
