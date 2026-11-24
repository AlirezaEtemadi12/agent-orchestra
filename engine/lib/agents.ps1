function Merge-ConfigObject {
    # Plan 0006 D2: the profile's config is the base, the project's whole file goes on top, key by key
    # and recursively. Arrays replace (a project chain is used as it stands); null removes a key.
    param($Base, $Over)
    if ($null -eq $Base) { return $Over }
    if ($null -eq $Over) { return $Base }
    if ($Base -isnot [pscustomobject] -or $Over -isnot [pscustomobject]) { return $Over }
    $result = [pscustomobject]@{}
    foreach ($p in $Base.PSObject.Properties) {
        $result | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value
    }
    foreach ($p in $Over.PSObject.Properties) {
        if ($null -eq $p.Value) {
            if ($result.PSObject.Properties[$p.Name]) { $result.PSObject.Properties.Remove($p.Name) }
            continue
        }
        $existing = $result.PSObject.Properties[$p.Name]
        if ($existing -and $existing.Value -is [pscustomobject] -and $p.Value -is [pscustomobject]) {
            $merged = Merge-ConfigObject $existing.Value $p.Value
            $result.PSObject.Properties.Remove($p.Name)
            $result | Add-Member -NotePropertyName $p.Name -NotePropertyValue $merged
        }
        else {
            if ($existing) { $result.PSObject.Properties.Remove($p.Name) }
            $result | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value
        }
    }
    return $result
}

function Get-ProfilesPath {
    # Plan 0006 D5: the dashboard's profiles folder comes from the project's git-ignored
    # orchestrator.local.json, beside the config - never from the config itself (a machine path never
    # enters a project's git).
    param([string]$ConfigPath)
    $local = Join-Path (Split-Path -Parent $ConfigPath) 'orchestrator.local.json'
    $missing = 'config: set "profilesPath" in orchestrator.local.json to the dashboard''s profiles folder'
    if (-not (Test-Path -LiteralPath $local)) { throw $missing }
    $settings = $null
    try { $settings = Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json } catch { throw $missing }
    $key = $settings.PSObject.Properties['profilesPath']
    if (-not $key -or -not $key.Value) { throw $missing }
    return [string]$key.Value
}

function Test-LockedStepOff {
    # Plan 0006 D6: these steps are always on; only `preview` is adjustable.
    param($Config, [string]$Step)
    $w = $Config.PSObject.Properties['workflow']
    if (-not $w -or -not $w.Value) { return $false }
    $steps = $w.Value.PSObject.Properties['steps']
    if (-not $steps -or -not $steps.Value) { return $false }
    $one = $steps.Value.PSObject.Properties[$Step]
    if (-not $one -or $one.Value -isnot [pscustomobject]) { return $false }
    $enabled = $one.Value.PSObject.Properties['enabled']
    if (-not $enabled) { return $false }
    return (-not [bool]$enabled.Value)
}

function Import-OrchConfig {
    param([Parameter(Mandatory)][string]$Path)
    $full = (Resolve-Path -LiteralPath $Path).Path
    $cfg = Get-Content -LiteralPath $full -Raw -Encoding UTF8 | ConvertFrom-Json
    $orchDir = Split-Path -Parent $full
    # Plan 0006 D9: the run records which profile and version it started on.
    $profileId = $null
    $profileVersion = $null
    $profileProp = $cfg.PSObject.Properties['profile']
    if ($profileProp -and $profileProp.Value) {
        $profileId = [string]$profileProp.Value
        $profilesPath = Get-ProfilesPath $full
        $profileFile = Join-Path $profilesPath "$profileId.json"
        if (-not (Test-Path -LiteralPath $profileFile)) {
            throw ("config: profile '{0}' not found at {1}; open the dashboard once or copy the export there" -f $profileId, $profileFile)
        }
        $export = Get-Content -LiteralPath $profileFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionProp = $export.PSObject.Properties['version']
        if ($versionProp -and $null -ne $versionProp.Value) { $profileVersion = [int]$versionProp.Value }
        $configProp = $export.PSObject.Properties['config']
        if (-not $configProp -or $configProp.Value -isnot [pscustomobject]) {
            throw ("config: profile '{0}' at {1} has no config object" -f $profileId, $profileFile)
        }
        # D2: profile base, the project's whole file on top (its own overrides stay in place).
        $cfg = Merge-ConfigObject $configProp.Value $cfg
        foreach ($step in @('understanding', 'plan', 'review', 'approve', 'implement', 'verify')) {
            if (Test-LockedStepOff $cfg $step) {
                throw ("config: step '{0}' is always on and cannot be turned off" -f $step)
            }
        }
    }
    $root = [System.IO.Path]::GetFullPath((Join-Path $orchDir $cfg.root))
    $runs = if ($cfg.PSObject.Properties['runsDir'] -and $cfg.runsDir) { [System.IO.Path]::GetFullPath((Join-Path $orchDir $cfg.runsDir)) } else { Join-Path $orchDir 'runs' }
    # D8: a manual (human) hop is forbidden only in the planner chain - the plan's author must be a model.
    # Every other role (plan-reviewer, readback, implementer, verifier) may end in a manual hop, answered
    # by a human-driven agent of the owner's choice.
    foreach ($role in $cfg.roles.PSObject.Properties.Name) {
        $chain = @($cfg.roles.$role.chain)
        $manual = @($chain | Where-Object { $_ -and (Get-AdapterTransport -Config $cfg -Adapter $_.adapter) -eq 'inbox' })
        if (-not $manual.Count) { continue }
        if ($role -eq 'planner') {
            throw ("config: '{0}' has a manual hop; a manual hop never acts on its own, so the planner is the one role that may not have one (its plan must be written by a model)." -f $role)
        }
        foreach ($h in $manual) {
            if (-not $h.PSObject.Properties['family'] -or -not $h.family) {
                throw ("config: the manual hop in '{0}' must declare 'family' (the model the human answers with) so gate 2 can compare families." -f $role)
            }
        }
    }
    # How the orchestrator talks to each adapter: 'cli' runs its command and reads the answer from stdout;
    # 'inbox' writes the prompt to runs/<id>/inbox/<key>.md and waits for a human to put the answer in
    # runs/<id>/answers/<key>.md (a desktop app or a tool whose terms forbid scripting it).
    foreach ($name in $cfg.adapters.PSObject.Properties.Name) {
        $t = Get-AdapterTransport -Config $cfg -Adapter $name
        if (@('cli', 'inbox') -notcontains $t) {
            throw ("config: adapter '{0}' has transport '{1}'; use 'cli' or 'inbox'." -f $name, $t)
        }
    }
    [pscustomobject]@{
        Config         = $cfg
        ConfigPath     = $full
        OrchDir        = $orchDir
        Root           = $root
        RunsDir        = $runs
        ProfileId      = $profileId
        ProfileVersion = $profileVersion
    }
}

function Get-AdapterTransport {
    # 'cli' or 'inbox' for one adapter: its own `transport` when the config gives one; otherwise `manual`
    # is the inbox and every other adapter is a CLI, which is how configs before `transport` behaved.
    param($Config, [string]$Adapter)
    $spec = $Config.adapters.$Adapter
    if ($spec -and $spec.PSObject.Properties['transport'] -and $spec.transport) { return ([string]$spec.transport).ToLower() }
    if ($Adapter -eq 'manual') { return 'inbox' }
    return 'cli'
}

function Get-CfgValue {
    # Reads $Ctx.Config.<section>.<name>, or $Default when either is missing.
    param($Ctx, [string]$Section, [string]$Name, $Default)
    $s = $Ctx.Config.PSObject.Properties[$Section]
    if ($null -eq $s -or $null -eq $s.Value) { return $Default }
    $p = $s.Value.PSObject.Properties[$Name]
    if ($null -eq $p -or $null -eq $p.Value) { return $Default }
    return $p.Value
}

function Test-AdapterEnabled {
    param($Ctx, [string]$Adapter)
    $a = $Ctx.Config.adapters.$Adapter
    if ($null -eq $a) { return $false }
    if ($null -ne $a.PSObject.Properties['enabled']) { return [bool]$a.enabled }
    return $true
}

function Get-HopFamily {
    # The underlying model family of a chain hop. Gate 2 compares families, not model strings,
    # so "deepseek-v4.1-flash" (codebuddy) and "opencode/deepseek-v4.1-flash-free" count as the same.
    param($Hop)
    if ($Hop.PSObject.Properties['family'] -and $Hop.family) { return ([string]$Hop.family).ToLower() }
    if ($Hop.adapter -eq 'claude') {
        # D1: Claude families are per model, so gate 2 can tell an Opus plan from a Sonnet review.
        $name = ([string]$Hop.model) -replace '^claude-', ''
        return ('claude-' + (($name -split '[-_.]')[0])).ToLower()
    }
    $name = ([string]$Hop.model) -replace '^.*/', '' -replace '-free$', ''
    return (($name -split '[-_.]')[0]).ToLower()
}

function Get-ReadBackRole {
    # The chain the read-back runs on: its own `readback` role when the config has one (a human-driven
    # agent of the owner's choice), else the implementer's chain run read-only, as before.
    param($Ctx)
    if ($Ctx.Config.roles.PSObject.Properties['readback']) { return 'readback' }
    return 'implementer'
}

