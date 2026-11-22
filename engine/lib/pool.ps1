# lib/pool.ps1 - quota pools (QUOTA-POOLS-PLAN.md, D1-D14).
#
# A pool is a shared QUOTA, not a model: every free opencode model resolves to the pool `opencode`
# (D1). A pool hands out `maxConcurrent` slots, one OS file lock each, so a slot is cross-process and
# dies with its holder: no stale-lock clean-up and no PID check (D5).
#
# Dot-sourced wherever lib/agents.ps1 is (it uses Get-CfgValue and Get-AdapterTransport from there).
# Nothing in this file calls a model; `run.ps1 pools` and `health` only touch files.

function Get-FirstLimit {
    # The narrowest source that carries the key: hop, then adapter, then the tier (D2).
    param($Sources, [string]$Name)
    foreach ($s in $Sources) {
        if (-not $s) { continue }
        if ($s -is [System.Collections.IDictionary]) {
            if ($s.Contains($Name) -and $null -ne $s[$Name]) { return $s[$Name] }
            continue
        }
        $p = $s.PSObject.Properties[$Name]
        if ($p -and $null -ne $p.Value) { return $p.Value }
    }
    return $null
}

function Get-HopLimits {
    # D2: the hop's own keys, else its adapter's, else the tier block the two of them name, else the
    # built-in values - which are exactly the behaviour before pools existed, so an old config that
    # never heard of `tiers` runs as it always did.
    param($Ctx, $Hop)
    $adapter = [string]$Hop.adapter
    $adSpec = $Ctx.Config.adapters.$adapter

    $tier = ''
    if ($Hop.PSObject.Properties['tier'] -and $Hop.tier) { $tier = ([string]$Hop.tier).ToLower() }
    elseif ($adSpec -and $adSpec.PSObject.Properties['tier'] -and $adSpec.tier) { $tier = ([string]$adSpec.tier).ToLower() }

    $tierSpec = $null
    if ($tier -and $Ctx.Config.PSObject.Properties['tiers']) {
        $tiers = $Ctx.Config.tiers
        if ($tiers -and $tiers.PSObject.Properties[$tier]) { $tierSpec = $tiers.$tier }
    }

    $sources = @($Hop, $adSpec, $tierSpec)

    # D1: a pool is named by the hop, else the adapter, else the adapter itself is the pool.
    $pool = [string](Get-FirstLimit @($Hop, $adSpec) 'pool')
    if (-not $pool) { $pool = $adapter }

    $max = Get-FirstLimit $sources 'maxConcurrent'
    $gap = Get-FirstLimit $sources 'minGapSec'
    $retries = Get-FirstLimit $sources 'rateLimitRetries'
    $backoff = Get-FirstLimit $sources 'rateLimitBackoffSec'
    $whenBusy = Get-FirstLimit $sources 'whenBusy'
    $queueWait = Get-FirstLimit $sources 'queueWaitSec'

    $retryDelay = [int](Get-CfgValue $Ctx 'defaults' 'retryDelaySec' 20)

    [pscustomobject]@{
        Tier                = $tier
        Pool                = $pool
        MaxConcurrent       = [int]$(if ($null -ne $max) { $max } else { 0 })
        MinGapSec           = [double]$(if ($null -ne $gap) { $gap } else { 0 })
        RateLimitRetries    = [int]$(if ($null -ne $retries) { $retries } else { 1 })
        RateLimitBackoffSec = [double]$(if ($null -ne $backoff) { $backoff } else { $retryDelay })
        WhenBusy            = [string]$(if ($whenBusy) { ([string]$whenBusy).ToLower() } else { 'wait' })
        QueueWaitSec        = [double]$(if ($null -ne $queueWait) { $queueWait } else { 7200 })
    }
}

