# Offline end-to-end test for the orchestrator. No model is called: every role runs the mock agent
# through the generic "exec" adapter, in a throwaway workspace under %TEMP% with three git repos
# named Agent/Ui/Web. Nothing outside the temp folder is written.
#
#   powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1 [-Keep]
#
# Exit code 0 when every check passes, 1 otherwise.
#
# When to run it (owner's rule, 2026-11-14): a full run takes ~10 minutes, so
#   - only when the orchestrator's own code or config changes (lib/, run.ps1, orchestrator.json, tests/);
#     a real task run never runs this suite - it runs the target repo's gate commands instead;
#   - once at the END of an orchestrator change (and once as a baseline before it), never after every
#     phase or every edit; a doc-only change needs no run;
#   - a red check that passes on an immediate rerun (e.g. a timing-sensitive pool test) is flaky, not a
#     regression - rerun once, do not loop.
# Orchestrator plans must say "suite once at the end", not "green after each phase".
[CmdletBinding()]
param([switch]$Keep, [string]$Config)

$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
$Orch = Split-Path -Parent (Split-Path -Parent $Here)
$RunPs1 = Join-Path $Orch 'run.ps1'

# Plan 0005 file 5: the whole suite runs once per project config in configs/ (number and slug plan
# filing). Each pass is a child process, so its workspaces, its results and its exit code stay its own.
$configsDir = Join-Path $Here 'configs'
if (-not $Config) {
    $matrix = @(Get-ChildItem -LiteralPath $configsDir -Filter '*.orchestrator.json' -File | Sort-Object Name)
    if (-not $matrix.Count) { Write-Host "no config in $configsDir" -ForegroundColor Red; exit 1 }
    $failed = 0
    $passes = @()
    foreach ($c in $matrix) {
        Write-Host ''
        Write-Host ("=== offline suite with {0} ===" -f $c.Name) -ForegroundColor Cyan
        $childArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Config', $c.FullName)
        if ($Keep) { $childArgs += '-Keep' }
        $out = (& powershell @childArgs 2>&1 | Out-String)
        Write-Host $out
        $code = $LASTEXITCODE
        $m = [regex]::Match($out, '(\d+)/(\d+) checks passed')
        $passes += ("{0}: {1}" -f $c.Name, $(if ($m.Success) { $m.Value } elseif ($code -eq 0) { 'passed' } else { "exit $code" }))
        if ($code -ne 0 -or -not $m.Success) { $failed++ }
    }
    Write-Host ''
    Write-Host '=== engine offline suite ===' -ForegroundColor Cyan
    foreach ($p in $passes) { Write-Host "  $p" }
    if ($failed) { Write-Host "$failed of $($matrix.Count) config(s) did not pass" -ForegroundColor Red; exit 1 }
    Write-Host 'all configs passed' -ForegroundColor Green
    exit 0
}
$script:activeConfig = (Resolve-Path -LiteralPath $Config).Path
$script:activeConfigName = (Split-Path -Leaf $script:activeConfig)
$script:results = New-Object System.Collections.ArrayList
$script:failures = 0
$script:workspaces = New-Object System.Collections.ArrayList
# No network: live model prices come from a seeded cache file only (T-L2), never from opencode.ai.
$env:PROJECTA_ORCH_NO_RATE_FETCH = '1'

# The graph checks call the status/refresh functions directly, so the harness and the orchestrator
# agree on one definition instead of re-testing a copy.
. (Join-Path $Orch 'lib\agents.ps1')
. (Join-Path $Orch 'lib\pool.ps1')
. (Join-Path $Orch 'lib\gates.ps1')
# The harness calls Write-ProgressFile and the pool functions directly, and those report their own events
# (plan 0004 §6.6). Sourcing the reporter keeps them defined; without a report block each call is a no-op.
. (Join-Path $Orch 'lib\report.ps1')

function Check {
    param([string]$Id, [string]$Name, [bool]$Ok, [string]$Detail = '')
    [void]$script:results.Add([pscustomobject]@{
            Id     = $Id
            Check  = $Name
            Result = $(if ($Ok) { 'PASS' } else { 'FAIL' })
            Detail = $Detail
        })
    if (-not $Ok) { $script:failures++ }
}

function Fail-Scenario {
    param([string]$Id, [string]$Name, [string]$Why)
    Check $Id $Name $false $Why
}

# Never name this "Git": a function of that name shadows git.exe for every `& git` in this script
# (including the workspace setup), and the calls recurse into the helper instead of running git.
function Get-GitText {
    param([string]$Repo, [string[]]$Argv)
    return ((& git -C $Repo @Argv 2>&1) -join "`n").Trim()
}

function New-WorkSpace {
    param([string]$Tag)
    $dir = Join-Path $env:TEMP ("project-b-orch-$Tag-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Path $dir | Out-Null
    [void]$script:workspaces.Add($dir)
    Write-Host "  workspace $dir" -ForegroundColor DarkGray
    foreach ($r in @('Agent', 'Ui', 'Web')) {
        $rp = Join-Path $dir $r
        New-Item -ItemType Directory -Path $rp | Out-Null
        [void](& git -C $rp init -q 2>&1)
        [void](& git -C $rp checkout -q -b work 2>&1)
        Set-Content -LiteralPath (Join-Path $rp 'README.md') -Value "# $r" -Encoding UTF8
        # Every real repo git-ignores graphify-out/; the sidecar depends on that, so the fixture has it
        # too (T7 removes it deliberately).
        Set-Content -LiteralPath (Join-Path $rp '.gitignore') -Value 'graphify-out/' -NoNewline -Encoding ASCII
        [void](& git -C $rp config user.name 'alireza' 2>&1)
        [void](& git -C $rp config user.email 'alireza@example.com' 2>&1)
        [void](& git -C $rp add -A 2>&1)
        [void](& git -C $rp commit -q -m 'init' 2>&1)
    }
    Copy-Item -LiteralPath (Join-Path $Here 'mock-agent.ps1') -Destination $dir

    $mock = Join-Path $dir 'mock-agent.ps1'
    # The pass's project config (configs/*.orchestrator.json) is this workspace's config; only the mock
    # agent's path is per-workspace. Roles, filing style and description come from that file.
    $config = Get-Content -LiteralPath $script:activeConfig -Raw -Encoding UTF8 | ConvertFrom-Json
    $config.adapters.mock.args = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $mock)
    ($config | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath (Join-Path $dir 'config.json') -Encoding UTF8
    Write-Host '  workspace ready: 3 repos, mock agent, config' -ForegroundColor DarkGray
    return $dir
}

function Run-Orch {
    param(
        [string]$Ws,
        [string[]]$Argv,
        [string]$Mode = '',
        [string]$PathPrepend = '',
        [string]$PromptDir = ''
    )
    $log = Join-Path $Ws 'mock-calls.log'
    $oldMode = $env:MOCK_MODE
    $oldLog = $env:MOCK_LOG
    $oldPath = $env:PATH
    $oldPromptDir = $env:MOCK_PROMPT_DIR
    $env:MOCK_MODE = $Mode
    $env:MOCK_LOG = $log
    $env:MOCK_PROMPT_DIR = $PromptDir
    if ($PathPrepend) { $env:PATH = "$PathPrepend;$oldPath" }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
        $all = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $RunPs1) + $Argv + @('-Config', (Join-Path $Ws 'config.json'))
        $out = (& powershell @all 2>&1 | Out-String)
        $code = $LASTEXITCODE
        Write-Host ("  run.ps1 {0} -> exit {1} ({2:n1}s)" -f ($Argv -join ' '), $code, $sw.Elapsed.TotalSeconds) -ForegroundColor DarkGray
    }
    finally {
        $env:MOCK_MODE = $oldMode
        $env:MOCK_LOG = $oldLog
        $env:PATH = $oldPath
        $env:MOCK_PROMPT_DIR = $oldPromptDir
    }
    return [pscustomobject]@{ Code = $code; Out = $out; Log = $log }
}

function New-WriteGraph {
    # A minimal graph.json with an optional LLM-extracted ("semantic") node, dated $MinutesOld.
    param([string]$Repo, [string]$SemanticId = '', [int]$MinutesOld = -10)
    $dir = Join-Path $Repo 'graphify-out'
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $nodes = if ($SemanticId) { '{"id":"' + $SemanticId + '","_origin":"semantic"}' } else { '' }
    $path = Join-Path $dir 'graph.json'
    Set-Content -LiteralPath $path -Value ('{"nodes":[' + $nodes + '],"edges":[]}') -Encoding UTF8
    (Get-Item -LiteralPath $path).LastWriteTime = (Get-Date).AddMinutes($MinutesOld)
    return $path
}

function New-FakeGraphify {
    # A graphify stand-in placed first on PATH. The real tool cannot be made to emit a warning or to
    # leave graph.json untouched on demand, and those are exactly the refresh cases under test.
    param([string]$Dir, [ValidateSet('quiet', 'warn', 'fail')][string]$Mode = 'quiet')
    New-Item -ItemType Directory -Path $Dir -Force | Out-Null
    $body = @'
# -Out is declared so that graphify's own "--out" binds to it: undeclared, PowerShell reads
# "--out" as -out and refuses it as ambiguous with -OutVariable/-OutBuffer.
param(
    [Parameter(Position = 0)][string]$Command,
    [Parameter(Position = 1, ValueFromRemainingArguments = $true)][string[]]$Rest,
    [string]$Out
)
$Mode = '__MODE__'
if ($Command -eq 'merge-graphs') {
    if ($Out) {
        New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
        Set-Content -LiteralPath $Out -Value '{"nodes":[],"edges":[]}' -Encoding UTF8
    }
    if ($Mode -eq 'warn') { [Console]::Error.WriteLine('WARNING: tree_sitter_sql not installed') }
    exit 0
}
switch ($Mode) {
    'warn' { [Console]::Error.WriteLine('WARNING: tree_sitter_sql not installed'); exit 0 }
    'fail' { [Console]::Error.WriteLine('error: fake graphify failure'); exit 1 }
    default { exit 0 }
}
'@
    Set-Content -LiteralPath (Join-Path $Dir 'fake-graphify.ps1') -Value $body.Replace('__MODE__', $Mode) -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $Dir 'graphify.cmd') -Value '@powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0fake-graphify.ps1" %*' -Encoding ASCII
    return $Dir
}

function Invoke-Refresh {
    # -Command, not -File: a -File argument list cannot express a [string[]] parameter ("-Repos Ui Web"
    # binds Web to the next parameter instead). The real caller invokes the script in-process.
    param([string]$Ws, [string[]]$Repos = @(), [switch]$MergeRoot, [string]$PathPrepend = '')
    $cmd = "& '" + (Join-Path $Orch 'refresh-graph.ps1') + "' -Repos @('" + ($Repos -join "','") + "') -Config '" + (Join-Path $Ws 'config.json') + "'"
    if ($MergeRoot) { $cmd += ' -MergeRoot' }
    $oldPath = $env:PATH
    if ($PathPrepend) { $env:PATH = "$PathPrepend;$oldPath" }
    try {
        $out = (& powershell -NoProfile -ExecutionPolicy Bypass -Command $cmd 2>&1 | Out-String)
        $code = $LASTEXITCODE
    }
    finally { $env:PATH = $oldPath }
    Write-Host ("  refresh-graph.ps1 -Repos {0} -> exit {1}" -f ($Repos -join ','), $code) -ForegroundColor DarkGray
    return [pscustomobject]@{ Code = $code; Out = $out }
}

function Get-State {
    param([string]$Ws, [string]$Id)
    $f = Join-Path $Ws "runs\$Id\task.json"
    if (-not (Test-Path -LiteralPath $f)) { throw "no task.json for $Id" }
    return (Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Write-Plan {
    param([string]$Ws, [string]$Name = 'plan.md', [string]$PlannedBy = 'Claude Opus 5.5 (test seat), 2026-01-01')
    $path = Join-Path $Ws $Name
    $by = if ($PlannedBy) { "**Planned by:** $PlannedBy

" } else { '' }
    $by + @'
# Plan — mock demo task

Baseline: the orchestrator creates the branch; nothing is committed.

## File manifest
- `phase1.txt` (NEW) — one line of text.
- `phase2.txt` (NEW) — one line of text.

## Phase 1
Create `phase1.txt` containing `one`.

## Phase 2
Create `phase2.txt` containing `two`.

## Definition of done
The orchestrator's gate command is green and `phase1.txt` exists.
'@ | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function Invoke-New {
    param([string]$Ws, [string]$Id, [string]$Repo, [string]$Mode = '')
    $plan = Write-Plan $Ws "plan-$Id.md"
    return (Run-Orch -Ws $Ws -Argv @('new', $Id, $Repo, 'mock demo task', '-PlanFile', $plan) -Mode $Mode)
}

function Invoke-Approve {
    param([string]$Ws, [string]$Id, [string]$Notes = '')
    $argv = @('approve', $Id)
    if ($Notes) { $argv += $Notes }
    return (Run-Orch -Ws $Ws -Argv $argv)
}

function Set-ManualChain {
    # Replaces a role's chain with the manual adapter: the hop then defers and a human answers (D7).
    param([string]$Ws, [string]$Role)
    $f = Join-Path $Ws 'config.json'
    $cfg = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $cfg.adapters.PSObject.Properties['manual']) {
        $cfg.adapters | Add-Member -NotePropertyName 'manual' -NotePropertyValue ([pscustomobject]@{ enabled = $true; command = $null }) -Force
    }
    $hop = [pscustomobject]@{ adapter = 'manual'; model = 'human'; family = 'humanfam' }
    $cfg.roles.$Role.chain = @($hop)
    ($cfg | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $f -Encoding UTF8
}

function Set-InboxAdapterChain {
    # Like Set-ManualChain, but through an adapter of any name whose config says `transport: inbox`
    # (a desktop app, say): the transport, not the adapter's name, decides that a human answers.
    param([string]$Ws, [string]$Role, [string]$Transport = 'inbox')
    $f = Join-Path $Ws 'config.json'
    $cfg = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json
    $cfg.adapters | Add-Member -NotePropertyName 'deskapp' -NotePropertyValue ([pscustomobject]@{ enabled = $true; transport = $Transport; command = $null }) -Force
    $hop = [pscustomobject]@{ adapter = 'deskapp'; model = 'deskapp/some-model'; family = 'deskfam' }
    $cfg.roles.$Role.chain = @($hop)
    ($cfg | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $f -Encoding UTF8
}

function Set-ManualReadBack {
    # Gives the config its own `readback` role, answered by hand (Freebuff in the real config); the
    # implementer chain stays the mock.
    param([string]$Ws)
    $f = Join-Path $Ws 'config.json'
    $cfg = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $cfg.adapters.PSObject.Properties['manual']) {
        $cfg.adapters | Add-Member -NotePropertyName 'manual' -NotePropertyValue ([pscustomobject]@{ enabled = $true; command = $null }) -Force
    }
    $role = [pscustomobject]@{ gate = 2.5; readOnly = $true; auto = $false; promptFile = 'implement.md'
        chain = @([pscustomobject]@{ adapter = 'manual'; model = 'freebuff'; family = 'deepseek' }) }
    $cfg.roles | Add-Member -NotePropertyName 'readback' -NotePropertyValue $role -Force
    ($cfg | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $f -Encoding UTF8
}

function Get-InboxKeys {
    param([string]$Ws, [string]$Id)
    $dir = Join-Path $Ws "runs\$Id\inbox"
    if (-not (Test-Path -LiteralPath $dir)) { return @() }
    return @(Get-ChildItem -LiteralPath $dir -Filter '*.md' | ForEach-Object { $_.BaseName } | Sort-Object)
}

function Count-MockCalls {
    param([string]$Log, [string]$Role, [bool]$ReadBack)
    if (-not (Test-Path -LiteralPath $Log)) { return 0 }
    $lines = @(Get-Content -LiteralPath $Log)
    $needle = "$Role`t"
    return @($lines | Where-Object { $_ -like "$needle*" -and ($_ -split "`t")[2] -eq "$ReadBack" }).Count
}

# ---------------------------------------------------------------- quota-pool helpers (P cases)
# A pool is a file lock plus config, so a case needs three things the older cases never did: a second
# PROCESS (a run.ps1 command or a holder that dot-sources lib\pool.ps1 and keeps a slot), a way to say
# what a tier means in its own workspace, and the time a call happened.
function Set-CfgProp {
    param($Obj, [string]$Name, $Value)
    if ($Obj.PSObject.Properties[$Name]) { $Obj.$Name = $Value }
    else { $Obj | Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force }
}

function Edit-Config {
    # Rewrite one workspace's config.json through a scriptblock: pools are a config surface, so each case
    # says what it changes instead of hand-editing JSON.
    param([string]$Ws, [scriptblock]$Change)
    $f = Join-Path $Ws 'config.json'
    $cfg = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json
    & $Change $cfg
    ($cfg | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $f -Encoding UTF8
}

function Set-Tiers {
    # Replaces the whole `tiers` block, so a case states exactly what free/paid mean for it.
    param([string]$Ws, [hashtable]$Free, [hashtable]$Paid)
    $block = [ordered]@{}
    if ($Free) { $block['free'] = $Free }
    if ($Paid) { $block['paid'] = $Paid }
    Edit-Config -Ws $Ws -Change { param($cfg) Set-CfgProp $cfg 'tiers' ([pscustomobject]$block) }.GetNewClosure()
}

function Add-SecondAdapter {
    # A copy of the mock adapter under another pool, so a chain can name two quotas.
    param([string]$Ws, [string]$Pool, [string]$Tier = 'free')
    Edit-Config -Ws $Ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters 'mock2' ([pscustomobject]@{ enabled = $true; type = 'exec'; command = 'powershell'; args = @($cfg.adapters.mock.args); pool = $Pool; tier = $Tier })
    }.GetNewClosure()
}

function New-PoolHolderScript {
    # A second caller that is only a slot holder: it dot-sources lib\pool.ps1, takes the slot of one
    # adapter's pool, holds it for -Seconds and lets go. Every step is stamped so a case can read the
    # order of events across two processes.
    param([string]$Ws)
    $path = Join-Path $Ws 'pool-holder.ps1'
    @'
param(
    [Parameter(Mandatory)][string]$OrchDir,
    [Parameter(Mandatory)][string]$Config,
    [string]$Adapter = 'mock',
    [string]$Model = 'mock-planner',
    [string]$Seconds = '3',
    [string]$TaskId = 'HOLDER',
    [string]$Role = 'holder',
    [string]$Events = ''
)
$ErrorActionPreference = 'Stop'
. (Join-Path $OrchDir 'lib\agents.ps1')
. (Join-Path $OrchDir 'lib\pool.ps1')
function Write-Event {
    param([string]$Verb)
    if ($Events) { Add-Content -LiteralPath $Events -Value ("{0}`t{1}`t{2}" -f (Get-Date).Ticks, $Verb, $TaskId) }
}
$ctx = Import-OrchConfig $Config
$hop = [pscustomobject]@{ adapter = $Adapter; model = $Model }
$limits = Get-HopLimits $ctx $hop
$slot = Enter-PoolSlot -Ctx $ctx -Limits $limits -Meta @{ taskId = $TaskId; role = $Role; adapter = $Adapter; model = $Model }
# 'unlimited' is a real acquisition too (maxConcurrent 0: nothing to hold, nothing to release).
if (@('acquired', 'unlimited') -notcontains $slot.Status) { Write-Event 'fail'; exit 2 }
try {
    Write-Event 'start'
    Start-Sleep -Milliseconds ([int]([double]$Seconds * 1000))
}
finally {
    Exit-PoolSlot $slot.Handle
    # After the release: "end" is the moment the slot became free again.
    Write-Event 'end'
}
exit 0
'@ | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function Start-PoolHolder {
    param([string]$Ws, [double]$Seconds = 3, [string]$TaskId = 'HOLDER', [string]$Adapter = 'mock', [string]$Role = 'holder', [string]$Tag = 'holder', [string]$Events = '')
    $holderScript = Join-Path $Ws 'pool-holder.ps1'
    if (-not (Test-Path -LiteralPath $holderScript)) { [void](New-PoolHolderScript $Ws) }
    if (-not $Events) { $Events = Join-Path $Ws "events-$Tag.log" }
    $sec = $Seconds.ToString('0.###', [System.Globalization.CultureInfo]::InvariantCulture)
    $argv = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -OrchDir "{1}" -Config "{2}" -Seconds {3} -TaskId "{4}" -Adapter "{5}" -Role "{6}" -Events "{7}"' -f $holderScript, $Orch, (Join-Path $Ws 'config.json'), $sec, $TaskId, $Adapter, $Role, $Events
    $p = Start-Process -FilePath 'powershell' -ArgumentList $argv -PassThru `
        -RedirectStandardOutput (Join-Path $Ws "$Tag.out") -RedirectStandardError (Join-Path $Ws "$Tag.err")
    return [pscustomobject]@{ Proc = $p; Out = (Join-Path $Ws "$Tag.out"); Events = $Events }
}

function Stop-PoolHolder {
    param($Holder)
    if ($Holder -and $Holder.Proc -and -not $Holder.Proc.HasExited) {
        Stop-Process -Id $Holder.Proc.Id -Force -ErrorAction SilentlyContinue
        [void]$Holder.Proc.WaitForExit(10000)
    }
}

function Get-Events {
    # <ticks> <verb> <taskId> per line, one file per holder.
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return @() }
    return @(Get-Content -LiteralPath $Path | Where-Object { $_.Trim() } | ForEach-Object {
            $p = $_ -split "`t"
            [pscustomobject]@{ Ticks = [long]$p[0]; Verb = $p[1]; TaskId = $p[2] }
        })
}

function Wait-Events {
    param([string]$Path, [string]$Verb = 'start', [int]$TimeoutSec = 20)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (@(Get-Events $Path | Where-Object { $_.Verb -eq $Verb }).Count) { return $true }
        Start-Sleep -Milliseconds 100
    }
    return $false
}

function Get-MockCalls {
    # Every mock call with its time: role, model, isReadBack, mode, ticks.
    param([string]$Log)
    if (-not (Test-Path -LiteralPath $Log)) { return @() }
    return @(Get-Content -LiteralPath $Log | Where-Object { $_.Trim() } | ForEach-Object {
            $p = $_ -split "`t"
            [pscustomobject]@{
                Role     = $p[0]
                Model    = $p[1]
                ReadBack = $p[2]
                Mode     = $p[3]
                Ticks    = $(if ($p.Count -ge 5 -and $p[4]) { [long]$p[4] } else { 0 })
            }
        })
}

function Enable-MockSessions {
    # D5/D7: make the mock exec adapter session-capable (sessions:true) so its hops mint and continue
    # session ids. The real opencode adapter is session-capable by name; the mock needs the flag.
    param([string]$Ws)
    Edit-Config -Ws $Ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'sessions' $true }
}

function Get-CapturedPrompts {
    # The prompts the mock saved under MOCK_PROMPT_DIR, in call order, optionally for one role. Each item
    # is { Order, Session, Role, Text }. Filename: <nnnn>-<sessionId>-<role>.md (session id has no hyphen).
    param([string]$Dir, [string]$Role = '')
    if (-not (Test-Path -LiteralPath $Dir)) { return @() }
    $files = @(Get-ChildItem -LiteralPath $Dir -Filter '*.md' -File | Sort-Object Name)
    $out = @()
    foreach ($f in $files) {
        $parts = $f.BaseName -split '-', 3
        if ($parts.Count -lt 3) { continue }
        if ($Role -and $parts[2] -ne $Role) { continue }
        $out += [pscustomobject]@{ Order = [int]$parts[0]; Session = $parts[1]; Role = $parts[2]; Text = (Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8) }
    }
    return $out
}

function Start-Orch {
    # run.ps1 in its own process, so a second caller can run beside it. -Wait runs inside the job: only
    # then does the returned Process object carry an ExitCode.
    param([string]$Ws, [string[]]$Argv, [string]$Tag = 'bg', [string]$Mode = '')
    $out = Join-Path $Ws "$Tag.out"
    $err = Join-Path $Ws "$Tag.err"
    $quoted = ($Argv | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
    $line = '-NoProfile -ExecutionPolicy Bypass -File "{0}" {1} -Config "{2}"' -f $RunPs1, $quoted, (Join-Path $Ws 'config.json')
    $poll = [string]$env:ORCH_POOL_POLL_MS
    $job = Start-Job -ScriptBlock {
        param($line, $out, $err, $log, $Mode, $poll)
        $env:MOCK_MODE = $Mode
        $env:MOCK_LOG = $log
        $env:ORCH_POOL_POLL_MS = $poll
        $p = Start-Process -FilePath 'powershell' -ArgumentList $line -PassThru -Wait -RedirectStandardOutput $out -RedirectStandardError $err
        $p.ExitCode
    } -ArgumentList $line, $out, $err, (Join-Path $Ws 'mock-calls.log'), $Mode, $poll
    return [pscustomobject]@{ Job = $job; Out = $out; Err = $err; Log = (Join-Path $Ws 'mock-calls.log') }
}

function Complete-Orch {
    param($Bg, [int]$TimeoutSec = 120)
    $finished = $null -ne (Wait-Job $Bg.Job -Timeout $TimeoutSec)
    $code = if ($finished) { @(Receive-Job $Bg.Job)[-1] } else { -999 }
    if (-not $finished) { Stop-Job $Bg.Job }
    Remove-Job $Bg.Job -Force
    $text = if (Test-Path -LiteralPath $Bg.Out) { Get-Content -LiteralPath $Bg.Out -Raw } else { '' }
    return [pscustomobject]@{ Code = $code; Out = $text; Done = $finished }
}

function Wait-QueuedProgress {
    # The run's progress.json once its hop says it is waiting for a pool slot (D10).
    param([string]$Path, [int]$TimeoutSec = 20)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            try {
                $j = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
                if ($j -and $j.phase -eq 'queued') { return $j }
            }
            catch { }
        }
        Start-Sleep -Milliseconds 100
    }
    return $null
}