function Test-ReadBackIsManual {
    # True when the read-back's first usable hop is `manual`: it then never runs a model, so new/revise
    # request it BEFORE the review and the human works while the reviewer does.
    param($Ctx)
    foreach ($hop in @($Ctx.Config.roles.(Get-ReadBackRole $Ctx).chain)) {
        if (-not (Test-AdapterEnabled $Ctx $hop.adapter)) { continue }
        return ((Get-AdapterTransport -Config $Ctx.Config -Adapter $hop.adapter) -eq 'inbox')
    }
    return $false
}

function ConvertTo-CmdArg {
    param([string]$Value)
    if ([string]::IsNullOrEmpty($Value)) { return '""' }
    if ($Value -match '[\s"]') { return '"' + ($Value -replace '(\\*)"', '$1$1\"') + '"' }
    return $Value
}

function Stop-ProcessTree {
    # npm CLIs run as cmd.exe -> node; Process.Kill() on PS 5.1 kills only cmd.exe, and the orphaned
    # node keeps the stdout pipe open. taskkill /T takes the whole tree.
    param([System.Diagnostics.Process]$Proc)
    try { & "$env:SystemRoot\System32\taskkill.exe" /T /F /PID $Proc.Id 2>&1 | Out-Null } catch { }
    try { if (-not $Proc.HasExited) { $Proc.Kill() } } catch { }
    try { [void]$Proc.WaitForExit(5000) } catch { }
}

function Format-StripAnsi {
    # CLI output is full of colour codes; progress.json is read by a human and by ConvertFrom-Json,
    # so the escape sequences go before a line is stored.
    param([string]$Text)
    if (-not $Text) { return '' }
    return ($Text -replace '\x1b\][^\x07]*(\x07|\x1b\\)', '' -replace '\x1b\[[0-9;?]*[ -/]*[@-~]', '')
}

function Receive-PendingLines {
    # Drains every complete line that is ready from one reader task into the sink and returns the task
    # to poll next, or $null at end-of-stream. ReadLineAsync is used on purpose: OutputDataReceived /
    # ErrorDataReceived handlers only run when the runspace is idle, so a wait loop in the same
    # runspace receives nothing for the whole run (measured on PS 5.1: 0 lines from a child that
    # printed every 200 ms for 3 s).
    #
    # Chunks, not lines: one ReadLineAsync per line cost ~5 ms of PowerShell overhead each, and with the
    # loop's 500 ms wait between drains a Windows pipe (a few KB) refilled once per wait - 800 KB of output
    # took over 100 s and held the child blocked on a full pipe. An 8 KB ReadAsync per call drains the
    # same in well under a second. Every \r is dropped: callers split on "`n" (git porcelain lines would
    # otherwise carry a trailing \r into a path). $State.Got tells the loop data flowed, so it re-polls
    # at once instead of waiting.
    param([System.IO.StreamReader]$Reader, [System.Threading.Tasks.Task]$Task, [char[]]$Buffer, [System.Text.StringBuilder]$Sink, [hashtable]$State)
    while ($Task -and $Task.IsCompleted) {
        $n = 0
        try { $n = [int]$Task.Result } catch { $n = 0 }
        if ($n -le 0) { return $null }
        $text = (New-Object string($Buffer, 0, $n)).Replace("`r", '')
        [void]$Sink.Append($text)
        $State.Last = Get-Date
        $State.Got = $true
        $lastLine = @($text -split "`n" | Where-Object { $_.Trim() }) | Select-Object -Last 1
        if ($lastLine) { $State.Line = (Format-StripAnsi "$lastLine") }
        $Task = $Reader.ReadAsync($Buffer, 0, $Buffer.Length)
    }
    return $Task
}

