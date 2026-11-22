# Conventions for `engine/`

> Read this before changing anything under [`../../engine/`](../../engine/README.md).

## Windows PowerShell 5.1, not `pwsh`

The engine runs as `powershell -File run.ps1` on the owner's Windows machine. Its syntax is 5.1:

- no `??`, no `?.`, no ternary `? :`, no `-Parallel`, no `using` statements;
- no class/module syntax that only PowerShell 7 parses;
- `[string]::IsNullOrEmpty`, not a null-conditional;
- JSON in: `ConvertFrom-Json` (5.1 has no `-AsHashtable`); JSON out: `ConvertTo-Json -Depth`.

Check a file before you finish it:

```powershell
powershell -NoProfile -Command "$e=$null; [void][System.Management.Automation.Language.Parser]::ParseFile('engine/run.ps1',[ref]$null,[ref]$e); if ($e) { $e | % { $_.Extent.StartLineNumber.ToString() + ': ' + $_.Message } } else { 'parses' }"
```

## Behaviour-preserving

The engine was moved, not rewritten (plan 0005). A change to it is behaviour-preserving unless a plan's
decision says otherwise: same commands, same arguments, same exit codes, same files under `runs/`, same
ledger format, same prompt text, same verdict and finding rules. No rename, no reorder, no "while I am
here", no new dependency.

Files are edited by **anchor** — the exact line or block a plan quotes — never by line number.

## The two folders

- `$PSScriptRoot` (the engine's own folder) holds `lib/`, `refresh-graph.ps1`, `docs/`.
- `$Ctx.ConfigPath`'s folder holds `orchestrator.json` and, through `root`, the project's repos;
  `runs/` hangs off `runsDir` relative to that folder.

Never look for a project file next to `run.ps1` and never look for an engine file in the project's
folder: that separation is what lets one engine serve many projects.

## Reading settings

- `Get-CfgValue $Ctx '<section>' '<name>' <default>` for a key inside a section. It cannot read a
  top-level scalar (`planFiling`): read that from `$Ctx.Config.PSObject.Properties['<name>']`.
- Everything a project may differ in is a setting, not a branch on a project name. If code needs to know
  which project it runs in, the config is missing a key.

## Profiles

Plan 0006 adds one merge: when `orchestrator.json` names a `profile`, `Import-OrchConfig` reads
`<profilesPath>/<profile>.json` from the project's git-ignored `orchestrator.local.json` and merges the
export's `config` **under** the project's whole file — key by key and recursively, arrays replace, null
removes (`Merge-ConfigObject`). Every validation then runs on the effective config, and the six locked
workflow steps may not be turned off. The dashboard implements the same rule in `ConfigMerge.cs`; the
two move together. A project without a `profile` key behaves exactly as before.

## The offline suite is the test

```powershell
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

It needs no model, no network and no project: it builds temp folders with a mock agent. It takes 15+
minutes on Windows PowerShell 5.1 — run it in the background with its output in a log, wait for the
tally, and never stop it on a timeout. Its count never drops (see [`.ai/ci.md`](../ci.md)).

## Logs and secrets

- No token, no machine path, no personal path in an engine file. Paths come from the config; a secret
  comes from an environment variable named by the config.
- The engine never commits, merges, pushes or deletes anything in a repo. `close` deletes only the run's
  own folder and drafts under `runs/`.

## History is not a rule

`engine/docs/history/` is reference. A rule that still binds lives in this file, in
`engine/README.md`, or in an ADR under [`.ai/decisions/`](../decisions/README.md).
