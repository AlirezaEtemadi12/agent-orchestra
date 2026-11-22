function Get-TaskDir {
    param($Ctx, [string]$TaskId)
    Join-Path $Ctx.RunsDir $TaskId
}

function New-TaskRun {
    param($Ctx, [string]$TaskId, [string]$Repo, [string]$Task)
    $dir = Get-TaskDir $Ctx $TaskId
    foreach ($sub in @('', 'inbox', 'outbox', 'artifacts', 'transcript')) {
        $p = if ($sub) { Join-Path $dir $sub } else { $dir }
        New-Item -ItemType Directory -Force -Path $p | Out-Null
    }
    $state = [ordered]@{
        id        = $TaskId
        repo      = $Repo
        task      = $Task
        status    = 'Planning'
        createdAt = (Get-Date).ToString('s')
        updatedAt = (Get-Date).ToString('s')
        round     = 0
        plan      = $null
        review    = $null
        approved  = $null
        branch    = $null
        baseSha   = $null
        implement = $null
        phases    = @()
        verify    = $null
        sessions  = $null
        history   = @()
    }
    Save-TaskState -Ctx $Ctx -TaskId $TaskId -State ([pscustomobject]$state)
    # Plan 0006 D9: the run records the config it started with, once. Later commands never rewrite it.
    # `profile`/`profileVersion` are null for a project that uses no profile (D7).
    $payload = [ordered]@{
        profile        = $(if ($Ctx.PSObject.Properties['ProfileId']) { $Ctx.ProfileId } else { $null })
        profileVersion = $(if ($Ctx.PSObject.Properties['ProfileVersion']) { $Ctx.ProfileVersion } else { $null })
        writtenAt      = (Get-Date).ToString('s')
        config         = $Ctx.Config
    }
    $effective = Join-Path $dir 'config.effective.json'
    ([pscustomobject]$payload | ConvertTo-Json -Depth 30) | Set-Content -LiteralPath $effective -Encoding UTF8
    return (Get-TaskState -Ctx $Ctx -TaskId $TaskId)
}

function Get-TaskProgressFile {
    # The one file `run.ps1 watch` reads: rewritten by every hop while it runs (D8).
    param($Ctx, [string]$TaskId)
    Join-Path (Get-TaskDir $Ctx $TaskId) 'progress.json'
}