function Write-ProgressFile {
    # runs/<id>/progress.json - what the owner watches with `run.ps1 watch`. Temp file + Move-Item so a
    # reader never sees half a document; a failed write is never fatal for the hop itself.
    param([string]$Path, [hashtable]$Meta, [hashtable]$State)
    if (-not $Path) { return }
    try {
        $dir = Split-Path -Parent $Path
        if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        $now = Get-Date
        $started = $State.Started
        $last = if ($State.Last) { $State.Last } else { $started }
        $line = [string]$State.Line
        if ($line.Length -gt 160) { $line = $line.Substring(0, 160) }
        $obj = [ordered]@{
            taskId           = [string]$Meta.taskId
            command          = [string]$Meta.command
            role             = [string]$Meta.role
            adapter          = [string]$Meta.adapter
            model            = [string]$Meta.model
            startedAt        = $started.ToString('s')
            elapsedSec       = [int]($now - $started).TotalSeconds
            lastOutputAgoSec = [int]($now - $last).TotalSeconds
            lastLine         = $line
            estimate         = $(if ($Meta.estimate) { $Meta.estimate } else { [pscustomobject]@{ typicalSec = $null; basis = 'no history' } })
            state            = [string]$State.State
        }
        if ($Meta.phase) { $obj['phase'] = [string]$Meta.phase }
        # D10: a hop waiting for its pool's slot says so here, so `watch`/`status` can report it.
        if ($Meta.pool) { $obj['pool'] = [string]$Meta.pool }
        if ($Meta.heldBy) { $obj['heldBy'] = [string]$Meta.heldBy }
        $tmp = "$Path.tmp"
        ($obj | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $tmp -Encoding UTF8
        Move-Item -LiteralPath $tmp -Destination $Path -Force
        # Plan 0004 §6.1: the same numbers `watch` reads go to the dashboard, at most every
        # report.progressEverySec (D5). This file has no $Ctx, so the reporter keeps the one run.ps1 set.
        # Guarded: agents.ps1 is dot-sourced on its own by the offline suite and by a pool holder.
        if (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue) {
            Send-OrchestraEvent -Ctx $script:ReportCtx -TaskId ([string]$obj.taskId) -Kind 'progress' -Data $obj
        }
    }
    catch { }
}

function Get-RoleEstimate {
    # Median seconds of the SUCCESSFUL attempts of the same role and adapter:model, read from every
    # runs/*/transcript/*.json (they already record `seconds` per attempt). A hint for `watch`, never a
    # timeout: fewer than two runs means no number at all.
    param($Ctx, [string]$Role, [string]$Adapter, [string]$Model)
    $none = [pscustomobject]@{ typicalSec = $null; basis = 'no history' }
    if (-not $Ctx.RunsDir -or -not (Test-Path -LiteralPath $Ctx.RunsDir)) { return $none }
    $secs = @()
    foreach ($d in @(Get-ChildItem -LiteralPath $Ctx.RunsDir -Directory -ErrorAction SilentlyContinue)) {
        $tdir = Join-Path $d.FullName 'transcript'
        if (-not (Test-Path -LiteralPath $tdir)) { continue }
        foreach ($f in @(Get-ChildItem -LiteralPath $tdir -Filter '*.json' -File -ErrorAction SilentlyContinue)) {
            # plan-reviewer.v3 / readback.v3 / implementer.phase-1 / verifier
            $label = ($f.BaseName -split '\.')[0]
            if ($label -ne $Role) { continue }
            try { $t = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
            foreach ($a in @($t.Attempts | Where-Object { $_ })) {
                if (-not $a.ok) { continue }
                if ($a.adapter -ne $Adapter -or $a.model -ne $Model) { continue }
                if ($null -eq $a.seconds) { continue }
                $secs += [int]$a.seconds
            }
        }
    }
    if ($secs.Count -lt 2) { return $none }
    $sorted = @($secs | Sort-Object)
    $med = if ($sorted.Count % 2) { $sorted[[int](($sorted.Count - 1) / 2)] } else { [int](($sorted[$sorted.Count / 2 - 1] + $sorted[$sorted.Count / 2]) / 2) }
    return [pscustomobject]@{ typicalSec = $med; basis = "median of $($sorted.Count) runs" }
}

function Invoke-External {
    param(
        [Parameter(Mandatory)][string]$Exe,
        [string[]]$ArgList = @(),
        [string]$StdIn,
        [string]$Cwd,
        [int]$TimeoutSec = 300,
        [int]$IdleTimeoutSec = 0,
        [string]$ProgressFile = '',
        [hashtable]$ProgressMeta = $null,
        [string[]]$RemoveEnv = @()
    )
    $cmd = Get-Command $Exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { $Exe = $cmd.Source }
    if ($Exe -match '\.(cmd|bat)$') {
        $ArgList = @('/c', $Exe) + $ArgList
        $Exe = $env:ComSpec
    }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.Arguments = (($ArgList | ForEach-Object { ConvertTo-CmdArg $_ }) -join ' ')
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $psi.CreateNoWindow = $true
    if ($Cwd) {
        $psi.WorkingDirectory = $Cwd
        # A caller launched from Git Bash inherits PWD (its own directory), and opencode's `run` trusts PWD
        # over the real working directory: the agent then worked in - and filed its session under - the
        # caller's folder instead of $Cwd. Keep PWD in step with the directory we actually start in.
        $psi.EnvironmentVariables['PWD'] = $Cwd
    }
    foreach ($k in $RemoveEnv) { try { [void]$psi.EnvironmentVariables.Remove($k) } catch { } }

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    # .NET Framework builds the stdin writer from Console.InputEncoding with AutoFlush on, so a UTF-8
    # console (code page 65001) sends a BOM before the prompt. Start with a BOM-less encoding.
    $prevIn = $null
    try { $prevIn = [Console]::InputEncoding; [Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }
    try { [void]$proc.Start() }
    finally { if ($prevIn) { try { [Console]::InputEncoding = $prevIn } catch { } } }
    $outSb = New-Object System.Text.StringBuilder
    $errSb = New-Object System.Text.StringBuilder
    $state = @{ Started = (Get-Date); Last = (Get-Date); Line = ''; State = 'running'; Got = $false }
    $meta = @{}
    if ($ProgressMeta) { foreach ($k in $ProgressMeta.Keys) { $meta[$k] = $ProgressMeta[$k] } }
    $meta.command = (([System.IO.Path]::GetFileName($psi.FileName)) + ' ' + ($ArgList -join ' ')).Trim()
    # Start reading BEFORE writing stdin, and write stdin asynchronously, closing it once the write
    # completes inside the wait loop. A blocking write of a large prompt deadlocks against a child that
    # prints more than a pipe buffer before it reads its stdin: both sides wait on each other, and no
    # timeout can fire because the wait loop is never reached (reproduced: 300 KB in, 800 KB out).
    $outBuf = New-Object char[] 8192
    $errBuf = New-Object char[] 8192
    $outTask = $proc.StandardOutput.ReadAsync($outBuf, 0, $outBuf.Length)
    $errTask = $proc.StandardError.ReadAsync($errBuf, 0, $errBuf.Length)
    $inTask = $null
    if ($null -ne $StdIn) {
        try {
            $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($StdIn)
            $inTask = $proc.StandardInput.BaseStream.WriteAsync($bytes, 0, $bytes.Length)
        } catch { $inTask = $null }
    }
    if (-not $inTask) { try { $proc.StandardInput.Close() } catch { } }
    Write-ProgressFile -Path $ProgressFile -Meta $meta -State $state
    $timedOut = $false; $kind = $null; $exited = $false
    $nextBeat = $state.Started.AddSeconds(60)
    $nextProgress = $state.Started.AddSeconds(30)
    $label = [System.IO.Path]::GetFileName($psi.FileName)
    while ($true) {
        if ($inTask -and $inTask.IsCompleted) {
            # Closing stdin is the child's end-of-prompt; a faulted write (the child exited early) is ignored.
            try { $proc.StandardInput.BaseStream.Flush() } catch { }
            try { $proc.StandardInput.Close() } catch { }
            $inTask = $null
        }
        if ($outTask) { $outTask = Receive-PendingLines -Reader $proc.StandardOutput -Task $outTask -Buffer $outBuf -Sink $outSb -State $state }
        if ($errTask) { $errTask = Receive-PendingLines -Reader $proc.StandardError -Task $errTask -Buffer $errBuf -Sink $errSb -State $state }
        if ($proc.HasExited) { $exited = $true; break }
        $now = Get-Date
        # Idle first: a hop that keeps printing is never killed for being slow (P1).
        if ($IdleTimeoutSec -gt 0 -and ($now - $state.Last).TotalSeconds -ge $IdleTimeoutSec) { $kind = 'idle'; break }
        if (($now - $state.Started).TotalSeconds -ge $TimeoutSec) { $kind = 'total'; break }
        if ($now -ge $nextProgress) { Write-ProgressFile -Path $ProgressFile -Meta $meta -State $state; $nextProgress = $now.AddSeconds(30) }
        if ($now -ge $nextBeat) {
            Write-Host ("  [{0}] working, last output {1:n0} s ago" -f $label, ($now - $state.Last).TotalSeconds) -ForegroundColor DarkGray
            $nextBeat = $now.AddSeconds(60)
        }
        # Data flowed: poll again at once so a full pipe is drained before the child blocks on it.
        if ($state.Got) { $state.Got = $false; [void]$proc.WaitForExit(10) } else { [void]$proc.WaitForExit(500) }
    }
    if (-not $exited) { $timedOut = $true; Stop-ProcessTree $proc }

    # A grandchild that outlives its parent can still hold the pipes; never wait on it forever.
    $drainUntil = (Get-Date).AddSeconds(10)
    while (($outTask -or $errTask) -and (Get-Date) -lt $drainUntil) {
        if ($outTask) { $outTask = Receive-PendingLines -Reader $proc.StandardOutput -Task $outTask -Buffer $outBuf -Sink $outSb -State $state }
        if ($errTask) { $errTask = Receive-PendingLines -Reader $proc.StandardError -Task $errTask -Buffer $errBuf -Sink $errSb -State $state }
        if ($outTask -or $errTask) { Start-Sleep -Milliseconds 100 }
    }
    $stdout = $outSb.ToString()
    $stderr = $errSb.ToString()
    if ($outTask) { $stdout += "`n(stdout unavailable: pipe held open by an orphaned child process)" }
    if ($errTask) { $stderr += "`n(stderr unavailable: pipe held open by an orphaned child process)" }
    $exitCode = -1
    if (-not $timedOut) { try { $exitCode = $proc.ExitCode } catch { } }
    $state.State = $(if ($timedOut) { 'failed' } elseif ($exitCode -eq 0) { 'done' } else { 'failed' })
    Write-ProgressFile -Path $ProgressFile -Meta $meta -State $state

    [pscustomobject]@{
        ExitCode    = $exitCode
        Stdout      = $stdout
        Stderr      = $stderr
        TimedOut    = $timedOut
        TimeoutKind = $kind
    }
}

function Test-ReadOnlyEnforced {
    # True when a read-only hop of this adapter cannot write at the tool level: opencode runs it as the
    # "plan" agent (edit and bash denied), claude and codebuddy with read-only --allowedTools. A generic
    # exec adapter, an inbox/manual hop or any other CLI is read-only by prompt only, so it stays policed.
    param($Ctx, [string]$Adapter)
    if ($Adapter -notin @('opencode', 'claude', 'codebuddy')) { return $false }
    $spec = $Ctx.Config.adapters.$Adapter
    if ($spec -and $spec.PSObject.Properties['type'] -and $spec.type -eq 'exec') { return $false }
    return (Get-AdapterTransport -Config $Ctx.Config -Adapter $Adapter) -eq 'cli'
}

function Invoke-OpenCode {
    param([string]$Model, [string]$Prompt, [string]$Cwd, [bool]$Auto, [bool]$ReadOnly, [int]$TimeoutSec, [int]$IdleTimeoutSec = 0, [string]$ProgressFile = '', [hashtable]$ProgressMeta = $null, [string]$SessionId = '', [string]$SessionTitle = '')
    $a = @('run', '-m', $Model, '--format', 'default')
    # opencode's built-in "plan" agent has edit and bash denied; without it a read-only role
    # would be read-only by prompt only.
    # A write role names "build" explicitly: a continued session otherwise keeps the agent it was
    # started with, so implement continuing the read-only read-back session stayed in "plan" and
    # every write was refused (T-AGENT-027 phase 1, 2026-11-14).
    if ($ReadOnly) { $a += @('--agent', 'plan') } else { $a += @('--agent', 'build') }
    if ($Auto) { $a += '--auto' }
    # D5: continue an existing session, or title a new one so its id can be looked up afterwards.
    if ($SessionId) { $a += @('--session', $SessionId) }
    elseif ($SessionTitle) { $a += @('--title', $SessionTitle) }
    $res = Invoke-External -Exe 'opencode' -ArgList $a -StdIn $Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -IdleTimeoutSec $IdleTimeoutSec -ProgressFile $ProgressFile -ProgressMeta $ProgressMeta
    $sid = $null
    if ($SessionId) { $sid = $SessionId }
    elseif ($SessionTitle -and $res.ExitCode -eq 0 -and -not $res.TimedOut) {
        # D5: sessions are per project directory, so list them in the SAME cwd. A failed lookup never
        # fails the hop; it just leaves SessionId unknown.
        try {
            $list = Invoke-External -Exe 'opencode' -ArgList @('session', 'list', '--format', 'json', '-n', '20') -Cwd $Cwd -TimeoutSec 60
            if ($list.ExitCode -eq 0 -and $list.Stdout) {
                # Windows PowerShell 5.1 emits a JSON array as ONE pipeline object; assigning it first lets
                # foreach enumerate the entries (inline @(... | ConvertFrom-Json) yields a single element).
                $entries = $list.Stdout | ConvertFrom-Json
                foreach ($e in $entries) { if ([string]$e.title -eq $SessionTitle) { $sid = [string]$e.id; break } }
                if (-not $sid) { Write-Host "  [opencode] session '$SessionTitle' not found among $(@($entries).Count) listed sessions in $Cwd" -ForegroundColor DarkYellow }
            }
            else { Write-Host "  [opencode] session list failed (exit $($list.ExitCode), $(([string]$list.Stdout).Length) chars)" -ForegroundColor DarkYellow }
        }
        catch { $sid = $null; Write-Host "  [opencode] session lookup threw: $($_.Exception.Message)" -ForegroundColor DarkYellow }
    }
    $res | Add-Member -NotePropertyName SessionId -NotePropertyValue $sid -Force
    return $res
}

function Invoke-Claude {
    param([string]$Model, [string]$Prompt, [string]$Cwd, [bool]$ReadOnly, [int]$TimeoutSec, [int]$IdleTimeoutSec = 0, [string]$ProgressFile = '', [hashtable]$ProgressMeta = $null)
    $a = @('-p', '--output-format', 'text')
    if ($Model) { $a += @('--model', $Model) }
    if ($ReadOnly) {
        $a += @('--allowedTools', 'Read,Grep,Glob,LS,WebFetch,WebSearch')
        $a += @('--disallowedTools', 'Edit,Write,MultiEdit,NotebookEdit,Bash')
    }
    Invoke-External -Exe 'claude' -ArgList $a -StdIn $Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -IdleTimeoutSec $IdleTimeoutSec -ProgressFile $ProgressFile -ProgressMeta $ProgressMeta `
        -RemoveEnv @('ANTHROPIC_BASE_URL', 'ANTHROPIC_MODEL', 'ANTHROPIC_AUTH_TOKEN', 'ANTHROPIC_API_KEY', 'ANTHROPIC_SMALL_FAST_MODEL')
}

function Invoke-CodeBuddy {
    param([string]$Model, [string]$Prompt, [string]$Cwd, [bool]$Auto, [bool]$ReadOnly, [int]$TimeoutSec, [int]$IdleTimeoutSec = 0, [string]$ProgressFile = '', [hashtable]$ProgressMeta = $null)
    $a = @('-p', '--output-format', 'text')
    if ($Model) { $a += @('--model', $Model) }
    if ($Auto) { $a += '--dangerously-skip-permissions' }
    elseif ($ReadOnly) { $a += @('--allowedTools', 'Read,Grep,Glob') }
    Invoke-External -Exe 'codebuddy' -ArgList $a -StdIn $Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -IdleTimeoutSec $IdleTimeoutSec -ProgressFile $ProgressFile -ProgressMeta $ProgressMeta
}

function Invoke-ExecAdapter {
    # Generic adapter: "type": "exec" runs `command args... <model>` with the prompt on stdin.
    # Used for the offline test agent and for any CLI without a dedicated adapter.
    param($Spec, [string]$Model, [string]$Prompt, [string]$Cwd, [bool]$ReadOnly, [int]$TimeoutSec, [int]$IdleTimeoutSec = 0, [string]$ProgressFile = '', [hashtable]$ProgressMeta = $null, [string]$SessionId = '', [string]$SessionTitle = '')
    $sessions = [bool]($Spec.PSObject.Properties['sessions'] -and $Spec.sessions)
    $a = @($Spec.args | Where-Object { $null -ne $_ })
    # D5: a session-capable exec agent takes -Session <id> to continue and reports its id on stderr.
    if ($sessions -and $SessionId) { $a += @('-Session', $SessionId) }
    $a += @($Model)
    $exe = [string]$Spec.command
    $res = Invoke-External -Exe $exe -ArgList $a -StdIn $Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -IdleTimeoutSec $IdleTimeoutSec -ProgressFile $ProgressFile -ProgressMeta $ProgressMeta
    $sid = $null
    if ($sessions) {
        $m = [regex]::Match([string]$res.Stderr, '(?m)^ORCH-SESSION:\s*(\S+)')
        if ($m.Success) { $sid = $m.Groups[1].Value }
    }
    $res | Add-Member -NotePropertyName SessionId -NotePropertyValue $sid -Force
    return $res
}

function Invoke-Manual {
    <#
      The bridge for a tool with no headless mode (a desktop app, an interactive TUI): the prompt is
      written to runs/<id>/inbox/<key>.md and a human writes runs/<id>/answers/<key>.md. It never waits
      (D7): the hop returns -2 'manual: deferred' at once, run.ps1 exits 3, and the same command is
      rerun after `run.ps1 answer`.
      The key says what the answer is valid for (plan-reviewer.v<round>, verifier.<hash>); a prompt whose
      tree moved is refused with -3, because the answer was written for a different repo. An implementer
      phase key (D8) is the exception: the repo is meant to change, so that refusal is skipped and the
      whole-workspace fingerprint is saved as <key>.workspace.json for Cmd-Implement's post-checks.
    #>
    param($Ctx, [string]$TaskId, [string]$Key, [string]$Prompt, [string]$RepoPath, [string[]]$AvoidFamilies = @())
    if (-not $Key) { throw "manual adapter: this hop has no manual key (the caller must pass -ManualKey)" }
    $isImpl = $Key -like 'implementer.phase-*'
    $dir = Get-TaskDir $Ctx $TaskId
    $inbox = Join-Path $dir 'inbox'
    $answers = Join-Path $dir 'answers'
    foreach ($d in @($inbox, $answers)) { if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Force -Path $d | Out-Null } }
    $answerFile = Join-Path $answers "$Key.md"
    $promptFile = Join-Path $inbox "$Key.md"
    $fpFile = Join-Path $inbox "$Key.fingerprint"
    $wsFile = Join-Path $inbox "$Key.workspace.json"

    if ((Test-Path -LiteralPath $answerFile) -and (Get-Item -LiteralPath $answerFile).Length -gt 0) {
        $now = if ($RepoPath) { Get-RepoFingerprint $RepoPath } else { '' }
        $then = if (Test-Path -LiteralPath $fpFile) { (Get-Content -LiteralPath $fpFile -Raw).Trim() } else { '' }
        if (-not $isImpl -and $then -and $now -ne $then) {
            # Plan 0011 D7: a value written by an older engine (no "v2:" prefix) is not comparable with a
            # fresh one; accept the human's answer with a note instead of refusing it over an upgrade.
            if ($then -ne '(missing)' -and -not $then.StartsWith('v2:')) {
                Write-Host '  note: fingerprint from an older engine, not compared' -ForegroundColor Yellow
            }
            else {
                return [pscustomobject]@{ ExitCode = -3; Stdout = ''; Stderr = 'manual: the repo changed after the prompt was written'; TimedOut = $false; TimeoutKind = $null }
            }
        }
        # ReadAllText, not Get-Content: a Get-Content string carries PSPath/PSDrive/PSProvider note
        # properties, and serializing it into a transcript drags the whole provider object graph along
        # (98 MB of JSON for a 46-character answer - it looked like a hang).
        return [pscustomobject]@{
            ExitCode    = 0
            Stdout      = [System.IO.File]::ReadAllText($answerFile)
            Stderr      = ''
            TimedOut    = $false
            TimeoutKind = $null
        }
    }

    if (-not (Test-Path -LiteralPath $promptFile)) {
        # The human hands this file to any agent, so the request says where its answer goes, what must
        # not change meanwhile, and which model families to avoid (gate-2 independence, D8).
        $lines = @("# MANUAL REQUEST - key: $Key", '')
        if ($AvoidFamilies -and @($AvoidFamilies).Count) {
            $lines += "Any agent can answer this. Do not answer with a model of family: $(@($AvoidFamilies) -join ', ')"
        }
        $lines += 'If you would rather wait for the quota to reset, rerun the same command with -Retry instead.'
        $lines += ''
        if ($isImpl) {
            $lines += 'Implement this phase in the repo as the request below says.'
            $lines += 'Do not commit, do not create, switch or delete branches, and write only inside the target repo.'
            $lines += "When done, write your report into this file (create it; nothing else): $answerFile"
            $lines += 'Its last line must be the VERDICT line the request below asks for.'
        }
        else {
            $lines += "Write your WHOLE reply into this file (create it; nothing else): $answerFile"
            $lines += 'Its last line must be the VERDICT line the request below asks for.'
            $lines += 'Do not edit, create or delete any other file, and do not run git: an answer written while the target repo changed is refused.'
        }
        if ($RepoPath) { $lines += "Read every relative path in the request against the repo root: $RepoPath" }
        $lines += ''
        $lines += '---'
        $lines += ''
        $head = $lines -join "`n"
        # Atomic: a half-written prompt or fingerprint is worse than none.
        $tmp = "$promptFile.tmp"
        Set-Content -LiteralPath $tmp -Value ($head + $Prompt) -Encoding UTF8
        Move-Item -LiteralPath $tmp -Destination $promptFile -Force
        if ($isImpl) {
            # D8: the repo is meant to change; snapshot the whole workspace so a sibling repo the human
            # touched is still a violation in Cmd-Implement's post-checks.
            $tmpWs = "$wsFile.tmp"
            Set-Content -LiteralPath $tmpWs -Value ((Get-WorkspaceFingerprint $Ctx) | ConvertTo-Json -Depth 4) -Encoding UTF8
            Move-Item -LiteralPath $tmpWs -Destination $wsFile -Force
        }
        elseif ($RepoPath) {
            $tmpFp = "$fpFile.tmp"
            Set-Content -LiteralPath $tmpFp -Value (Get-RepoFingerprint $RepoPath) -Encoding UTF8 -NoNewline
            Move-Item -LiteralPath $tmpFp -Destination $fpFile -Force
        }
    }
    [pscustomobject]@{
        ExitCode    = -2
        Stdout      = ''
        Stderr      = "manual: deferred - answer expected at $answerFile"
        TimedOut    = $false
        TimeoutKind = $null
    }
}

function Invoke-Agent {
    param($Ctx, [string]$Adapter, [string]$Model, [string]$Prompt, [string]$Cwd, [bool]$ReadOnly, [bool]$Auto, [int]$TimeoutSec, [int]$IdleTimeoutSec = 0, [string]$ProgressFile = '', [hashtable]$ProgressMeta = $null, [string]$TaskId = '', [string]$ManualKey = '', [string]$RepoPath = '', [string]$SessionId = '', [string]$SessionTitle = '', [string[]]$AvoidFamilies = @())
    $spec = $Ctx.Config.adapters.$Adapter
    $common = @{ IdleTimeoutSec = $IdleTimeoutSec; ProgressFile = $ProgressFile; ProgressMeta = $ProgressMeta }
    $res = $null
    if ((Get-AdapterTransport -Config $Ctx.Config -Adapter $Adapter) -eq 'inbox') {
        $res = Invoke-Manual -Ctx $Ctx -TaskId $TaskId -Key $ManualKey -Prompt $Prompt -RepoPath $RepoPath -AvoidFamilies $AvoidFamilies
    }
    elseif ($spec -and $spec.PSObject.Properties['type'] -and $spec.type -eq 'exec') {
        $res = Invoke-ExecAdapter -Spec $spec -Model $Model -Prompt $Prompt -Cwd $Cwd -ReadOnly $ReadOnly -TimeoutSec $TimeoutSec @common -SessionId $SessionId -SessionTitle $SessionTitle
    }
    else {
        switch ($Adapter) {
            'opencode' { $res = Invoke-OpenCode -Model $Model -Prompt $Prompt -Cwd $Cwd -Auto $Auto -ReadOnly $ReadOnly -TimeoutSec $TimeoutSec @common -SessionId $SessionId -SessionTitle $SessionTitle }
            'claude'   { $res = Invoke-Claude   -Model $Model -Prompt $Prompt -Cwd $Cwd -ReadOnly $ReadOnly -TimeoutSec $TimeoutSec @common }
            'codebuddy'{ $res = Invoke-CodeBuddy -Model $Model -Prompt $Prompt -Cwd $Cwd -Auto $Auto -ReadOnly $ReadOnly -TimeoutSec $TimeoutSec @common }
            default    { throw "Unknown adapter: $Adapter" }
        }
    }
    # D5: every result carries a SessionId ($null when the adapter has no sessions).
    if ($null -ne $res -and -not $res.PSObject.Properties['SessionId']) { $res | Add-Member -NotePropertyName SessionId -NotePropertyValue $null -Force }
    return $res
}

function Get-FailureClass {
    # Why a hop failed, from its exit/stdout/stderr. Only 'rate-limit' is retried on the same hop.
    param($Res, $Ctx = $null)
    if ($Res.TimedOut) { return 'timeout' }
    $text = "$($Res.Stderr)`n$($Res.Stdout)"
    if ($Res.ExitCode -eq 0) {
        if (-not ($Res.Stdout -and $Res.Stdout.Trim())) { return 'empty-output' }
        return $null
    }
    # D7: the manual hop does not wait - 'deferred' stops the command (exit 3) so the owner can answer.
    if ($Res.ExitCode -eq -2 -and $text -match '(?i)manual: deferred') { return 'deferred' }
    if ($Res.ExitCode -eq -3) { return 'manual-violation' }
    # D2: quota exhaustion is its own class, checked before rate-limit: a literal, case-insensitive
    # match on any configured pattern means the whole pool is out of quota, not a retryable 429.
    if ($Ctx) {
        foreach ($p in @(Get-CfgValue $Ctx 'quota' 'patterns' @())) {
            if ($p -and $text -match [regex]::Escape([string]$p)) { return 'quota-exhausted' }
        }
    }
    if ($text -match '(?i)rate.?limit|\b429\b|too many requests|overloaded|\b529\b') { return 'rate-limit' }
    if ($text -match '(?i)(model|provider).{0,40}(not found|unknown|invalid|not available|does not exist|unsupported)|unknown model|invalid model') { return 'bad-model' }
    if ($text -match '(?i)unauthori[sz]ed|\b401\b|\b403\b|not logged in|please log ?in|authenticat|api key') { return 'auth' }
    return "exit $($Res.ExitCode)"
}

function Test-HopSessions {
    # D5: a hop can carry a session when its adapter is opencode, or an exec adapter whose spec says
    # sessions:true (the offline mock). Everything else is stateless.
    param($Ctx, $Hop)
    if ([string]$Hop.adapter -eq 'opencode') { return $true }
    $spec = $Ctx.Config.adapters.([string]$Hop.adapter)
    if ($spec -and $spec.PSObject.Properties['type'] -and $spec.type -eq 'exec' -and $spec.PSObject.Properties['sessions'] -and $spec.sessions) { return $true }
    return $false
}

function Invoke-HopOnce {
    # Runs ONE chain hop with its pool slot, fingerprint guards and rate-limit retries. Factored out of
    # the Invoke-Role walk so a session continuation (D6) runs a hop under exactly the same rules.
    # Returns an outcome (Status ok|fail|skip|throw); the caller returns, falls through, or throws.
    param($Ctx, [string]$Role, $Hop, [string]$Prompt, [string]$Cwd, [int]$TimeoutSec, [string[]]$ExcludeFamilies, [bool]$ReadOnly, [bool]$Auto, [string]$EstRole, [string]$Repo, [string]$ProgressFile, [string]$Phase, [string]$TaskId, [string]$ManualKey, [string]$RepoPath, [string]$SessionId, [string]$SessionTitle, [string]$ReasonPrefix = '', [switch]$ManualPending)
    $family = Get-HopFamily $Hop
    $label = "$($Hop.adapter):$($Hop.model)"
    # D2, before the skips: every attempt records the quota it belongs to, including one never called.
    $limits = Get-HopLimits $Ctx $Hop
    $mkAttempt = {
        param($reason, $ok = $false, $extra = $null)
        $a = [ordered]@{ adapter = $Hop.adapter; model = $Hop.model; family = $family; pool = $limits.Pool; tier = $limits.Tier; ok = $ok; reason = ($ReasonPrefix + $reason) }
        if ($extra) { foreach ($k in $extra.Keys) { $a[$k] = $extra[$k] } }
        [pscustomobject]$a
    }
    # D9: a pending manual request is answered, not overtaken - every non-inbox hop waits for it.
    if ($ManualPending -and (Get-AdapterTransport -Config $Ctx.Config -Adapter $Hop.adapter) -ne 'inbox') {
        Write-Host "  [$Role] skip $label - manual request $ManualKey is pending" -ForegroundColor DarkYellow
        return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt "skipped: manual request $ManualKey is pending") }
    }
    if (-not (Test-AdapterEnabled $Ctx $Hop.adapter)) {
        return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt 'adapter disabled') }
    }
    if ($ExcludeFamilies -contains $family) {
        Write-Host "  [$Role] skip $label - same model family as the planner ($family)" -ForegroundColor DarkYellow
        return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt "skipped: same family as planner ($family)") }
    }
    # D1: the cap is per run, the idle timeout per adapter (claude prints nothing until it ends).
    $idle = [int](Get-CfgValue $Ctx 'defaults' 'idleTimeoutSec' 300)
    $adSpec = $Ctx.Config.adapters.([string]$Hop.adapter)
    if ($adSpec -and $null -ne $adSpec.PSObject.Properties['idleTimeoutSec']) { $idle = [int]$adSpec.idleTimeoutSec }
    $progressMeta = @{
        taskId   = $(if ($ProgressFile) { Split-Path -Leaf (Split-Path -Parent $ProgressFile) } else { '' })
        command  = [string]$script:OrchCommand
        role     = $EstRole
        adapter  = $Hop.adapter
        model    = $Hop.model
        phase    = $Phase
        estimate = (Get-RoleEstimate -Ctx $Ctx -Role $EstRole -Adapter $Hop.adapter -Model $Hop.model)
        pool     = $limits.Pool
        tier     = $limits.Tier
    }
    # D4/D5: a hop a human answers takes no slot; every other hop holds one for its whole life - the call
    # plus its rate-limit retries and their sleeps - and hands it back in the finally.
    $slot = $null
    if ((Get-AdapterTransport -Config $Ctx.Config -Adapter $Hop.adapter) -ne 'inbox') {
        # D2: a pool whose quota is spent is skipped without a call, until it resets.
        $exhausted = Get-PoolExhaustedUntil $Ctx $limits.Pool
        if ($exhausted -gt (Get-Date)) {
            $until = $exhausted.ToString('HH:mm')
            Write-Host "  [$Role] skip $label - pool $($limits.Pool) quota exhausted until $until" -ForegroundColor DarkYellow
            return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt "skipped: pool $($limits.Pool) quota exhausted until $until") }
        }
        $slot = Enter-PoolSlot -Ctx $Ctx -Limits $limits -Meta @{ taskId = $progressMeta.taskId; role = $EstRole; adapter = $Hop.adapter; model = $Hop.model } -ProgressFile $ProgressFile
        if ($slot.Status -eq 'busy') {
            # D10/`whenBusy next`: nothing waits, the chain moves on, and the reason is recorded.
            Write-Host "  [$Role] skip $label - pool $($limits.Pool) busy ($($slot.HeldBy))" -ForegroundColor DarkYellow
            return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt "skipped: pool $($limits.Pool) busy ($($slot.HeldBy))" $false @{ poolBusy = $true }) }
        }
        if ($slot.Status -eq 'timeout') {
            Write-Host "  [$Role] skip $label - no free slot in pool $($limits.Pool) after $($limits.QueueWaitSec) s ($($slot.HeldBy))" -ForegroundColor DarkYellow
            return [pscustomobject]@{ Status = 'skip'; Family = $family; Limits = $limits; Label = $label; Attempts = @(& $mkAttempt 'queue-timeout' $false @{ poolBusy = $true }) }
        }
    }
    $attempts = @()
    try {
        $tries = 0
        while ($true) {
            $tries++
            $before = Get-WorkspaceFingerprint $Ctx
            $started = Get-Date
            $res = Invoke-Agent -Ctx $Ctx -Adapter $Hop.adapter -Model $Hop.model -Prompt $Prompt -Cwd $Cwd -ReadOnly $ReadOnly -Auto $Auto -TimeoutSec $TimeoutSec -IdleTimeoutSec $idle -ProgressFile $ProgressFile -ProgressMeta $progressMeta -TaskId $TaskId -ManualKey $ManualKey -RepoPath $RepoPath -SessionId $SessionId -SessionTitle $SessionTitle -AvoidFamilies $ExcludeFamilies
            $after = Get-WorkspaceFingerprint $Ctx
            $allChanged = @(Compare-WorkspaceFingerprint $before $after)
            # Only the target repo is this run's business. A sibling that moved is reported, not fatal.
            $changed = $(if ($Repo) { @($allChanged | Where-Object { $_ -eq $Repo }) } else { $allChanged })
            $elsewhere = $(if ($Repo) { @($allChanged | Where-Object { $_ -ne $Repo }) } else { @() })
            if ($elsewhere.Count) { Write-Host "  [$Role] note: $($elsewhere -join ', ') changed meanwhile (not this run)" -ForegroundColor Yellow }
            $fail = Get-FailureClass $res $Ctx
            $secs = [int]((Get-Date) - $started).TotalSeconds
            $attempts += (& $mkAttempt $(if ($fail) { $fail } else { 'ok' }) ($null -eq $fail) @{ seconds = $secs; timeoutKind = $res.TimeoutKind; changedRepos = $allChanged; stderrTail = (Get-Tail $res.Stderr 800) })

            if ($ReadOnly -and $changed.Count -and (Test-ReadOnlyEnforced -Ctx $Ctx -Adapter $Hop.adapter)) {
                # The agent could not write (its tools are read-only), so the change is the owner's: testing,
                # or closing the previous plan while gate 2 ran. A note, and the hop's answer stands.
                Write-Host "  [$Role] note: $($changed -join ', ') changed during this read-only hop; $label cannot write, so it was not the agent (the answer may have read a moving tree)" -ForegroundColor Yellow
                $attempts[-1] | Add-Member -NotePropertyName ownerChanged -NotePropertyValue $true -Force
            }
            elseif ($ReadOnly -and $changed.Count) {
                return [pscustomobject]@{ Status = 'throw'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Message = "read-only role '$Role' ($label) changed the target repo: $($changed -join ', '). Stopped; inspect with git status. Attempts: $($attempts | ConvertTo-Json -Compress -Depth 4)" }
            }
            # Deferred: nothing failed, nothing moved - the command stops here (exit 3) and waits for a
            # human answer, so neither a retry nor the next hop makes sense.
            if ($fail -eq 'deferred') { return [pscustomobject]@{ Status = 'throw'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Message = "DEFERRED: $($res.Stderr.Trim())" } }
            if ($fail -eq 'manual-violation') {
                return [pscustomobject]@{ Status = 'throw'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Message = "manual hop '$Role' ($label): $($res.Stderr.Trim()). The answer was written for a different tree; re-prompt by rerunning the command. Attempts: $($attempts | ConvertTo-Json -Compress -Depth 4)" }
            }
            if ($null -eq $fail) {
                return [pscustomobject]@{ Status = 'ok'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Result = $res; SessionId = $res.SessionId }
            }
            Write-Host "  [$Role] $label failed: $fail ($secs s)" -ForegroundColor Yellow
            if (-not $ReadOnly -and $changed.Count) {
                return [pscustomobject]@{ Status = 'throw'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Message = "write role '$Role' ($label) failed ($fail) after changing $($changed -join ', '). No fallback: the next agent would start on a half-edited tree. Inspect git diff, reset or finish by hand. Attempts: $($attempts | ConvertTo-Json -Compress -Depth 4)" }
            }
            # D2: out of quota - mark the whole pool exhausted, no retry, the chain moves on.
            if ($fail -eq 'quota-exhausted') {
                $exhaustedSec = [double](Get-CfgValue $Ctx 'quota' 'exhaustedSec' 1800)
                Set-PoolExhausted -Ctx $Ctx -Pool $limits.Pool -Seconds $exhaustedSec
                Write-Host "  [$Role] $label out of quota - pool $($limits.Pool) exhausted for $([int]$exhaustedSec) s; next hop" -ForegroundColor DarkYellow
                return [pscustomobject]@{ Status = 'fail'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Reason = $fail }
            }
            # D9: RateLimitRetries more attempts on this hop, each waiting twice as long as the last.
            # D8: the pool is marked cooling down for exactly that wait, so the other runs stop knocking.
            if ($fail -eq 'rate-limit' -and $tries -le $limits.RateLimitRetries) {
                $backoff = Get-PoolScaledSeconds ([double]$limits.RateLimitBackoffSec * [Math]::Pow(2, $tries - 1))
                Set-PoolCooldown -Ctx $Ctx -Pool $limits.Pool -Seconds $backoff
                Write-Host "  [$Role] retrying $label in $([Math]::Round($backoff)) s (rate limit, attempt $($tries + 1) of $($limits.RateLimitRetries + 1))" -ForegroundColor DarkYellow
                Start-PoolSleep $backoff
                continue
            }
            return [pscustomobject]@{ Status = 'fail'; Family = $family; Limits = $limits; Label = $label; Attempts = $attempts; Reason = $fail }
        }
    }
    finally { Exit-PoolSlot $slot.Handle }
}