function Get-PoolDir {
    # runs/_pool/<pool>. A pool name travels through a path, so everything outside [A-Za-z0-9._-]
    # becomes '_' (D5). Pure: the callers that write create the folder.
    param($Ctx, [string]$Pool)
    $name = ([string]$Pool) -replace '[^A-Za-z0-9._-]', '_'
    if (-not $name) { $name = 'default' }
    return (Join-Path (Join-Path $Ctx.RunsDir '_pool') $name)
}

function Get-PoolScale {
    # D14: the offline suite compresses every wait, so 60 s of backoff costs it 0.6 s. Never set in
    # production. InvariantCulture: a fa-IR console must not read "0.01" as 1.
    $raw = $env:ORCH_POOL_BACKOFF_SCALE
    if (-not $raw) { return 1.0 }
    $v = 1.0
    try { $v = [double]::Parse($raw, [System.Globalization.CultureInfo]::InvariantCulture) } catch { return 1.0 }
    if ($v -lt 0) { return 0.0 }
    return $v
}

function Get-PoolScaledSeconds {
    param([double]$Seconds)
    if ($Seconds -le 0) { return 0.0 }
    return ($Seconds * (Get-PoolScale))
}

function Start-PoolSleep {
    # Milliseconds, not seconds: a scaled 60 s backoff is 0.6 s and Start-Sleep -Seconds would round it
    # to nothing or to one whole second.
    param([double]$Seconds)
    if ($Seconds -le 0) { return }
    Start-Sleep -Milliseconds ([int][Math]::Round($Seconds * 1000))
}

function Get-PoolPollMs {
    # D14: the wait loop's poll, 5 s unless the suite says otherwise.
    $raw = $env:ORCH_POOL_POLL_MS
    $ms = 5000
    if ($raw) { try { $ms = [int]$raw } catch { $ms = 5000 } }
    if ($ms -lt 20) { $ms = 20 }
    return $ms
}

function Get-PoolCooldownUntil {
    # D8: UTC ticks of the moment the pool may be called again. MinValue when there is no cool-down.
    param($Ctx, [string]$Pool)
    $f = Join-Path (Get-PoolDir $Ctx $Pool) 'cooldown-until.txt'
    if (-not (Test-Path -LiteralPath $f)) { return [datetime]::MinValue }
    $raw = ''
    try { $raw = (Get-Content -LiteralPath $f -Raw -Encoding UTF8).Trim() } catch { return [datetime]::MinValue }
    $ticks = 0L
    if (-not [long]::TryParse($raw, [ref]$ticks)) { return [datetime]::MinValue }
    try { return [datetime]$ticks } catch { return [datetime]::MinValue }
}

function Get-PoolCooldownSeconds {
    param($Ctx, [string]$Pool)
    $until = Get-PoolCooldownUntil $Ctx $Pool
    if ($until -eq [datetime]::MinValue) { return 0.0 }
    $left = ($until - (Get-Date)).TotalSeconds
    if ($left -le 0) { return 0.0 }
    return $left
}

function Set-PoolCooldown {
    # D8: called right before a rate-limited hop sleeps, so every OTHER run of this pool waits instead
    # of knocking on a quota that is already spent. Atomic write: a reader never sees half a number.
    param($Ctx, [string]$Pool, [double]$Seconds)
    if ($Seconds -le 0) { return }
    $dir = Get-PoolDir $Ctx $Pool
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $f = Join-Path $dir 'cooldown-until.txt'
    $until = (Get-Date).AddSeconds($Seconds)
    $tmp = "$f.tmp"
    Set-Content -LiteralPath $tmp -Value ([string]$until.Ticks) -Encoding ASCII -NoNewline
    Move-Item -LiteralPath $tmp -Destination $f -Force
}

