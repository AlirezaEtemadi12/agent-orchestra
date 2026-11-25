# lib/report.ps1 - the dashboard's event reporter (plan 0004 §6.6).
#
# Fire-and-forget, by ADR 0002: a short timeout, no retry, one warning line per process, and nothing here
# changes what a run does or returns. With the dashboard down the run is identical and only the events of
# that time are lost - the ledger still has every run.
#
# Nothing in this file may throw: every hook calls it, and every hook sits on a path a run takes.

$script:ReportSeq = 0
$script:ReportWarned = $false
$script:ReportProgressAt = @{}

# Set once by run.ps1 after the config loads, so a hook that has no $Ctx (the progress writer, a slot that
# is being released) still finds the project. Never set by a test that only dot-sources the libs.
$script:ReportCtx = $null

function Get-ReportSetting {
    # A key inside the `report` block, else the default. Missing block, missing key and null all mean the
    # default: a project that never heard of `report` reports nothing.
    param($Ctx, [string]$Name, $Default)
    if (-not $Ctx) { return $Default }
    $config = $Ctx.Config
    if (-not $config -or -not $config.PSObject.Properties['report']) { return $Default }
    $block = $config.report
    if (-not $block) { return $Default }
    $p = $block.PSObject.Properties[$Name]
    if (-not $p -or $null -eq $p.Value) { return $Default }
    return $p.Value
}

function Test-ReportEnabled {
    # Off unless the block says enabled AND names a url: half a setting must not turn reporting on.
    param($Ctx)
    if (-not $Ctx) { return $false }
    if (-not [bool](Get-ReportSetting $Ctx 'enabled' $false)) { return $false }
    $url = [string](Get-ReportSetting $Ctx 'url' '')
    return [bool]$url
}

function Get-HistoryEventData {
    # Plan 0004 §6.1: the history event carries the same counts task.json records at that moment, so the
    # dashboard can show per-round blockers/should/nits without a file.
    param($State, [string]$Event, $Data)
    $d = [ordered]@{ event = $Event }
    if ($Data) {
        foreach ($k in @('round', 'phase', 'model', 'verdict')) {
            if ($Data.ContainsKey($k) -and $null -ne $Data[$k]) { $d[$k] = $Data[$k] }
        }
    }
    $stage = switch ($Event) {
        'review' { $State.review }
        'readback' { $State.readback }
        'implement' { $State.implement }
        'verify' { $State.verify }
        default { $null }
    }
    if ($stage) {
        foreach ($k in @('blockers', 'should', 'nits', 'changed', 'outsidePlan', 'violations', 'gates', 'modelVerdict')) {
            $p = $stage.PSObject.Properties[$k]
            if ($p -and $null -ne $p.Value) { $d[$k] = $p.Value }
        }
    }
    return $d
}

function Get-ReportToken {
    # Plan 0009 D10: the environment variable named by tokenEnv (as before), else `reportToken` in the
    # project's git-ignored orchestrator.local.json beside the config. Neither means the request goes
    # without Authorization and the dashboard answers 401 - the run is unaffected.
    param($Ctx)
    $name = [string](Get-ReportSetting $Ctx 'tokenEnv' 'ORCHESTRA_REPORT_TOKEN')
    if ($name) {
        $value = [string][System.Environment]::GetEnvironmentVariable($name)
        if ($value) { return $value }
    }
    if (-not $Ctx -or -not $Ctx.ConfigPath) { return '' }
    $local = Join-Path (Split-Path -Parent $Ctx.ConfigPath) 'orchestrator.local.json'
    try {
        if (-not (Test-Path -LiteralPath $local)) { return '' }
        $doc = Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json
        $p = $doc.PSObject.Properties['reportToken']
        if ($p -and $p.Value) { return [string]$p.Value }
    }
    catch { }
    return ''
}