function Invoke-Role {
    <#
      Walks the role's fallback chain; the first hop that exits 0 with output wins.
      - ExcludeFamilies: hops of these model families are skipped (gate-2 independence).
      - Repo: the run's target repo. Only a change there is a violation; a change in another
        configured repo is one note and never throws (P2: a Ui/ commit used to cancel an Agent/ read-back).
      - ProgressFile: runs/<id>/progress.json, rewritten while the hop runs (D8). Optional.
      - ManualKey: what a `manual` hop's answer is valid for (D7). No retry, no fallback when it defers.
      - Continue: hashtable Adapter,Model,SessionId,Prompt to continue an earlier session (D6) before the chain.
      - Retry: clear a pending manual request (D9) and walk the chain normally instead of skipping to it.
    #>
    param($Ctx, [string]$Role, [string]$Prompt, [string]$Cwd, [int]$TimeoutSec, [string[]]$ExcludeFamilies = @(), [switch]$ReadOnly, [string]$Repo, [string]$ProgressFile = '', [string]$Phase = '', [string]$TaskId = '', [string]$ManualKey = '', [hashtable]$Continue = $null, [switch]$Retry)
    $spec = $Ctx.Config.roles.$Role
    if ($null -eq $spec) { throw "Unknown role: $Role" }
    # -ReadOnly runs a write role's chain without write access (the implementer read-back).
    $readOnly = [bool]$spec.readOnly -or $ReadOnly
    $auto = if ($ReadOnly) { $false } else { [bool]$spec.auto }
    $estRole = if ($ReadOnly -and $Role -eq 'implementer') { 'readback' } else { $Role }
    $repoPath = if ($Repo) { Get-RepoPath $Ctx $Repo } else { '' }
    # D5: the title a new session is created under; only session-capable hops use it.
    $sessionTitle = "orch $TaskId $ManualKey " + (Get-Date).ToString('yyyyMMddHHmmss')
    $attempts = @()
    # D9: a pending manual request is answered, not overtaken. Without -Retry every non-inbox hop is
    # skipped so the manual hop consumes the answer (or defers again); -Retry deletes the request files
    # (never the answer) and walks the chain normally.
    $manualPending = $false
    if ($ManualKey -and $TaskId) {
        $inboxDir = Join-Path (Get-TaskDir $Ctx $TaskId) 'inbox'
        if (Test-Path -LiteralPath (Join-Path $inboxDir "$ManualKey.md")) {
            if ($Retry) {
                foreach ($ext in @('.md', '.fingerprint', '.workspace.json')) {
                    $f = Join-Path $inboxDir "$ManualKey$ext"
                    if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force }
                }
            }
            else { $manualPending = $true }
        }
    }
    # D6: continue an earlier session of this role before walking the chain. The matching hop runs once
    # with the short follow-up prompt; a failure that did not change the repo falls through to the chain.
    if ($Continue -and $Continue.SessionId -and -not $manualPending) {
        $hop = @($spec.chain) | Where-Object { $_.adapter -eq $Continue.Adapter -and $_.model -eq $Continue.Model } | Select-Object -First 1
        if ($hop) {
            $family = Get-HopFamily $hop
            $limits = Get-HopLimits $Ctx $hop
            $eligible = (Test-AdapterEnabled $Ctx $hop.adapter) -and ($ExcludeFamilies -notcontains $family) -and ((Get-PoolExhaustedUntil $Ctx $limits.Pool) -le (Get-Date)) -and (Test-HopSessions $Ctx $hop)
            if ($eligible) {
                $o = Invoke-HopOnce -Ctx $Ctx -Role $Role -Hop $hop -Prompt $Continue.Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -ExcludeFamilies $ExcludeFamilies -ReadOnly $readOnly -Auto $auto -EstRole $estRole -Repo $Repo -ProgressFile $ProgressFile -Phase $Phase -TaskId $TaskId -ManualKey $ManualKey -RepoPath $repoPath -SessionId $Continue.SessionId -SessionTitle '' -ReasonPrefix 'continue: '
                $attempts += $o.Attempts
                if ($o.Status -eq 'throw') { throw $o.Message }
                if ($o.Status -eq 'ok') {
                    return [pscustomobject]@{ Adapter = $hop.adapter; Model = $hop.model; Family = $family; Result = $o.Result; Attempts = $attempts; SessionId = $Continue.SessionId }
                }
                Write-Host "  [$Role] continuation of session $($Continue.SessionId) did not succeed ($($o.Reason)); falling back to the chain" -ForegroundColor DarkYellow
            }
        }
    }
    foreach ($hop in $spec.chain) {
        $stitle = ''
        if (Test-HopSessions $Ctx $hop) { $stitle = $sessionTitle }
        $o = Invoke-HopOnce -Ctx $Ctx -Role $Role -Hop $hop -Prompt $Prompt -Cwd $Cwd -TimeoutSec $TimeoutSec -ExcludeFamilies $ExcludeFamilies -ReadOnly $readOnly -Auto $auto -EstRole $estRole -Repo $Repo -ProgressFile $ProgressFile -Phase $Phase -TaskId $TaskId -ManualKey $ManualKey -RepoPath $repoPath -SessionId '' -SessionTitle $stitle -ReasonPrefix '' -ManualPending:$manualPending
        $attempts += $o.Attempts
        if ($o.Status -eq 'throw') { throw $o.Message }
        if ($o.Status -eq 'ok') {
            return [pscustomobject]@{ Adapter = $hop.adapter; Model = $hop.model; Family = $o.Family; Result = $o.Result; Attempts = $attempts; SessionId = $o.Result.SessionId }
        }
        # skip/fail: the attempt is recorded; move to the next hop.
    }
    throw "All hops failed for role '$Role': $($attempts | ConvertTo-Json -Compress -Depth 4)"
}