function Get-PoolExhaustedUntil {
    # D2: local ticks of the moment the pool's quota resets. MinValue when the pool is not exhausted.
    # Read and compared exactly like cooldown-until.txt.
    param($Ctx, [string]$Pool)
    $f = Join-Path (Get-PoolDir $Ctx $Pool) 'exhausted-until.txt'
    if (-not (Test-Path -LiteralPath $f)) { return [datetime]::MinValue }
    $raw = ''
    try { $raw = (Get-Content -LiteralPath $f -Raw -Encoding UTF8).Trim() } catch { return [datetime]::MinValue }
    $ticks = 0L
    if (-not [long]::TryParse($raw, [ref]$ticks)) { return [datetime]::MinValue }
    try { return [datetime]$ticks } catch { return [datetime]::MinValue }
}

function Set-PoolExhausted {
    # D2: a hop whose output said "out of quota" marks its whole pool exhausted for $Seconds, so every
    # other hop of that pool is skipped until then. Atomic write of local ticks, like Set-PoolCooldown.
    param($Ctx, [string]$Pool, [double]$Seconds)
    if ($Seconds -le 0) { return }
    $dir = Get-PoolDir $Ctx $Pool
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $f = Join-Path $dir 'exhausted-until.txt'
    $until = (Get-Date).AddSeconds($Seconds)
    $tmp = "$f.tmp"
    Set-Content -LiteralPath $tmp -Value ([string]$until.Ticks) -Encoding ASCII -NoNewline
    Move-Item -LiteralPath $tmp -Destination $f -Force
}

function Get-SlotHolder {
    # D6: who holds slot <n>. Only trustworthy while its .lock cannot be opened; the callers check that
    # first, so a leftover JSON from a killed process is never reported.
    param([string]$Dir, [int]$N)
    $f = Join-Path $Dir ("slot-$N.json")
    if (-not (Test-Path -LiteralPath $f)) { return $null }
    try { return (Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json) } catch { return $null }
}

function Format-SlotHolder {
    param($Holder)
    if (-not $Holder) { return 'an unknown holder' }
    $who = (@([string]$Holder.taskId, [string]$Holder.role) | Where-Object { $_ }) -join ' '
    if (-not $who) { $who = 'an unknown holder' }
    $since = ''
    if ($Holder.since) {
        try { $since = ([datetime]::Parse([string]$Holder.since)).ToString('HH:mm:ss') } catch { $since = '' }
    }
    if ($since) { return "held by $who since $since" }
    return "held by $who"
}

function Get-PoolBusyReason {
    # Why the pool cannot be called right now, as one phrase a human reads in a wait line or in
    # `run.ps1 pools`: "cooling down until 10:04:31" or "held by T-AGENT-024 plan-reviewer since 10:02".
    param($Ctx, [string]$Pool, [int]$Max)
    $exhausted = Get-PoolExhaustedUntil $Ctx $Pool
    if ($exhausted -gt (Get-Date)) {
        return ("quota exhausted until {0}" -f $exhausted.ToString('HH:mm'))
    }
    $cool = Get-PoolCooldownSeconds $Ctx $Pool
    if ($cool -gt 0) {
        $until = (Get-Date).AddSeconds($cool)
        return ("cooling down until {0}" -f $until.ToString('HH:mm:ss'))
    }
    $dir = Get-PoolDir $Ctx $Pool
    $holders = @()
    for ($n = 1; $n -le $Max; $n++) {
        $lockPath = Join-Path $dir ("slot-$n.lock")
        if (-not (Test-Path -LiteralPath $lockPath)) { continue }
        $held = $false
        try {
            $fs = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $fs.Dispose()
        }
        catch [System.IO.IOException] { $held = $true }
        catch { }
        if ($held) { $holders += (Format-SlotHolder (Get-SlotHolder $dir $n)) }
    }
    if ($holders.Count) { return ($holders -join ', ') }
    return 'a holder that has just released it'
}