Write-Host "offline orchestrator test" -ForegroundColor Cyan
Write-Host "  run.ps1: $RunPs1"

# ---------------------------------------------------------------- S1/S2/S4/S5/S6/S7/S13: full conductor run
try {
    $ws = New-WorkSpace 'main'
    $r = Invoke-New $ws 'T-P1-001' 'Ui' 'findings'
    $s = Get-State $ws 'T-P1-001'
    Check 'S1' 'conductor plan enters at gate 2 as conductor:chat' `
        ($r.Code -eq 0 -and $s.plan.adapter -eq 'conductor' -and $s.plan.family -eq 'claude-opus' -and $s.status -eq 'AwaitingApproval') `
        ("code=$($r.Code) adapter=$($s.plan.adapter)/$($s.plan.family) status=$($s.status)")
    Check 'S1' 'review FINDINGS stops before read-back' `
        ($s.review.verdict -eq 'FINDINGS' -and -not $s.PSObject.Properties['readback'] -and -not $s.branch) `
        ("review=$($s.review.verdict) readback=$(if ($s.PSObject.Properties['readback']) { 'present' } else { 'absent' })")

    $plan2 = Write-Plan $ws 'plan.v2.md'
    $r = Run-Orch -Ws $ws -Argv @('revise', 'T-P1-001', '-PlanFile', $plan2)
    $s = Get-State $ws 'T-P1-001'
    Check 'S2' 'revise -PlanFile records v2 and reviews it' `
        ($r.Code -eq 0 -and $s.round -eq 2 -and $s.plan.adapter -eq 'conductor' -and $s.review.round -eq 2 -and $s.review.verdict -eq 'PASS') `
        ("round=$($s.round) review=$($s.review.verdict)/v$($s.review.round)")
    Check 'S2' 'read-back (gate 2.5) runs after a PASS review' `
        ($s.PSObject.Properties['readback'] -and $s.readback.verdict -eq 'PASS' -and $s.readback.round -eq 2) `
        ("readback=$(if ($s.PSObject.Properties['readback']) { "$($s.readback.verdict)/v$($s.readback.round)" } else { 'absent' })")

    $r = Invoke-Approve $ws 'T-P1-001' 'keep it small'
    $s = Get-State $ws 'T-P1-001'
    $artifact = $s.approved.artifact
    $sha = if ($artifact -and (Test-Path -LiteralPath $artifact)) { (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash } else { '' }
    $amend = if ($artifact -and (Test-Path -LiteralPath $artifact)) { (Get-Content -LiteralPath $artifact -Raw) -match 'APPROVED AMENDMENTS' } else { $false }
    Check 'S4' 'approve freezes plan.approved.md with notes and sha256' `
        ($r.Code -eq 0 -and $s.status -eq 'Approved' -and $amend -and $sha -eq $s.approved.sha256 -and $s.branch -eq 'alireza/t-p1-001') `
        ("code=$($r.Code) status=$($s.status) branch=$($s.branch) sha=$($sha.Substring(0, [Math]::Min(8, $sha.Length)))")

    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P1-001', '1')
    $s = Get-State $ws 'T-P1-001'
    $cur = Get-GitText (Join-Path $ws 'Ui') @('branch', '--show-current')
    Check 'S5' 'implement creates the branch and records the phase' `
        ($r.Code -eq 0 -and $s.status -eq 'Implemented' -and $s.baseSha -and $cur -eq $s.branch -and @($s.phases).Count -eq 1) `
        ("code=$($r.Code) status=$($s.status) branch=$cur base=$($s.baseSha)")
    Check 'S5' 'changed files are the ones the plan names (no outside-plan warning)' `
        (@($s.phases)[0].changed -contains 'phase1.txt' -and @($s.phases)[0].outsidePlan.Count -eq 0 -and @($s.phases)[0].violations.Count -eq 0) `
        ("changed=$( @(@($s.phases)[0].changed) -join ',' ) outside=$( @(@($s.phases)[0].outsidePlan) -join ',' )")

    $r = Run-Orch -Ws $ws -Argv @('verify', 'T-P1-001')
    $s = Get-State $ws 'T-P1-001'
    Check 'S6' 'verify with green gates ends Verified/PASS' `
        ($r.Code -eq 0 -and $s.status -eq 'Verified' -and $s.verify.verdict -eq 'PASS' -and $s.verify.gates -eq 'green') `
        ("status=$($s.status) verdict=$($s.verify.verdict) gates=$($s.verify.gates)")

    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P1-001', '2')
    $s = Get-State $ws 'T-P1-001'
    $cur2 = Get-GitText (Join-Path $ws 'Ui') @('branch', '--show-current')
    Check 'S7' 'phase 2 runs after Verified on the same branch' `
        ($r.Code -eq 0 -and $s.status -eq 'Implemented' -and @($s.phases).Count -eq 2 -and $cur2 -eq $s.branch) `
        ("status=$($s.status) phases=$(@($s.phases).Count) branch=$cur2")

    Add-Content -LiteralPath $artifact -Value "`nTAMPERED"
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P1-001', '2')
    Check 'S13' 'a tampered plan.approved.md is refused' `
        ($r.Code -eq 1 -and $r.Out -match 'plan\.approved\.md changed after approval') `
        ("code=$($r.Code) msg=$(($r.Out -split "`n" | Where-Object { $_ -match 'ERROR' } | Select-Object -First 1))")
}
catch { Fail-Scenario 'S1-13' 'full conductor run' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S3: approve refused with open findings
try {
    $ws = New-WorkSpace 'readback'
    $r = Invoke-New $ws 'T-P3-001' 'Ui' 'readback-findings'
    $s = Get-State $ws 'T-P3-001'
    Check 'S3' 'read-back FINDINGS leave the run awaiting approval' `
        ($r.Code -eq 0 -and $s.status -eq 'AwaitingApproval' -and $s.review.verdict -eq 'PASS' -and $s.readback.verdict -eq 'FINDINGS') `
        ("status=$($s.status) review=$($s.review.verdict) readback=$($s.readback.verdict)")
    $r = Invoke-Approve $ws 'T-P3-001'
    $s = Get-State $ws 'T-P3-001'
    Check 'S3' 'approve without notes is refused while findings are open' `
        ($r.Code -eq 1 -and $r.Out -match 'open findings' -and $s.status -eq 'AwaitingApproval' -and -not $s.approved) `
        ("code=$($r.Code) status=$($s.status)")
}
catch { Fail-Scenario 'S3' 'approve with open findings' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S8: dirty tree refused
try {
    $ws = New-WorkSpace 'dirty'
    [void](Invoke-New $ws 'T-P8-001' 'Ui')
    [void](Invoke-Approve $ws 'T-P8-001')
    Add-Content -LiteralPath (Join-Path $ws 'Ui\README.md') -Value 'uncommitted edit'
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P8-001', '1')
    $s = Get-State $ws 'T-P8-001'
    $branch = Get-GitText (Join-Path $ws 'Ui') @('branch', '--show-current')
    Check 'S8' 'gate 3 refuses a dirty tree and creates no branch' `
        ($r.Code -eq 1 -and $r.Out -match 'uncommitted changes' -and $s.status -eq 'Approved' -and $branch -eq 'work') `
        ("code=$($r.Code) status=$($s.status) branch=$branch")
}
catch { Fail-Scenario 'S8' 'dirty tree guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S9: read-only role that writes
try {
    $ws = New-WorkSpace 'readonly'
    $r = Invoke-New $ws 'T-P9-001' 'Ui' 'write-on-review'
    $s = Get-State $ws 'T-P9-001'
    $wrote = Test-Path -LiteralPath (Join-Path $ws 'Ui\review-wrote.txt')
    Check 'S9' 'a read-only role that edits the repo is stopped' `
        ($r.Code -eq 1 -and $r.Out -match 'read-only role' -and $wrote -and $s.status -eq 'Planning' -and -not $s.review) `
        ("code=$($r.Code) wrote=$wrote status=$($s.status)")
}
catch { Fail-Scenario 'S9' 'read-only write guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S10: write role fails after editing
try {
    $ws = New-WorkSpace 'halfedit'
    [void](Invoke-New $ws 'T-P10-001' 'Ui')
    [void](Invoke-Approve $ws 'T-P10-001')
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P10-001', '1') -Mode 'edit-then-fail'
    $s = Get-State $ws 'T-P10-001'
    $calls = Count-MockCalls -Log $r.Log -Role 'implementer' -ReadBack $false
    Check 'S10' 'a write role failing after an edit gets no fallback' `
        ($r.Code -eq 1 -and $r.Out -match 'No fallback' -and $calls -eq 1 -and $s.status -eq 'Approved') `
        ("code=$($r.Code) implementerCalls=$calls status=$($s.status)")
}
catch { Fail-Scenario 'S10' 'write-fail guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S11: sibling repo changed
try {
    $ws = New-WorkSpace 'sibling'
    [void](Invoke-New $ws 'T-P11-001' 'Ui')
    [void](Invoke-Approve $ws 'T-P11-001')
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P11-001', '1') -Mode 'touch-sibling'
    $s = Get-State $ws 'T-P11-001'
    $viol = @(@($s.phases)[0].violations) -join '; '
    # A sibling checkout moving is another run's or the owner's work: a note, never a Violation.
    Check 'S11' 'a sibling-repo change during implement is a note, not a Violation' `
        ($s.status -eq 'Implemented' -and -not $viol -and $r.Out -match 'note: Web changed meanwhile' -and (Test-Path -LiteralPath (Join-Path $ws 'Web\sibling.txt'))) `
        ("status=$($s.status) violations=$viol note=$($r.Out -match 'note: Web changed meanwhile')")
}
catch { Fail-Scenario 'S11' 'sibling guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S12: commit made
try {
    $ws = New-WorkSpace 'commit'
    [void](Invoke-New $ws 'T-P12-001' 'Ui')
    [void](Invoke-Approve $ws 'T-P12-001')
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-P12-001', '1') -Mode 'commit'
    $s = Get-State $ws 'T-P12-001'
    $viol = @(@($s.phases)[0].violations) -join '; '
    Check 'S12' 'a commit made by the worker is a Violation' `
        ($s.status -eq 'Violation' -and $viol -match '\d+ commit\(s\) made') `
        ("status=$($s.status) violations=$viol")
}
catch { Fail-Scenario 'S12' 'commit guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S14: red gate fails the verify
try {
    $ws = New-WorkSpace 'redgate'
    [void](Invoke-New $ws 'T-P14-001' 'Ui')
    [void](Invoke-Approve $ws 'T-P14-001')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-P14-001', '1'))
    Set-Content -LiteralPath (Join-Path $ws 'Ui\.verify-fail') -Value '' -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('verify', 'T-P14-001')
    $s = Get-State $ws 'T-P14-001'
    Check 'S14' 'a red gate command fails the verify whatever the model says' `
        ($r.Code -eq 0 -and $s.verify.verdict -eq 'FAIL' -and $s.status -eq 'VerifyFailed' -and $s.verify.gates -match '^red:') `
        ("verdict=$($s.verify.verdict) status=$($s.status) gates=$($s.verify.gates)")
}
catch { Fail-Scenario 'S14' 'red gate' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S15: free model-name check
try {
    . (Join-Path $Orch 'lib\agents.ps1')
    $ctx = Import-OrchConfig (Join-Path $Orch 'examples\orchestrator.json')
    $rows = @(Test-ModelNames -Ctx $ctx)
    $unknown = @($rows | Where-Object { $_.Name -eq 'UNKNOWN MODEL' })
    # The chains' opencode hops = 6 of the 16 configured (planner 0, plan-reviewer 2, implementer 1,
    # readback 1, verifier 2). A chain that grows changes this count: keep it equal to the real config
    # rather than freezing a number, or this check fails for a config edit it has nothing to do with.
    $openCode = @($rows | Where-Object { $_.Adapter -eq 'opencode' })
    $badOpenCode = @($openCode | Where-Object { $_.Name -ne 'ok' })
    $hops = @($ctx.Config.roles.PSObject.Properties.Name | ForEach-Object { @($ctx.Config.roles.$_.chain) })
    Check 'S15' 'every configured opencode model name resolves (free check)' `
        ($unknown.Count -eq 0 -and $badOpenCode.Count -eq 0 -and $openCode.Count -eq 9 -and $rows.Count -eq $hops.Count) `
        ("rows=$($rows.Count) opencode=$($openCode.Count) unknown=$($unknown.Count) badOpenCode=$(($badOpenCode | ForEach-Object { "$($_.Model)=$($_.Name)" }) -join ',')")
}
catch { Fail-Scenario 'S15' 'Test-ModelNames' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S16-S18: free graph status
#   Ui: graph older than HEAD (stale) | Web: newer (fresh, with a semantic node) | Agent: no graph
try {
    $ws = New-WorkSpace 'graph'
    $uiDir = Join-Path $ws 'Ui\graphify-out'
    New-Item -ItemType Directory -Path $uiDir -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $uiDir 'graph.json') -Value '{"nodes":[]}' -Encoding UTF8
    (Get-Item -LiteralPath (Join-Path $uiDir 'graph.json')).LastWriteTime = (Get-Date).AddDays(-15)
    $webDir = Join-Path $ws 'Web\graphify-out'
    New-Item -ItemType Directory -Path $webDir -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $webDir 'graph.json') -Value '{"nodes":[{"_origin":"semantic"}]}' -Encoding UTF8
    (Get-Item -LiteralPath (Join-Path $webDir 'graph.json')).LastWriteTime = (Get-Date).AddMinutes(5)

    $r = Run-Orch -Ws $ws -Argv @('graph', '-Check')
    $uiStale = $r.Out -match '(?m)^Ui\s+stale\b'
    $webFresh = $r.Out -match '(?m)^Web\s+fresh\b'
    $agentMissing = $r.Out -match '(?m)^Agent\s+missing\b'
    $semanticYes = $r.Out -match '(?m)^Web\s+fresh\b.*\byes\b'
    Check 'S16' 'graph -Check reports stale / fresh / missing and the semantic flag, writing nothing' `
        ($r.Code -eq 0 -and $uiStale -and $webFresh -and $agentMissing -and $semanticYes -and -not (Test-Path -LiteralPath (Join-Path $ws 'Agent\graphify-out'))) `
        ("code=$($r.Code) stale=$uiStale fresh=$webFresh missing=$agentMissing semantic=$semanticYes")

    $r = Run-Orch -Ws $ws -Argv @('graph', '-Check', 'Ui')
    Check 'S17' 'graph -Check <repo> reports only that repo' `
        ($r.Code -eq 0 -and $r.Out -match '(?m)^Ui\s+stale\b' -and $r.Out -notmatch '(?m)^Web\s' -and $r.Out -notmatch '(?m)^Agent\s') `
        ("code=$($r.Code)")

    $r = Run-Orch -Ws $ws -Argv @('graph', 'Bogus')
    Check 'S18' 'graph with an unknown repo is refused' `
        ($r.Code -eq 1 -and $r.Out -match "unknown repo 'Bogus'") `
        ("code=$($r.Code)")
}
catch { Fail-Scenario 'S16-18' 'graph status' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- S19: graph <repo> refreshes only it
try {
    $ws = New-WorkSpace 'graphrefresh'
    Set-Content -LiteralPath (Join-Path $ws 'Ui\demo.py') -Value "def hello():`n    return 1" -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('graph', 'Ui')
    $uiGraph = Test-Path -LiteralPath (Join-Path $ws 'Ui\graphify-out\graph.json')
    $agentGraph = Test-Path -LiteralPath (Join-Path $ws 'Agent\graphify-out')
    $webGraph = Test-Path -LiteralPath (Join-Path $ws 'Web\graphify-out')
    Check 'S19' 'graph <repo> refreshes that repo and touches no sibling' `
        ($r.Code -eq 0 -and $uiGraph -and -not $agentGraph -and -not $webGraph) `
        ("code=$($r.Code) uiGraph=$uiGraph agentGraph=$agentGraph webGraph=$webGraph")
}
catch { Fail-Scenario 'S19' 'graph refresh' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T1/T2: missing graph, uncommitted edit
try {
    $ws = New-WorkSpace 'graph-status'
    $s = Get-GraphStatus (Join-Path $ws 'Ui')
    Check 'T1' 'a repo with no graph.json is missing, and says why' `
        ($s.State -eq 'missing' -and $s.Reason -eq 'no graph.json' -and $s.Head.Length -eq 7) `
        ("state=$($s.State) reason=$($s.Reason) head=$($s.Head)")

    $ui = Join-Path $ws 'Ui'
    # A code file, not a doc: since D6 an uncommitted .md never stales the graph (T-G1 pins that down).
    Set-Content -LiteralPath (Join-Path $ui 'demo.cs') -Value 'class Demo { }' -Encoding UTF8
    [void](& git -C $ui add -A 2>&1)
    [void](& git -C $ui commit -q -m 'code file' 2>&1)
    [void](New-WriteGraph -Repo $ui -MinutesOld 0)
    $s0 = Get-GraphStatus $ui
    Add-Content -LiteralPath (Join-Path $ui 'demo.cs') -Value '// uncommitted edit'
    $s1 = Get-GraphStatus $ui
    Check 'T2' 'a tracked file edited after the graph makes it stale' `
        ($s0.State -eq 'fresh' -and $s1.State -eq 'stale' -and $s1.Reason -match '^1 uncommitted file') `
        ("before=$($s0.State) after=$($s1.State) reason=$($s1.Reason)")

    Set-Content -LiteralPath (Join-Path $ui 'scratch.txt') -Value 'new' -Encoding UTF8
    # graphify writes here itself; a file under it is not an edit to the repo.
    Set-Content -LiteralPath (Join-Path $ui 'graphify-out\notes.txt') -Value 'x' -Encoding UTF8
    $s2 = Get-GraphStatus $ui
    Check 'T2' 'an untracked file counts too, a file under graphify-out/ does not' `
        ($s2.State -eq 'stale' -and $s2.Reason -match '^2 uncommitted file') `
        ("state=$($s2.State) reason=$($s2.Reason)")
}
catch { Fail-Scenario 'T1-2' 'graph status: missing + uncommitted' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T3/T4: .orch-head, and a no-op update
try {
    $ws = New-WorkSpace 'graph-head'
    $ui = Join-Path $ws 'Ui'
    $graph = New-WriteGraph -Repo $ui -MinutesOld -60
    $fake = New-FakeGraphify -Dir (Join-Path $ws 'fake-quiet')
    Add-Content -LiteralPath (Join-Path $ui 'README.md') -Value 'second commit'
    [void](& git -C $ui add -A 2>&1)
    [void](& git -C $ui commit -q -m 'second' 2>&1)
    $head = Get-GitText $ui @('rev-parse', 'HEAD')

    $r = Invoke-Refresh -Ws $ws -Repos Ui -PathPrepend $fake
    $sidecar = Join-Path $ui 'graphify-out\.orch-head'
    $recorded = if (Test-Path -LiteralPath $sidecar) { (Get-Content -LiteralPath $sidecar -Raw).Trim() } else { '' }
    Check 'T3' 'a successful refresh records HEAD in graphify-out/.orch-head' `
        ($r.Code -eq 0 -and $recorded -eq $head) `
        ("code=$($r.Code) sidecar=$(if ($recorded) { $recorded.Substring(0, 7) } else { '-' }) head=$($head.Substring(0, 7))")

    $sFresh = Get-GraphStatus $ui
    [void](& git -C $ui checkout -q HEAD~1 2>&1)
    $sOld = Get-GraphStatus $ui
    Check 'T3' 'checking out an older commit makes the graph stale (an mtime check would not)' `
        ($sFresh.State -eq 'fresh' -and $sOld.State -eq 'stale' -and $sOld.Reason -match 'HEAD moved since refresh') `
        ("before=$($sFresh.State) afterCheckout=$($sOld.State) reason=$($sOld.Reason)")

    [void](& git -C $ui checkout -q work 2>&1)
    [void](& git -C $ui commit -q --allow-empty -m 'empty' 2>&1)
    $head2 = Get-GitText $ui @('rev-parse', 'HEAD')
    $r2 = Invoke-Refresh -Ws $ws -Repos Ui -PathPrepend $fake
    $recorded2 = if (Test-Path -LiteralPath $sidecar) { (Get-Content -LiteralPath $sidecar -Raw).Trim() } else { '' }
    $mtime = (Get-Item -LiteralPath $graph).LastWriteTime
    $commitAt = [DateTimeOffset]::FromUnixTimeSeconds([long](Get-GitText $ui @('log', '-1', '--format=%ct'))).LocalDateTime
    $sAfter = Get-GraphStatus $ui
    Check 'T4' 'after a no-op update the sidecar wins over the graph mtime' `
        ($r2.Code -eq 0 -and $recorded2 -eq $head2 -and $mtime -lt $commitAt -and $sAfter.State -eq 'fresh') `
        ("sidecar=$(if ($recorded2) { $recorded2.Substring(0, 7) } else { '-' }) head=$($head2.Substring(0, 7)) graphOlderThanCommit=$($mtime -lt $commitAt) state=$($sAfter.State)")

    Remove-Item -LiteralPath $sidecar -Force
    $sNoSidecar = Get-GraphStatus $ui
    Check 'T4' 'without the sidecar the same state is stale (the check above is not vacuous)' `
        ($sNoSidecar.State -eq 'stale' -and $sNoSidecar.Reason -match 'commit newer than graph') `
        ("state=$($sNoSidecar.State) reason=$($sNoSidecar.Reason)")
}
catch { Fail-Scenario 'T3-4' 'graph status: HEAD moves + sidecar' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T5/T7: stderr warning, git-ignore precondition
try {
    $ws = New-WorkSpace 'graph-warn'
    $fakeWarn = New-FakeGraphify -Dir (Join-Path $ws 'fake-warn') -Mode 'warn'
    [void](New-WriteGraph -Repo (Join-Path $ws 'Ui') -MinutesOld 0)
    [void](New-WriteGraph -Repo (Join-Path $ws 'Web') -MinutesOld 0)
    $r = Invoke-Refresh -Ws $ws -Repos Ui, Web -PathPrepend $fakeWarn
    $uiHead = Test-Path -LiteralPath (Join-Path $ws 'Ui\graphify-out\.orch-head')
    $webHead = Test-Path -LiteralPath (Join-Path $ws 'Web\graphify-out\.orch-head')
    $warnLine = ($r.Out -split "`n" | Where-Object { $_ -match 'not installed' } | Select-Object -First 1)
    Check 'T5' 'a graphify warning on stderr is printed and does not abort the refresh' `
        ($r.Code -eq 0 -and $warnLine -and $uiHead -and $webHead) `
        ("code=$($r.Code) printed='$($warnLine.Trim())' uiSidecar=$uiHead webSidecar=$webHead")

    # Web loses its .gitignore: the sidecar must not be written into an un-ignored directory.
    Remove-Item -LiteralPath (Join-Path $ws 'Web\.gitignore') -Force
    Remove-Item -LiteralPath (Join-Path $ws 'Web\graphify-out\.orch-head') -Force
    [void](New-WriteGraph -Repo (Join-Path $ws 'Web') -MinutesOld 0)
    $r2 = Invoke-Refresh -Ws $ws -Repos Web -PathPrepend $fakeWarn
    $webHead2 = Test-Path -LiteralPath (Join-Path $ws 'Web\graphify-out\.orch-head')
    Check 'T7' 'no .orch-head is written when graphify-out/ is not git-ignored' `
        ($r2.Code -eq 0 -and -not $webHead2 -and $r2.Out -match 'not git-ignored') `
        ("code=$($r2.Code) sidecar=$webHead2 warned=$($r2.Out -match 'not git-ignored')")

    Set-Content -LiteralPath (Join-Path $ws 'Web\graphify-out\notes.txt') -Value 'x' -Encoding UTF8
    $sWeb = Get-GraphStatus (Join-Path $ws 'Web')
    Check 'T7' 'an untracked file under graphify-out/ is not counted as an edit' `
        ($sWeb.State -eq 'fresh') `
        ("state=$($sWeb.State) reason=$($sWeb.Reason)")
}
catch { Fail-Scenario 'T5-7' 'refresh: warning + gitignore' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T6: backup pruning
try {
    $ws = New-WorkSpace 'graph-backup'
    $ui = Join-Path $ws 'Ui'
    $graph = New-WriteGraph -Repo $ui -SemanticId 'ui-concept'
    foreach ($n in 1..4) { Copy-Item -LiteralPath $graph -Destination "$graph.bak-20200101-00000$n" }
    $fake = New-FakeGraphify -Dir (Join-Path $ws 'fake-quiet')
    $r = Invoke-Refresh -Ws $ws -Repos Ui -PathPrepend $fake
    $baks = @(Get-ChildItem -LiteralPath (Join-Path $ui 'graphify-out') -Filter 'graph.json.bak-*' | Sort-Object Name)
    $names = ($baks | ForEach-Object { $_.Name }) -join ','
    Check 'T6' 'only the newest two graph backups are kept' `
        ($r.Code -eq 0 -and $baks.Count -eq 2 -and $baks[0].Name -match 'bak-20200101-000004' -and $baks[1].Name -match ('bak-' + (Get-Date).ToString('yyyyMMdd'))) `
        ("code=$($r.Code) count=$($baks.Count) names=$names")
}
catch { Fail-Scenario 'T6' 'backup pruning' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T8: root merge refusal is id-based
try {
    $ws = New-WorkSpace 'graph-merge'
    [void](New-WriteGraph -Repo (Join-Path $ws 'Ui') -SemanticId 'ui-concept')
    [void](New-WriteGraph -Repo (Join-Path $ws 'Web') -SemanticId 'web-concept')
    New-Item -ItemType Directory -Path (Join-Path $ws 'graphify-out') -Force | Out-Null
    $root = Join-Path $ws 'graphify-out\graph.json'
    Set-Content -LiteralPath $root -Value '{"nodes":[{"id":"ui-concept","_origin":"semantic"},{"id":"root-only","_origin":"semantic"}],"edges":[]}' -Encoding UTF8
    $fake = New-FakeGraphify -Dir (Join-Path $ws 'fake-quiet')

    $r = Invoke-Refresh -Ws $ws -Repos Ui, Web -MergeRoot -PathPrepend $fake
    $untouched = ((Get-Content -LiteralPath $root -Raw) -match 'root-only')
    Check 'T8' 'a root graph with a semantic id no repo graph has is not re-merged' `
        ($r.Code -eq 0 -and $r.Out -match 'found in no repo graph' -and $r.Out -match 'root-only' -and $untouched) `
        ("code=$($r.Code) refused=$($r.Out -match 'refusing') rootUntouched=$untouched")

    # ui-concept also lives in the Ui graph, so a re-merge cannot lose it.
    Set-Content -LiteralPath $root -Value '{"nodes":[{"id":"ui-concept","_origin":"semantic"}],"edges":[]}' -Encoding UTF8
    $r2 = Invoke-Refresh -Ws $ws -Repos Ui, Web -MergeRoot -PathPrepend $fake
    $merged = (Get-Content -LiteralPath $root -Raw)
    Check 'T8' 'a root graph whose semantic ids all live in a repo graph is re-merged' `
        ($r2.Code -eq 0 -and $r2.Out -match 'root graph rebuilt' -and $merged -match '"nodes":\[\]') `
        ("code=$($r2.Code) rebuilt=$($r2.Out -match 'root graph rebuilt') merged=$($merged.Trim())")
}
catch { Fail-Scenario 'T8' 'root merge refusal' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-F1: numbered findings count (D3)
try {
    . (Join-Path $Orch 'lib\bus.ps1')
    $text = @'
VERDICT: FINDINGS
1. `FINDING [blocker] Phase 2: the file is not named (demo.cs)
2. FINDING [should] Phase 1: the shape is unstated
- FINDING [nit] wording only
'@
    $f = @(Get-Findings $text)
    $b = @($f | Where-Object Level -eq 'blocker').Count
    $s = @($f | Where-Object Level -eq 'should').Count
    $n = @($f | Where-Object Level -eq 'nit').Count
    Check 'T-F1' 'a numbered `1. FINDING [blocker]` line counts as a blocker' `
        ($f.Count -eq 3 -and $b -eq 1 -and $s -eq 1 -and $n -eq 1) `
        ("count=$($f.Count) blocker=$b should=$s nit=$n")
}
catch { Fail-Scenario 'T-F1' 'numbered findings' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-F3: a level without brackets counts (D3)
try {
    . (Join-Path $Orch 'lib\bus.ps1')
    $text = @'
FINDING should §9 (T1–T3): the test seam is unstated (MofidVmInitializeTests.cs:1615)
2. FINDING nit §6.3: no insertion point
FINDING [blocker] §4: still counted with brackets
VERDICT: FINDINGS
'@
    $f = @(Get-Findings $text)
    $b = @($f | Where-Object Level -eq 'blocker').Count
    $s = @($f | Where-Object Level -eq 'should').Count
    $n = @($f | Where-Object Level -eq 'nit').Count
    Check 'T-F3' 'a `FINDING should` line without brackets counts as a should' `
        ($f.Count -eq 3 -and $b -eq 1 -and $s -eq 1 -and $n -eq 1 -and $f[0].Text -like '§9*') `
        ("count=$($f.Count) blocker=$b should=$s nit=$n first='$($f[0].Text)'")
}
catch { Fail-Scenario 'T-F3' 'bracket-less findings' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-F2: no verdict is its own state (D4)
try {
    $ws = New-WorkSpace 'noverdict'
    $r = Invoke-New $ws 'T-F2-001' 'Ui' 'no-verdict'
    $s = Get-State $ws 'T-F2-001'
    $verdict = if ($s.PSObject.Properties['review'] -and $s.review) { $s.review.verdict } else { 'absent' }
    $a = Invoke-Approve $ws 'T-F2-001'
    $s2 = Get-State $ws 'T-F2-001'
    Check 'T-F2' 'a reply without VERDICT: records NO_VERDICT and approve refuses it' `
        ($r.Code -eq 0 -and $verdict -eq 'NO_VERDICT' -and $r.Out -match 'NO_VERDICT \(reply ended without a verdict' -and
         $a.Code -eq 1 -and $a.Out -match 'has not been reviewed' -and $s2.status -eq 'AwaitingApproval' -and -not $s2.approved) `
        ("new=$($r.Code) verdict=$verdict approve=$($a.Code) status=$($s2.status)")
}
catch { Fail-Scenario 'T-F2' 'no-verdict review' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-B1: the plan's own branch command wins (D5)
try {
    $ws = New-WorkSpace 'planbranch'
    $plan = Join-Path $ws 'plan-branch.md'
    @'
# Plan - mock branch task

**Planned by:** Claude Opus 5.5 (test seat), 2026-01-01

## 0. Before the first edit
```bash
git switch -c plan-0081-mofid-quantity-after-freeze
```

## Phase 1
Create `phase1.txt` containing `one`.
'@ | Set-Content -LiteralPath $plan -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('new', 'T-B1-001', 'Ui', 'mock branch task', '-PlanFile', $plan)
    $a = Invoke-Approve $ws 'T-B1-001'
    $s = Get-State $ws 'T-B1-001'
    Check 'T-B1' "the plan's git switch -c name wins over the alireza/<taskid> fallback" `
        ($r.Code -eq 0 -and $a.Code -eq 0 -and $s.branch -eq 'plan-0081-mofid-quantity-after-freeze') `
        ("new=$($r.Code) approve=$($a.Code) branch=$($s.branch)")
}
catch { Fail-Scenario 'T-B1' 'plan branch' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-G1: docs never stale the graph (D6)
try {
    $ws = New-WorkSpace 'graph-docs'
    $ui = Join-Path $ws 'Ui'
    [void](New-WriteGraph -Repo $ui -MinutesOld 0)
    Set-Content -LiteralPath (Join-Path $ui 'NOTES.MD') -Value 'uncommitted doc' -Encoding UTF8
    $sDoc = Get-GraphStatus $ui
    Set-Content -LiteralPath (Join-Path $ui 'Program.cs') -Value 'class P { }' -Encoding UTF8
    $sCode = Get-GraphStatus $ui
    Check 'T-G1' 'an uncommitted .md leaves the repo fresh, an uncommitted .cs still makes it stale' `
        ($sDoc.State -eq 'fresh' -and $sCode.State -eq 'stale' -and $sCode.Reason -match '^1 uncommitted file') `
        ("doc=$($sDoc.State) code=$($sCode.State) reason=$($sCode.Reason)")
}
catch { Fail-Scenario 'T-G1' 'docs vs code staleness' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-T1/T-T2/T-T3: activity timeout (D1)
try {
    $ws = New-WorkSpace 'timeout'
    $mockPath = Join-Path $ws 'mock-agent.ps1'
    $oldStream = $env:MOCK_STREAM_MS; $oldEvery = $env:MOCK_STREAM_INTERVAL_MS; $oldSleep = $env:MOCK_SLEEP_MS
    $mockArgv = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $mockPath)
    try {
        $env:MOCK_STREAM_MS = '3000'; $env:MOCK_STREAM_INTERVAL_MS = '200'; $env:MOCK_SLEEP_MS = ''
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $r1 = Invoke-External -Exe 'powershell' -ArgList $mockArgv -StdIn 'x' -TimeoutSec 10 -IdleTimeoutSec 1
        $streamSecs = $sw.Elapsed.TotalSeconds

        $env:MOCK_STREAM_MS = ''; $env:MOCK_SLEEP_MS = '30000'
        $sw.Restart()
        $r2 = Invoke-External -Exe 'powershell' -ArgList $mockArgv -StdIn 'x' -TimeoutSec 30 -IdleTimeoutSec 1
        $idleSecs = $sw.Elapsed.TotalSeconds

        $sw.Restart()
        $r3 = Invoke-External -Exe 'powershell' -ArgList $mockArgv -StdIn 'x' -TimeoutSec 2 -IdleTimeoutSec 0
        $totalSecs = $sw.Elapsed.TotalSeconds
    }
    finally {
        $env:MOCK_STREAM_MS = $oldStream; $env:MOCK_STREAM_INTERVAL_MS = $oldEvery; $env:MOCK_SLEEP_MS = $oldSleep
    }
    Check 'T-T1' 'a hop that keeps printing runs past the idle timeout and finishes OK' `
        ($r1.ExitCode -eq 0 -and -not $r1.TimedOut -and $null -eq $r1.TimeoutKind -and $r1.Stdout -match 'mock progress line 1[0-9]' -and $streamSecs -lt 12) `
        ("exit=$($r1.ExitCode) timedOut=$($r1.TimedOut) kind=$($r1.TimeoutKind) secs=$([int]$streamSecs) lines=$(@($r1.Stdout -split "`r?`n" | Where-Object { $_.Trim() }).Count)")
    Check 'T-T2' 'a silent hop is killed by the idle timeout, long before the cap' `
        ($r2.TimedOut -and $r2.TimeoutKind -eq 'idle' -and $idleSecs -lt 10) `
        ("timedOut=$($r2.TimedOut) kind=$($r2.TimeoutKind) secs=$([math]::Round($idleSecs, 1)) cap=30")
    Check 'T-T3' 'idleTimeoutSec 0 falls back to the absolute cap' `
        ($r3.TimedOut -and $r3.TimeoutKind -eq 'total' -and $totalSecs -ge 2 -and $totalSecs -lt 15) `
        ("timedOut=$($r3.TimedOut) kind=$($r3.TimeoutKind) secs=$([math]::Round($totalSecs, 1)) cap=2")
}
catch { Fail-Scenario 'T-T1-3' 'activity timeout' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-T4: a big prompt never deadlocks
# A child that prints more than a pipe buffer before it reads stdin, given a prompt larger than a pipe
# buffer. With a blocking stdin write both sides waited forever and no timeout could fire. The call runs
# in a job so a regression shows as a failed check instead of hanging the suite.
try {
    $job = Start-Job -ArgumentList $Orch -ScriptBlock {
        param($orchDir)
        . (Join-Path $orchDir 'lib\bus.ps1'); . (Join-Path $orchDir 'lib\agents.ps1')
        $big = ('y' * 99 + "`n") * 3000
        $cmd = '1..20000 | ForEach-Object { ''x'' * 40 }; $in = [Console]::In.ReadToEnd(); ''read '' + $in.Length'
        $r = Invoke-External -Exe 'powershell' -ArgList @('-NoProfile', '-Command', $cmd) -StdIn $big -TimeoutSec 60
        [pscustomobject]@{ ExitCode = $r.ExitCode; TimedOut = $r.TimedOut; Tail = (($r.Stdout -split "`n" | Where-Object { $_.Trim() }) | Select-Object -Last 1) }
    }
    if (Wait-Job $job -Timeout 90) {
        $r = Receive-Job $job
        Check 'T-T4' 'a 300 KB prompt to a child that prints 800 KB first completes, all of stdin read' `
            ($r.ExitCode -eq 0 -and -not $r.TimedOut -and "$($r.Tail)" -match '^read \d+$' -and [int]("$($r.Tail)" -replace '\D', '') -ge 299000) `
            ("exit=$($r.ExitCode) timedOut=$($r.TimedOut) tail=$($r.Tail)")
    }
    else {
        Check 'T-T4' 'a big prompt never deadlocks' $false 'HUNG: Invoke-External did not return within 90 s'
        Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match '1\.\.20000' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    }
    Remove-Job $job -Force -ErrorAction SilentlyContinue
}
catch { Fail-Scenario 'T-T4' 'big prompt deadlock' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-W1: the guard is repo-scoped (D2)
try {
    $ws = New-WorkSpace 'scoped'
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $uiPath = Join-Path $ws 'Ui'
    $prompt = "# ORCHESTRATOR CALL - role: plan-reviewer`nRepo root on disk: $uiPath`n`nVERDICT: PASS"
    $oldMode = $env:MOCK_MODE
    try {
        $env:MOCK_MODE = 'touch-sibling-any'
        $siblingRes = $null; $siblingOut = ''; $siblingThrew = $false
        try {
            # 6>&1 turns the `Write-Host` note into records so the test can read it back.
            $captured = @(Invoke-Role -Ctx $ctx -Role 'plan-reviewer' -Prompt $prompt -Cwd $uiPath -TimeoutSec 60 -Repo 'Ui' 6>&1)
            $siblingRes = @($captured | Where-Object { $_ -and $_.PSObject.Properties['Attempts'] }) | Select-Object -First 1
            $siblingOut = (@($captured | Where-Object { $_ -is [System.Management.Automation.InformationRecord] } | ForEach-Object { "$_" }) -join "`n")
        }
        catch { $siblingThrew = $true; $siblingOut = $_.Exception.Message }

        $env:MOCK_MODE = 'write-on-review'
        $targetThrew = $false; $targetMsg = ''
        try { Invoke-Role -Ctx $ctx -Role 'plan-reviewer' -Prompt $prompt -Cwd $uiPath -TimeoutSec 60 -Repo 'Ui' | Out-Null }
        catch { $targetThrew = $true; $targetMsg = $_.Exception.Message }
    }
    finally { $env:MOCK_MODE = $oldMode }

    $sawSibling = ($siblingRes -and (@(@($siblingRes.Attempts)[0].changedRepos) -contains 'Web'))
    Check 'T-W1' 'a sibling repo changing is a note, the target repo changing still throws' `
        (-not $siblingThrew -and $sawSibling -and $siblingOut -match 'note: Web changed meanwhile' -and
         $targetThrew -and $targetMsg -match "changed the target repo: Ui") `
        ("siblingThrew=$siblingThrew note=$($siblingOut -match 'note: Web') targetThrew=$targetThrew msg=$(($targetMsg -split "`n")[0])")
}
catch { Fail-Scenario 'T-W1' 'repo-scoped guard' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-P1: progress.json while a hop runs (D8)
try {
    $ws = New-WorkSpace 'progress'
    $mockPath = Join-Path $ws 'mock-agent.ps1'
    $taskDir = Join-Path $ws 'runs\T-PRG-001'
    New-Item -ItemType Directory -Force -Path $taskDir | Out-Null
    $prog = Join-Path $taskDir 'progress.json'
    $oldStream = $env:MOCK_STREAM_MS; $oldEvery = $env:MOCK_STREAM_INTERVAL_MS
    $env:MOCK_STREAM_MS = '6000'; $env:MOCK_STREAM_INTERVAL_MS = '250'
    try {
        $job = Start-Job -ScriptBlock {
            param($Orch, $Mock, $Prog)
            . (Join-Path $Orch 'lib\agents.ps1')
            Invoke-External -Exe 'powershell' -ArgList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Mock) -StdIn 'x' `
                -TimeoutSec 30 -IdleTimeoutSec 10 -ProgressFile $Prog `
                -ProgressMeta @{ taskId = 'T-PRG-001'; command = 'revise'; role = 'plan-reviewer'; adapter = 'mock'; model = 'mock-reviewer'; estimate = @{ typicalSec = 60; basis = 'median of 3 runs' } }
        } -ArgumentList $Orch, $mockPath, $prog

        $deadline = (Get-Date).AddSeconds(20)
        while (-not (Test-Path -LiteralPath $prog) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 200 }
        # Five reads spread over the whole hop (and past its end write), so at least one lands while
        # the file is being rewritten: a non-atomic writer would surface as invalid JSON here.
        $valid = $true; $lines = @(); $states = @()
        for ($i = 0; $i -lt 5; $i++) {
            Start-Sleep -Milliseconds 1400
            try { $p = Get-Content -LiteralPath $prog -Raw -Encoding UTF8 | ConvertFrom-Json; $lines += [string]$p.lastLine; $states += [string]$p.state }
            catch { $valid = $false }
        }
        $res = Receive-Job $job -Wait -AutoRemoveJob
    }
    finally { $env:MOCK_STREAM_MS = $oldStream; $env:MOCK_STREAM_INTERVAL_MS = $oldEvery }

    $final = Get-Content -LiteralPath $prog -Raw -Encoding UTF8 | ConvertFrom-Json
    $sawRunning = @($states | Where-Object { $_ -eq 'running' }).Count
    $sawDone = @($states | Where-Object { $_ -eq 'done' }).Count
    Check 'T-P1' 'progress.json is valid at every read and ends done with the last output line' `
        ($valid -and $sawRunning -ge 1 -and $sawDone -ge 1 -and $res.ExitCode -eq 0 -and $final.state -eq 'done' -and
         $final.role -eq 'plan-reviewer' -and $final.lastLine -match '^mock progress line \d+$' -and $final.elapsedSec -ge 4 -and
         $final.estimate.typicalSec -eq 60) `
        ("valid=$valid running=$sawRunning done=$sawDone exit=$($res.ExitCode) state=$($final.state) last='$($final.lastLine)' elapsed=$($final.elapsedSec)")
}
catch { Fail-Scenario 'T-P1' 'progress.json' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-P2: the estimate is a median (D8)
try {
    $ws = New-WorkSpace 'estimate'
    foreach ($pair in @(@('A', 60), @('B', 90), @('C', 120))) {
        $d = Join-Path $ws ("runs\T-$($pair[0])\transcript")
        New-Item -ItemType Directory -Force -Path $d | Out-Null
        $obj = @{ Adapter = 'mock'; Model = 'mock-reviewer'; Attempts = @(@{ adapter = 'mock'; model = 'mock-reviewer'; ok = $true; seconds = [int]$pair[1] }) }
        ($obj | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath (Join-Path $d 'plan-reviewer.v1.json') -Encoding UTF8
    }
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $e3 = Get-RoleEstimate -Ctx $ctx -Role 'plan-reviewer' -Adapter 'mock' -Model 'mock-reviewer'
    Remove-Item -LiteralPath (Join-Path $ws 'runs\T-B') -Recurse -Force
    Remove-Item -LiteralPath (Join-Path $ws 'runs\T-C') -Recurse -Force
    $e1 = Get-RoleEstimate -Ctx $ctx -Role 'plan-reviewer' -Adapter 'mock' -Model 'mock-reviewer'
    Check 'T-P2' 'three runs of 60/90/120 s give 90 s; a single run gives no number' `
        ($e3.typicalSec -eq 90 -and $e3.basis -match '3 runs' -and $null -eq $e1.typicalSec -and $e1.basis -eq 'no history') `
        ("three=$(if ($null -eq $e3.typicalSec) { 'null' } else { $e3.typicalSec }) basis='$($e3.basis)' one=$(if ($null -eq $e1.typicalSec) { 'null' } else { $e1.typicalSec })")
}
catch { Fail-Scenario 'T-P2' 'role estimate' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-P3: watch prints and exits (D8)
try {
    $ws = New-WorkSpace 'watch'
    $taskDir = Join-Path $ws 'runs\T-WCH-001'
    New-Item -ItemType Directory -Force -Path $taskDir | Out-Null
    $fake = [ordered]@{
        taskId = 'T-WCH-001'; command = 'revise'; role = 'plan-reviewer'; adapter = 'opencode'; model = 'opencode/glm-5.3-flash-free'
        startedAt = (Get-Date).ToString('s'); elapsedSec = 760; lastOutputAgoSec = 8
        lastLine = '-> Read Src/03.Endpoint/ProjectB.Agent/Pages/Mofid/MofidVm.cs'
        estimate = @{ typicalSec = 840; basis = 'median of 5 runs' }; state = 'done'
    }
    ($fake | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath (Join-Path $taskDir 'progress.json') -Encoding UTF8
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $r = Run-Orch -Ws $ws -Argv @('watch', 'T-WCH-001', '-Every', '1', '-Once')
    $secs = $sw.Elapsed.TotalSeconds
    Check 'T-P3' 'watch prints the block once and exits instead of polling a finished run' `
        ($r.Code -eq 0 -and $r.Out -match 'T-WCH-001\s+revise -> plan-reviewer' -and $r.Out -match 'elapsed 12:40' -and
         $r.Out -match 'typical ~= 14:00 \(median of 5 runs\)' -and $r.Out -match '~90%' -and $r.Out -match 'state: done' -and $secs -lt 30) `
        ("code=$($r.Code) secs=$([math]::Round($secs, 1)) block=$(($r.Out -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 2) -join ' | ')")
}
catch { Fail-Scenario 'T-P3' 'watch' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M1: a manual review defers at once (D7)
try {
    $ws = New-WorkSpace 'manual-review'
    Set-ManualChain $ws 'plan-reviewer'
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-New $ws 'T-M1-001' 'Ui'
    $elapsed = $sw.Elapsed.TotalSeconds
    $s = Get-State $ws 'T-M1-001'
    $keys = Get-InboxKeys $ws 'T-M1-001'
    $fp = Join-Path $ws 'runs\T-M1-001\inbox\plan-reviewer.v1.fingerprint'
    Check 'T-M1' 'a manual review writes inbox/<key>.md + fingerprint, exits 3 and leaves the run alone' `
        ($r.Code -eq 3 -and $keys -contains 'plan-reviewer.v1' -and (Test-Path -LiteralPath $fp) -and
         $s.status -eq 'Planning' -and -not $s.review -and $r.Out -match 'manual: deferred' -and $elapsed -lt 30) `
        ("code=$($r.Code) keys=$($keys -join ',') fingerprint=$(Test-Path -LiteralPath $fp) status=$($s.status) secs=$([math]::Round($elapsed, 1))")
}
catch { Fail-Scenario 'T-M1' 'manual review deferral' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M2: the answer is consumed by a rerun (D7)
try {
    $ws = New-WorkSpace 'manual-verify'
    Set-ManualChain $ws 'verifier'
    [void](Invoke-New $ws 'T-M2-001' 'Ui')
    [void](Invoke-Approve $ws 'T-M2-001')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-M2-001', '1'))
    $defer = Run-Orch -Ws $ws -Argv @('verify', 'T-M2-001')
    $s1 = Get-State $ws 'T-M2-001'
    # @() around it: a single-element result would otherwise collapse to a string and $keys[0] to a char.
    $keys = @(Get-InboxKeys $ws 'T-M2-001')
    $key = if ($keys.Count) { $keys[0] } else { '' }
    $answer = Join-Path $ws 'verifier-answer.md'
    Set-Content -LiteralPath $answer -Value "Everything matches the plan.`nVERDICT: PASS" -Encoding UTF8
    $stored = Run-Orch -Ws $ws -Argv @('answer', 'T-M2-001', $key, $answer)
    $r = Run-Orch -Ws $ws -Argv @('verify', 'T-M2-001')
    $s2 = Get-State $ws 'T-M2-001'
    Check 'T-M2' 'verify defers (exit 3), then consumes the stored answer and records the review' `
        ($defer.Code -eq 3 -and $s1.status -eq 'Implemented' -and -not $s1.verify -and $key -like 'verifier.*' -and
         $stored.Code -eq 0 -and $r.Code -eq 0 -and $s2.verify -and $s2.verify.modelVerdict -eq 'PASS' -and $s2.status -eq 'Verified') `
        ("defer=$($defer.Code) status1=$($s1.status) key=$($key) stored=$($stored.Code) verify=$($r.Code) verdict=$($s2.verify.modelVerdict)")
}
catch { Fail-Scenario 'T-M2' 'manual answer consumed' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M3: an answer for an old key is not reused (D7)
try {
    $ws = New-WorkSpace 'manual-stale'
    Set-ManualChain $ws 'verifier'
    [void](Invoke-New $ws 'T-M3-001' 'Ui')
    [void](Invoke-Approve $ws 'T-M3-001')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-M3-001', '1'))
    [void](Run-Orch -Ws $ws -Argv @('verify', 'T-M3-001'))
    $k1 = @(Get-InboxKeys $ws 'T-M3-001')[0]
    $answer = Join-Path $ws 'stale-answer.md'
    Set-Content -LiteralPath $answer -Value 'VERDICT: PASS' -Encoding UTF8
    [void](Run-Orch -Ws $ws -Argv @('answer', 'T-M3-001', $k1, $answer))
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-M3-001', '2'))
    $r = Run-Orch -Ws $ws -Argv @('verify', 'T-M3-001')
    $s = Get-State $ws 'T-M3-001'
    $keys = Get-InboxKeys $ws 'T-M3-001'
    $k2 = @($keys | Where-Object { $_ -ne $k1 })
    Check 'T-M3' 'an answer for the old diff is ignored; the new verify defers under a different key' `
        ($r.Code -eq 3 -and $k2.Count -eq 1 -and $k2[0] -like 'verifier.*' -and -not $s.verify -and $s.status -eq 'Implemented') `
        ("code=$($r.Code) k1=$($k1) k2=$($k2 -join ',') verify=$(if ($s.verify) { $s.verify.verdict } else { 'absent' }) status=$($s.status)")
}
catch { Fail-Scenario 'T-M3' 'stale manual key' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M4: the tree moved after the prompt (D7)
try {
    $ws = New-WorkSpace 'manual-moved'
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $ui = Join-Path $ws 'Ui'
    $taskDir = Join-Path $ws 'runs\T-M4-001'
    New-Item -ItemType Directory -Force -Path $taskDir | Out-Null
    $first = Invoke-Manual -Ctx $ctx -TaskId 'T-M4-001' -Key 'plan-reviewer.v1' -Prompt 'PROMPT' -RepoPath $ui
    $answerFile = Join-Path $taskDir 'answers\plan-reviewer.v1.md'
    Set-Content -LiteralPath $answerFile -Value 'VERDICT: PASS' -Encoding UTF8
    $second = Invoke-Manual -Ctx $ctx -TaskId 'T-M4-001' -Key 'plan-reviewer.v1' -Prompt 'PROMPT' -RepoPath $ui
    Add-Content -LiteralPath (Join-Path $ui 'README.md') -Value 'moved after the prompt'
    $third = Invoke-Manual -Ctx $ctx -TaskId 'T-M4-001' -Key 'plan-reviewer.v1' -Prompt 'PROMPT' -RepoPath $ui
    $class = Get-FailureClass $third
    Check 'T-M4' 'an answer whose repo moved since the prompt is a violation, not a pass' `
        ($first.ExitCode -eq -2 -and (Test-Path -LiteralPath (Join-Path $taskDir 'inbox\plan-reviewer.v1.md')) -and
         $second.ExitCode -eq 0 -and $second.Stdout -match 'VERDICT: PASS' -and
         $third.ExitCode -eq -3 -and $class -eq 'manual-violation' -and $third.Stderr -match 'repo changed after the prompt') `
        ("first=$($first.ExitCode) second=$($second.ExitCode) third=$($third.ExitCode) class=$class")
}
catch { Fail-Scenario 'T-M4' 'manual violation' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M5: answer refuses an unusable file (D7)
try {
    $ws = New-WorkSpace 'manual-answer'
    [void](Invoke-New $ws 'T-M5-001' 'Ui')
    $empty = Join-Path $ws 'empty.md'
    Set-Content -LiteralPath $empty -Value '' -Encoding UTF8
    $prose = Join-Path $ws 'prose.md'
    Set-Content -LiteralPath $prose -Value 'Looks fine to me, ship it.' -Encoding UTF8
    $good = Join-Path $ws 'good.md'
    Set-Content -LiteralPath $good -Value 'VERDICT: PASS' -Encoding UTF8
    $r1 = Run-Orch -Ws $ws -Argv @('answer', 'T-M5-001', 'plan-reviewer.v1', $empty)
    $r2 = Run-Orch -Ws $ws -Argv @('answer', 'T-M5-001', 'plan-reviewer.v1', $prose)
    $stored = Join-Path $ws 'runs\T-M5-001\answers\plan-reviewer.v1.md'
    $refusedLeftNothing = -not (Test-Path -LiteralPath $stored)
    $r3 = Run-Orch -Ws $ws -Argv @('answer', 'T-M5-001', 'plan-reviewer.v1', $good)
    $r4 = Run-Orch -Ws $ws -Argv @('answer', 'T-M5-001', 'plan-reviewer.v1', $good)
    Check 'T-M5' 'answer refuses an empty file and one without VERDICT:, and refuses to overwrite' `
        ($r1.Code -eq 1 -and $r1.Out -match 'answer file is empty' -and
         $r2.Code -eq 1 -and $r2.Out -match 'has no VERDICT' -and $refusedLeftNothing -and
         $r3.Code -eq 0 -and (Test-Path -LiteralPath $stored) -and $r4.Code -eq 1 -and $r4.Out -match 'already answered') `
        ("empty=$($r1.Code) prose=$($r2.Code) good=$($r3.Code) again=$($r4.Code) stored=$(Test-Path -LiteralPath $stored)")
}
catch { Fail-Scenario 'T-M5' 'answer guards' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M6: a manual hop in the planner chain is refused (D8)
try {
    $ws = New-WorkSpace 'manual-badconfig'
    Set-ManualChain $ws 'planner'
    $r = Run-Orch -Ws $ws -Argv @('status', 'T-NONE')
    Check 'T-M6' 'a config with a manual hop in planner fails to load' `
        ($r.Code -eq 1 -and $r.Out -match "'planner' has a manual hop" -and $r.Out -match 'the planner is the one role') `
        ("code=$($r.Code) msg=$(($r.Out -split "`n" | Where-Object { $_ -match 'ERROR' } | Select-Object -First 1).Trim())")
}
catch { Fail-Scenario 'T-M6' 'manual in planner chain' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M7: an inbox adapter of any name defers like manual
try {
    $ws = New-WorkSpace 'inbox-review'
    Set-InboxAdapterChain $ws 'plan-reviewer'
    $r = Invoke-New $ws 'T-M7-001' 'Ui'
    $s = Get-State $ws 'T-M7-001'
    $keys = Get-InboxKeys $ws 'T-M7-001'
    Check 'T-M7' 'a review through an adapter with transport inbox writes inbox/<key>.md and exits 3' `
        ($r.Code -eq 3 -and $keys -contains 'plan-reviewer.v1' -and $s.status -eq 'Planning' -and $r.Out -match 'manual: deferred') `
        ("code=$($r.Code) keys=$($keys -join ',') status=$($s.status)")
}
catch { Fail-Scenario 'T-M7' 'inbox adapter deferral' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M8: an inbox adapter in the planner chain is refused
try {
    $ws = New-WorkSpace 'inbox-badconfig'
    Set-InboxAdapterChain $ws 'planner'
    $r = Run-Orch -Ws $ws -Argv @('status', 'T-NONE')
    Check 'T-M8' 'a config with an inbox adapter in planner fails to load, whatever the adapter is called' `
        ($r.Code -eq 1 -and $r.Out -match "'planner' has a manual hop") `
        ("code=$($r.Code)")
}
catch { Fail-Scenario 'T-M8' 'inbox in planner chain' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-M9: an unknown transport is refused
try {
    $ws = New-WorkSpace 'bad-transport'
    Set-InboxAdapterChain $ws 'plan-reviewer' 'carrier-pigeon'
    $r = Run-Orch -Ws $ws -Argv @('status', 'T-NONE')
    Check 'T-M9' 'a config whose adapter has a transport other than cli or inbox fails to load' `
        ($r.Code -eq 1 -and $r.Out -match "transport 'carrier-pigeon'") `
        ("code=$($r.Code)")
}
catch { Fail-Scenario 'T-M9' 'unknown transport' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-Q1/T-R1: queue, then resume the review
try {
    $ws = New-WorkSpace 'manual-queue'
    Set-ManualChain $ws 'plan-reviewer'
    [void](Invoke-New $ws 'T-Q1-001' 'Ui')
    $keys = @(Get-InboxKeys $ws 'T-Q1-001')
    $key = if ($keys.Count) { $keys[0] } else { '' }
    $q = Run-Orch -Ws $ws -Argv @('queue', 'T-Q1-001')
    Check 'T-Q1' 'queue lists the waiting request with its paths and the next commands' `
        ($q.Code -eq 0 -and $q.Out -match 'T-Q1-001\s+plan-reviewer\.v1' -and $q.Out -match 'request:' -and
         $q.Out -match 'answer :' -and $q.Out -match 'run\.ps1 review T-Q1-001' -and $q.Out -match '1 request\(s\) waiting') `
        ("code=$($q.Code) out=$(($q.Out -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 2) -join ' | ')")

    $answer = Join-Path $ws 'review-answer.md'
    Set-Content -LiteralPath $answer -Value "The plan is buildable and matches the repo.`nVERDICT: PASS" -Encoding UTF8
    $stored = Run-Orch -Ws $ws -Argv @('answer', 'T-Q1-001', $key, $answer)
    $q2 = Run-Orch -Ws $ws -Argv @('queue', 'T-Q1-001')
    Check 'T-Q1' 'a stored answer takes the request off the queue' `
        ($stored.Code -eq 0 -and $q2.Code -eq 0 -and $q2.Out -match 'nothing is waiting') `
        ("stored=$($stored.Code) queue=$(($q2.Out -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1))")

    $r = Run-Orch -Ws $ws -Argv @('review', 'T-Q1-001')
    $s = Get-State $ws 'T-Q1-001'
    Check 'T-R1' 'review consumes the deferred plan-reviewer answer without creating a new version' `
        ($r.Code -eq 0 -and $s.round -eq 1 -and $s.review.round -eq 1 -and $s.review.verdict -eq 'PASS' -and
         $s.status -eq 'AwaitingApproval' -and $s.PSObject.Properties['readback']) `
        ("code=$($r.Code) round=$($s.round) review=$($s.review.verdict)/v$($s.review.round) status=$($s.status)")
}
catch { Fail-Scenario 'T-Q1' 'queue + review resume' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-W1: watch -ThenResume waits, then resumes
try {
    $ws = New-WorkSpace 'manual-watch-resume'
    Set-ManualChain $ws 'plan-reviewer'
    [void](Invoke-New $ws 'T-W1-001' 'Ui')
    $keys = @(Get-InboxKeys $ws 'T-W1-001')
    $key = if ($keys.Count) { $keys[0] } else { '' }
    $out = Join-Path $ws 'watch.out'
    $err = Join-Path $ws 'watch.err'
    $log = Join-Path $ws 'mock-calls.log'
    # One string, quoted: Start-Process joins an array with spaces and quotes nothing, and the workspace
    # path can contain spaces. The job runs the child with -Wait, because only then does the returned
    # Process object carry an ExitCode.
    $argv = '-NoProfile -ExecutionPolicy Bypass -File "{0}" watch T-W1-001 -ThenResume -PollSec 1 -Config "{1}"' -f $RunPs1, (Join-Path $ws 'config.json')
    $job = Start-Job -ScriptBlock {
        param($argv, $out, $err, $log)
        $env:MOCK_MODE = ''
        $env:MOCK_LOG = $log
        $p = Start-Process -FilePath 'powershell' -ArgumentList $argv -PassThru -Wait `
            -RedirectStandardOutput $out -RedirectStandardError $err
        $p.ExitCode
    } -ArgumentList $argv, $out, $err, $log
    Start-Sleep -Seconds 3
    # While it waits nothing is claimed: the run is exactly as the deferral left it, and the lock is free.
    $mid = Get-State $ws 'T-W1-001'
    $waiting = ($mid.status -eq 'Planning' -and -not $mid.review -and
        -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-W1-001\.lock')))
    # Written the way `answer` writes it: temp file in the same folder, then a move.
    $answers = Join-Path $ws 'runs\T-W1-001\answers'
    New-Item -ItemType Directory -Force -Path $answers | Out-Null
    $tmp = Join-Path $answers "$key.md.tmp"
    Set-Content -LiteralPath $tmp -Value "Reviewed; no blockers.`nVERDICT: PASS" -Encoding UTF8
    Move-Item -LiteralPath $tmp -Destination (Join-Path $answers "$key.md") -Force
    $finished = $null -ne (Wait-Job $job -Timeout 120)
    $code = if ($finished) { @(Receive-Job $job)[-1] } else { -999 }
    if (-not $finished) { Stop-Job $job }
    Remove-Job $job -Force
    $s = Get-State $ws 'T-W1-001'
    $text = if (Test-Path -LiteralPath $out) { Get-Content -LiteralPath $out -Raw } else { '' }
    Check 'T-W1' 'watch -ThenResume waits without claiming the run, then consumes the answer and reviews' `
        ($waiting -and $finished -and $code -eq 0 -and $s.review -and $s.review.verdict -eq 'PASS' -and
         $s.status -eq 'AwaitingApproval' -and $text -match 'resuming with') `
        ("waiting=$waiting finished=$finished code=$code review=$(if ($s.review) { $s.review.verdict } else { 'none' }) out=$(($text -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 2) -join ' | ')")
}
catch { Fail-Scenario 'T-W1' 'watch -ThenResume' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-RB1..T-RB3: the read-back by hand (Freebuff)
try {
    $ws = New-WorkSpace 'manual-readback'
    Set-ManualReadBack $ws
    $r = Invoke-New $ws 'T-RB-001' 'Ui'
    $s = Get-State $ws 'T-RB-001'
    $keys = @(Get-InboxKeys $ws 'T-RB-001')
    $req = Join-Path $ws 'runs\T-RB-001\inbox\readback.v1.md'
    $reqText = if (Test-Path -LiteralPath $req) { Get-Content -LiteralPath $req -Raw -Encoding UTF8 } else { '' }
    $log = Join-Path $ws 'mock-calls.log'
    Check 'T-RB1' 'a manual read-back is requested before the review; the review still runs and is recorded, exit 3' `
        ($r.Code -eq 3 -and $keys -contains 'readback.v1' -and $s.review -and $s.review.verdict -eq 'PASS' -and
         $s.status -eq 'AwaitingApproval' -and -not $s.PSObject.Properties['readback'] -and
         (Count-MockCalls $log 'plan-reviewer' $false) -eq 1 -and (Count-MockCalls $log 'implementer' $true) -eq 0 -and
         $reqText -match 'Write your WHOLE reply into this file' -and $reqText -match 'answers[\\/]readback\.v1\.md' -and
         $reqText -match 'READ-BACK ONLY' -and $r.Out -match 'run\.ps1 readback T-RB-001') `
        ("code=$($r.Code) keys=$($keys -join ',') review=$(if ($s.review) { $s.review.verdict } else { 'none' }) status=$($s.status) impl-readback-calls=$(Count-MockCalls $log 'implementer' $true)")

    # The human's tool writes the answer file itself, where the request's first lines say.
    $answers = Join-Path $ws 'runs\T-RB-001\answers'
    New-Item -ItemType Directory -Force -Path $answers | Out-Null
    Set-Content -LiteralPath (Join-Path $answers 'readback.v1.md') -Encoding UTF8 `
        -Value "FINDING [should] Phase 1: the file name is not given (plan.md)`nVERDICT: FINDINGS"
    $r2 = Run-Orch -Ws $ws -Argv @('readback', 'T-RB-001')
    $s2 = Get-State $ws 'T-RB-001'
    Check 'T-RB2' 'readback consumes the answer the tool wrote and records it for the current version' `
        ($r2.Code -eq 0 -and $s2.readback -and $s2.readback.round -eq 1 -and $s2.readback.verdict -eq 'FINDINGS' -and
         $s2.readback.adapter -eq 'manual' -and $s2.readback.should -eq 1) `
        ("code=$($r2.Code) readback=$(if ($s2.readback) { "$($s2.readback.verdict)/v$($s2.readback.round)/$($s2.readback.adapter)" } else { 'none' })")

    $plan2 = Write-Plan $ws 'plan-T-RB-001-v2.md'
    $r3 = Run-Orch -Ws $ws -Argv @('revise', 'T-RB-001', '-PlanFile', $plan2)
    $q = Run-Orch -Ws $ws -Argv @('queue', 'T-RB-001')
    $s3 = Get-State $ws 'T-RB-001'
    Check 'T-RB3' 'a new version asks for readback.v2, and queue shows only that one (v1 is history, not pending)' `
        ($r3.Code -eq 3 -and $s3.round -eq 2 -and $s3.review.round -eq 2 -and
         $q.Out -match 'T-RB-001\s+readback\.v2' -and $q.Out -notmatch 'readback\.v1' -and $q.Out -match '1 request\(s\) waiting') `
        ("revise=$($r3.Code) round=$($s3.round) queue=$(($q.Out -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1))")
}
catch { Fail-Scenario 'T-RB' 'manual read-back' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-RB4: a stale verifier request is not pending
try {
    $ws = New-WorkSpace 'manual-stale-queue'
    Set-ManualChain $ws 'verifier'
    [void](Invoke-New $ws 'T-RB4-001' 'Ui')
    [void](Invoke-Approve $ws 'T-RB4-001')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-RB4-001', '1'))
    [void](Run-Orch -Ws $ws -Argv @('verify', 'T-RB4-001'))
    Start-Sleep -Milliseconds 1100
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T-RB4-001', '2'))
    [void](Run-Orch -Ws $ws -Argv @('verify', 'T-RB4-001'))
    $keys = @(Get-InboxKeys $ws 'T-RB4-001')
    $q = Run-Orch -Ws $ws -Argv @('queue', 'T-RB4-001')
    Check 'T-RB4' 'two verifier requests on disk, only the newest is pending' `
        ($keys.Count -eq 2 -and $q.Out -match '1 request\(s\) waiting') `
        ("keys=$($keys.Count) queue=$(($q.Out -split "`n" | Where-Object { $_.Trim() } | Select-Object -Last 1))")
}
catch { Fail-Scenario 'T-RB4' 'stale verifier request' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-C1/T-C2/T-C3: close deletes a filed run only
try {
    $ws = New-WorkSpace 'close'
    # T-C1..T-C3 are the number-filing case (plan 0005 D3): say so, so the case means the same thing in
    # both config passes. The slug case is T-C5..T-C8 below.
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'planFiling' 'number' }
    [void](Invoke-New $ws 'T-C-001' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-C-001', '-Branch', 'plan-0099-close-test'))
    $drafts = Join-Path $ws 'runs\_drafts'
    New-Item -ItemType Directory -Force -Path $drafts | Out-Null
    foreach ($n in @('T-C-001.plan.md', 'T-C-001.plan.v2.md', 'T-C-0011.plan.md', 'shared.json')) { Set-Content -LiteralPath (Join-Path $drafts $n) -Value 'x' }
    $runDir = Join-Path $ws 'runs\T-C-001'

    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-001')
    Check 'T-C1' 'close is refused while the plan is not filed in done/, and deletes nothing' `
        ($r.Code -eq 1 -and $r.Out -match 'plan 0099 is not filed' -and (Test-Path -LiteralPath $runDir) -and (Test-Path -LiteralPath (Join-Path $drafts 'T-C-001.plan.md'))) `
        ("code=$($r.Code) run=$(Test-Path -LiteralPath $runDir)")

    $done = Join-Path $ws 'Ui\.ai\plans\done'
    New-Item -ItemType Directory -Force -Path $done | Out-Null
    Set-Content -LiteralPath (Join-Path $done '0099-close-test.md') -Value '# Plan 0099'
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-001')
    $left = @(Get-ChildItem -LiteralPath $drafts -File | ForEach-Object { $_.Name } | Sort-Object)
    Check 'T-C2' 'close deletes the run and only its own drafts once the plan is filed' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath $runDir) -and ($left -join ',') -eq 'shared.json,T-C-0011.plan.md') `
        ("code=$($r.Code) run=$(Test-Path -LiteralPath $runDir) drafts=$($left -join ',')")
    $arch = @(Get-ChildItem -LiteralPath (Join-Path $ws 'runs\_archive') -Filter 'T-C-001.*.json' -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^T-C-001\.\d{12}\.json$' } | ForEach-Object { $_.FullName }) | Select-Object -First 1
    $a = if ($arch -and (Test-Path -LiteralPath $arch)) { Get-Content -LiteralPath $arch -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
    Check 'T-C4' 'close leaves runs/_archive/<id>.<yyyyMMddHHmm>.json with the task, the review texts and the usage map' `
        ($a -and $a.schema -eq 1 -and $a.task.id -eq 'T-C-001' -and @($a.reviews).Count -ge 1 -and $a.reviews[0].round -eq 1 -and $a.reviews[0].text -and $a.PSObject.Properties['usage'] -and $r.Out -match 'archived: ') `
        ("file=$(Test-Path -LiteralPath $arch) task=$(if ($a) { $a.task.id }) reviews=$(if ($a) { @($a.reviews).Count })")

    [void](Invoke-New $ws 'T-C-002' 'Ui')
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-002')
    $r2 = Run-Orch -Ws $ws -Argv @('close', 'T-C-002', '-Force')
    Check 'T-C3' 'a run with no plan number needs -Force, which deletes it' `
        ($r.Code -eq 1 -and $r.Out -match 'names no plan number' -and $r2.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-C-002'))) `
        ("code=$($r.Code) force=$($r2.Code)")
}
catch { Fail-Scenario 'T-C' 'close' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-AR: the archive can be turned off, and a failed write never stops close
try {
    $ws = New-WorkSpace 'archive'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'archive' ([pscustomobject]@{ enabled = $false }) }
    [void](Invoke-New $ws 'T-AR-001' 'Ui')
    $r1 = Run-Orch -Ws $ws -Argv @('close', 'T-AR-001', '-Force')
    $off = -not @(Get-ChildItem -LiteralPath (Join-Path $ws 'runs\_archive') -Filter 'T-AR-001.*' -ErrorAction SilentlyContinue).Count
    Edit-Config -Ws $ws -Change { param($cfg) $cfg.archive.enabled = $true }
    # a file where the _archive folder should be: the write fails
    Set-Content -LiteralPath (Join-Path $ws 'runs\_archive') -Value 'not a folder'
    [void](Invoke-New $ws 'T-AR-002' 'Ui')
    $r2 = Run-Orch -Ws $ws -Argv @('close', 'T-AR-002', '-Force')
    $gone = -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-AR-002'))
    Check 'T-AR' 'archive.enabled=false writes no archive; a failed archive write is one line and close still deletes the run' `
        ($r1.Code -eq 0 -and $off -and $r1.Out -notmatch 'archived: ' -and $r2.Code -eq 0 -and $r2.Out -match '\[archive\] not written' -and $gone) `
        ("off=$($r1.Code)/$off fail=$($r2.Code) note=$($r2.Out -match '\[archive\] not written') gone=$gone")
}
catch { Fail-Scenario 'T-AR' 'archive on close' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-1: limits resolution (D1/D2)
try {
    $ws = New-WorkSpace 'pool-limits'
    # retryDelaySec 20, so the built-in backoff is a number this case can recognise.
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.defaults 'retryDelaySec' 20 }
    $hop0 = [pscustomobject]@{ adapter = 'mock'; model = 'mock-planner' }
    $l0 = Get-HopLimits (Import-OrchConfig (Join-Path $ws 'config.json')) $hop0

    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'maxConcurrent' 3
        Set-CfgProp $cfg.adapters.mock 'pool' 'shared'
    }
    $l1 = Get-HopLimits (Import-OrchConfig (Join-Path $ws 'config.json')) $hop0

    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 7; rateLimitRetries = 2; rateLimitBackoffSec = 60; whenBusy = 'wait'; queueWaitSec = 99 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'free' }
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $l2 = Get-HopLimits $ctx $hop0
    $hop3 = [pscustomobject]@{ adapter = 'mock'; model = 'mock-planner'; pool = 'own'; whenBusy = 'next'; maxConcurrent = 2 }
    $l3 = Get-HopLimits $ctx $hop3

    Check 'P-1' 'limits resolve hop -> adapter -> tier -> built-in, and the pool defaults to the adapter name' `
        ($l0.Tier -eq '' -and $l0.Pool -eq 'mock' -and $l0.MaxConcurrent -eq 0 -and $l0.MinGapSec -eq 0 -and
         $l0.RateLimitRetries -eq 1 -and $l0.RateLimitBackoffSec -eq 20 -and $l0.WhenBusy -eq 'wait' -and $l0.QueueWaitSec -eq 7200 -and
         $l1.Pool -eq 'shared' -and $l1.MaxConcurrent -eq 3 -and
         $l2.Tier -eq 'free' -and $l2.MaxConcurrent -eq 3 -and $l2.MinGapSec -eq 7 -and $l2.RateLimitRetries -eq 2 -and $l2.QueueWaitSec -eq 99 -and
         $l3.Pool -eq 'own' -and $l3.MaxConcurrent -eq 2 -and $l3.WhenBusy -eq 'next') `
        ("builtin: pool=$($l0.Pool) tier='$($l0.Tier)' max=$($l0.MaxConcurrent) gap=$($l0.MinGapSec) retries=$($l0.RateLimitRetries) backoff=$($l0.RateLimitBackoffSec) busy=$($l0.WhenBusy) wait=$($l0.QueueWaitSec) | adapter: pool=$($l1.Pool) max=$($l1.MaxConcurrent) | tier: max=$($l2.MaxConcurrent) gap=$($l2.MinGapSec) retries=$($l2.RateLimitRetries) | hop: pool=$($l3.Pool) max=$($l3.MaxConcurrent) busy=$($l3.WhenBusy)")
}
catch { Fail-Scenario 'P-1' 'limits resolution' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-2/P-12: the second caller waits
try {
    $ws = New-WorkSpace 'pool-queue'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; rateLimitRetries = 1; rateLimitBackoffSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'free' }

    $holder = Start-PoolHolder -Ws $ws -Seconds 4 -TaskId 'T-HOLDER-1' -Role 'plan-reviewer' -Tag 'holder'
    $held = Wait-Events -Path $holder.Events -Verb 'start'
    $prog = Join-Path $ws 'runs\T-PQ-001\progress.json'
    $oldPoll = $env:ORCH_POOL_POLL_MS
    $env:ORCH_POOL_POLL_MS = '200'
    try {
        $plan = Write-Plan $ws 'plan-T-PQ-001.md'
        $bg = Start-Orch -Ws $ws -Argv @('new', 'T-PQ-001', 'Ui', 'mock pooled task', '-PlanFile', $plan) -Tag 'queued'
        $queued = Wait-QueuedProgress -Path $prog -TimeoutSec 30
        $r = Complete-Orch $bg -TimeoutSec 180
    }
    finally { $env:ORCH_POOL_POLL_MS = $oldPoll }
    if ($holder.Proc -and -not $holder.Proc.HasExited) { [void]$holder.Proc.WaitForExit(30000) }

    $end1 = @(Get-Events $holder.Events | Where-Object { $_.Verb -eq 'end' } | ForEach-Object { $_.Ticks })[0]
    $first = @(Get-MockCalls $bg.Log)[0]
    Check 'P-12' 'a waiting hop writes phase queued with pool and heldBy to progress.json' `
        ($queued -and $queued.pool -eq 'mock' -and $queued.heldBy -match 'T-HOLDER-1' -and $queued.state -eq 'queued') `
        ("phase=$(if ($queued) { $queued.phase } else { '-' }) pool=$(if ($queued) { $queued.pool } else { '-' }) state=$(if ($queued) { $queued.state } else { '-' }) heldBy=$(if ($queued) { $queued.heldBy } else { '-' })")
    Check 'P-2' 'one free pool: the second caller names the holder and starts only after the first ends' `
        ($held -and $r.Code -eq 0 -and $r.Out -match 'waiting for pool mock: held by T-HOLDER-1' -and $end1 -gt 0 -and $first -and $first.Ticks -gt $end1) `
        ("held=$held code=$($r.Code) waited=$([bool]($r.Out -match 'waiting for pool mock')) startAfterEnd=$([bool]($first -and $first.Ticks -gt $end1)) line=$((($r.Out -split "`n" | Where-Object { $_ -match 'waiting for pool' } | Select-Object -First 1)) -replace '\s+', ' ')")
}
catch { Fail-Scenario 'P-2' 'free pool queue' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-3: two pools do not wait for each other
try {
    $ws = New-WorkSpace 'pool-two'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'free' }
    Add-SecondAdapter -Ws $ws -Pool 'pb'

    $a = Start-PoolHolder -Ws $ws -Seconds 3 -TaskId 'T-POOL-A' -Adapter 'mock' -Tag 'pa'
    $b = Start-PoolHolder -Ws $ws -Seconds 3 -TaskId 'T-POOL-B' -Adapter 'mock2' -Tag 'pb'
    $okA = Wait-Events -Path $a.Events -Verb 'start' -TimeoutSec 20
    $okB = Wait-Events -Path $b.Events -Verb 'start' -TimeoutSec 20
    if ($a.Proc -and -not $a.Proc.HasExited) { [void]$a.Proc.WaitForExit(30000) }
    if ($b.Proc -and -not $b.Proc.HasExited) { [void]$b.Proc.WaitForExit(30000) }

    $startA = @(Get-Events $a.Events | Where-Object { $_.Verb -eq 'start' } | ForEach-Object { $_.Ticks })[0]
    $endA = @(Get-Events $a.Events | Where-Object { $_.Verb -eq 'end' } | ForEach-Object { $_.Ticks })[0]
    $startB = @(Get-Events $b.Events | Where-Object { $_.Verb -eq 'start' } | ForEach-Object { $_.Ticks })[0]
    $waitedA = ((Get-Content -LiteralPath $a.Out -Raw -ErrorAction SilentlyContinue) -match 'waiting for pool')
    $waitedB = ((Get-Content -LiteralPath $b.Out -Raw -ErrorAction SilentlyContinue) -match 'waiting for pool')
    Check 'P-3' 'two pools: both callers hold a slot at the same time and neither waits' `
        ($okA -and $okB -and $startA -gt 0 -and $startB -gt 0 -and $endA -gt $startB -and -not $waitedA -and -not $waitedB -and
         (Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\mock')) -and (Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\pb'))) `
        ("startA=$startA startB=$startB endA=$endA overlapped=$([bool]($endA -gt $startB)) waitedA=$([bool]$waitedA) waitedB=$([bool]$waitedB)")
}
catch { Fail-Scenario 'P-3' 'two pools' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-4: paid is one slot; 0 is unlimited
try {
    $ws = New-WorkSpace 'pool-paid'
    Set-Tiers -Ws $ws -Paid @{ maxConcurrent = 1; minGapSec = 0; rateLimitRetries = 1; rateLimitBackoffSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'paid' }

    $a = Start-PoolHolder -Ws $ws -Seconds 2 -TaskId 'T-PAID-1' -Tag 'p1'
    $okA = Wait-Events -Path $a.Events -Verb 'start' -TimeoutSec 20
    $b = Start-PoolHolder -Ws $ws -Seconds 1 -TaskId 'T-PAID-2' -Tag 'p2'
    if ($a.Proc -and -not $a.Proc.HasExited) { [void]$a.Proc.WaitForExit(30000) }
    if ($b.Proc -and -not $b.Proc.HasExited) { [void]$b.Proc.WaitForExit(30000) }
    $startB = @(Get-Events $b.Events | Where-Object { $_.Verb -eq 'start' } | ForEach-Object { $_.Ticks })[0]
    $endA = @(Get-Events $a.Events | Where-Object { $_.Verb -eq 'end' } | ForEach-Object { $_.Ticks })[0]
    $waited = ((Get-Content -LiteralPath $b.Out -Raw -ErrorAction SilentlyContinue) -match 'waiting for pool mock: held by T-PAID-1')
    Check 'P-4' 'the shipped paid shape is one slot: the second caller waits for the first to end' `
        ($okA -and $startB -gt 0 -and $endA -gt 0 -and $startB -ge $endA -and $waited) `
        ("startB=$startB endA=$endA serial=$([bool]($startB -ge $endA)) waited=$([bool]$waited)")

    # D2: an explicit 0 means unlimited - no lock file at all, so two callers overlap.
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'maxConcurrent' 0 }
    Remove-Item -LiteralPath (Join-Path $ws 'runs\_pool') -Recurse -Force -ErrorAction SilentlyContinue
    $c = Start-PoolHolder -Ws $ws -Seconds 2 -TaskId 'T-UNL-1' -Tag 'u1'
    $d = Start-PoolHolder -Ws $ws -Seconds 2 -TaskId 'T-UNL-2' -Tag 'u2'
    $okC = Wait-Events -Path $c.Events -Verb 'start' -TimeoutSec 20
    $okD = Wait-Events -Path $d.Events -Verb 'start' -TimeoutSec 20
    if ($c.Proc -and -not $c.Proc.HasExited) { [void]$c.Proc.WaitForExit(30000) }
    if ($d.Proc -and -not $d.Proc.HasExited) { [void]$d.Proc.WaitForExit(30000) }
    $startC = @(Get-Events $c.Events | Where-Object { $_.Verb -eq 'start' } | ForEach-Object { $_.Ticks })[0]
    $endC = @(Get-Events $c.Events | Where-Object { $_.Verb -eq 'end' } | ForEach-Object { $_.Ticks })[0]
    $startD = @(Get-Events $d.Events | Where-Object { $_.Verb -eq 'start' } | ForEach-Object { $_.Ticks })[0]
    $locks = @(Get-ChildItem -LiteralPath (Join-Path $ws 'runs\_pool\mock') -Filter '*.lock' -File -ErrorAction SilentlyContinue)
    Check 'P-4' 'maxConcurrent 0 lets two callers overlap and creates no lock file' `
        ($okC -and $okD -and $startC -gt 0 -and $startD -gt 0 -and $startD -lt $endC -and $locks.Count -eq 0) `
        ("overlapped=$([bool]($startD -lt $endC)) locks=$($locks.Count)")
}
catch { Fail-Scenario 'P-4' 'paid pool' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-5: whenBusy next skips to the next hop
try {
    $ws = New-WorkSpace 'pool-next'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; whenBusy = 'next'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'tier' 'free'
        # The implementer's second hop moves to the other pool, so "the next hop" is a real fallback.
        $cfg.roles.implementer.chain[1].adapter = 'mock2'
    }
    Add-SecondAdapter -Ws $ws -Pool 'pb'
    [void](Invoke-New $ws 'T-NB-001' 'Ui')
    [void](Invoke-Approve $ws 'T-NB-001')

    $holder = Start-PoolHolder -Ws $ws -Seconds 6 -TaskId 'T-NB-HOLD' -Role 'implementer' -Tag 'next'
    $held = Wait-Events -Path $holder.Events -Verb 'start' -TimeoutSec 20
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T-NB-001', '1')
    Stop-PoolHolder $holder

    $s = Get-State $ws 'T-NB-001'
    $tr = Get-Content -LiteralPath (Join-Path $ws 'runs\T-NB-001\transcript\implementer.phase-1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $a0 = @($tr.Attempts)[0]
    $a1 = @($tr.Attempts)[1]
    Check 'P-5' 'whenBusy next: the busy hop is skipped with its reason and the next hop answers' `
        ($held -and $r.Code -eq 0 -and $s.status -eq 'Implemented' -and $a0.adapter -eq 'mock' -and $a0.reason -match 'skipped: pool mock busy \(held by T-NB-HOLD' -and $a0.pool -eq 'mock' -and $a1.adapter -eq 'mock2' -and $a1.ok) `
        ("code=$($r.Code) status=$($s.status) attempt1=$(($a0.reason)) attempt2=$($a1.adapter):$($a1.reason)")
}
catch { Fail-Scenario 'P-5' 'whenBusy next' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-6: queueWaitSec runs out
try {
    $ws = New-WorkSpace 'pool-timeout'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; whenBusy = 'wait'; queueWaitSec = 1 }
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'tier' 'free'
        $cfg.roles.implementer.chain[1].adapter = 'mock2'
    }
    Add-SecondAdapter -Ws $ws -Pool 'pb'
    [void](Invoke-New $ws 'T-QT-001' 'Ui')
    [void](Invoke-Approve $ws 'T-QT-001')

    $holder = Start-PoolHolder -Ws $ws -Seconds 8 -TaskId 'T-QT-HOLD' -Role 'implementer' -Tag 'qtimeout'
    $held = Wait-Events -Path $holder.Events -Verb 'start' -TimeoutSec 20
    $oldPoll = $env:ORCH_POOL_POLL_MS
    $env:ORCH_POOL_POLL_MS = '200'
    try { $r = Run-Orch -Ws $ws -Argv @('implement', 'T-QT-001', '1') }
    finally { $env:ORCH_POOL_POLL_MS = $oldPoll }
    Stop-PoolHolder $holder

    $s = Get-State $ws 'T-QT-001'
    $tr = Get-Content -LiteralPath (Join-Path $ws 'runs\T-QT-001\transcript\implementer.phase-1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $a0 = @($tr.Attempts)[0]
    $a1 = @($tr.Attempts)[1]
    Check 'P-6' 'queueWaitSec runs out: the hop fails as queue-timeout and the next hop answers' `
        ($held -and $r.Code -eq 0 -and $s.status -eq 'Implemented' -and $a0.reason -eq 'queue-timeout' -and $a0.pool -eq 'mock' -and $a1.adapter -eq 'mock2' -and $a1.ok) `
        ("code=$($r.Code) status=$($s.status) attempt1=$($a0.reason) attempt2=$($a1.adapter):$($a1.reason)")
}
catch { Fail-Scenario 'P-6' 'queue-timeout' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-7: rate-limit retries (D9)
try {
    $ws = New-WorkSpace 'pool-rate'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; rateLimitRetries = 2; rateLimitBackoffSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'tier' 'free'
        # A second planner hop, so "the chain moves on" has somewhere to move to.
        $cfg.roles.planner.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-planner'; family = 'plannerfam' },
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-planner-b'; family = 'plannerfamb' }
        )
    }
    $log = Join-Path $ws 'mock-calls.log'
    $oldTimes = $env:MOCK_RATE_LIMIT_TIMES
    try {
        # No -PlanFile: that is conductor mode, which skips the planner - and the planner is the hop
        # whose retries this case counts.
        $env:MOCK_RATE_LIMIT_TIMES = '2'
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($log + '.429') -Force -ErrorAction SilentlyContinue
        $r1 = Run-Orch -Ws $ws -Argv @('new', 'T-RL-001', 'Ui', 'mock rate-limited task')
        $calls1 = @(Get-MockCalls $log | Where-Object { $_.Model -eq 'mock-planner' }).Count

        $env:MOCK_RATE_LIMIT_TIMES = '3'
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($log + '.429') -Force -ErrorAction SilentlyContinue
        $r2 = Run-Orch -Ws $ws -Argv @('new', 'T-RL-002', 'Ui', 'mock rate-limited task')
        $calls2 = @(Get-MockCalls $log | Where-Object { $_.Model -eq 'mock-planner' }).Count
        $calls2b = @(Get-MockCalls $log | Where-Object { $_.Model -eq 'mock-planner-b' }).Count
    }
    finally { $env:MOCK_RATE_LIMIT_TIMES = $oldTimes }
    Check 'P-7' 'rateLimitRetries 2 gives three attempts, then the chain moves on to the next hop' `
        ($r1.Code -eq 0 -and $calls1 -eq 3 -and $r2.Code -eq 0 -and $calls2 -eq 3 -and $calls2b -eq 1) `
        ("two429: code=$($r1.Code) plannerCalls=$calls1 | three429: code=$($r2.Code) plannerCalls=$calls2 nextHopCalls=$calls2b")
}
catch { Fail-Scenario 'P-7' 'rate-limit retries' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-8: cool-down (D8)
try {
    $ws = New-WorkSpace 'pool-cool'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; rateLimitRetries = 2; rateLimitBackoffSec = 6; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'free' }
    $log = Join-Path $ws 'mock-calls.log'
    $oldTimes = $env:MOCK_RATE_LIMIT_TIMES
    $oldPoll = $env:ORCH_POOL_POLL_MS
    $oldScale = $env:ORCH_POOL_BACKOFF_SCALE
    try {
        $env:MOCK_RATE_LIMIT_TIMES = '1'   # exactly one 429, so only the first run is ever throttled
        $env:ORCH_POOL_POLL_MS = '200'
        $env:ORCH_POOL_BACKOFF_SCALE = ''
        $planA = Write-Plan $ws 'plan-T-CO-001.md'
        $planB = Write-Plan $ws 'plan-T-CO-002.md'
        $a = Start-Orch -Ws $ws -Argv @('new', 'T-CO-001', 'Ui', 'mock pooled task', '-PlanFile', $planA) -Tag 'coolA'
        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $deadline -and @(Get-MockCalls $log).Count -lt 1) { Start-Sleep -Milliseconds 100 }
        $throttled = @(Get-MockCalls $log).Count -ge 1
        Start-Sleep -Milliseconds 500   # let A print the 429 and write the cool-down
        $b = Start-Orch -Ws $ws -Argv @('new', 'T-CO-002', 'Ui', 'mock pooled task', '-PlanFile', $planB) -Tag 'coolB'
        $rb = Complete-Orch $b -TimeoutSec 180
        $ra = Complete-Orch $a -TimeoutSec 180
    }
    finally {
        $env:MOCK_RATE_LIMIT_TIMES = $oldTimes
        $env:ORCH_POOL_POLL_MS = $oldPoll
        $env:ORCH_POOL_BACKOFF_SCALE = $oldScale
    }
    $coolLine = (($rb.Out -split "`n" | Where-Object { $_ -match 'cooling down until' } | Select-Object -First 1) -replace '\s+', ' ')
    Check 'P-8' 'a pool inside its 429 backoff reports "cooling down" to the next caller, which waits' `
        ($throttled -and $ra.Code -eq 0 -and $rb.Code -eq 0 -and $coolLine) `
        ("throttled=$throttled A=$($ra.Code) B=$($rb.Code) line='$coolLine'")
}
catch { Fail-Scenario 'P-8' 'cool-down' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-9: a killed holder frees its slot (D5)
try {
    $ws = New-WorkSpace 'pool-kill'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'tier' 'free' }

    $holder = Start-PoolHolder -Ws $ws -Seconds 120 -TaskId 'T-KILL-1' -Tag 'kill'
    $held = Wait-Events -Path $holder.Events -Verb 'start' -TimeoutSec 20
    $prog = Join-Path $ws 'runs\T-KL-001\progress.json'
    $oldPoll = $env:ORCH_POOL_POLL_MS
    $env:ORCH_POOL_POLL_MS = '200'
    try {
        $plan = Write-Plan $ws 'plan-T-KL-001.md'
        $bg = Start-Orch -Ws $ws -Argv @('new', 'T-KL-001', 'Ui', 'mock pooled task', '-PlanFile', $plan) -Tag 'killed'
        $queued = Wait-QueuedProgress -Path $prog -TimeoutSec 30
        Stop-PoolHolder $holder
        $killedAt = (Get-Date).Ticks
        $r = Complete-Orch $bg -TimeoutSec 180
    }
    finally { $env:ORCH_POOL_POLL_MS = $oldPoll }

    $first = @(Get-MockCalls $bg.Log)[0]
    $afterSecs = if ($first -and $first.Ticks -gt 0) { [math]::Round([TimeSpan]::FromTicks($first.Ticks - $killedAt).TotalSeconds, 1) } else { -1 }
    Check 'P-9' 'a holder killed with Stop-Process -Force frees its slot for the next caller within two polls' `
        ($held -and $queued -and $holder.Proc.HasExited -and $r.Code -eq 0 -and $afterSecs -ge 0 -and $afterSecs -lt 3) `
        ("held=$held queued=$([bool]$queued) exited=$($holder.Proc.HasExited) code=$($r.Code) startedAfterSecs=$afterSecs")
}
catch { Fail-Scenario 'P-9' 'killed holder' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-10: an inbox hop takes no slot (D4)
try {
    $ws = New-WorkSpace 'pool-inbox'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1; minGapSec = 0; whenBusy = 'wait'; queueWaitSec = 60 }
    Set-ManualChain $ws 'plan-reviewer'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'tier' 'free'
        Set-CfgProp $cfg.adapters.manual 'tier' 'free'
    }
    # A plain `new`, so the planner (the mock, a real CLI hop) runs and its slot can be looked for.
    $r = Run-Orch -Ws $ws -Argv @('new', 'T-IN-001', 'Ui', 'mock inbox task')
    Check 'P-10' 'an inbox hop takes no slot, even when its adapter carries a free tier' `
        ($r.Code -eq 3 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\manual')) -and
         (Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\mock'))) `
        ("code=$($r.Code) manualPool=$(Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\manual')) mockPool=$(Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\mock'))")
}
catch { Fail-Scenario 'P-10' 'inbox hop' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P-11: run.ps1 pools (D12)
try {
    $ws = New-WorkSpace 'pool-list'
    Set-Tiers -Ws $ws -Free @{ maxConcurrent = 1 } -Paid @{ maxConcurrent = 1 }
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'tier' 'free'
        $cfg.roles.verifier.chain[0].adapter = 'mock2'
    }
    Add-SecondAdapter -Ws $ws -Pool 'paidpool' -Tier 'paid'
    $r = Run-Orch -Ws $ws -Argv @('pools')
    $lineMock = (($r.Out -split "`n" | Where-Object { $_ -match '^mock\s' } | Select-Object -First 1) -replace '\s+', ' ')
    $linePaid = (($r.Out -split "`n" | Where-Object { $_ -match '^paidpool\s' } | Select-Object -First 1) -replace '\s+', ' ')

    $holder = Start-PoolHolder -Ws $ws -Seconds 5 -TaskId 'T-POOLS-1' -Adapter 'mock' -Tag 'pools'
    $held = Wait-Events -Path $holder.Events -Verb 'start' -TimeoutSec 20
    $r2 = Run-Orch -Ws $ws -Argv @('pools')
    Stop-PoolHolder $holder
    $lineBusy = (($r2.Out -split "`n" | Where-Object { $_ -match '^mock\s' } | Select-Object -First 1) -replace '\s+', ' ')
    Check 'P-11' 'pools lists each configured pool with its tier and 0/1, and shows the holder while one is held' `
        ($r.Code -eq 0 -and $lineMock -match '^mock free 0/1' -and $linePaid -match '^paidpool paid 0/1' -and
         $held -and $r2.Code -eq 0 -and $lineBusy -match '^mock free 1/1' -and $lineBusy -match 'T-POOLS-1') `
        ("free='$lineMock' paid='$linePaid' held='$lineBusy'")
}
catch { Fail-Scenario 'P-11' 'pools command' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T1: a manual hop is allowed everywhere but the planner (D8)
try {
    $ws = New-WorkSpace 'manual-impl-ok'
    Set-ManualChain $ws 'implementer'
    $r1 = Run-Orch -Ws $ws -Argv @('status', 'T-NONE')
    $ws2 = New-WorkSpace 'manual-planner-bad'
    Set-ManualChain $ws2 'planner'
    $r2 = Run-Orch -Ws $ws2 -Argv @('status', 'T-NONE')
    Check 'T1' 'a manual implementer hop loads; a manual planner hop throws the planner-only message' `
        ($r1.Out -notmatch 'has a manual hop' -and
         $r2.Code -eq 1 -and $r2.Out -match "'planner' has a manual hop" -and $r2.Out -match 'the planner is the one role') `
        ("impl=$($r1.Code) planner=$($r2.Code)")
}
catch { Fail-Scenario 'T1' 'manual hop placement' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T2: per-model Claude families and gate-2 exclusion (D1)
try {
    $fOpus = Get-HopFamily ([pscustomobject]@{ adapter = 'claude'; model = 'opus' })
    $fSonnet = Get-HopFamily ([pscustomobject]@{ adapter = 'claude'; model = 'sonnet' })
    $fHaiku = Get-HopFamily ([pscustomobject]@{ adapter = 'claude'; model = 'claude-haiku-5-5' })
    $fExplicit = Get-HopFamily ([pscustomobject]@{ adapter = 'claude'; model = 'opus'; family = 'explicit' })
    Check 'T2' 'Get-HopFamily: claude/opus -> claude-opus, sonnet -> claude-sonnet, haiku -> claude-haiku, explicit wins' `
        ($fOpus -eq 'claude-opus' -and $fSonnet -eq 'claude-sonnet' -and $fHaiku -eq 'claude-haiku' -and $fExplicit -eq 'explicit') `
        ("opus=$fOpus sonnet=$fSonnet haiku=$fHaiku explicit=$fExplicit")

    $ws = New-WorkSpace 'family-exclude'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        $cfg.roles.'plan-reviewer'.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-rev-sonnet'; family = 'claude-sonnet' },
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-rev-glm'; family = 'glm' })
    }
    $planA = Write-Plan $ws 'plan-fam-a.md' -PlannedBy 'Claude Sonnet 5.5, 2026-01-01'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T-FAM-A', 'Ui', 'mock family task', '-PlanFile', $planA))
    $sA = Get-State $ws 'T-FAM-A'
    $plan = Write-Plan $ws 'plan-fam-b.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T-FAM-B', 'Ui', 'mock family task', '-PlanFile', $plan))
    $sB = Get-State $ws 'T-FAM-B'
    Check 'T2' 'a claude-sonnet plan skips the claude-sonnet reviewer hop; a claude-opus plan may use it' `
        ($sA.review.model -eq 'mock-rev-glm' -and $sB.review.model -eq 'mock-rev-sonnet') `
        ("sonnetPlan=$($sA.review.model) opusPlan=$($sB.review.model)")
}
catch { Fail-Scenario 'T2' 'families and exclusion' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T3: the plan's **Planned by:** line sets the family; a header-less revise keeps the previous
try {
    $ws = New-WorkSpace 'revise-family'
    $plan = Write-Plan $ws 'plan-rf.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T-RF-001', 'Ui', 'mock revise family', '-PlanFile', $plan))
    $s0 = Get-State $ws 'T-RF-001'
    # the header says Sonnet; a conflicting flag (the conductor's own model) is a slip, fixed with a note
    $v2 = Write-Plan $ws 'plan-rf.v2.md' -PlannedBy 'Claude Sonnet 5.5 (planner seat), 2026-01-02'
    $r1 = Run-Orch -Ws $ws -Argv @('revise', 'T-RF-001', '-PlanFile', $v2, '-PlanFamily', 'deepseek')
    $s1 = Get-State $ws 'T-RF-001'
    # no header, no flag: the previous family is kept
    $v3 = Write-Plan $ws 'plan-rf.v3.md' -PlannedBy ''
    $r2 = Run-Orch -Ws $ws -Argv @('revise', 'T-RF-001', '-PlanFile', $v3)
    $s2 = Get-State $ws 'T-RF-001'
    Check 'T3' 'new takes the family from Planned by; a conflicting -PlanFamily is ignored with a note; a header-less revise keeps the previous' `
        ($s0.plan.family -eq 'claude-opus' -and $r1.Code -eq 0 -and $s1.plan.family -eq 'claude-sonnet' -and $r1.Out -match '-PlanFamily deepseek ignored' -and
         $r2.Code -eq 0 -and $s2.plan.family -eq 'claude-sonnet' -and $r2.Out -match 'keeping the previous family') `
        ("new=$($s0.plan.family) header=$($s1.plan.family) headerless=$($s2.plan.family)")
}
catch { Fail-Scenario 'T3' 'revise family' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-PF: a plan that does not say who wrote it stops `new` before the run exists
try {
    $ws = New-WorkSpace 'plan-family-stop'
    $bare = Write-Plan $ws 'plan-pf.md' -PlannedBy ''
    $r1 = Run-Orch -Ws $ws -Argv @('new', 'T-PF-001', 'Ui', 'mock no author', '-PlanFile', $bare)
    $made = Test-Path -LiteralPath (Join-Path $ws 'runs\T-PF-001\task.json')
    # a person or an unknown model: an explicit -PlanFamily is accepted, with a warning
    $who = Write-Plan $ws 'plan-pf2.md' -PlannedBy 'the owner, 2026-01-01'
    $r2 = Run-Orch -Ws $ws -Argv @('new', 'T-PF-002', 'Ui', 'mock owner plan', '-PlanFile', $who, '-PlanFamily', 'human')
    $s2 = Get-State $ws 'T-PF-002'
    Check 'T-PF' 'no model in Planned by and no flag stops new and creates no run; with -PlanFamily it runs with a warning' `
        ($r1.Code -ne 0 -and $r1.Out -match 'does not say who wrote it' -and -not $made -and
         $r2.Code -eq 0 -and $s2.plan.family -eq 'human' -and $r2.Out -match 'names no known model') `
        ("stop=$($r1.Code) made=$made flagged=$($r2.Code)/$($s2.plan.family)")
}
catch { Fail-Scenario 'T-PF' 'plan family from header' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T4: quota exhaustion marks the pool and skips its other hops (D2)
try {
    $ws = New-WorkSpace 'quota-exhaust'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg 'quota' ([pscustomobject]@{ patterns = @('usage limit', 'limit reached', 'quota exceeded', 'exceeded your', 'insufficient balance', 'insufficient credit', 'free usage'); exhaustedSec = 1800 })
        # Two hops of poolA then one of poolB: the first hits the quota, the second must be skipped
        # without a call, the third (another pool) must still run.
        $cfg.roles.planner.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-plan-a'; family = 'famA'; pool = 'poolA' },
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-plan-b'; family = 'famA2'; pool = 'poolA' },
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-plan-c'; family = 'famB'; pool = 'poolB' })
    }
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $fake = [pscustomobject]@{ ExitCode = 1; Stdout = ''; Stderr = 'Usage limit reached'; TimedOut = $false; TimeoutKind = $null }
    $class = Get-FailureClass $fake $ctx
    $log = Join-Path $ws 'mock-calls.log'
    $oldQuota = $env:MOCK_QUOTA_TIMES
    try {
        $env:MOCK_QUOTA_TIMES = '1'
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($log + '.quota') -Force -ErrorAction SilentlyContinue
        $r = Run-Orch -Ws $ws -Argv @('new', 'T-Q-001', 'Ui', 'mock quota task')
    }
    finally { $env:MOCK_QUOTA_TIMES = $oldQuota }
    $calls = @(Get-MockCalls $log)
    $a = @($calls | Where-Object { $_.Role -eq 'planner' -and $_.Model -eq 'mock-plan-a' }).Count
    $b = @($calls | Where-Object { $_.Role -eq 'planner' -and $_.Model -eq 'mock-plan-b' }).Count
    $c = @($calls | Where-Object { $_.Role -eq 'planner' -and $_.Model -eq 'mock-plan-c' }).Count
    $marker = Test-Path -LiteralPath (Join-Path $ws 'runs\_pool\poolA\exhausted-until.txt')
    $p = Run-Orch -Ws $ws -Argv @('pools')
    Check 'T4' 'quota-exhausted: one call, the pool marker exists, the same-pool hop is skipped, another pool runs, pools says so' `
        ($class -eq 'quota-exhausted' -and $r.Code -eq 0 -and $a -eq 1 -and $b -eq 0 -and $c -eq 1 -and
         $marker -and $p.Out -match 'quota exhausted until') `
        ("class=$class code=$($r.Code) a=$a b=$b c=$c marker=$marker pools=$($p.Out -match 'quota exhausted until')")
}
catch { Fail-Scenario 'T4' 'quota exhaustion' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T5: a 429 is still rate-limit, not quota-exhausted (D2)
try {
    $ctx = Import-OrchConfig (Join-Path $Orch 'examples\orchestrator.json')
    $fake = [pscustomobject]@{ ExitCode = 1; Stdout = ''; Stderr = '429 Too Many Requests'; TimedOut = $false; TimeoutKind = $null }
    $class = Get-FailureClass $fake $ctx
    Check 'T5' 'a 429 stays rate-limit (retried), not quota-exhausted, with the quota patterns present' `
        ($class -eq 'rate-limit') ("class=$class")
}
catch { Fail-Scenario 'T5' '429 still rate-limit' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T6: reviewer/read-back suggest a fix; a FIX line still counts (D4)
try {
    . (Join-Path $Orch 'lib\bus.ps1')
    $rev = Get-RoleProtocol -Role 'plan-reviewer' -Branch 'alireza/x'
    $rb = Get-RoleProtocol -Role 'readback' -Branch 'alireza/x'
    $f = @(Get-Findings '1. FINDING [blocker] §3: x (doc) — FIX: y')
    Check 'T6' 'reviewer and read-back protocols carry — FIX:, and a numbered FIX finding still counts as one blocker' `
        ($rev -match '— FIX:' -and $rb -match '— FIX:' -and $f.Count -eq 1 -and $f[0].Level -eq 'blocker') `
        ("rev=$($rev -match '— FIX:') rb=$($rb -match '— FIX:') findings=$($f.Count) level=$(if ($f.Count) { $f[0].Level } else { '-' })")
}
catch { Fail-Scenario 'T6' 'suggested fix' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-R4: a finding restated as RESOLVED is not counted (T-AGENT-027 v5)
try {
    . (Join-Path $Orch 'lib\bus.ps1')
    $text = @"
1. **FINDING [should] §11 — Android gate:** RESOLVED. v5 says so.
2. **FINDING [nit] §6.4 — IsOpen:** RESOLVED.
3. FINDING [should] D8 — cite: NOT RESOLVED, still :44
4. FINDING [blocker] §3 — new problem — FIX: y
5. FINDING [nit] §2 — UNRESOLVED wording
VERDICT: FINDINGS
"@
    $f = @(Get-Findings $text)
    $levels = ($f | ForEach-Object { $_.Level }) -join ','
    Check 'T-R4' 'findings restated as RESOLVED are skipped; NOT RESOLVED / UNRESOLVED still count' `
        ($f.Count -eq 3 -and $levels -eq 'should,blocker,nit') ("count=$($f.Count) levels=$levels")
}
catch { Fail-Scenario 'T-R4' 'resolved findings' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-R3: a manifest path elided with a leading ellipsis still counts as in the plan (plan 0153)
try {
    . (Join-Path $Orch 'lib\gates.ps1')
    $el = [string][char]0x2026
    $planT = "| 4 | ``${el}Presentation/Pages/Shell/ShellText.cs`` | S12 |`n| 5 | ``...Android/ProjectB.Agent.Android.csproj`` | x |"
    $chg = @('Src/03.Endpoint/ProjectB.Agent.Presentation/Pages/Shell/ShellText.cs',
        'Src/03.Endpoint/ProjectB.Agent.Android/ProjectB.Agent.Android.csproj',
        'Src/03.Endpoint/ProjectB.Agent.Presentation/Pages/Shell/Other.cs')
    $out = @(Get-FilesOutsidePlan -Changed $chg -Mentions (Get-PlanPathMentions $planT 'Agent'))
    Check 'T-R3' 'manifest rows written with a leading ellipsis (unicode or ...) match as a path suffix; an unlisted sibling is still outside' `
        ($out.Count -eq 1 -and $out[0] -like '*/Other.cs') ("outside=$($out -join ',')")
}
catch { Fail-Scenario 'T-R3' 'elided manifest paths' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-R2: opencode write roles name the build agent (a continued plan-mode session stays read-only otherwise)
try {
    . (Join-Path $Orch 'lib\agents.ps1')
    $script:ocArgs = @()
    function Invoke-External { param($Exe, $ArgList, $StdIn, $Cwd, $TimeoutSec, $IdleTimeoutSec, $ProgressFile, $ProgressMeta) $script:ocArgs += , @($ArgList); [pscustomobject]@{ ExitCode = 0; Stdout = ''; TimedOut = $false } }
    Invoke-OpenCode -Model 'm' -Prompt 'p' -Cwd $env:TEMP -Auto $false -ReadOnly $false -TimeoutSec 5 -SessionId 'ses_x' | Out-Null
    Invoke-OpenCode -Model 'm' -Prompt 'p' -Cwd $env:TEMP -Auto $false -ReadOnly $true -TimeoutSec 5 -SessionId 'ses_x' | Out-Null
    $w = $script:ocArgs[0] -join ' '; $r = $script:ocArgs[1] -join ' '
    Remove-Item Function:\Invoke-External
    . (Join-Path $Orch 'lib\agents.ps1')
    Check 'T-R2' 'opencode: a write hop passes --agent build, a read-only hop --agent plan (also when continuing a session)' `
        ($w -match '--agent build' -and $w -notmatch '--agent plan' -and $r -match '--agent plan' -and $r -notmatch '--agent build') ("write='$w' readonly='$r'")
}
catch { Fail-Scenario 'T-R2' 'opencode agent flag' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-L1: the ledger block carries a cost column computed from modelRates
try {
    . (Join-Path $Orch 'lib\bus.ps1')
    . (Join-Path $Orch 'lib\agents.ps1')
    . (Join-Path $Orch 'lib\gates.ps1')
    $runAst = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $Orch 'run.ps1'), [ref]$null, [ref]$null)
    foreach ($fn in 'Format-VerdictHead', 'Format-TokenCount', 'Format-TokenUnit', 'Format-Duration', 'ConvertFrom-OpenCodeRateData', 'Get-LiveModelRates', 'Get-RateSourceNote', 'Get-ModelRate', 'Get-UsageCost', 'Format-Usd', 'Format-CostTotal', 'Get-RoleLabel', 'Get-SessionHopRoles', 'Get-RoleUsageRows', 'Format-RowCost', 'Write-LedgerEntry') {
        $def = $runAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq $fn }, $true)[0]
        . ([scriptblock]::Create($def.Extent.Text))
    }
    # Token totals the test controls: no opencode call.
    function Get-OpenCodeSessionUsage {
        param([string]$SessionId, [string]$Cwd)
        if ($SessionId -eq 'ses_glm') { return [pscustomobject]@{ requests = 3L; input = 233200L; output = 5600L; reasoning = 10600L; cacheRead = 1100000L; cacheWrite = 0L } }
        return [pscustomobject]@{ requests = 1L; input = 1000L; output = 0L; reasoning = 0L; cacheRead = 0L; cacheWrite = 0L }
    }
    $lws = Join-Path $env:TEMP ("project-a-orch-ledger-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Force -Path $lws | Out-Null
    $lctx = Import-OrchConfig (Join-Path $Orch 'examples\orchestrator.json')
    $lctx.RunsDir = $lws
    $lstate = [pscustomobject]@{ id = 'T-L1'; repo = 'Web'; status = 'Verified'; round = 1; phases = @()
        sessions = [pscustomobject]@{ ses_glm = [pscustomobject]@{ adapter = 'opencode'; model = 'opencode-go/glm-5.3-flash'; role = 'plan-reviewer'; turns = 2 } } }
    [void](Write-LedgerEntry -Ctx $lctx -State $lstate -Event 'verify' 6>$null)
    $l1 = Get-Content -LiteralPath (Join-Path $lws '_ledger.md') -Raw -Encoding UTF8
    # an unpriced model makes the total a lower bound, never 0
    $lstate.sessions | Add-Member -NotePropertyName ses_x -NotePropertyValue ([pscustomobject]@{ adapter = 'opencode'; model = 'x/unpriced'; role = 'readback'; turns = 1 })
    Remove-Item -LiteralPath (Join-Path $lws '_ledger.md') -Force
    [void](Write-LedgerEntry -Ctx $lctx -State $lstate -Event 'close' 6>$null)
    $l2 = Get-Content -LiteralPath (Join-Path $lws '_ledger.md') -Raw -Encoding UTF8
    Remove-Item Function:\Get-OpenCodeSessionUsage
    if (-not $Keep) { Remove-Item -LiteralPath $lws -Recurse -Force -ErrorAction SilentlyContinue }
    $head = $l1 -match '\| cache read \| cost \|'
    $row = $l1 -match '\| ses_glm \|[^\n]*\| \$0\.072 \|'
    $tot = $l1 -match '\| \*\*total\*\* \|[^\n]*\| \$0\.072 \|'
    $low = $l2 -match '\| ses_x \|[^\n]*\| \? \|' -and $l2 -match '\| \*\*total\*\* \|[^\n]*\| >= \$0\.072 \(1 model\(s\) unpriced\) \|'
    # The stage table: the stub has no per-turn data, so the session goes whole to its first role (no time).
    $stage = $l1 -match '\| stage \| model \| sessions \| calls \| requests \|' -and $l1 -match '\| review \| opencode-go/glm-5.3-flash \| 1 \| 2 \| 3 \|[^\n]*\| - \| \$0\.072 \|' -and $l1 -match '(?m)^total\s+\$0\.072'
    Check 'T-L1' 'the ledger has a cost column, a numeric total and a by-stage table; an unpriced model prints ? and a lower-bound total' `
        ($head -and $row -and $tot -and $low -and $stage) ("head=$head row=$row total=$tot unpriced=$low stage=$stage")
}
catch { Fail-Scenario 'T-L1' 'ledger cost column' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-L2: live prices (opencode.ai/data) win over modelRates; no cache = modelRates
try {
    # T-L1 dot-sourced the rate functions. PROJECTA_ORCH_NO_RATE_FETCH=1 keeps this off the network.
    $sample = [pscustomobject]@{ updatedAt = '2026-11-17T00:00:00Z'; tokenPricesUsdPer1M = @(
            [pscustomobject]@{ model = 'glm-5.3-flash'; input = 1; output = 2; cachedInput = 0.5 },
            [pscustomobject]@{ model = 'deepseek-v4.1-flash'; input = 0.15; output = 0.6; cachedInput = 0.003 }) }
    $parsed = ConvertFrom-OpenCodeRateData $sample
    $rws = Join-Path $env:TEMP ("project-a-orch-rates-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Force -Path $rws | Out-Null
    $rctx = Import-OrchConfig (Join-Path $Orch 'examples\orchestrator.json')
    $rctx.RunsDir = $rws
    # no cache file: the static modelRates price
    $script:LiveRatesLoaded = $false
    $static = Get-ModelRate $rctx 'opencode-go/glm-5.3-flash'
    $noteStatic = Get-RateSourceNote $rctx
    # a seeded cache, older than maxAgeHours: offline it is still used, looked up without the provider
    $old = [ordered]@{ fetchedAt = (Get-Date).AddDays(-3).ToString('o'); source = 'test'; updatedAt = $sample.updatedAt; rates = [pscustomobject]$parsed }
    ([pscustomobject]$old | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath (Join-Path $rws '_model-rates.json') -Encoding UTF8
    $script:LiveRatesLoaded = $false
    $live = Get-ModelRate $rctx 'opencode-go/glm-5.3-flash'
    $other = Get-ModelRate $rctx 'opencode-go/mimo-v2.6-flash'
    $noteLive = Get-RateSourceNote $rctx
    $script:LiveRatesLoaded = $false
    if (-not $Keep) { Remove-Item -LiteralPath $rws -Recurse -Force -ErrorAction SilentlyContinue }
    $okParse = $parsed.Count -eq 2 -and [double]$parsed['glm-5.3-flash'].cacheRead -eq 0.5
    $okStatic = $static -and [double]$static.input -eq [double]$rctx.Config.modelRates.'opencode-go/glm-5.3-flash'.input -and $noteStatic -eq 'orchestrator.json modelRates'
    $okLive = $live -and [double]$live.input -eq 1 -and [double]$live.output -eq 2 -and $noteLive -match '^opencode\.ai/data prices fetched'
    $okFall = $other -and [double]$other.input -eq [double]$rctx.Config.modelRates.'opencode-go/mimo-v2.6-flash'.input
    Check 'T-L2' 'live prices are parsed, used from the cache without the provider prefix, and fall back to modelRates' `
        ($okParse -and $okStatic -and $okLive -and $okFall) ("parse=$okParse static=$okStatic live=$okLive fallback=$okFall note='$noteLive'")
}
catch { Fail-Scenario 'T-L2' 'live model prices' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T7: a session-capable hop records its session (D5/D6)
try {
    $ws = New-WorkSpace 'sess7'
    Enable-MockSessions $ws
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t7.md'
    $r = Run-Orch -Ws $ws -Argv @('new', 'T7-001', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd
    $s = Get-State $ws 'T7-001'
    $sid = [string]$s.review.session
    $rec = if ($sid -and $s.sessions.PSObject.Properties[$sid]) { $s.sessions.$sid } else { $null }
    Check 'T7' 'a session-capable review hop records state.review.session and state.sessions.<id>.turns=1' `
        ($r.Code -eq 0 -and $sid -like 'ses_mock_*' -and $rec -and [int]$rec.turns -eq 1 -and $rec.role -eq 'plan-reviewer' -and $rec.adapter -eq 'mock' -and $rec.model -eq 'mock-reviewer') `
        ("code=$($r.Code) sid=$sid turns=$(if ($rec) { $rec.turns } else { '-' }) role=$(if ($rec) { $rec.role } else { '-' })")
}
catch { Fail-Scenario 'T7' 'session capture' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T8: round-2 review continues the session with a short diff follow-up (D7)
try {
    $ws = New-WorkSpace 'sess8'
    Enable-MockSessions $ws
    $pd = Join-Path $ws 'prompts'
    # A long plan so a one-line revision diff stays under 60% of it (the diff keeps the long ---/+++ paths).
    $p1file = Write-Plan $ws 'plan-t8-v1.md'
    $filler = (1..60 | ForEach-Object { "Filler paragraph $_ : the plan is deliberately long so a one-line revision diff stays well under sixty percent of the whole plan text." }) -join "`n"
    Add-Content -LiteralPath $p1file -Value "`n## Notes`n$filler" -Encoding UTF8
    $v1text = Get-Content -LiteralPath $p1file -Raw -Encoding UTF8
    $p2file = Join-Path $ws 'plan-t8-v2.md'
    ($v1text -replace 'containing `one`\.', 'containing `one` plus a small note.') | Set-Content -LiteralPath $p2file -Encoding UTF8
    [void](Run-Orch -Ws $ws -Argv @('new', 'T8-001', 'Ui', 'mock session task', '-PlanFile', $p1file) -Mode 'findings' -PromptDir $pd)
    $r = Run-Orch -Ws $ws -Argv @('revise', 'T8-001', '-PlanFile', $p2file) -PromptDir $pd
    $s = Get-State $ws 'T8-001'
    $ps = @(Get-CapturedPrompts -Dir $pd -Role 'plan-reviewer')
    $p1 = if ($ps.Count -ge 1) { $ps[0] } else { $null }
    $p2 = if ($ps.Count -ge 2) { $ps[1] } else { $null }
    $sid = [string]$s.review.session
    $turns = if ($sid -and $s.sessions.PSObject.Properties[$sid]) { [int]$s.sessions.$sid.turns } else { 0 }
    $ok = ($r.Code -eq 0 -and $s.review.round -eq 2 -and $p1 -and $p2 -and $p1.Session -eq $p2.Session -and $sid -eq $p2.Session -and $turns -eq 2 -and $p2.Text -match 'follow-up in the same session' -and $p2.Text -match '@@' -and $p2.Text.Length -lt $p1.Text.Length)
    Check 'T8' 'round-2 review continues the same session with a shorter follow-up carrying the diff; turns=2' `
        $ok ("code=$($r.Code) round=$($s.review.round) prompts=$($ps.Count) sameSid=$(if ($p1 -and $p2) { $p1.Session -eq $p2.Session } else { $false }) turns=$turns fu=$(if ($p2) { $p2.Text -match 'follow-up in the same session' } else { $false }) diff=$(if ($p2) { $p2.Text -match '@@' } else { $false }) len1=$(if ($p1) { $p1.Text.Length } else { 0 }) len2=$(if ($p2) { $p2.Text.Length } else { 0 })")
}
catch { Fail-Scenario 'T8' 'review continuation' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T9: implement continues the read-back session, else starts fresh (D7)
try {
    $ws = New-WorkSpace 'sess9a'
    Enable-MockSessions $ws
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t9a.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T9-001', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd)
    $sb = Get-State $ws 'T9-001'
    $rbSid = [string]$sb.readback.session
    [void](Invoke-Approve $ws 'T9-001' 'keep it small')
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T9-001', '1') -PromptDir $pd
    $s = Get-State $ws 'T9-001'
    $fu = @(Get-CapturedPrompts -Dir $pd -Role 'implementer' | Where-Object { $_.Text -match 'follow-up in the same session' })
    $f = if ($fu.Count) { $fu[0] } else { $null }
    Check 'T9' 'implement continues the read-back id when readback.round==approved.version, prompt carries the amendments' `
        ($r.Code -eq 0 -and $rbSid -and [string]$s.implementSession -eq $rbSid -and $f -and $f.Session -eq $rbSid -and $f.Text -match 'keep it small' -and $f.Text -match 'The plan you read back' -and $f.Text -match 'Phase to execute: 1' -and $f.Text -notmatch 'READ-BACK ONLY') `
        ("code=$($r.Code) rbSid=$rbSid implSid=$($s.implementSession) fu=$($fu.Count) amend=$(if ($f) { $f.Text -match 'keep it small' } else { $false })")
}
catch { Fail-Scenario 'T9' 'implement continues read-back' ("exception: " + $_.Exception.Message) }

try {
    $ws = New-WorkSpace 'sess9b'
    Enable-MockSessions $ws
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t9b.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T9-002', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd)
    $plan2 = Write-Plan $ws 'plan-t9b-v2.md'
    # revise to v2 WITHOUT a read-back, so readback.round (1) < approved.version (2): implement still continues
    # the read-back session, and the follow-up carries the plan diff v1 -> v2 (owner's rule, 2026-11-16).
    [void](Run-Orch -Ws $ws -Argv @('revise', 'T9-002', '-PlanFile', $plan2, '-NoReadBack') -PromptDir $pd)
    $sb = Get-State $ws 'T9-002'
    $rbSid = [string]$sb.readback.session
    [void](Invoke-Approve $ws 'T9-002')
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T9-002', '1') -PromptDir $pd
    $s = Get-State $ws 'T9-002'
    $fu = @(Get-CapturedPrompts -Dir $pd -Role 'implementer' | Where-Object { $_.Text -match 'follow-up in the same session' })
    $f = if ($fu.Count) { $fu[0] } else { $null }
    Check 'T9' 'when readback.round < approved.version, implement continues the read-back session with the plan diff since that version' `
        ($r.Code -eq 0 -and $sb.readback.round -lt $s.approved.version -and $f -and [string]$s.implementSession -eq $rbSid -and $f.Session -eq $rbSid -and $f.Text -match 'You read back version 1; version 2 is now approved' -and $f.Text -match 'Phase to execute: 1') `
        ("code=$($r.Code) rbRound=$($sb.readback.round) approved=$($s.approved.version) rbSid=$rbSid implSid=$($s.implementSession) fu=$($fu.Count)")
}
catch { Fail-Scenario 'T9' 'implement fresh session' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T10: verify continues the review session, else the full chain (D7)
try {
    $ws = New-WorkSpace 'sess10a'
    Enable-MockSessions $ws
    # The verifier chain must share the review hop (adapter+model) for the review session to continue into it.
    Edit-Config -Ws $ws -Change { param($cfg) $cfg.roles.verifier.chain = @([pscustomobject]@{ adapter = 'mock'; model = 'mock-reviewer'; family = 'reviewerfam' }) }
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t10a.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T10-001', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd)
    $sb = Get-State $ws 'T10-001'
    $revSid = [string]$sb.review.session
    [void](Invoke-Approve $ws 'T10-001')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T10-001', '1') -PromptDir $pd)
    $r = Run-Orch -Ws $ws -Argv @('verify', 'T10-001') -PromptDir $pd
    $s = Get-State $ws 'T10-001'
    $fu = @(Get-CapturedPrompts -Dir $pd -Role 'verifier' | Where-Object { $_.Text -match 'follow-up in the same session' })
    $f = if ($fu.Count) { $fu[0] } else { $null }
    Check 'T10' 'verify continues the review id (family differs from the implementer), prompt says the plan was approved' `
        ($r.Code -eq 0 -and $revSid -and [string]$s.verifySession -eq $revSid -and $f -and $f.Session -eq $revSid -and $f.Text -match 'The plan you reviewed' -and $s.verify.verdict -eq 'PASS') `
        ("code=$($r.Code) revSid=$revSid verifySid=$($s.verifySession) fu=$($fu.Count) verdict=$($s.verify.verdict)")
}
catch { Fail-Scenario 'T10' 'verify continues review' ("exception: " + $_.Exception.Message) }

try {
    $ws = New-WorkSpace 'sess10b'
    # No sessions on the mock: the review hop records no session, so verify cannot continue it - full chain.
    Edit-Config -Ws $ws -Change { param($cfg) $cfg.roles.verifier.chain = @([pscustomobject]@{ adapter = 'mock'; model = 'mock-reviewer'; family = 'reviewerfam' }) }
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t10b.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T10-002', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd)
    $sb = Get-State $ws 'T10-002'
    [void](Invoke-Approve $ws 'T10-002')
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T10-002', '1') -PromptDir $pd)
    $r = Run-Orch -Ws $ws -Argv @('verify', 'T10-002') -PromptDir $pd
    $s = Get-State $ws 'T10-002'
    $vp = @(Get-CapturedPrompts -Dir $pd -Role 'verifier')
    $f = if ($vp.Count) { $vp[0] } else { $null }
    Check 'T10' 'when the review hop had no sessions, verify runs the full chain with the full prompt' `
        ($r.Code -eq 0 -and -not [string]$sb.review.session -and $f -and $f.Text -notmatch 'follow-up in the same session' -and $f.Text -match 'Plan \(approved, frozen\)') `
        ("code=$($r.Code) revSession=$($sb.review.session) verifyPrompts=$($vp.Count) full=$(if ($f) { $f.Text -match 'Plan \(approved, frozen\)' } else { $false })")
}
catch { Fail-Scenario 'T10' 'verify full chain' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T-SW: review on the implementer's model, read-back on another family (owner, 2026-11-17)
try {
    $ws = New-WorkSpace 'sess-swap'
    Enable-MockSessions $ws
    Edit-Config -Ws $ws -Change {
        param($cfg)
        # gate 2 runs on the implementer's first hop; the read-back and the verifier on another family
        $cfg.roles.'plan-reviewer'.chain = @([pscustomobject]@{ adapter = 'mock'; model = 'mock-impl-a'; family = 'implfam' })
        Set-CfgProp $cfg.roles 'readback' ([pscustomobject]@{ gate = 2.5; readOnly = $true; auto = $false; promptFile = 'implement.md'
                chain = @([pscustomobject]@{ adapter = 'mock'; model = 'mock-reviewer'; family = 'reviewerfam' }) })
        $cfg.roles.verifier.chain = @([pscustomobject]@{ adapter = 'mock'; model = 'mock-reviewer'; family = 'reviewerfam' })
    }
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-sw.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T-SW-001', 'Ui', 'mock swap task', '-PlanFile', $plan) -PromptDir $pd)
    $sb = Get-State $ws 'T-SW-001'
    $revSid = [string]$sb.review.session
    $rbSid = [string]$sb.readback.session
    [void](Invoke-Approve $ws 'T-SW-001')
    $ri = Run-Orch -Ws $ws -Argv @('implement', 'T-SW-001', '1') -PromptDir $pd
    $si = Get-State $ws 'T-SW-001'
    $ip = @(Get-CapturedPrompts -Dir $pd -Role 'implementer' | Where-Object { $_.Session -eq $revSid -and $_.Text -match 'same session, new role' })
    $rv = Run-Orch -Ws $ws -Argv @('verify', 'T-SW-001') -PromptDir $pd
    $sv = Get-State $ws 'T-SW-001'
    $vp = @(Get-CapturedPrompts -Dir $pd -Role 'verifier' | Where-Object { $_.Session -eq $rbSid -and $_.Text -match 'same session, new role' })
    $okImpl = $ri.Code -eq 0 -and $revSid -and [string]$si.implementSession -eq $revSid -and $ip.Count -eq 1 -and $ip[0].Text -match 'you reviewed version 1' -and $ip[0].Text -match 'Phase to execute: 1'
    $okVer = $rv.Code -eq 0 -and $rbSid -and $rbSid -ne $revSid -and [string]$sv.verifySession -eq $rbSid -and $vp.Count -eq 1 -and $vp[0].Text -match 'read back version 1' -and $sv.verify.verdict -eq 'PASS'
    Check 'T-SW' 'implement continues the review session on its own family (full prompt, new role); verify continues the other-family read-back session' `
        ($okImpl -and $okVer -and $sb.readback.family -eq 'reviewerfam') ("impl=$okImpl verify=$okVer rev=$revSid rb=$rbSid implSid=$($si.implementSession) verSid=$($sv.verifySession) rbFam=$($sb.readback.family)")
}
catch { Fail-Scenario 'T-SW' 'cross-family session reuse' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T11: maxFollowUps reached -> fresh session, full prompt (D6/D7)
try {
    $ws = New-WorkSpace 'sess11'
    Enable-MockSessions $ws
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'sessions' ([pscustomobject]@{ maxFollowUps = 1 }) }
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t11.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T11-001', 'Ui', 'mock session task', '-PlanFile', $plan) -Mode 'findings' -PromptDir $pd)
    $sb = Get-State $ws 'T11-001'
    $sid1 = [string]$sb.review.session
    $plan2 = Write-Plan $ws 'plan-t11-v2.md'
    $r = Run-Orch -Ws $ws -Argv @('revise', 'T11-001', '-PlanFile', $plan2) -PromptDir $pd
    $s = Get-State $ws 'T11-001'
    $sid2 = [string]$s.review.session
    $ps = @(Get-CapturedPrompts -Dir $pd -Role 'plan-reviewer')
    $p2 = if ($ps.Count -ge 2) { $ps[1] } else { $null }
    $t1 = if ($sid1 -and $s.sessions.PSObject.Properties[$sid1]) { [int]$s.sessions.$sid1.turns } else { 0 }
    Check 'T11' 'once maxFollowUps is reached the next review starts a fresh session with the full prompt' `
        ($r.Code -eq 0 -and $sid1 -and $sid2 -and $sid1 -ne $sid2 -and $t1 -eq 1 -and $p2 -and $p2.Text -notmatch 'follow-up in the same session' -and $p2.Session -eq $sid2) `
        ("code=$($r.Code) sid1=$sid1 sid2=$sid2 turns1=$t1 prompts=$($ps.Count) fu=$(if ($p2) { $p2.Text -match 'follow-up in the same session' } else { $false })")
}
catch { Fail-Scenario 'T11' 'maxFollowUps cap' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T12: a failed continuation falls through to the chain (D6)
try {
    $ws = New-WorkSpace 'sess12'
    Enable-MockSessions $ws
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.adapters.mock 'rateLimitRetries' 0 }
    $pd = Join-Path $ws 'prompts'
    $plan = Write-Plan $ws 'plan-t12.md'
    [void](Run-Orch -Ws $ws -Argv @('new', 'T12-001', 'Ui', 'mock session task', '-PlanFile', $plan) -PromptDir $pd)
    [void](Invoke-Approve $ws 'T12-001')
    # The first implement call (the continuation) 429s and is not retried (rateLimitRetries 0); the chain's
    # next call succeeds. The continuation changed no repo file, so it falls through instead of throwing.
    $old = $env:MOCK_RATE_LIMIT_TIMES
    $env:MOCK_RATE_LIMIT_TIMES = '1'
    try { $r = Run-Orch -Ws $ws -Argv @('implement', 'T12-001', '1') -PromptDir $pd }
    finally { $env:MOCK_RATE_LIMIT_TIMES = $old }
    $s = Get-State $ws 'T12-001'
    $tr = Get-Content -LiteralPath (Join-Path $ws 'runs\T12-001\transcript\implementer.phase-1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $att = @($tr.Attempts)
    $cont = @($att | Where-Object { [string]$_.reason -like 'continue: *' })
    Check 'T12' 'a continuation that fails without repo changes falls through to the chain (reason starts continue: )' `
        ($r.Code -eq 0 -and $s.status -eq 'Implemented' -and $cont.Count -ge 1 -and $att.Count -ge 2) `
        ("code=$($r.Code) status=$($s.status) attempts=$($att.Count) continue=$($cont.Count) reason0=$(if ($att.Count) { $att[0].reason } else { '-' })")
}
catch { Fail-Scenario 'T12' 'continuation fallback' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T13: a manual implement phase (D8)
try {
    $ws = New-WorkSpace 'manual-impl'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'rateLimitRetries' 0
        Set-CfgProp $cfg.adapters 'manual' ([pscustomobject]@{ enabled = $true; command = $null })
        $cfg.roles.implementer.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-impl-a'; family = 'implfam' },
            [pscustomobject]@{ adapter = 'manual'; model = 'human'; family = 'humanfam' })
    }
    [void](Invoke-New $ws 'T13-001' 'Ui')
    [void](Invoke-Approve $ws 'T13-001')
    $log = Join-Path $ws 'mock-calls.log'
    # The automatic implementer hop 429s once and is not retried, so the chain reaches the manual hop.
    $old = $env:MOCK_RATE_LIMIT_TIMES
    $env:MOCK_RATE_LIMIT_TIMES = '1'
    try {
        Remove-Item -LiteralPath ($log + '.429') -Force -ErrorAction SilentlyContinue
        $defer = Run-Orch -Ws $ws -Argv @('implement', 'T13-001', '1')
    }
    finally { $env:MOCK_RATE_LIMIT_TIMES = $old }
    $inbox = Join-Path $ws 'runs\T13-001\inbox'
    $promptOk = Test-Path -LiteralPath (Join-Path $inbox 'implementer.phase-1.md')
    $wsOk = Test-Path -LiteralPath (Join-Path $inbox 'implementer.phase-1.workspace.json')
    # The human edits a planned file and writes the answer; the rerun must make no automatic call.
    Set-Content -LiteralPath (Join-Path $ws 'Ui\phase1.txt') -Value 'one' -Encoding UTF8
    $answer = Join-Path $ws 'impl-answer.md'
    Set-Content -LiteralPath $answer -Value "phase1.txt created.`nVERDICT: PASS" -Encoding UTF8
    [void](Run-Orch -Ws $ws -Argv @('answer', 'T13-001', 'implementer.phase-1', $answer))
    $before = @(Get-MockCalls $log | Where-Object { $_.Role -eq 'implementer' -and $_.ReadBack -eq 'False' }).Count
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T13-001', '1')
    $after = @(Get-MockCalls $log | Where-Object { $_.Role -eq 'implementer' -and $_.ReadBack -eq 'False' }).Count
    $s = Get-State $ws 'T13-001'
    Check 'T13' 'a manual implement phase defers with a workspace snapshot, then the answer implements with no automatic call' `
        ($defer.Code -eq 3 -and $promptOk -and $wsOk -and $r.Code -eq 0 -and $s.status -eq 'Implemented' -and $after -eq $before) `
        ("defer=$($defer.Code) prompt=$promptOk ws=$wsOk code=$($r.Code) status=$($s.status) callsBefore=$before callsAfter=$after")
}
catch { Fail-Scenario 'T13' 'manual implement' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T13: a sibling the human touched is still a Violation (D8)
try {
    $ws = New-WorkSpace 'manual-impl-sibling'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'rateLimitRetries' 0
        Set-CfgProp $cfg.adapters 'manual' ([pscustomobject]@{ enabled = $true; command = $null })
        $cfg.roles.implementer.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-impl-a'; family = 'implfam' },
            [pscustomobject]@{ adapter = 'manual'; model = 'human'; family = 'humanfam' })
    }
    [void](Invoke-New $ws 'T13-002' 'Ui')
    [void](Invoke-Approve $ws 'T13-002')
    $log = Join-Path $ws 'mock-calls.log'
    $old = $env:MOCK_RATE_LIMIT_TIMES
    $env:MOCK_RATE_LIMIT_TIMES = '1'
    try {
        Remove-Item -LiteralPath ($log + '.429') -Force -ErrorAction SilentlyContinue
        $defer = Run-Orch -Ws $ws -Argv @('implement', 'T13-002', '1')
    }
    finally { $env:MOCK_RATE_LIMIT_TIMES = $old }
    # The human edits the planned file AND a sibling repo: the snapshot baseline still sees the sibling,
    # and reports it as a note (the sibling is not this worker's business), never a Violation.
    Set-Content -LiteralPath (Join-Path $ws 'Ui\phase1.txt') -Value 'one' -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $ws 'Web\sibling.txt') -Value 'touched' -Encoding UTF8
    $answer = Join-Path $ws 'impl-answer.md'
    Set-Content -LiteralPath $answer -Value "phase1.txt created.`nVERDICT: PASS" -Encoding UTF8
    [void](Run-Orch -Ws $ws -Argv @('answer', 'T13-002', 'implementer.phase-1', $answer))
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T13-002', '1')
    $s = Get-State $ws 'T13-002'
    $viol = @(@($s.phases)[0].violations) -join '; '
    Check 'T13' 'a sibling repo the human touched during a manual implement phase is a note, not a Violation' `
        ($defer.Code -eq 3 -and $r.Code -eq 0 -and $s.status -eq 'Implemented' -and -not $viol -and $r.Out -match 'note: Web changed meanwhile') `
        ("defer=$($defer.Code) code=$($r.Code) status=$($s.status) violations=$viol note=$($r.Out -match 'note: Web changed meanwhile')")
}
catch { Fail-Scenario 'T13' 'manual implement sibling' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T14: a pending request is answered, not overtaken (D9)
try {
    $ws = New-WorkSpace 'pending-review'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg.adapters.mock 'rateLimitRetries' 0
        Set-CfgProp $cfg.adapters 'manual' ([pscustomobject]@{ enabled = $true; command = $null })
        $cfg.roles.'plan-reviewer'.chain = @(
            [pscustomobject]@{ adapter = 'mock'; model = 'mock-reviewer'; family = 'reviewerfam' },
            [pscustomobject]@{ adapter = 'manual'; model = 'human'; family = 'humanfam' })
    }
    $plan = Write-Plan $ws 'plan-t14.md'
    $log = Join-Path $ws 'mock-calls.log'
    $old = $env:MOCK_RATE_LIMIT_TIMES
    $env:MOCK_RATE_LIMIT_TIMES = '1'
    try {
        Remove-Item -LiteralPath ($log + '.429') -Force -ErrorAction SilentlyContinue
        $new = Run-Orch -Ws $ws -Argv @('new', 'T14-001', 'Ui', 'mock pending', '-PlanFile', $plan)
    }
    finally { $env:MOCK_RATE_LIMIT_TIMES = $old }
    $req = Join-Path $ws 'runs\T14-001\inbox\plan-reviewer.v1.md'
    $reqAfterNew = Test-Path -LiteralPath $req
    $c0 = @(Get-MockCalls $log | Where-Object { $_.Role -eq 'plan-reviewer' }).Count
    # Without -Retry the automatic hop is skipped and the request stays pending (the manual hop defers).
    $skip = Run-Orch -Ws $ws -Argv @('review', 'T14-001', '-NoReadBack')
    $c1 = @(Get-MockCalls $log | Where-Object { $_.Role -eq 'plan-reviewer' }).Count
    $stillPending = Test-Path -LiteralPath $req
    # -Retry deletes the request and calls the first hop, which now answers.
    $retry = Run-Orch -Ws $ws -Argv @('review', 'T14-001', '-NoReadBack', '-Retry')
    $c2 = @(Get-MockCalls $log | Where-Object { $_.Role -eq 'plan-reviewer' }).Count
    $gone = -not (Test-Path -LiteralPath $req)
    $s = Get-State $ws 'T14-001'
    Check 'T14' 'a pending review request skips the automatic hops; -Retry deletes it and calls the first hop' `
        ($new.Code -eq 3 -and $reqAfterNew -and $skip.Code -eq 3 -and $c1 -eq $c0 -and $stillPending -and
         $retry.Code -eq 0 -and $c2 -gt $c1 -and $gone -and $s.review.verdict -eq 'PASS') `
        ("new=$($new.Code) req=$reqAfterNew skip=$($skip.Code) c0=$c0 c1=$c1 stillPending=$stillPending retry=$($retry.Code) c2=$c2 gone=$gone verdict=$(if ($s.review) { $s.review.verdict } else { '-' })")
}
catch { Fail-Scenario 'T14' 'pending request routing' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T15: an implementer answer resumes with implement <id> <phase> (D8)
try {
    $ws = New-WorkSpace 'resume-implement'
    Set-ManualChain $ws 'implementer'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg.readback 'enabled' $false }
    [void](Invoke-New $ws 'T15-001' 'Ui')
    [void](Invoke-Approve $ws 'T15-001')
    $d1 = Run-Orch -Ws $ws -Argv @('implement', 'T15-001', '1')
    $a1 = Join-Path $ws 'a1.md'
    Set-Content -LiteralPath $a1 -Value "phase 1 done.`nVERDICT: PASS" -Encoding UTF8
    [void](Run-Orch -Ws $ws -Argv @('answer', 'T15-001', 'implementer.phase-1', $a1))
    [void](Run-Orch -Ws $ws -Argv @('implement', 'T15-001', '1'))
    $d2 = Run-Orch -Ws $ws -Argv @('implement', 'T15-001', '2')
    $q = Run-Orch -Ws $ws -Argv @('queue', 'T15-001')
    $deferOk = $d2.Out -match 'implement\s+T15-001\s+2'
    $queueOk = $q.Out -match 'implement\s+T15-001\s+2'
    Check 'T15' 'an implementer.phase-2 answer resumes with implement <id> 2 (DEFERRED message and queue then: line)' `
        ($d1.Code -eq 3 -and $d2.Code -eq 3 -and $deferOk -and $queueOk) `
        ("d1=$($d1.Code) d2=$($d2.Code) deferred=$deferOk queue=$queueOk")
}
catch { Fail-Scenario 'T15' 'implement resume command' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T16: the manual request header is generic (D8)
try {
    $ws = New-WorkSpace 'manual-header'
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $ui = Join-Path $ws 'Ui'
    $inbox = Join-Path $ws 'runs\T16-001\inbox'
    New-Item -ItemType Directory -Force -Path $inbox | Out-Null
    [void](Invoke-Manual -Ctx $ctx -TaskId 'T16-001' -Key 'plan-reviewer.v1' -Prompt 'PROMPT' -RepoPath $ui -AvoidFamilies @('claude-opus'))
    $hdr = Get-Content -LiteralPath (Join-Path $inbox 'plan-reviewer.v1.md') -Raw -Encoding UTF8
    [void](Invoke-Manual -Ctx $ctx -TaskId 'T16-001' -Key 'implementer.phase-1' -Prompt 'PROMPT' -RepoPath $ui -AvoidFamilies @('claude-opus'))
    $ihdr = Get-Content -LiteralPath (Join-Path $inbox 'implementer.phase-1.md') -Raw -Encoding UTF8
    $wsOk = Test-Path -LiteralPath (Join-Path $inbox 'implementer.phase-1.workspace.json')
    $noImplFp = -not (Test-Path -LiteralPath (Join-Path $inbox 'implementer.phase-1.fingerprint'))
    [void](Invoke-Manual -Ctx $ctx -TaskId 'T16-001' -Key 'verifier.empty' -Prompt 'PROMPT' -RepoPath $ui -AvoidFamilies @())
    $ehdr = Get-Content -LiteralPath (Join-Path $inbox 'verifier.empty.md') -Raw -Encoding UTF8
    Check 'T16' 'the manual header names any agent and the avoided family, offers -Retry, and never names a tool' `
        ($hdr -match 'Any agent can answer this' -and $hdr -match 'claude-opus' -and $hdr -match '-Retry' -and
         $hdr -notmatch '(?i)freebuff' -and $ihdr -match 'Implement this phase' -and $ihdr -notmatch '(?i)freebuff' -and
         $wsOk -and $noImplFp -and $ehdr -notmatch 'Any agent can answer this') `
        ("anyAgent=$($hdr -match 'Any agent can answer this') family=$($hdr -match 'claude-opus') retry=$($hdr -match '-Retry') noFreebuff=$($hdr -notmatch '(?i)freebuff') impl=$($ihdr -match 'Implement this phase') ws=$wsOk noImplFp=$noImplFp emptyOmits=$($ehdr -notmatch 'Any agent can answer this')")
}
catch { Fail-Scenario 'T16' 'manual header' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T17: D10 no tool name for the manual hop
try {
    $files = @(Get-ChildItem -LiteralPath (Join-Path $Orch 'lib') -Filter '*.ps1' -File | ForEach-Object { $_.FullName })
    $files += @((Join-Path $Orch 'run.ps1'), (Join-Path $Orch 'examples\orchestrator.json'), (Join-Path $Orch 'docs\CONDUCTOR.md'))
    $hits = @()
    foreach ($f in $files) {
        if (Test-Path -LiteralPath $f) {
            foreach ($m in @(Select-String -LiteralPath $f -Pattern 'freebuff' -SimpleMatch)) {
                $hits += ("{0}:{1}" -f (Split-Path -Leaf $f), $m.LineNumber)
            }
        }
    }
    Check 'T17' 'no tool name for the manual hop: freebuff appears nowhere in the code, config or CONDUCTOR' `
        ($hits.Count -eq 0) ("hits=$($hits -join ', ') files=$($files.Count)")
}
catch { Fail-Scenario 'T17' 'no freebuff' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T18: D11 ignored records pass gate 3
try {
    $ws = New-WorkSpace 'cleantree'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'cleanTree' ([pscustomobject]@{ ignore = @('.ai/plans/', '.ai/context/'); note = 'offline' }) }
    [void](Invoke-New $ws 'T18-001' 'Ui')
    [void](Invoke-Approve $ws 'T18-001')
    $ui = Join-Path $ws 'Ui'
    # An untracked plan record and a tracked-then-modified session log: both under cleanTree.ignore.
    New-Item -ItemType Directory -Force -Path (Join-Path $ui '.ai\plans') | Out-Null
    Set-Content -LiteralPath (Join-Path $ui '.ai\plans\0001-x.md') -Value '# plan record' -Encoding UTF8
    New-Item -ItemType Directory -Force -Path (Join-Path $ui '.ai\context') | Out-Null
    Set-Content -LiteralPath (Join-Path $ui '.ai\context\session-1.md') -Value 'v1' -Encoding UTF8
    [void](& git -C $ui add '.ai/context/session-1.md' 2>&1)
    [void](& git -C $ui commit -q -m 'session log' 2>&1)
    Set-Content -LiteralPath (Join-Path $ui '.ai\context\session-1.md') -Value 'v2' -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T18-001', '1')
    $s = Get-State $ws 'T18-001'
    $pre = @($s.preexistingDirty)
    $ph = @($s.phases)[0]
    $changed = @($ph.changed); $outside = @($ph.outsidePlan)
    $pd = Join-Path $ws 'prompts18'
    [void](Run-Orch -Ws $ws -Argv @('verify', 'T18-001') -PromptDir $pd)
    $vp = @(Get-CapturedPrompts -Dir $pd -Role 'verifier')
    $vtext = $(if ($vp.Count) { $vp[0].Text } else { '' })
    Check 'T18' 'ignored records pass gate 3, are remembered, stay out of changed/outsidePlan, and reach the verifier' `
        ($r.Code -eq 0 -and $s.status -eq 'Implemented' -and $s.baseSha `
         -and ($pre -contains '.ai/plans/0001-x.md') -and ($pre -contains '.ai/context/session-1.md') `
         -and ($changed -notcontains '.ai/plans/0001-x.md') -and ($changed -notcontains '.ai/context/session-1.md') `
         -and ($outside -notcontains '.ai/plans/0001-x.md') -and ($outside -notcontains '.ai/context/session-1.md') `
         -and ($vtext -match 'Pre-existing uncommitted paths')) `
        ("code=$($r.Code) status=$($s.status) pre=$($pre -join ',') changed=$($changed -join ',') outside=$($outside -join ',') vline=$([bool]($vtext -match 'Pre-existing'))")
}
catch { Fail-Scenario 'T18' 'clean-tree ignore' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- T19: D11 a dirty code file still blocks
try {
    $ws = New-WorkSpace 'cleantree-block'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'cleanTree' ([pscustomobject]@{ ignore = @('.ai/plans/', '.ai/context/'); note = 'offline' }) }
    [void](Invoke-New $ws 'T19-001' 'Ui')
    [void](Invoke-Approve $ws 'T19-001')
    $ui = Join-Path $ws 'Ui'
    New-Item -ItemType Directory -Force -Path (Join-Path $ui '.ai\plans') | Out-Null
    Set-Content -LiteralPath (Join-Path $ui '.ai\plans\0001-x.md') -Value '# plan record' -Encoding UTF8
    New-Item -ItemType Directory -Force -Path (Join-Path $ui '.ai\context') | Out-Null
    Set-Content -LiteralPath (Join-Path $ui '.ai\context\session-1.md') -Value 'v1' -Encoding UTF8
    [void](& git -C $ui add '.ai/context/session-1.md' 2>&1)
    [void](& git -C $ui commit -q -m 'session log' 2>&1)
    Set-Content -LiteralPath (Join-Path $ui '.ai\context\session-1.md') -Value 'v2' -Encoding UTF8
    # A real code file is dirty too: gate 3 must refuse, naming only it, never the ignored records.
    Set-Content -LiteralPath (Join-Path $ui 'README.md') -Value '# changed' -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('implement', 'T19-001', '1')
    $s = Get-State $ws 'T19-001'
    $msg = $r.Out
    Check 'T19' 'a dirty code file blocks gate 3 naming only it, while the records stay ignored' `
        ($r.Code -ne 0 -and $msg -match 'gate 3' -and $msg -match 'README\.md' `
         -and $msg -notmatch '\.ai/plans' -and $msg -notmatch '\.ai/context' -and -not $s.baseSha) `
        ("code=$($r.Code) baseSha=$([bool]$s.baseSha) namesReadme=$([bool]($msg -match 'README\.md')) noRecords=$([bool]($msg -notmatch '\.ai/'))")
}
catch { Fail-Scenario 'T19' 'clean-tree block' ("exception: " + $_.Exception.Message) }

# ------------------------------------------------------ T-C5..T-C8: planFiling=slug (plan 0005 D3)
# Project A files plans by slug: done/<slug>.md, then *-<slug>.md, then a done file naming the branch.
try {
    $ws = New-WorkSpace 'close-slug'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'planFiling' 'slug' }
    $done = Join-Path $ws 'Ui\.ai\plans\done'
    New-Item -ItemType Directory -Force -Path $done | Out-Null

    [void](Invoke-New $ws 'T-C-101' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-C-101', '-Branch', 'alireza/close-by-slug'))
    Set-Content -LiteralPath (Join-Path $done 'close-by-slug.md') -Value '# Plan'
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-101')
    Check 'T-C5' 'planFiling=slug: close finds done/<slug>.md from the branch topic' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-C-101'))) ("code=$($r.Code)")

    [void](Invoke-New $ws 'T-C-102' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-C-102', '-Branch', 'alireza/close-suffix'))
    Set-Content -LiteralPath (Join-Path $done '0007-close-suffix.md') -Value '# Plan'
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-102')
    Check 'T-C6' 'planFiling=slug: close falls back to *-<slug>.md' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-C-102'))) ("code=$($r.Code)")

    [void](Invoke-New $ws 'T-C-103' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-C-103', '-Branch', 'alireza/close-named'))
    Set-Content -LiteralPath (Join-Path $done 'other-name.md') -Value 'filed on alireza/close-named'
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-103')
    Check 'T-C7' 'planFiling=slug: close falls back to a done file that names the branch' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-C-103'))) ("code=$($r.Code)")

    [void](Invoke-New $ws 'T-C-104' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-C-104', '-Branch', 'alireza/close-missing'))
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-C-104')
    Check 'T-C8' 'planFiling=slug: close is refused, naming the slug, while nothing is filed' `
        ($r.Code -eq 1 -and $r.Out -match "plan 'close-missing' is not filed" -and (Test-Path -LiteralPath (Join-Path $ws 'runs\T-C-104'))) ("code=$($r.Code)")
}
catch { Fail-Scenario 'T-C5' 'close by slug' ("exception: " + $_.Exception.Message) }

# ------------------------------- T-PD1..T-PD6: close by plansDir + `**Status:** Done` (plan 0007 D3)
# A project that sets `plansDir` keeps every plan of a repo in one folder for its whole life: close
# finds it there and accepts it only when its header says Done. Without `plansDir`, today's done/ path.
try {
    # Both filing styles are exercised here, whatever this config's default is (number or slug).
    $ws = New-WorkSpace 'close-plansdir'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg 'plansDir' '.orchestra/plans/{repo}'
        Set-CfgProp $cfg 'planFiling' 'number'
    }
    $plansUi = Join-Path $ws '.orchestra\plans\Ui'
    New-Item -ItemType Directory -Force -Path $plansUi | Out-Null

    [void](Invoke-New $ws 'T-PD-101' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-PD-101', '-Branch', 'alireza/plan-0042-done'))
    Set-Content -LiteralPath (Join-Path $plansUi '0042-done.md') -Value "**Status:** Done - filed`n# Plan 0042" -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-PD-101')
    Check 'T-PD1' 'plansDir: a plan whose Status is Done in plans/<repo>/ files the run' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-PD-101'))) ("code=$($r.Code)")

    [void](Invoke-New $ws 'T-PD-102' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-PD-102', '-Branch', 'alireza/plan-0043-open'))
    Set-Content -LiteralPath (Join-Path $plansUi '0043-open.md') -Value "**Status:** Running - in progress`n# Plan 0043" -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-PD-102')
    Check 'T-PD2' 'plansDir: a Running plan is refused, naming the folder and the Status rule' `
        ($r.Code -eq 1 -and $r.Out -match 'Status is not Done' -and (Test-Path -LiteralPath (Join-Path $ws 'runs\T-PD-102'))) ("code=$($r.Code)")

    # The run's own open plan names its branch; the Done copy must win over it.
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'planFiling' 'slug' }
    [void](Invoke-New $ws 'T-PD-103' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-PD-103', '-Branch', 'alireza/pd-slug'))
    Set-Content -LiteralPath (Join-Path $plansUi 'pd-slug.md') -Value "**Status:** Running - open copy`n**Branch:** ``alireza/pd-slug```n" -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $plansUi '0007-pd-slug.md') -Value "**Status:** Done - filed`n# Plan 0007" -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-PD-103')
    Check 'T-PD3' 'planFiling=slug: an open plan naming the branch does not hide the Done copy in plansDir' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\T-PD-103'))) ("code=$($r.Code) out=$($r.Out -split "`n" | Select-String -Pattern 'not Done|is filed' | Select-Object -First 1)")

    # T-PD4: a workspace with no plansDir keeps today's done/ behaviour, Status line or not.
    $ws2 = New-WorkSpace 'close-plansdone'
    Edit-Config -Ws $ws2 -Change { param($cfg) Set-CfgProp $cfg 'planFiling' 'number' }
    $done = Join-Path $ws2 'Ui\.ai\plans\done'
    New-Item -ItemType Directory -Force -Path $done | Out-Null
    [void](Invoke-New $ws2 'T-PD-104' 'Ui')
    [void](Run-Orch -Ws $ws2 -Argv @('approve', 'T-PD-104', '-Branch', 'alireza/plan-0044-old'))
    Set-Content -LiteralPath (Join-Path $done '0044-old.md') -Value '# Plan 0044' -Encoding UTF8
    $r = Run-Orch -Ws $ws2 -Argv @('close', 'T-PD-104')
    Check 'T-PD4' 'no plansDir: the done/ folder still files a run with no Status line' `
        ($r.Code -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $ws2 'runs\T-PD-104'))) ("code=$($r.Code)")

    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'planFiling' 'number' }
    [void](Invoke-New $ws 'T-PD-105' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-PD-105', '-Branch', 'alireza/plan-0045-none'))
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-PD-105')
    Check 'T-PD5' 'plansDir: no plan at all names the folder and says where it belongs' `
        ($r.Code -eq 1 -and $r.Out -match 'is not in' -and $r.Out -match 'Write it there first \(gate 1\)') ("code=$($r.Code)")

    [void](Invoke-New $ws 'T-PD-106' 'Ui')
    [void](Run-Orch -Ws $ws -Argv @('approve', 'T-PD-106', '-Branch', 'alireza/plan-0046-norepo'))
    $tf = Join-Path $ws 'runs\T-PD-106\task.json'
    $st = Get-Content -LiteralPath $tf -Raw -Encoding UTF8 | ConvertFrom-Json
    $st.PSObject.Properties.Remove('repo')
    ($st | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $tf -Encoding UTF8
    $r = Run-Orch -Ws $ws -Argv @('close', 'T-PD-106')
    Check 'T-PD6' 'plansDir: a run with no repo stops with the one line that says so' `
        ($r.Code -eq 1 -and $r.Out -match 'has no repo in task.json') ("code=$($r.Code)")
}
catch { Fail-Scenario 'T-PD1' 'close by plansDir' ("exception: " + $_.Exception.Message) }

# ---------------------------------------- T-CG1..T-CG3: Project A behaviours kept by the engine (phase C)
try {
    $b = Get-PlanBranch -PlanText "# Plan`n**Branch:** ``someone/topic-x```n" -GitUser 'other user' -TaskId 'T-G-1'
    Check 'T-CG1' 'the plan''s own **Branch:** line names the branch, whoever the git user is' ($b -eq 'someone/topic-x') ("branch=$b")

    $ws = New-WorkSpace 'contract-doc'
    New-Item -ItemType Directory -Force -Path (Join-Path $ws 'Web\.ai\context') | Out-Null
    Set-Content -LiteralPath (Join-Path $ws 'Web\.ai\context\frontend-handoff.md') -Value '# handoff'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'contractDoc' 'Web\.ai\context\frontend-handoff.md' }
    $ctx = Import-OrchConfig (Join-Path $ws 'config.json')
    $p = New-RolePrompt -Ctx $ctx -Role 'implementer' -Repo 'Ui' -Task 'x'
    Check 'T-CG2' 'config contractDoc puts that doc in a non-Web prompt' ($p -match [regex]::Escape('frontend-handoff.md')) ''

    $src = Get-Content -LiteralPath (Join-Path $Orch 'run.ps1') -Raw -Encoding UTF8
    Check 'T-CG3' 'the planner phase is expanded (plan1, plan2, ...), not the literal text' `
        ($src -notmatch "-Phase 'plan\$\(") ''
}
catch { Fail-Scenario 'T-CG1' 'Project A behaviours' ("exception: " + $_.Exception.Message) }

# ------------------------------------------------- T-A1/T-D1/T-D2: engine line and description (D5)
try {
    $ws = New-WorkSpace 'engine-line'
    $r = Run-Orch -Ws $ws -Argv @('agents')
    $engineLine = "engine: $Orch"
    Check 'T-A1' 'agents prints the engine folder after the config line' `
        ($r.Code -eq 0 -and $r.Out.Contains($engineLine)) ("line=$([bool]$r.Out.Contains($engineLine))")

    # A config without `description` prints the fallback, never a project name (D5).
    Edit-Config -Ws $ws -Change { param($cfg) [void]$cfg.PSObject.Properties.Remove('description') }
    $h = Run-Orch -Ws $ws -Argv @('help')
    Check 'T-D1' 'help without a description prints the fallback title and the repos line' `
        ($h.Code -eq 0 -and $h.Out -match 'four-gate driver' -and $h.Out -match "Repos: listed under 'repos'") ("code=$($h.Code)")

    # Help with no loadable config at all still prints the usage, saying why (D5).
    $h2 = (& powershell -NoProfile -ExecutionPolicy Bypass -File $RunPs1 help -Config (Join-Path $ws 'no-such.json') 2>&1 | Out-String)
    Check 'T-D2' 'help with an unloadable config prints the usage and says no config is loaded' `
        ($h2 -match '\(no project config loaded\)' -and $h2 -match 'USAGE') ("saysNoConfig=$([bool]($h2 -match 'no project config loaded'))")
}
catch { Fail-Scenario 'T-A1' 'engine line and description' ("exception: " + $_.Exception.Message) }

# ------------------------------------------------- T-RP1..T-RP3: the dashboard reporter (plan 0004 §6.6)
# The reporter must be invisible: off unless a project asks for it, and a dashboard that is down costs a
# warning line, not a behaviour. `enabled = false` must make no request at all - proved by the absence of
# the warning, because any request to 127.0.0.1:9 fails and would warn.
try {
    $ws = New-WorkSpace 'report-off'
    $t0 = Get-Date
    $r = Invoke-New $ws 'T-RP-001' 'Ui'
    $secs = ((Get-Date) - $t0).TotalSeconds
    $s = Get-State $ws 'T-RP-001'
    # The baseline the next check compares against: the same `new` with nothing reporting.
    $script:ReportBaselineSecs = $secs
    Check 'T-RP1' 'with no report block a run is unchanged and nothing is attempted' `
        ($r.Code -eq 0 -and $s.review -and $s.review.verdict -eq 'PASS' -and $r.Out -notmatch '\[report\]') `
        ("code=$($r.Code) review=$(if ($s.review) { $s.review.verdict } else { 'none' }) warned=$([bool]($r.Out -match '\[report\]')) secs=$([int]$secs)")

    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg 'report' ([pscustomobject]@{
                enabled = $false; url = 'http://127.0.0.1:9/api/events'; project = 'demo'
                tokenEnv = 'ORCHESTRA_REPORT_TOKEN'; timeoutSec = 1; progressEverySec = 10
            })
    }
    $r2 = Invoke-New $ws 'T-RP-002' 'Ui'
    Check 'T-RP-003' 'report.enabled=false makes no request at all' `
        ($r2.Code -eq 0 -and $r2.Out -notmatch '\[report\]') `
        ("code=$($r2.Code) warned=$([bool]($r2.Out -match '\[report\]'))")
}
catch { Fail-Scenario 'T-RP1' 'the reporter off' ("exception: " + $_.Exception.Message) }

try {
    $ws = New-WorkSpace 'report-down'
    Edit-Config -Ws $ws -Change {
        param($cfg)
        Set-CfgProp $cfg 'report' ([pscustomobject]@{
                enabled = $true; url = 'http://127.0.0.1:9/api/events'; project = 'demo'
                tokenEnv = 'ORCHESTRA_REPORT_TOKEN'; timeoutSec = 1; progressEverySec = 10
            })
    }
    $t0 = Get-Date
    $r = Invoke-New $ws 'T-RP-010' 'Ui'
    $secs = ((Get-Date) - $t0).TotalSeconds
    $s = Get-State $ws 'T-RP-010'
    $warned = ([regex]::Matches($r.Out, '\[report\] dashboard not reachable')).Count
    # The cost of the reporter is the difference against T-RP1's run, not the run's own length (the mock
    # agent alone takes seconds). One failed attempt, then the process stops trying (D4).
    $overhead = $secs - [double]$script:ReportBaselineSecs
    Check 'T-RP2' 'a dashboard that is down costs one warning line, under 3 s more, and changes nothing' `
        ($r.Code -eq 0 -and $s.review -and $s.review.verdict -eq 'PASS' -and $warned -eq 1 -and $overhead -lt 3) `
        ("code=$($r.Code) warnings=$warned secs=$([math]::Round($secs, 1)) baseline=$([math]::Round([double]$script:ReportBaselineSecs, 1)) overhead=$([math]::Round($overhead, 1)) review=$(if ($s.review) { $s.review.verdict } else { 'none' })")
}
catch { Fail-Scenario 'T-RP2' 'the reporter with the dashboard down' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- P: profiles (plan 0006)
# The merge (D2), the export the engine reads (D5), the config a run records (D9) and the archive that
# carries it (D10). Fixtures live in profile-fixtures/, outside configs/ so they add no third pass.
try {
    $fixtures = Join-Path $Here 'profile-fixtures'
    $tmp = Join-Path $env:TEMP ('project-a-orch-profile-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Force -Path (Join-Path $tmp 'profiles') | Out-Null
    Copy-Item -LiteralPath (Join-Path $fixtures 'profile.json') -Destination (Join-Path $tmp 'profiles\default.json')
    Copy-Item -LiteralPath (Join-Path $fixtures 'project.orchestrator.json') -Destination (Join-Path $tmp 'orchestrator.json')
    ([pscustomobject]@{ profilesPath = (Join-Path $tmp 'profiles') } | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $tmp 'orchestrator.local.json') -Encoding UTF8

    $pctx = Import-OrchConfig (Join-Path $tmp 'orchestrator.json')
    $plannerModel = [string]$pctx.Config.roles.planner.chain[0].model
    $reviewerModel = [string]$pctx.Config.roles.'plan-reviewer'.chain[0].model
    $okMerge = ($plannerModel -eq 'project-planner' -and $reviewerModel -eq 'profile-reviewer' `
            -and [string]$pctx.Config.workflow.gatesPath -eq 'gates.md' `
            -and [bool]$pctx.Config.workflow.steps.preview.enabled `
            -and -not [bool]$pctx.Config.report.enabled `
            -and [string]$pctx.Config.contractDoc -eq 'docs/api.md' `
            -and $pctx.ProfileId -eq 'default' -and [int]$pctx.ProfileVersion -eq 2)
    Check 'P1' 'the profile is the base and the project overrides in place; workflow paths and steps both survive' `
        $okMerge ("planner=$plannerModel reviewer=$reviewerModel gates=$(if ($pctx.Config.workflow.PSObject.Properties['gatesPath']) { $pctx.Config.workflow.gatesPath }) preview=$(if ($pctx.Config.workflow.PSObject.Properties['steps']) { $pctx.Config.workflow.steps.preview.enabled }) profile=$($pctx.ProfileId) v$($pctx.ProfileVersion)")

    $base = [pscustomobject]@{ a = 1; drop = 'x'; keep = [pscustomobject]@{ one = 1; two = 2 } }
    $over = [pscustomobject]@{ a = 2; drop = $null; keep = [pscustomobject]@{ two = 3; three = 4 } }
    $m = Merge-ConfigObject $base $over
    $okDeep = ($m.a -eq 2 -and -not $m.PSObject.Properties['drop'] -and $m.keep.one -eq 1 -and $m.keep.two -eq 3 -and $m.keep.three -eq 4)
    Check 'P2' 'Merge-ConfigObject merges objects, replaces scalars and removes a null key' `
        $okDeep ("a=$($m.a) drop=$([bool]$m.PSObject.Properties['drop']) one=$($m.keep.one) two=$($m.keep.two) three=$($m.keep.three)")

    # No orchestrator.local.json beside the config: the one-line error names the setting.
    $bare = Join-Path $tmp 'bare'
    New-Item -ItemType Directory -Force -Path $bare | Out-Null
    Copy-Item -LiteralPath (Join-Path $fixtures 'project.orchestrator.json') -Destination (Join-Path $bare 'orchestrator.json')
    $errLocal = ''
    try { [void](Import-OrchConfig (Join-Path $bare 'orchestrator.json')) } catch { $errLocal = $_.Exception.Message }
    # A local file that names a folder with no export: the error says which file is missing.
    ([pscustomobject]@{ profilesPath = $bare } | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $bare 'orchestrator.local.json') -Encoding UTF8
    $errMissing = ''
    try { [void](Import-OrchConfig (Join-Path $bare 'orchestrator.json')) } catch { $errMissing = $_.Exception.Message }
    $okErrors = ($errLocal -match 'profilesPath' -and $errMissing -match "not found at" -and $errMissing -match 'default\.json')
    Check 'P3' 'a missing profilesPath or export is one clear line naming the file' `
        $okErrors ("local='$errLocal' missing='$errMissing'")

    # D6: a locked step turned off is rejected; preview is adjustable.
    $locked = Get-Content -LiteralPath (Join-Path $fixtures 'profile.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $locked.version = 3
    $locked.config.workflow.steps.review.enabled = $false
    ($locked | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath (Join-Path $tmp 'profiles\default.json') -Encoding UTF8
    $errLocked = ''
    try { [void](Import-OrchConfig (Join-Path $tmp 'orchestrator.json')) } catch { $errLocked = $_.Exception.Message }
    $locked.config.workflow.steps.review.enabled = $true
    $locked.config.workflow.steps.preview.enabled = $false
    ($locked | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath (Join-Path $tmp 'profiles\default.json') -Encoding UTF8
    # A second project copy with no workflow.steps of its own, so the profile's preview=false decides.
    $noSteps = Get-Content -LiteralPath (Join-Path $tmp 'orchestrator.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $noSteps.workflow.PSObject.Properties.Remove('steps')
    ($noSteps | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath (Join-Path $tmp 'preview.orchestrator.json') -Encoding UTF8
    $previewCtx = $null
    try { $previewCtx = Import-OrchConfig (Join-Path $tmp 'preview.orchestrator.json') } catch { $previewCtx = $null }
    $okLocked = ($errLocked -match "'review' is always on" -and $previewCtx -and -not [bool]$previewCtx.Config.workflow.steps.preview.enabled)
    Check 'P4' 'turning review off is rejected; turning preview off is accepted' `
        $okLocked ("locked='$errLocked' preview=$(if ($previewCtx) { $previewCtx.Config.workflow.steps.preview.enabled } else { 'n/a' })")

    if (-not $Keep) { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }
}
catch { Fail-Scenario 'P1' 'profile merge' ("exception: " + $_.Exception.Message) }

try {
    $ws = New-WorkSpace 'prof-run'
    Edit-Config -Ws $ws -Change { param($cfg) Set-CfgProp $cfg 'profile' 'default' }
    $profDir = Join-Path $ws 'profiles'
    New-Item -ItemType Directory -Force -Path $profDir | Out-Null
    $profileFile = Join-Path $profDir 'default.json'
    Copy-Item -LiteralPath (Join-Path $Here 'profile-fixtures\profile.json') -Destination $profileFile
    ([pscustomobject]@{ profilesPath = $profDir } | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $ws 'orchestrator.local.json') -Encoding UTF8

    $plan = Write-Plan $ws 'plan-prof-v1.md'
    $r = Run-Orch -Ws $ws -Argv @('new', 'P-RUN-001', 'Ui', 'mock profile task', '-PlanFile', $plan)
    $effFile = Join-Path $ws 'runs\P-RUN-001\config.effective.json'
    $eff = if (Test-Path -LiteralPath $effFile) { Get-Content -LiteralPath $effFile -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
    # The workspace config has its own plan-reviewer chain (D2: the project replaces the array), and the
    # tier is the profile's; both prove the merge ran.
    $okRecord = ($r.Code -eq 0 -and $eff -and $eff.profile -eq 'default' -and [int]$eff.profileVersion -eq 2 `
            -and [string]$eff.config.tiers.free.maxConcurrent -eq '1' `
            -and [string]$eff.config.roles.'plan-reviewer'.chain[0].model -eq 'mock-reviewer')
    $hash1 = if (Test-Path -LiteralPath $effFile) { (Get-FileHash -LiteralPath $effFile -Algorithm SHA256).Hash } else { '' }

    # The profile moves on to v3; a later command says so once and the run's own record never changes.
    $p = Get-Content -LiteralPath $profileFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $p.version = 3
    ($p | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $profileFile -Encoding UTF8
    $plan2 = Write-Plan $ws 'plan-prof-v2.md'
    $r2 = Run-Orch -Ws $ws -Argv @('revise', 'P-RUN-001', '-PlanFile', $plan2)
    $hash2 = if (Test-Path -LiteralPath $effFile) { (Get-FileHash -LiteralPath $effFile -Algorithm SHA256).Hash } else { '' }
    $oneLine = ($r2.Out -match 'profile default is v3 now; this run started on v2')
    Check 'P5' 'a run records its effective config once, and a newer profile prints one line' `
        ($okRecord -and $hash1 -eq $hash2 -and $oneLine) `
        ("code=$($r.Code) record=$okRecord hashSame=$($hash1 -eq $hash2) line=$oneLine profile=$(if ($eff) { $eff.profile }) v$(if ($eff) { $eff.profileVersion }) tier=$(if ($eff) { $eff.config.tiers.free.maxConcurrent })")

    $r3 = Run-Orch -Ws $ws -Argv @('close', 'P-RUN-001', '-Force')
    $arch = @(Get-ChildItem -LiteralPath (Join-Path $ws 'runs\_archive') -Filter 'P-RUN-001.*.json' -File -ErrorAction SilentlyContinue | Select-Object -First 1)
    $archText = if ($arch.Count) { Get-Content -LiteralPath $arch[0].FullName -Raw -Encoding UTF8 } else { '' }
    $okArchive = ($r3.Code -eq 0 -and $arch.Count -eq 1 -and $archText -match '"effectiveConfig"' -and $archText -match 'mock-reviewer' -and -not (Test-Path -LiteralPath (Join-Path $ws 'runs\P-RUN-001')))
    Check 'P6' 'close writes effectiveConfig into the archive before deleting the run folder' `
        $okArchive ("code=$($r3.Code) archives=$($arch.Count) hasEffective=$([bool]($archText -match '\"effectiveConfig\"'))")
}
catch { Fail-Scenario 'P5' 'a run with a profile' ("exception: " + $_.Exception.Message) }

# ---------------------------------------------------------------- report
Write-Host ''
$script:results | Format-Table -AutoSize | Out-String -Width 400 | Write-Host
$pass = @($script:results | Where-Object { $_.Result -eq 'PASS' }).Count
$total = @($script:results).Count
Write-Host ("{0}/{1} checks passed" -f $pass, $total) -ForegroundColor $(if ($script:failures) { 'Red' } else { 'Green' })

if (-not $Keep) {
    foreach ($w in $script:workspaces) {
        if (Test-Path -LiteralPath $w) { Remove-Item -LiteralPath $w -Recurse -Force -ErrorAction SilentlyContinue }
    }
}
else {
    Write-Host "workspaces kept: $($script:workspaces -join ', ')" -ForegroundColor DarkGray
}

if ($script:failures) { exit 1 }
exit 0