function Get-Tail {
    param([string]$Text, [int]$Max)
    if (-not $Text) { return '' }
    if ($Text.Length -le $Max) { return $Text }
    return $Text.Substring($Text.Length - $Max)
}

function Get-RoleProtocol {
    # Orchestrator-mode rules appended to every prompt. They override the stage prompt where the
    # stage prompt was written for an interactive chat (asking for approval, branching, the close).
    param([string]$Role, [string]$Branch)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('## Orchestrator mode (overrides the stage prompt where they differ)')
    [void]$sb.AppendLine('You run headless. Nobody reads your questions until you finish, and approvals happen outside your run.')
    [void]$sb.AppendLine('You are a worker called BY the orchestrator: never run its run.ps1 (orchestrator/run.ps1, or .orchestra/run.ps1 in the new layout) and ignore any "orchestrate" skill or conductor instructions you may find.')
    switch ($Role) {
        'readback' {
            [void]$sb.AppendLine('- READ-BACK ONLY. You will implement this plan later, but NOT now: do not edit any file, do not run commands, do not create branches.')
            [void]$sb.AppendLine('- Read the plan and the files it names. Answer one question: could you implement every phase exactly as written WITHOUT making a single decision of your own?')
            [void]$sb.AppendLine('- List every place where you would have to guess, as: FINDING [blocker|should|nit] <plan section>: <what is ambiguous or contradicts the code> (<file you checked>) — FIX: <what the plan must say so you need not guess>')
            [void]$sb.AppendLine('- blocker = you cannot implement it as written; should = you could, but would be guessing; nit = cosmetic.')
            [void]$sb.AppendLine('- Before the plan, open the repo''s .ai/README.md and, for each area the plan touches, the doc it names as the owner (layers and feature folders, screens and design, tests) - cited by the plan or not. A step that contradicts one of them, or a screen whose layout the plan leaves to you, is a FINDING.')
            $verdicts = 'PASS (no guessing needed; nits allowed) | FINDINGS'
        }
        'planner' {
            [void]$sb.AppendLine('- You cannot write files. Put the WHOLE plan in your reply (it becomes the plan artifact).')
            [void]$sb.AppendLine('- Do not ask for approval or for a review; the orchestrator runs the review and asks the user.')
            [void]$sb.AppendLine('- Name the branch in the Baseline step as `<person>/<topic>` in backticks; the orchestrator creates it.')
            [void]$sb.AppendLine('- When revising, output the complete revised plan, not a diff, and add a short "Changes from previous version" list at the end.')
            $verdicts = 'PASS (plan complete, no open decisions) | NEED_INPUT: <a question only the user can answer> | BLOCKED: <reason>'
        }
        'plan-reviewer' {
            [void]$sb.AppendLine('- Write each finding on its own line as: FINDING [blocker|should|nit] <plan section>: <finding> (<doc that proves it>) — FIX: <the smallest change to the plan that resolves it, one line>')
            [void]$sb.AppendLine('- FIX is a suggestion for the plan''s author, who decides. You still do not edit the plan.')
            [void]$sb.AppendLine('- Check the plan against the repo''s structure, not only against itself: open .ai/README.md and read the owning doc for every area the plan touches, cited or not (your role file). A plan that contradicts or skips one is a FINDING.')
            [void]$sb.AppendLine('- PASS means no blocker and no should-fix finding remains; nits alone still PASS.')
            $verdicts = 'PASS | FINDINGS | BLOCKED: <reason>'
        }
        'implementer' {
            [void]$sb.AppendLine("- The orchestrator already created and switched to branch ``$Branch``. Do not create, switch or delete branches.")
            [void]$sb.AppendLine('- Do not ask the user to approve the next phase; just report this phase and stop.')
            $verdicts = 'PASS (phase done, its gate commands green) | BLOCKED: <reason> | NEED_INPUT: <question>'
        }
        'verifier' {
            [void]$sb.AppendLine('- Do not ask the close questions and never offer to commit, merge or delete; the human closes outside your run.')
            [void]$sb.AppendLine('- The gate command results above are authoritative; a non-zero exit is a FAIL.')
            $verdicts = 'PASS | FAIL: <the failing items>'
        }
        default { $verdicts = 'PASS | BLOCKED: <reason>' }
    }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('The LAST line of your reply must be exactly one verdict line:')
    [void]$sb.AppendLine("  VERDICT: $verdicts")
    $sb.ToString()
}