function Test-PoolSlotLocked {
    # Is slot <n> held? Try-open and release at once: the answer is the moment, nothing is kept.
    param([string]$Dir, [int]$N)
    $lockPath = Join-Path $Dir ("slot-$N.lock")
    if (-not (Test-Path -LiteralPath $lockPath)) { return $false }
    try {
        $fs = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $fs.Dispose()
        return $false
    }
    catch [System.IO.IOException] { return $true }
    catch { return $false }
}

function Enter-PoolSlot {
    <#
      Takes one of the pool's slots and holds it until Exit-PoolSlot. Returns
        Status = 'acquired' | 'unlimited' | 'busy' | 'timeout'
        Handle = what Exit-PoolSlot needs ('unlimited' has none)
        HeldBy = the phrase that says why it was busy ('busy'/'timeout' only)
      `wait` (the default) polls until a slot frees up or queueWaitSec runs out (D10); `next` and
      -NoWait return 'busy' at once. A live cool-down counts as busy in both modes (D8).
    #>
    param($Ctx, $Limits, [hashtable]$Meta = @{}, [string]$ProgressFile = '', [switch]$NoWait)
    $pool = [string]$Limits.Pool
    $max = [int]$Limits.MaxConcurrent
    # maxConcurrent 0 is unlimited: no file, no wait, nothing to release (D2).
    if ($max -le 0) { return @{ Status = 'unlimited'; Handle = $null; HeldBy = '' } }

    $dir = Get-PoolDir $Ctx $pool
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

    $whenBusy = if ($Limits.WhenBusy) { [string]$Limits.WhenBusy } else { 'wait' }
    $deadline = (Get-Date).AddSeconds([double]$Limits.QueueWaitSec)
    $pollMs = Get-PoolPollMs
    $role = [string]$Meta.role
    $announced = ''
    $reason = ''

    while ($true) {
        $reason = ''
        $fs = $null
        $lockPath = ''
        $cool = Get-PoolCooldownSeconds $Ctx $pool
        if ($cool -gt 0) {
            $reason = Get-PoolBusyReason -Ctx $Ctx -Pool $pool -Max $max
        }
        else {
            for ($n = 1; $n -le $max; $n++) {
                $lockPath = Join-Path $dir ("slot-$n.lock")
                try {
                    $fs = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
                    break
                }
                catch [System.IO.IOException] { $fs = $null }
                catch { $fs = $null }
            }
            if (-not $fs) {
                $reason = Get-PoolBusyReason -Ctx $Ctx -Pool $pool -Max $max
            }
            else {
                # A cool-down may have been set by another run while we were taking the slot. Hand the
                # slot back and wait like everyone else instead of calling a quota that just said 429.
                if ((Get-PoolCooldownSeconds $Ctx $pool) -gt 0) {
                    try { $fs.Dispose() } catch { }
                    $fs = $null
                    $reason = Get-PoolBusyReason -Ctx $Ctx -Pool $pool -Max $max
                }
            }
        }

        if ($fs) {
            # Pool and taskId ride along so Exit-PoolSlot can report the release without a context.
            $handle = @{ Stream = $fs; LockPath = $lockPath; Dir = $dir; Slot = $n; Pool = $pool; TaskId = [string]$Meta.taskId }
            $handle.JsonPath = [System.IO.Path]::ChangeExtension($lockPath, '.json')
            # D6: the holder is recorded beside the lock, after the lock is taken.
            try {
                $obj = [ordered]@{
                    taskId  = [string]$Meta.taskId
                    role    = [string]$Meta.role
                    adapter = [string]$Meta.adapter
                    model   = [string]$Meta.model
                    pid     = $PID
                    since   = (Get-Date).ToString('s')
                }
                $tmp = "$($handle.JsonPath).tmp"
                ($obj | ConvertTo-Json) | Set-Content -LiteralPath $tmp -Encoding UTF8
                Move-Item -LiteralPath $tmp -Destination $handle.JsonPath -Force
            }
            catch { }
            # D7: keep minGapSec between two hops of the same pool, then record this start.
            $gap = Get-PoolScaledSeconds ([double]$Limits.MinGapSec)
            if ($gap -gt 0) {
                $lastFile = Join-Path $dir 'last-start.txt'
                if (Test-Path -LiteralPath $lastFile) {
                    $lastTicks = 0L
                    try { $lastTicks = [long]((Get-Content -LiteralPath $lastFile -Raw -Encoding UTF8).Trim()) } catch { $lastTicks = 0 }
                    if ($lastTicks -gt 0) {
                        $elapsed = ((Get-Date) - [datetime]$lastTicks).TotalSeconds
                        if ($elapsed -lt $gap) { Start-PoolSleep ($gap - $elapsed) }
                    }
                }
            }
            try {
                $lastFile = Join-Path $dir 'last-start.txt'
                $tmp = "$lastFile.tmp"
                Set-Content -LiteralPath $tmp -Value ([string](Get-Date).Ticks) -Encoding ASCII -NoNewline
                Move-Item -LiteralPath $tmp -Destination $lastFile -Force
            }
            catch { }
            # Plan 0004 §6.1: the slot is ours now, and the dashboard's pools screen learns who holds it.
            # Guarded: lib/report.ps1 is not dot-sourced by every caller of this file (the suite's pool
            # holder loads pool.ps1 alone).
            if (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue) {
                $times = Get-PoolEventTimes -Ctx $Ctx -Pool $pool
                Send-OrchestraEvent -Ctx $Ctx -TaskId ([string]$Meta.taskId) -Kind 'pool.acquired' -Data ([ordered]@{
                        pool = $pool; slot = $n; heldBy = [string]$Meta.taskId
                        cooldownUntil = $times.cooldownUntil; exhaustedUntil = $times.exhaustedUntil
                    })
            }
            return @{ Status = 'acquired'; Handle = $handle; HeldBy = '' }
        }

        # Busy. `next` never waits, and -NoWait (health) must not either (D10, D11).
        if ($NoWait -or $whenBusy -eq 'next') { return @{ Status = 'busy'; Handle = $null; HeldBy = $reason } }

        if ((Get-Date) -ge $deadline) { return @{ Status = 'timeout'; Handle = $null; HeldBy = $reason } }

        if ($reason -ne $announced) {
            $who = if ($role) { "[$role] " } else { '' }
            Write-Host ("  {0}waiting for pool {1}: {2}" -f $who, $pool, $reason) -ForegroundColor DarkYellow
            # One event per reason, not per poll: a two-hour wait must not be 1440 requests (D5's spirit).
            if (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue) {
                $times = Get-PoolEventTimes -Ctx $Ctx -Pool $pool
                Send-OrchestraEvent -Ctx $Ctx -TaskId ([string]$Meta.taskId) -Kind 'pool.waiting' -Data ([ordered]@{
                        pool = $pool; slot = $null; heldBy = $reason
                        cooldownUntil = $times.cooldownUntil; exhaustedUntil = $times.exhaustedUntil
                    })
            }
            $announced = $reason
        }
        $meta = @{}
        foreach ($k in $Meta.Keys) { $meta[$k] = $Meta[$k] }
        $meta['phase'] = 'queued'
        $meta['pool'] = $pool
        $meta['heldBy'] = $reason
        $state = @{ Started = (Get-Date); Last = (Get-Date); Line = ''; State = 'queued'; Got = $false }
        Write-ProgressFile -Path $ProgressFile -Meta $meta -State $state
        Start-Sleep -Milliseconds $pollMs
    }
}