function Send-OrchestraEvent {
    <#
      One event to the dashboard (plan 0004 §6.1). Never throws, never retries, returns nothing.
        -Ctx       the project context; $script:ReportCtx when omitted.
        -Kind      one of the kinds in the plan; an unknown kind is refused by the API, not here.
        -Data      the payload as a hashtable/[ordered] hashtable.
        -State     supplies taskId and branch (task.json).
        -TaskId    for a hook that has no state (progress, pool slot).
      `progress` is throttled per task to report.progressEverySec, default 10 (D5): a hop that prints
      constantly must not become a request per line.
    #>
    param($Ctx, [string]$Kind, $Data, $State = $null, [string]$TaskId = '', [string]$Branch = '')

    try {
        # D4: no retry, and one warning line per run. A dashboard that is already down stays down for this
        # process, so a second event costs nothing instead of another timeout.
        if ($script:ReportWarned) { return }
        if (-not $Ctx) { $Ctx = $script:ReportCtx }
        if (-not (Test-ReportEnabled $Ctx)) { return }

        if ($State) {
            if (-not $TaskId) { $TaskId = [string]$State.id }
            if (-not $Branch -and $State.PSObject.Properties['branch']) { $Branch = [string]$State.branch }
        }
        if (-not $TaskId) { return }

        if ($Kind -eq 'progress') {
            $every = [double](Get-ReportSetting $Ctx 'progressEverySec' 10)
            $now = Get-Date
            if ($script:ReportProgressAt.ContainsKey($TaskId)) {
                if (($now - $script:ReportProgressAt[$TaskId]).TotalSeconds -lt $every) { return }
            }
            $script:ReportProgressAt[$TaskId] = $now
        }

        $url = [string](Get-ReportSetting $Ctx 'url' '')
        $project = [string](Get-ReportSetting $Ctx 'project' '')
        if (-not $project) { return }

        # Plan 0013 §6.1.7: progress and status name the repo whose phase is running, so one card can show a
        # chip per repo. Additive: the schema version stays.
        if (($Kind -eq 'progress' -or $Kind -eq 'status') -and $State) {
            try {
                if ($State.PSObject.Properties['phaseRepo'] -and $State.phaseRepo) {
                    if (-not $Data) { $Data = [ordered]@{} }
                    $Data['repo'] = [string]$State.phaseRepo
                }
            }
            catch { }
        }

        $script:ReportSeq++
        $tokenEnv = [string](Get-ReportSetting $Ctx 'tokenEnv' 'ORCHESTRA_REPORT_TOKEN')
        $timeout = [double](Get-ReportSetting $Ctx 'timeoutSec' 2)
        if ($timeout -le 0) { $timeout = 2 }

        $body = [ordered]@{
            schema  = 1
            id      = [guid]::NewGuid().ToString()
            project = $project
            taskId  = $TaskId
            branch  = $(if ($Branch) { $Branch } else { $null })
            at      = (Get-Date).ToString('o')
            seq     = $script:ReportSeq
            kind    = $Kind
            data    = $(if ($Data) { $Data } else { @{} })
        }

        $headers = @{}
        $token = Get-ReportToken $Ctx
        if ($token) { $headers['Authorization'] = "Bearer $token" }

        # Bytes, not the string: the body may hold Persian text and a length in characters is not a length
        # in bytes. -TimeoutSec is whole seconds.
        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 8 -Compress))
        Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json; charset=utf-8' -Body $bytes `
            -Headers $headers -TimeoutSec ([int][Math]::Ceiling($timeout)) | Out-Null
    }
    catch {
        if (-not $script:ReportWarned) {
            $script:ReportWarned = $true
            try { Write-Host '  [report] dashboard not reachable' -ForegroundColor DarkGray } catch { }
        }
    }
}

function Get-PoolEventTimes {
    # What a pool event says about the pool's clock, as ISO strings the API parses (null when nothing is
    # cooling down or exhausted).
    param($Ctx, [string]$Pool)
    $coolUntil = $null
    $exhaustedUntil = $null
    if ($Ctx) {
        $cool = Get-PoolCooldownSeconds $Ctx $Pool
        if ($cool -gt 0) { $coolUntil = (Get-Date).AddSeconds($cool).ToString('o') }
        $ex = Get-PoolExhaustedUntil $Ctx $Pool
        if ($ex -gt (Get-Date)) { $exhaustedUntil = $ex.ToString('o') }
    }
    return @{ cooldownUntil = $coolUntil; exhaustedUntil = $exhaustedUntil }
}