function Resolve-WorkflowPath {
    # A path from orchestrator.json's workflow block; "{repo}" becomes the target repo folder name.
    param($Ctx, [string]$Template, [string]$Repo)
    if (-not $Template) { return $null }
    return (Join-Path $Ctx.Root ($Template -replace '\{repo\}', $Repo))
}

function Get-RoleSection {
    # Some repos (Ui) keep the four roles as "## <role>" sections of .ai/agents/README.md instead of
    # one file per role. Returns that section's text, or '' when the role is not there.
    param([string]$Path, [string]$Role)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $lines = @(Get-Content -LiteralPath $Path -Encoding UTF8)
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match ('^##\s+' + [regex]::Escape($Role) + '\s*$')) { $start = $i; break }
    }
    if ($start -lt 0) { return '' }
    $end = $lines.Count
    for ($j = $start + 1; $j -lt $lines.Count; $j++) {
        if ($lines[$j] -match '^##\s+') { $end = $j; break }
    }
    return (($lines[$start..($end - 1)]) -join "`n")
}

function Get-RoleText {
    param($Ctx, [string]$Role, [string]$Repo)
    $dir = Resolve-WorkflowPath $Ctx $Ctx.Config.workflow.rolesPath $Repo
    if ($dir) {
        $file = Join-Path $dir "$Role.md"
        if (Test-Path -LiteralPath $file) { return (Get-Content -LiteralPath $file -Raw -Encoding UTF8) }
    }
    $index = Resolve-WorkflowPath $Ctx $Ctx.Config.workflow.rolesIndex $Repo
    if ($index) { return (Get-RoleSection -Path $index -Role $Role) }
    return ''
}