function Exit-PoolSlot {
    # Releases the slot: closing the handle frees the OS lock, and the JSON goes with it. A no-op for
    # 'unlimited' (no handle) and safe to call twice.
    param($Handle)
    if (-not $Handle) { return }
    try { if ($Handle.Stream) { $Handle.Stream.Dispose() } } catch { }
    try { if ($Handle.JsonPath -and (Test-Path -LiteralPath $Handle.JsonPath)) { Remove-Item -LiteralPath $Handle.JsonPath -Force } } catch { }
    # Plan 0004 §6.1: reported after the lock is gone, so no reader ever sees it held by a finished hop.
    if ($Handle.Slot -and $Handle.Pool -and (Get-Command Send-OrchestraEvent -ErrorAction SilentlyContinue)) {
        $times = Get-PoolEventTimes -Ctx $script:ReportCtx -Pool ([string]$Handle.Pool)
        Send-OrchestraEvent -Ctx $script:ReportCtx -TaskId ([string]$Handle.TaskId) -Kind 'pool.released' -Data ([ordered]@{
                pool = [string]$Handle.Pool; slot = [int]$Handle.Slot; heldBy = $null
                cooldownUntil = $times.cooldownUntil; exhaustedUntil = $times.exhaustedUntil
            })
    }
}