function Get-TaskState {
    param($Ctx, [string]$TaskId)
    $f = Join-Path (Get-TaskDir $Ctx $TaskId) 'task.json'
    if (-not (Test-Path -LiteralPath $f)) { throw "no run '$TaskId' (expected $f)" }
    return (Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Save-TaskState {
    param($Ctx, [string]$TaskId, $State)
    $dir = Get-TaskDir $Ctx $TaskId
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $file = Join-Path $dir 'task.json'
    # Plan 0004 §6.1: the status event IS the change, so the old word is read before the write replaces it.
    # A brand-new run has no old word: `run.created` is its first event, not a change.
    $before = ''
    if (Test-Path -LiteralPath $file) {
        try { $before = [string](Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json).status } catch { $before = '' }
    }
    if ($State.PSObject.Properties['updatedAt']) { $State.updatedAt = (Get-Date).ToString('s') }
    ($State | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $file -Encoding UTF8
    if ($before -and $before -ne [string]$State.status -and (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue)) {
        Send-OrchestraEvent -Ctx $Ctx -State $State -Kind 'status' -Data ([ordered]@{ from = $before; to = [string]$State.status })
    }
}

function Test-ManualKeyStale {
    # A request the run has moved past: a plan-reviewer/readback key for an older version (or for a run
    # already approved), or a verifier key that is not the newest one while a verify is still due. The
    # file stays on disk as history; nobody waits for its answer any more, so queue and -ThenResume skip it.
    param([string]$Key, $State, [string]$Inbox)
    if (-not $State) { return $false }
    if ($Key -match '^(plan-reviewer|readback)\.v(\d+)$') {
        # Older rounds only: `revise` writes v<round+1>'s request before it saves the new round to task.json,
        # so a request one round ahead is the version in flight, not history.
        if ([int]$Matches[2] -lt [int]$State.round) { return $true }
        return (@('Planning', 'NeedsInput', 'AwaitingApproval') -notcontains [string]$State.status)
    }
    if ($Key -like 'verifier.*') {
        if (@('Implemented', 'VerifyFailed') -notcontains [string]$State.status) { return $true }
        $newest = Get-ChildItem -LiteralPath $Inbox -Filter 'verifier.*.md' -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        return [bool]($newest -and $newest.BaseName -ne $Key)
    }
    return $false
}

function Get-PendingManual {
    # Every `manual` request on disk that has no usable answer yet. Filesystem only: no model, no lock,
    # nothing is claimed or changed. A request is a file in inbox/; its answer is answers/<same name>.
    param($Ctx, [string]$TaskId)
    if (-not (Test-Path -LiteralPath $Ctx.RunsDir)) { return @() }
    $dirs = if ($TaskId) { @(Join-Path $Ctx.RunsDir $TaskId) } else {
        @(Get-ChildItem -LiteralPath $Ctx.RunsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
    }
    $rows = @()
    foreach ($dir in $dirs) {
        $inbox = Join-Path $dir 'inbox'
        if (-not (Test-Path -LiteralPath $inbox)) { continue }
        $id = Split-Path -Leaf $dir
        $repo = ''
        $status = ''
        $st = $null
        $taskJson = Join-Path $dir 'task.json'
        if (Test-Path -LiteralPath $taskJson) {
            try {
                $st = Get-Content -LiteralPath $taskJson -Raw -Encoding UTF8 | ConvertFrom-Json
                $repo = $st.repo
                $status = $st.status
            }
            catch { }
        }
        $answers = Join-Path $dir 'answers'
        foreach ($f in @(Get-ChildItem -LiteralPath $inbox -Filter '*.md' -File -ErrorAction SilentlyContinue)) {
            $key = $f.BaseName
            if (Test-ManualKeyStale -Key $key -State $st -Inbox $inbox) { continue }
            $answerFile = Join-Path $answers "$key.md"
            $answered = $false
            if (Test-Path -LiteralPath $answerFile) {
                $text = Get-Content -LiteralPath $answerFile -Raw -Encoding UTF8
                $answered = [bool]($text -and $text.Trim())
            }
            if ($answered) { continue }
            $rows += [pscustomobject]@{
                TaskId      = $id
                Key         = $key
                Repo        = $repo
                Status      = $status
                RequestedAt = $f.LastWriteTime
                WaitMin     = [int][math]::Floor(((Get-Date) - $f.LastWriteTime).TotalMinutes)
                Request     = $f.FullName
                Answer      = $answerFile
            }
        }
    }
    return @($rows | Sort-Object RequestedAt)
}

function Save-Outbox {
    param($Ctx, [string]$TaskId, [string]$Role, [string]$Name, [string]$Content)
    $dir = Join-Path (Join-Path (Get-TaskDir $Ctx $TaskId) 'outbox') $Role
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $f = Join-Path $dir ($Name + '.md')
    Set-Content -LiteralPath $f -Value $Content -Encoding UTF8
    return $f
}

function Save-Artifact {
    param($Ctx, [string]$TaskId, [string]$Name, [string]$Content)
    $dir = Join-Path (Get-TaskDir $Ctx $TaskId) 'artifacts'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $f = Join-Path $dir ($Name + '.md')
    Set-Content -LiteralPath $f -Value $Content -Encoding UTF8
    return $f
}

function Save-Transcript {
    param($Ctx, [string]$TaskId, [string]$Name, $Data)
    $dir = Join-Path (Get-TaskDir $Ctx $TaskId) 'transcript'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $f = Join-Path $dir ($Name + '.json')
    ($Data | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $f -Encoding UTF8
    return $f
}

function Get-Verdict {
    # The LAST "VERDICT: X" line in a reply (markdown decoration tolerated). $null when missing:
    # callers treat a missing verdict as not-passed, never as PASS.
    param([string]$Text)
    if (-not $Text) { return $null }
    $rx = [regex]'(?im)^[\s>*_`#-]*VERDICT\s*[:=]\s*[*_`]*\s*(PASS|FINDINGS|BLOCKED|FAIL|NEED_INPUT)\b[*_`]*\s*:?\s*([^\r\n]*)'
    $all = $rx.Matches($Text)
    if ($all.Count -eq 0) { return $null }
    $last = $all[$all.Count - 1]
    [pscustomobject]@{ Kind = $last.Groups[1].Value.ToUpper(); Message = ($last.Groups[2].Value.Trim() -replace '[*_`]+$', '').Trim() }
}

function Get-Findings {
    # An optional list marker is allowed before the decoration ("1. `FINDING [blocker] ...`"):
    # reviewers number their findings, and a numbered line used to count as no finding at all.
    # The level is also accepted without brackets ("FINDING should §9 ..."): T-AGENT-004's v2 read-back
    # wrote every finding that way and the summary counted 0/0/0.
    param([string]$Text)
    # A finding a reviewer restates as fixed ("FINDING [should] D8 — ...: RESOLVED.") is not a finding:
    # T-AGENT-027's v5 review listed the four v4 findings that way under PASS and the summary recorded
    # FINDINGS (2 should, 2 nit). "NOT RESOLVED" / "UNRESOLVED" / "PARTIALLY RESOLVED" still count.
    $rx = [regex]'(?im)^\s*(?:\d+[.)]\s+|[-*+]\s+)?[\s>*_`#-]*FINDING\s*(?:\[(blocker|should|nit)\]|(blocker|should|nit)\b)\s*([^\r\n]*)'
    $resolved = [regex]'(?i)(?<![\w-])RESOLVED\b'
    $notResolved = [regex]'(?i)\b(not|partially|partly)\s+resolved\b'
    @($rx.Matches($Text) | Where-Object {
            $t = $_.Groups[3].Value
            -not ($resolved.IsMatch($t) -and -not $notResolved.IsMatch($t))
        } | ForEach-Object {
            $level = if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value }
            [pscustomobject]@{ Level = $level.ToLower(); Text = $_.Groups[3].Value.Trim() }
        })
}

function Set-Prop {
    # Sets a property on a PSCustomObject read back from JSON, adding it when an older task.json lacks it.
    param($Obj, [string]$Name, $Value)
    if ($Obj.PSObject.Properties[$Name]) { $Obj.$Name = $Value }
    else { $Obj | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
}

function Add-History {
    # -Ctx is optional so the offline suite can keep calling this without a project (plan 0004 §6.6).
    param($State, [string]$Event, $Data, $Ctx = $null)
    $h = @($State.history | Where-Object { $_ }) + [pscustomobject]@{ at = (Get-Date).ToString('s'); event = $Event; data = $Data }
    Set-Prop $State 'history' $h
    # After the state is updated, so the event carries the counts history just recorded. Guarded: this file
    # is dot-sourced on its own by the offline suite.
    if (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue) {
        Send-OrchestraEvent -Ctx $Ctx -State $State -Kind 'history' -Data (Get-HistoryEventData -State $State -Event $Event -Data $Data)
    }
}

function Enter-TaskLock {
    # One command per run at a time. A crash leaves the lock behind; the message says how to clear it.
    param($Ctx, [string]$TaskId)
    $dir = Get-TaskDir $Ctx $TaskId
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $f = Join-Path $dir '.lock'
    try { $fs = [System.IO.File]::Open($f, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None) }
    catch {
        $who = try { Get-Content -LiteralPath $f -Raw } catch { '?' }
        throw "run '$TaskId' is locked by another command ($who). If none is running, delete $f"
    }
    $b = [System.Text.Encoding]::UTF8.GetBytes("pid $PID since $((Get-Date).ToString('s'))")
    $fs.Write($b, 0, $b.Length); $fs.Close()
    return $f
}

function Exit-TaskLock {
    param([string]$LockFile)
    if ($LockFile -and (Test-Path -LiteralPath $LockFile)) { Remove-Item -LiteralPath $LockFile -Force }
}