function New-RolePrompt {
    param($Ctx, [string]$Role, [string]$Repo, [string]$Task, [string]$Extra, [string]$Branch, [string]$Protocol)
    if (-not $Protocol) { $Protocol = $Role }
    $spec = $Ctx.Config.roles.$Role
    $gatesPath  = Resolve-WorkflowPath $Ctx $Ctx.Config.workflow.gatesPath $Repo
    $promptPath = Resolve-WorkflowPath $Ctx (Join-Path $Ctx.Config.workflow.promptsPath $spec.promptFile) $Repo
    $gatesText  = if ($gatesPath  -and (Test-Path -LiteralPath $gatesPath))  { Get-Content -LiteralPath $gatesPath  -Raw -Encoding UTF8 } else { '' }
    $roleText   = Get-RoleText -Ctx $Ctx -Role $Role -Repo $Repo
    $promptText = if ($promptPath -and (Test-Path -LiteralPath $promptPath)) { Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8 } else { '' }
    $repoPath   = Join-Path $Ctx.Root $Ctx.Config.repos.$Repo
    $cwd        = Get-AgentCwd $Ctx $Repo

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("# ORCHESTRATOR CALL - role: $Role (gate $($spec.gate))")
    [void]$sb.AppendLine("Repo: $Repo")
    [void]$sb.AppendLine("Repo root on disk: $repoPath")
    [void]$sb.AppendLine("Working directory (your cwd): $cwd")
    if ($Ctx.Config.defaults.agentCwd -eq 'repo') {
        $siblings = @($Ctx.Config.repos.PSObject.Properties.Name | Where-Object { $_ -ne $Repo } | ForEach-Object { "../$_" }) -join ', '
        [void]$sb.AppendLine("Workspace mode: your working directory IS the target repo root. You may READ the sibling repos beside it ($siblings) - the API contract and the other clients live there - but only WRITE inside $Repo. The orchestrator fingerprints every repo before and after your run and rejects the run if anything else changed. Run git in this directory.")
    }
    else {
        [void]$sb.AppendLine("Workspace mode: your working directory is the workspace root; the target repo above is a subfolder of it. You may READ any sibling project in the workspace, but only WRITE inside the target repo. The orchestrator fingerprints every repo before and after your run and rejects the run if anything else changed. Run git against the target repo with: git -C `"$repoPath`" ... (or cd into it first).")
    }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## Repo state (read-only, gathered by the orchestrator just now)")
    [void]$sb.AppendLine((Get-RepoState $repoPath))
    [void]$sb.AppendLine("You may be denied shell access; the block above is authoritative repo state provided for you.")
    $docs = @()
    foreach ($t in @($Ctx.Config.workflow.agentsDoc, $Ctx.Config.workflow.indexDoc, $Ctx.Config.workflow.planTemplate)) {
        $p = Resolve-WorkflowPath $Ctx $t $Repo
        if ($p -and (Test-Path -LiteralPath $p)) { $docs += $p }
    }
    if ($Repo -ne 'Web') {
        # The frontend's contract doc, relative to the workspace root: config `contractDoc` (default Project B's).
        $contractRel = 'Web\.ai\architecture\api-surface-v3.md'
        $cd = $Ctx.Config.PSObject.Properties['contractDoc']
        if ($cd -and $cd.Value) { $contractRel = [string]$cd.Value }
        $contract = Join-Path $Ctx.Root $contractRel
        if (Test-Path -LiteralPath $contract) { $docs += $contract }
    }
    if ($docs.Count) {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("## Repo docs (open the ones your role needs; they own the rules the workflow doc below points at)")
        foreach ($d in $docs) { [void]$sb.AppendLine("- $d") }
    }
    [void]$sb.AppendLine()
    if ($Role -eq 'planner') {
        # Plan 0006 D6: only `preview` is adjustable, and the planner is the one who acts on it.
        $preview = $true
        $w = $Ctx.Config.PSObject.Properties['workflow']
        if ($w -and $w.Value) {
            $steps = $w.Value.PSObject.Properties['steps']
            if ($steps -and $steps.Value) {
                $one = $steps.Value.PSObject.Properties['preview']
                if ($one -and $one.Value -is [pscustomobject]) {
                    $enabled = $one.Value.PSObject.Properties['enabled']
                    if ($enabled) { $preview = [bool]$enabled.Value }
                }
            }
        }
        $line = if ($preview) { 'Workflow: the preview step is on - write the HTML preview before the plan.' } else { 'Workflow: the preview step is off - skip the HTML preview and write the plan.' }
        [void]$sb.AppendLine($line)
        [void]$sb.AppendLine()
    }
    [void]$sb.AppendLine("## Your task")
    [void]$sb.AppendLine($Task)
    if ($Extra) {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("## Input from the previous step")
        [void]$sb.AppendLine($Extra)
    }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## Shared workflow (authoritative - the four gates in this repo)")
    [void]$sb.AppendLine($gatesText)
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## Role definition")
    [void]$sb.AppendLine($roleText)
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## Stage prompt")
    [void]$sb.AppendLine($promptText)
    [void]$sb.AppendLine()
    [void]$sb.AppendLine((Get-RoleProtocol -Role $Protocol -Branch $Branch))
    $sb.ToString()
}

function Get-OpenCodeModels {
    param([int]$TimeoutSec = 60)
    $res = Invoke-External -Exe 'opencode' -ArgList @('models') -TimeoutSec $TimeoutSec
    if ($res.ExitCode -ne 0) { return $null }
    return @($res.Stdout -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

function Test-ModelNames {
    # Free preflight: checks every opencode hop against `opencode models` (no LLM call).
    # Other adapters cannot list models cheaply and are reported as 'unchecked'.
    param($Ctx)
    $oc = $null; $ocLoaded = $false
    $rows = @()
    foreach ($role in $Ctx.Config.roles.PSObject.Properties.Name) {
        foreach ($hop in $Ctx.Config.roles.$role.chain) {
            $state = 'unchecked'
            if (-not (Test-AdapterEnabled $Ctx $hop.adapter)) { $state = 'adapter disabled' }
            elseif ($hop.adapter -eq 'opencode') {
                if (-not $ocLoaded) { $oc = Get-OpenCodeModels; $ocLoaded = $true }
                if ($null -eq $oc) { $state = 'opencode models failed' }
                elseif ($oc -contains $hop.model) { $state = 'ok' }
                else { $state = 'UNKNOWN MODEL' }
            }
            $rows += [pscustomobject]@{ Role = $role; Adapter = $hop.adapter; Model = $hop.model; Family = (Get-HopFamily $hop); Name = $state }
        }
    }
    return $rows
}

function Test-AgentHealth {
    param($Ctx, [int]$TimeoutSec)
    $results = @()
    foreach ($role in $Ctx.Config.roles.PSObject.Properties.Name) {
        $spec = $Ctx.Config.roles.$role
        $head = $spec.chain[0]
        $ok = $false
        $headOk = $false
        $served = ''
        $tried = @()
        $i = 0
        foreach ($hop in $spec.chain) {
            $i++
            if (-not (Test-AdapterEnabled $Ctx $hop.adapter)) { $tried += "$($hop.model):disabled"; continue }
            if ((Get-AdapterTransport -Config $Ctx.Config -Adapter $hop.adapter) -eq 'inbox') {
                # A human answers this hop; there is nothing to ping.
                $ok = $true
                if ($i -eq 1) { $headOk = $true }
                $served = "$($hop.adapter):$($hop.model)"
                $tried += "$($hop.model):manual (not pinged)"
                break
            }
            # D11: health never waits. A busy pool is reported as busy for this adapter instead of being
            # pinged; a free one is taken for the ping and handed back right after it.
            $slot = Enter-PoolSlot -Ctx $Ctx -Limits (Get-HopLimits $Ctx $hop) -Meta @{ taskId = 'health'; role = $role; adapter = $hop.adapter; model = $hop.model } -NoWait
            if ($slot.Status -eq 'busy' -or $slot.Status -eq 'timeout') {
                $tried += "$($hop.model):busy ($($slot.HeldBy))"
                continue
            }
            try {
                $res = Invoke-Agent -Ctx $Ctx -Adapter $hop.adapter -Model $hop.model -Prompt $Ctx.Config.preflight.pingPrompt -Cwd $Ctx.Root -ReadOnly $true -Auto $false -TimeoutSec $TimeoutSec
            }
            finally { Exit-PoolSlot $slot.Handle }
            if (($res.ExitCode -eq 0) -and ($res.Stdout -match 'PONG')) {
                $ok = $true
                if ($i -eq 1) { $headOk = $true }
                $served = "$($hop.adapter):$($hop.model)"
                $tried += "$($hop.model):PONG"
                break
            }
            $why = Get-FailureClass $res
            if (-not $why) { $why = 'no PONG' }
            $tried += "$($hop.model):$why"
        }
        $results += [pscustomobject]@{
            Role     = $role
            Head     = "$($head.adapter):$($head.model)"
            HeadOk   = $headOk
            ServedBy = $served
            Ok       = $ok
            Detail   = ($tried -join '  |  ')
        }
    }
    return $results
}