function Get-PoolStatus {
    # D12: one row per pool the config's chains name, read live. Free: it opens lock files for a moment
    # and releases them; no model, no file is created or changed.
    param($Ctx)
    $byPool = [ordered]@{}
    foreach ($role in $Ctx.Config.roles.PSObject.Properties.Name) {
        foreach ($hop in @($Ctx.Config.roles.$role.chain)) {
            if (-not $hop) { continue }
            $lim = Get-HopLimits $Ctx $hop
            $name = [string]$lim.Pool
            $inbox = ((Get-AdapterTransport -Config $Ctx.Config -Adapter ([string]$hop.adapter)) -eq 'inbox')
            if (-not $byPool.Contains($name)) {
                $byPool[$name] = [pscustomobject]@{
                    Pool     = $name
                    Tier     = $(if ($lim.Tier) { $lim.Tier } else { '-' })
                    Max      = [int]$lim.MaxConcurrent
                    Used     = 0
                    Slots    = ''
                    Holder   = '-'
                    Cooldown = '-'
                    Inbox    = $true
                    Adapters = (New-Object System.Collections.ArrayList)
                    Roles    = (New-Object System.Collections.ArrayList)
                }
            }
            $row = $byPool[$name]
            if (-not $inbox) { $row.Inbox = $false }
            if ($row.Adapters -notcontains [string]$hop.adapter) { [void]$row.Adapters.Add([string]$hop.adapter) }
            if ($row.Roles -notcontains $role) { [void]$row.Roles.Add($role) }
        }
    }

    foreach ($row in @($byPool.Values)) {
        $row | Add-Member -NotePropertyName 'AdapterList' -NotePropertyValue (($row.Adapters) -join ', ') -Force
        if ($row.Inbox) {
            # D4: a human answers these hops; they never take a slot.
            $row.Slots = 'inbox (no slot)'
            continue
        }
        if ($row.Max -le 0) {
            $row.Slots = 'unlimited'
        }
        else {
            $dir = Get-PoolDir $Ctx $row.Pool
            $used = 0
            $holders = @()
            for ($n = 1; $n -le $row.Max; $n++) {
                if (Test-PoolSlotLocked $dir $n) {
                    $used++
                    $holders += (Format-SlotHolder (Get-SlotHolder $dir $n))
                }
            }
            $row.Used = $used
            $row.Slots = "$used/$($row.Max)"
            if ($holders.Count) { $row.Holder = ($holders -join ', ') }
        }
        $exhausted = Get-PoolExhaustedUntil $Ctx $row.Pool
        if ($exhausted -gt (Get-Date)) {
            $row.Cooldown = ("quota exhausted until {0}" -f $exhausted.ToString('HH:mm'))
        }
        else {
            $cool = Get-PoolCooldownSeconds $Ctx $row.Pool
            if ($cool -gt 0) { $row.Cooldown = ("cooling {0}s" -f [int][Math]::Ceiling($cool)) }
        }
    }
    return @($byPool.Values)
}
