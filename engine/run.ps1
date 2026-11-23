[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Command = 'help',
    [Parameter(Position = 1)][string]$Arg1,
    [Parameter(Position = 2)][string]$Arg2,
    [Parameter(Position = 3)][string]$Arg3,
    [string]$Branch,
    [string]$Config,
    [string]$PlanFile,
    [string]$PlanFamily = 'claude-opus',
    [switch]$AutoRevise,
    [switch]$NoReadBack,
    [switch]$Force,
    [switch]$DryRun,
    [switch]$Check,
    [int]$Every = 90,
    [int]$PollSec = 5,
    [switch]$Once,
    [switch]$ThenResume,
    [switch]$Retry
)

$ErrorActionPreference = 'Stop'
# D1: an explicit -PlanFamily wins on revise; without it the previous conductor family is kept.
$script:PlanFamilyGiven = $PSBoundParameters.ContainsKey('PlanFamily')
$OrchDir = $PSScriptRoot
. (Join-Path $OrchDir 'lib\agents.ps1')
. (Join-Path $OrchDir 'lib\pool.ps1')
. (Join-Path $OrchDir 'lib\bus.ps1')
. (Join-Path $OrchDir 'lib\gates.ps1')
. (Join-Path $OrchDir 'lib\report.ps1')

function Show-Usage {
    # D5: the two project-specific lines come from orchestrator.json -> description { title, repos }.
    $title = '(no project config loaded)'
    $repos = '(no project config loaded)'
    if ($Ctx -and $Ctx.Config) {
        $title = 'orchestrator / run.ps1 - multi-agent, four-gate driver'
        $repos = "  - Repos: listed under 'repos' in orchestrator.json"
        $d = $Ctx.Config.PSObject.Properties['description']
        if ($d -and $d.Value) {
            if ($d.Value.PSObject.Properties['title'] -and $d.Value.title) { $title = [string]$d.Value.title }
            if ($d.Value.PSObject.Properties['repos'] -and $d.Value.repos) { $repos = [string]$d.Value.repos }
        }
    }
    if ($Ctx -and $Ctx.Config -and $Ctx.PSObject.Properties['ProfileId'] -and $Ctx.ProfileId) {
        Write-Host ("profile: {0} v{1}" -f $Ctx.ProfileId, $Ctx.ProfileVersion) -ForegroundColor DarkGray
    }
    $usage = @'
{0}

USAGE
  run.ps1 health                              free model-name check, then ping every agent
  run.ps1 agents                              list roles, fallback chains and model families
  run.ps1 graph [-Check] [repo]               FREE: graph age vs HEAD; -Check writes nothing, without
                                              it refreshes that repo (AST only, no LLM, no model call)
  run.ps1 pools                               FREE: the quota pools - tier, slots in use, who holds one,
                                              and until when a rate-limited pool is cooling down
  run.ps1 new <taskId> <repo> "<task>"        gate 1+2: auto planner, review, auto-revise (capped), read-back
  run.ps1 new <taskId> <repo> "<task>" -PlanFile plan.md [-PlanFamily claude-opus] [-AutoRevise]
                                              conductor mode: your plan enters at gate 2; FINDINGS stop
                                              for YOU to revise (or -AutoRevise hands it to the planner chain)
  run.ps1 new <taskId> <repo> "<task>" -DryRun   build prompts only, no model calls, no repo writes
  run.ps1 revise <taskId> ["notes"] [-PlanFile v2.md]
                                              one more round: your new plan file, or the planner with your notes
  run.ps1 readback <taskId>                   implementer (read-only) reports where it would have to guess
  run.ps1 review <taskId>                     gate 2 again for the plan already on disk: it writes no new
                                              version, so a deferred plan-reviewer answer still matches
  run.ps1 status <taskId>                     summary + the run's task.json
  run.ps1 queue [taskId]                      FREE: manual requests still waiting for an answer, with the
                                              paths and the key; reads the filesystem only
  run.ps1 close <taskId> [-Force]             after the plan is filed (Status: Done in the project's
                                              plansDir, or in <repo>/<plansDoneDir> for a project without
                                              one): delete runs/<id>/ and runs/_drafts/<id>.*
                                              (-Force: without the check)
  run.ps1 watch [taskId] [-Every 90] [-Once]  read progress.json and print what each running hop is
                                              doing (no model, no lock, read-only; -Once prints one block)
  run.ps1 watch <taskId> -ThenResume [-PollSec 5]
                                              wait for that run's pending answer, then run the command that
                                              consumes it (review / readback / verify). Ctrl+C while waiting
                                              is safe: nothing has been claimed yet.
  run.ps1 answer <taskId> <key> <file> [-Force]
                                              give the answer a `manual` hop is waiting for; review,
                                              readback or verify then consumes it (not revise: that is a
                                              new version, and so a new key)
  run.ps1 approve <taskId> ["notes"] [-Branch x] [-Force]
                                              gate 2: freeze plan.approved.md (+ your notes), pick the branch
  run.ps1 implement <taskId> [phase] [-Force] gate 3: orchestrator creates the branch, agent edits, checks run
  run.ps1 verify <taskId>                     gate 4: run verifyCommands, verifier reads diff -> PASS/FAIL
  Every command except help/agents/health/close ends with a short "=== SUMMARY ===" block.
  -NoReadBack skips the automatic read-back after a PASS review.

STATES
  Planning -> (NeedsInput) -> AwaitingApproval -> Approved -> Implemented -> Verified | VerifyFailed
  A `manual` hop does not wait: it writes runs/<id>/inbox/<key>.md and the command exits 3 (DEFERRED),
  leaving task.json untouched. `queue` shows what is waiting; `answer` stores the reply; then rerun the
  command that consumes it (review / readback / implement / verify) - or `watch <id> -ThenResume` waits
  and does it. A pending request is answered, not overtaken: rerunning skips the automatic hops until the
  answer is stored; -Retry deletes the request and runs the chain normally instead.
  implement/verify can repeat per phase. Violation = a hard rule broke (commit, sibling repo, branch).

NOTES
  - Nothing in a repo is ever committed, merged or deleted by this tool. You close by hand; then
    `close` deletes only the run's own working files under runs/.
  - plan-reviewer must be a different model family than the plan's author (enforced at run time).
  - Quota pools: every hop takes a slot of its pool (named by `pool`, else its adapter) before it calls
    a model, and holds it for the call and its retries. Two runs on one pool run one after the other and
    the waiting one says whom it waits for; two runs on different pools do not wait. A 429 marks the pool
    cooling down until the retry. Inbox hops (a human answers) take no slot. See `pools`.
{1}
  - Conductor procedure and rules: engine/docs/CONDUCTOR.md and engine/docs/WORKING-MODEL.md, in the engine's
    own repository (a project that still has the old copy keeps orchestrator/ until plan 0007's companion).
'@ -f $title, $repos
    Write-Host $usage
}

function Get-Timeout { param($Ctx) [int](Get-CfgValue $Ctx 'defaults' 'timeoutSec' 900) }

function Cmd-Agents {
    param($Ctx)
    Write-Host ("config: {0}" -f $Ctx.ConfigPath) -ForegroundColor DarkGray
    if ($Ctx.PSObject.Properties['ProfileId'] -and $Ctx.ProfileId) {
        Write-Host ("profile: {0} v{1}" -f $Ctx.ProfileId, $Ctx.ProfileVersion) -ForegroundColor DarkGray
    }
    Write-Host ("engine: {0}" -f $OrchDir) -ForegroundColor DarkGray
    Write-Host ("root:   {0}" -f $Ctx.Root) -ForegroundColor DarkGray
    $rows = foreach ($role in $Ctx.Config.roles.PSObject.Properties.Name) {
        $spec = $Ctx.Config.roles.$role
        [pscustomobject]@{
            Role     = $role
            Gate     = $spec.gate
            ReadOnly = $spec.readOnly
            Chain    = (($spec.chain | ForEach-Object { "$($_.adapter):$($_.model) [$(Get-HopFamily $_)]" }) -join '  ->  ')
        }
    }
    $rows | Format-Table -AutoSize | Out-String -Width 400 | Write-Host
    Write-Host "adapters:" -ForegroundColor DarkGray
    foreach ($a in $Ctx.Config.adapters.PSObject.Properties.Name) {
        $ad = $Ctx.Config.adapters.$a
        "{0,-10} enabled={1} transport={2}" -f $a, $ad.enabled, (Get-AdapterTransport -Config $Ctx.Config -Adapter $a) | Write-Host
    }
}

function Cmd-Graph {
    # Free: no model call, and with -Check no write either. The planner/read-only roles cannot run
    # graphify themselves (no bash), so this is how the conductor sees whether the graph it is about
    # to query still matches the code.
    param($Ctx, [string]$Repo, [switch]$Check)
    $known = @($Ctx.Config.repos.PSObject.Properties.Name)
    if ($Repo) {
        if ($known -notcontains $Repo) { throw "unknown repo '$Repo' (have: $($known -join ', '))" }
        $targets = @($Repo)
    }
    else { $targets = $known }

    $rows = @(foreach ($r in $targets) {
            $s = Get-GraphStatus (Get-RepoPath $Ctx $r)
            [pscustomobject]@{
                Repo     = $r
                State    = $s.State
                Head     = $s.Head
                Graph    = $(if ($s.GraphAt) { $s.GraphAt.ToString('yyyy-MM-dd HH:mm') } else { '-' })
                Behind   = (Format-Age $s.Behind)
                Semantic = $(if ($s.Semantic) { 'yes' } else { 'no' })
                Reason   = $s.Reason
            }
        })
    $rows | Format-Table -AutoSize | Out-String -Width 250 | Write-Host

    if (-not $Check) {
        Write-Host "refreshing $($targets -join ', ') (AST only, no LLM)..." -ForegroundColor DarkGray
        & (Join-Path $OrchDir 'refresh-graph.ps1') -Config $Ctx.ConfigPath -Repos $targets
        return
    }
    $stale = @($rows | Where-Object { $_.State -ne 'fresh' })
    if ($stale.Count) {
        Write-Host ("stale: {0} - refresh with: run.ps1 graph{1}" -f (($stale | ForEach-Object { $_.Repo }) -join ', '), $(if ($Repo) { " $Repo" } else { '' })) -ForegroundColor Yellow
    }
    else {
        Write-Host 'all graphs match the code.' -ForegroundColor Green
    }
}

function Cmd-Pools {
    # D12: one line per pool the config's chains name. FREE and read-only in effect: it opens slot lock
    # files for a moment and lets go, calls no model and writes no file.
    param($Ctx)
    $rows = @(Get-PoolStatus -Ctx $Ctx)
    if (-not $rows.Count) { Write-Host 'no pools: no chain hop resolves to an adapter.' -ForegroundColor DarkGray; return }
    Write-Host 'quota pools (a pool runs maxConcurrent hops at once; one slot per agent, free or paid)' -ForegroundColor Cyan
    $rows | Select-Object @{ n = 'Pool'; e = { $_.Pool } }, @{ n = 'Tier'; e = { $_.Tier } }, @{ n = 'Slots'; e = { $_.Slots } }, @{ n = 'Holder'; e = { $_.Holder } }, @{ n = 'Cool-down'; e = { $_.Cooldown } }, @{ n = 'Adapter(s)'; e = { $_.AdapterList } } |
        Format-Table -AutoSize | Out-String -Width 300 | Write-Host
    foreach ($r in $rows) {
        if ($r.Inbox) { Write-Host ("{0}: a human answers its hops, so it never takes a slot." -f $r.Pool) -ForegroundColor DarkGray }
        elseif ($r.Max -le 0) { Write-Host ("{0}: maxConcurrent 0 - runs may call it at the same time." -f $r.Pool) -ForegroundColor DarkGray }
        elseif ($r.Used -ge $r.Max) { Write-Host ("{0}: full - a run that needs it waits (or takes the next hop, with whenBusy next)." -f $r.Pool) -ForegroundColor Yellow }
    }
}

function Cmd-Health {
    param($Ctx, [int]$TimeoutSec)
    Write-Host "preflight 1/2: model names (free, no LLM call)..." -ForegroundColor Cyan
    $names = @(Test-ModelNames -Ctx $Ctx)
    $names | Format-Table Role, Adapter, Model, Family, Name -AutoSize | Out-String -Width 300 | Write-Host
    Write-Host "preflight 2/2: pinging agents..." -ForegroundColor Cyan
    $rows = @(Test-AgentHealth -Ctx $Ctx -TimeoutSec $TimeoutSec)
    $rows | Format-Table Role, Head, HeadOk, ServedBy, Ok, Detail -AutoSize | Out-String -Width 400 | Write-Host
    $bad = $false
    if ($names | Where-Object { $_.Name -eq 'UNKNOWN MODEL' }) { Write-Host "unknown model names in the config (see above)." -ForegroundColor Red; $bad = $true }
    if ($rows | Where-Object { -not $_.Ok }) { Write-Host "one or more roles have no working agent." -ForegroundColor Red; $bad = $true }
    $degraded = @($rows | Where-Object { $_.Ok -and -not $_.HeadOk })
    if ($degraded.Count) { Write-Host ("degraded (served by a fallback): " + (($degraded | ForEach-Object { "$($_.Role) -> $($_.ServedBy)" }) -join '; ')) -ForegroundColor Yellow }
    if ($bad) { Write-Host "refusing to start." -ForegroundColor Red; return 1 }
    Write-Host "all roles have a working agent." -ForegroundColor Green
    return 0
}

function New-PlannerExtra {
    param([string]$RepoPath, [string]$PrevPlan, [string]$Findings, [string]$Notes)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("Working directory (repo root): $RepoPath")
    if ($PrevPlan) {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("REVISION ROUND. Revise the plan below. Resolve every blocker and should-fix finding, or explain in the plan why a finding is wrong. Keep what was not criticised.")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("### Previous plan")
        [void]$sb.AppendLine($PrevPlan)
        if ($Findings) { [void]$sb.AppendLine(); [void]$sb.AppendLine("### Findings on it (reviewer and/or implementer read-back)"); [void]$sb.AppendLine($Findings) }
    }
    if ($Notes) { [void]$sb.AppendLine(); [void]$sb.AppendLine("### The user's notes / answers (authoritative)"); [void]$sb.AppendLine($Notes) }
    $sb.ToString()
}

function Get-Counts {
    param([string]$Text)
    $f = @(Get-Findings $Text)
    [pscustomobject]@{
        blockers = @($f | Where-Object Level -eq 'blocker').Count
        should   = @($f | Where-Object Level -eq 'should').Count
        nits     = @($f | Where-Object Level -eq 'nit').Count
    }
}

function Get-StateNotes {
    param($State)
    if ($State.PSObject.Properties['notes'] -and $State.notes) { return [string]$State.notes }
    return ''
}

function Add-PlanVersion {
    # Records a new plan version (from the planner chain or from the conductor's file).
    param($Ctx, $State, [string]$PlanText, [string]$Adapter, [string]$Model, [string]$Family)
    $round = [int]$State.round + 1
    Set-Prop $State 'round' $round
    $planFile = Save-Artifact -Ctx $Ctx -TaskId $State.id -Name "plan.v$round" -Content $PlanText
    Save-Outbox -Ctx $Ctx -TaskId $State.id -Role 'planner' -Name 'plan' -Content $PlanText | Out-Null
    $pv = Get-Verdict $PlanText
    $pk = if ($pv) { $pv.Kind } elseif ($Adapter -eq 'conductor') { 'PASS' } else { 'NONE' }
    Set-Prop $State 'plan' ([pscustomobject]@{ adapter = $Adapter; model = $Model; family = $Family; artifact = $planFile; verdict = $pk; message = $(if ($pv) { $pv.Message } else { '' }) })
    Add-History -Ctx $Ctx -State $State -Event 'plan' -Data @{ round = $round; model = "${Adapter}:$Model"; verdict = $pk }
    Write-Host ""
    Write-Host "===== PLAN v$round ($Adapter / $Model) - verdict=$pk =====" -ForegroundColor Cyan
    Write-Host $PlanText
    return $pk
}

function Invoke-PlannerStep {
    param($Ctx, $State, [string]$PrevPlan, [string]$PrevFindings, [string]$Notes)
    $repoPath = Get-RepoPath $Ctx $State.repo
    Write-Host "gate 1: planner chain, round $([int]$State.round + 1) ($($State.repo))..." -ForegroundColor Cyan
    $prompt = New-RolePrompt -Ctx $Ctx -Role 'planner' -Repo $State.repo -Task $State.task -Extra (New-PlannerExtra $repoPath $PrevPlan $PrevFindings $Notes)
    $plan = Invoke-Role -Ctx $Ctx -Role 'planner' -Prompt $prompt -Cwd (Get-AgentCwd $Ctx $State.repo) -TimeoutSec (Get-Timeout $Ctx) -Repo $State.repo -ProgressFile (Get-TaskProgressFile $Ctx $State.id) -TaskId $State.id -Phase "plan$($State.round + 1)"
    $pk = Add-PlanVersion -Ctx $Ctx -State $State -PlanText $plan.Result.Stdout -Adapter $plan.Adapter -Model $plan.Model -Family $plan.Family
    Save-Transcript -Ctx $Ctx -TaskId $State.id -Name "planner.v$($State.round)" -Data $plan | Out-Null
    if ($pk -eq 'NEED_INPUT' -or $pk -eq 'BLOCKED') {
        Set-Prop $State 'status' 'NeedsInput'
        Save-TaskState -Ctx $Ctx -TaskId $State.id -State $State
        Write-Host "planner stopped ($pk): $($State.plan.message)" -ForegroundColor Yellow
        return $false
    }
    return $true
}

# ---------------------------------------------------------------- D5-D7: session capture and continuation
function Get-MaxFollowUps {
    param($Ctx)
    return [int](Get-CfgValue $Ctx 'sessions' 'maxFollowUps' 6)
}

function Add-SessionUse {
    # D6: state.sessions is keyed by session id -> { adapter, model, role, turns }; turns counts the calls
    # made in that session. A hop with no session id (a stateless adapter) records nothing.
    param($State, [string]$SessionId, [string]$Adapter, [string]$Model, [string]$Role)
    if (-not $SessionId) { return }
    if (-not $State.PSObject.Properties['sessions'] -or $null -eq $State.sessions) { Set-Prop $State 'sessions' ([pscustomobject]@{}) }
    if ($State.sessions.PSObject.Properties[$SessionId]) {
        $State.sessions.$SessionId.turns = [int]$State.sessions.$SessionId.turns + 1
    }
    else {
        $State.sessions | Add-Member -NotePropertyName $SessionId -NotePropertyValue ([pscustomobject]@{ adapter = $Adapter; model = $Model; role = $Role; turns = 1 }) -Force
    }
}

function Test-SessionCanContinue {
    # D6: a session may carry another call only while its turn count is below sessions.maxFollowUps.
    param($Ctx, $State, [string]$SessionId)
    if (-not $SessionId) { return $false }
    if (-not $State.PSObject.Properties['sessions'] -or $null -eq $State.sessions) { return $false }
    if (-not $State.sessions.PSObject.Properties[$SessionId]) { return $false }
    return ([int]$State.sessions.$SessionId.turns -lt (Get-MaxFollowUps $Ctx))
}

function Find-StageSession {
    # D7: the earlier stage session a later stage may continue. Stages are tried in order; a stage qualifies
    # when it has a session, saw the approved version or an earlier one, and its family is -SameFamily
    # (implement: the implementer's own model) or is not -OtherFamily (verify: anything but the implementer,
    # so the check stays independent). Returns @{ Stage; Id; Round } or $null.
    param($State, [string[]]$Stages, [string]$SameFamily, [string]$OtherFamily)
    foreach ($st in $Stages) {
        if (-not $State.PSObject.Properties[$st]) { continue }
        $s = $State.$st
        if (-not $s -or -not $s.PSObject.Properties['session'] -or -not $s.session) { continue }
        if ([int]$s.round -gt [int]$State.approved.version) { continue }
        $fam = if ($s.PSObject.Properties['family'] -and $s.family) { ([string]$s.family).ToLower() } else { Get-HopFamily ([pscustomobject]@{ adapter = $s.adapter; model = $s.model }) }
        if ($SameFamily -and $fam -ne $SameFamily.ToLower()) { continue }
        if ($OtherFamily -and $fam -eq $OtherFamily.ToLower()) { continue }
        return @{ Stage = $st; Id = [string]$s.session; Round = [int]$s.round }
    }
    return $null
}

function Get-FirstHopFamily {
    # The family of a role's first enabled hop: the model that will most likely answer.
    param($Ctx, [string]$Role)
    foreach ($h in @($Ctx.Config.roles.$Role.chain)) {
        $a = $Ctx.Config.adapters.PSObject.Properties[[string]$h.adapter]
        if ($a -and $a.Value.PSObject.Properties['enabled'] -and -not $a.Value.enabled) { continue }
        return (Get-HopFamily $h)
    }
    return ''
}

function New-FollowUpPrompt {
    # D7: a short prompt for a continued session - no repo docs, workflow, role or stage prompt (the
    # session already has them). Just the header, the body, and the role's orchestrator-mode protocol.
    param([string]$Role, [string]$Body, [string]$Branch, [string]$Protocol)
    if (-not $Protocol) { $Protocol = $Role }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("# ORCHESTRATOR CALL - role: $Role (follow-up in the same session)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine($Body)
    [void]$sb.AppendLine()
    [void]$sb.AppendLine((Get-RoleProtocol -Role $Protocol -Branch $Branch))
    return $sb.ToString()
}

function Get-PlanDiffText {
    # D7: unified diff old->new plan, minus the git header lines (diff --git / index); ---/+++ and the
    # hunks stay. Exit 1 (the files differ) is normal for --no-index, so the text is what matters.
    param([string]$OldFile, [string]$NewFile)
    $r = Invoke-External -Exe 'git' -ArgList @('diff', '--no-index', '--no-color', '-U3', $OldFile, $NewFile) -TimeoutSec 60
    $kept = @($r.Stdout -split "`r?`n" | Where-Object { $_ -notmatch '^diff --git ' -and $_ -notmatch '^index ' })
    return ($kept -join "`n").Trim()
}

function Get-RevisionBody {
    # D7: the review/read-back follow-up body for round n>=2 - the plan diff (or the full new plan when
    # the diff is over 60% of it), the "check your previous findings" instruction, and the user's notes.
    # -FromRound is the version the continued session last saw (default: the one before); the diff runs
    # from there, so a session that skipped versions still sees every change since its last turn.
    param($Ctx, $State, [int]$Round, [string]$Notes, [string]$Kind, [int]$FromRound = 0)
    if ($FromRound -lt 1 -or $FromRound -ge $Round) { $FromRound = $Round - 1 }
    $oldFile = Join-Path (Get-TaskDir $Ctx $State.id) "artifacts\plan.v$FromRound.md"
    $newFile = $State.plan.artifact
    if (-not (Test-Path -LiteralPath $oldFile)) { return '' }
    $newText = Get-Content -LiteralPath $newFile -Raw -Encoding UTF8
    $diff = Get-PlanDiffText -OldFile $oldFile -NewFile $newFile
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("The plan was revised to version $Round.")
    [void]$sb.AppendLine()
    if ($diff.Length -gt (0.6 * $newText.Length)) {
        [void]$sb.AppendLine("The revised plan (version $Round) in full:")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine($newText)
    }
    else {
        [void]$sb.AppendLine("Changes from version $FromRound to version $Round (unified diff):")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine($diff)
    }
    [void]$sb.AppendLine()
    if ($Kind -eq 'readback') {
        [void]$sb.AppendLine('Check each of your previous read-back findings is resolved; look for NEW places you would have to guess only in the changed parts; do not re-litigate settled points.')
    }
    else {
        [void]$sb.AppendLine('Check each of your previous findings is resolved; look for NEW blockers only in the changed parts; do not re-litigate settled points.')
    }
    if ($Notes) {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("The user's notes (authoritative; a plan that follows them is correct on those points):")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine($Notes)
    }
    return $sb.ToString()
}

function Get-ApprovedAmendments {
    # D7: the text after the "## APPROVED AMENDMENTS" heading in the approved plan, or '' when absent.
    param([string]$PlanText)
    if (-not $PlanText) { return '' }
    $lines = @($PlanText -split "`r?`n")
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s+APPROVED AMENDMENTS') {
            if ($i + 1 -ge $lines.Count) { return '' }
            return (($lines[($i + 1)..($lines.Count - 1)]) -join "`n").Trim()
        }
    }
    return ''
}

function Invoke-ReviewStep {
    param($Ctx, $State, [string]$PrevFindings, [string]$Notes, [switch]$Retry)
    $round = [int]$State.round
    $planText = Get-Content -LiteralPath $State.plan.artifact -Raw -Encoding UTF8
    Write-Host "gate 2: independent review of v$round (excluding family '$($State.plan.family)')..." -ForegroundColor Cyan
    $extra = "The plan under review (version $round):`n`n$planText"
    if ($PrevFindings) { $extra += "`n`nFindings on the previous version. Check each one is resolved; then look for NEW blockers only - do not re-litigate settled points:`n`n$PrevFindings" }
    if ($Notes) { $extra += "`n`nThe user's notes (authoritative; a plan that follows them is correct on those points):`n`n$Notes" }
    $prompt = New-RolePrompt -Ctx $Ctx -Role 'plan-reviewer' -Repo $State.repo -Task $State.task -Extra $extra
    # D7: continue the previous review session with a short follow-up (the plan diff) when it may continue.
    $continue = $null
    $prevSession = if ($State.review -and $State.review.PSObject.Properties['session']) { [string]$State.review.session } else { '' }
    # Any earlier version will do: the diff runs from the version that session last reviewed.
    if ($round -ge 2 -and $prevSession -and [int]$State.review.round -lt $round -and (Test-SessionCanContinue -Ctx $Ctx -State $State -SessionId $prevSession)) {
        $body = Get-RevisionBody -Ctx $Ctx -State $State -Round $round -Notes $Notes -Kind 'review' -FromRound ([int]$State.review.round)
        if ($body) { $continue = @{ Adapter = $State.review.adapter; Model = $State.review.model; SessionId = $prevSession; Prompt = (New-FollowUpPrompt -Role 'plan-reviewer' -Body $body) } }
    }
    $review = Invoke-Role -Ctx $Ctx -Role 'plan-reviewer' -Prompt $prompt -Cwd (Get-AgentCwd $Ctx $State.repo) -TimeoutSec (Get-Timeout $Ctx) -ExcludeFamilies @($State.plan.family) -Repo $State.repo -ProgressFile (Get-TaskProgressFile $Ctx $State.id) -TaskId $State.id -ManualKey "plan-reviewer.v$round" -Continue $continue -Retry:$Retry
    $text = $review.Result.Stdout
    $file = Save-Artifact -Ctx $Ctx -TaskId $State.id -Name "plan.findings.v$round" -Content $text
    Save-Outbox -Ctx $Ctx -TaskId $State.id -Role 'plan-reviewer' -Name 'review' -Content $text | Out-Null
    Save-Transcript -Ctx $Ctx -TaskId $State.id -Name "plan-reviewer.v$round" -Data $review | Out-Null

    $rv = Get-Verdict $text
    $c = Get-Counts $text
    # A missing verdict line is its own state: the reply stopped mid-way, which is neither a finding round
    # nor a PASS. Neither is a PASS that still lists blockers/should-fixes.
    $verdict = if ($rv) { $rv.Kind } else { 'NO_VERDICT' }
    if ($verdict -eq 'PASS' -and ($c.blockers + $c.should) -gt 0) { $verdict = 'FINDINGS' }
    $differ = Test-Gate2ModelDiffer -Ctx $Ctx -PlannerFamily $State.plan.family -ReviewerFamily $review.Family
    Add-SessionUse -State $State -SessionId $review.SessionId -Adapter $review.Adapter -Model $review.Model -Role 'plan-reviewer'
    Set-Prop $State 'review' ([pscustomobject]@{
        adapter = $review.Adapter; model = $review.Model; family = $review.Family; artifact = $file; round = $round
        verdict = $verdict; verdictLine = [bool]$rv; modelsDiffer = $differ
        blockers = $c.blockers; should = $c.should; nits = $c.nits; session = $review.SessionId
    })
    Add-History -Ctx $Ctx -State $State -Event 'review' -Data @{ round = $round; model = "$($review.Adapter):$($review.Model)"; verdict = $verdict }
    Set-Prop $State 'status' 'AwaitingApproval'
    Save-TaskState -Ctx $Ctx -TaskId $State.id -State $State

    Write-Host ""
    Write-Host "===== REVIEW v$round ($($review.Adapter) / $($review.Model)) - verdict=$verdict familiesDiffer=$differ =====" -ForegroundColor Yellow
    Write-Host $text
    if (-not $rv) { Write-Host "(reviewer gave no VERDICT line - recorded as NO_VERDICT: rerun the review)" -ForegroundColor Yellow }
    return $verdict
}

function Invoke-ReadBack {
    # Gate 2.5: the implementer chain, read-only, says where it would have to guess.
    param($Ctx, $State, [switch]$Retry)
    $round = [int]$State.round
    $planText = Get-Content -LiteralPath $State.plan.artifact -Raw -Encoding UTF8
    $notes = Get-StateNotes $State
    $extra = "The plan you will implement later (version $round):`n`n$planText"
    if ($notes) { $extra += "`n`nThe user's notes (authoritative):`n`n$notes" }
    $prompt = New-RolePrompt -Ctx $Ctx -Role 'implementer' -Protocol 'readback' -Repo $State.repo -Task $State.task -Extra $extra
    Write-Host "gate 2.5: implementer read-back of v$round (read-only)..." -ForegroundColor Cyan
    # D7: continue the previous read-back session with a short follow-up when it may continue.
    $continue = $null
    $prevSession = if ($State.readback -and $State.readback.PSObject.Properties['session']) { [string]$State.readback.session } else { '' }
    # Any earlier version will do: the diff runs from the version that session last read back.
    if ($round -ge 2 -and $prevSession -and [int]$State.readback.round -lt $round -and (Test-SessionCanContinue -Ctx $Ctx -State $State -SessionId $prevSession)) {
        $body = Get-RevisionBody -Ctx $Ctx -State $State -Round $round -Notes $notes -Kind 'readback' -FromRound ([int]$State.readback.round)
        if ($body) { $continue = @{ Adapter = $State.readback.adapter; Model = $State.readback.model; SessionId = $prevSession; Prompt = (New-FollowUpPrompt -Role 'implementer' -Protocol 'readback' -Body $body) } }
    }
    $rb = Invoke-Role -Ctx $Ctx -Role (Get-ReadBackRole $Ctx) -ReadOnly -Prompt $prompt -Cwd (Get-AgentCwd $Ctx $State.repo) -TimeoutSec (Get-Timeout $Ctx) -Repo $State.repo -ProgressFile (Get-TaskProgressFile $Ctx $State.id) -TaskId $State.id -ManualKey "readback.v$round" -Continue $continue -Retry:$Retry
    $text = $rb.Result.Stdout
    $file = Save-Artifact -Ctx $Ctx -TaskId $State.id -Name "plan.readback.v$round" -Content $text
    Save-Transcript -Ctx $Ctx -TaskId $State.id -Name "readback.v$round" -Data $rb | Out-Null
    $v = Get-Verdict $text
    $c = Get-Counts $text
    $verdict = if ($v) { $v.Kind } else { 'NO_VERDICT' }
    if ($verdict -eq 'PASS' -and ($c.blockers + $c.should) -gt 0) { $verdict = 'FINDINGS' }
    Add-SessionUse -State $State -SessionId $rb.SessionId -Adapter $rb.Adapter -Model $rb.Model -Role 'readback'
    Set-Prop $State 'readback' ([pscustomobject]@{ adapter = $rb.Adapter; model = $rb.Model; family = $rb.Family; artifact = $file; round = $round; verdict = $verdict; blockers = $c.blockers; should = $c.should; nits = $c.nits; session = $rb.SessionId })
    Add-History -Ctx $Ctx -State $State -Event 'readback' -Data @{ round = $round; model = "$($rb.Adapter):$($rb.Model)"; verdict = $verdict }
    Save-TaskState -Ctx $Ctx -TaskId $State.id -State $State
    Write-Host ""
    Write-Host "===== READ-BACK v$round ($($rb.Adapter) / $($rb.Model)) - verdict=$verdict =====" -ForegroundColor Yellow
    Write-Host $text
    return $verdict
}

function Invoke-Gate2 {
    # Gate 2 for the current version: the review, then the implementer read-back. A `manual` read-back
    # is requested FIRST, so the human answers it while the reviewer works; its deferral is
    # kept and raised by the caller only after the review is recorded. An automatic read-back still runs
    # only after a PASS, as before.
    param($Ctx, $State, [string]$PrevFindings, [string]$Notes, [bool]$ReadBack, [switch]$Retry)
    $deferred = ''
    Write-DirtyTreeWarning -Ctx $Ctx -Repo $State.repo
    $manual = $ReadBack -and (Test-ReadBackIsManual $Ctx)
    if ($manual) {
        try { [void](Invoke-ReadBack -Ctx $Ctx -State $State -Retry:$Retry) }
        catch {
            if ($_.Exception.Message -notlike 'DEFERRED:*') { throw }
            $deferred = $_.Exception.Message
            Write-Host "gate 2.5: read-back of v$($State.round) is waiting for a human (run.ps1 queue $($State.id)); the review runs meanwhile" -ForegroundColor Cyan
        }
    }
    $verdict = Invoke-ReviewStep -Ctx $Ctx -State $State -PrevFindings $PrevFindings -Notes $Notes -Retry:$Retry
    if (-not $manual -and $verdict -eq 'PASS' -and $ReadBack) { [void](Invoke-ReadBack -Ctx $Ctx -State $State -Retry:$Retry) }
    return [pscustomobject]@{ Verdict = $verdict; Deferred = $deferred }
}

function Get-OpenFindings {
    # Review + read-back findings on the current version, as one block for the next revision.
    param($State)
    $parts = @()
    if ($State.review -and $State.review.round -eq $State.round -and $State.review.verdict -ne 'PASS') {
        $parts += "## Reviewer ($($State.review.model))`n" + (Get-Content -LiteralPath $State.review.artifact -Raw -Encoding UTF8)
    }
    if ($State.PSObject.Properties['readback'] -and $State.readback -and $State.readback.round -eq $State.round -and $State.readback.verdict -ne 'PASS') {
        $parts += "## Implementer read-back ($($State.readback.model))`n" + (Get-Content -LiteralPath $State.readback.artifact -Raw -Encoding UTF8)
    }
    return ($parts -join "`n`n")
}

function Invoke-ReviseLoop {
    # Planner chain revises while review or read-back has findings, up to MaxRounds. Returns a pending
    # manual read-back's DEFERRED message ('' when none): the loop stops there, a human has to answer.
    param($Ctx, $State, [int]$MaxRounds, [bool]$ReadBack)
    for ($i = 0; $i -lt $MaxRounds; $i++) {
        $open = Get-OpenFindings $State
        if ($State.status -ne 'AwaitingApproval' -or -not $open) { break }
        $prevPlan = Get-Content -LiteralPath $State.plan.artifact -Raw -Encoding UTF8
        Write-Host ""
        Write-Host "auto-revise $($i + 1)/$MaxRounds ..." -ForegroundColor Magenta
        if (-not (Invoke-PlannerStep -Ctx $Ctx -State $State -PrevPlan $prevPlan -PrevFindings $open -Notes (Get-StateNotes $State))) { break }
        $g = Invoke-Gate2 -Ctx $Ctx -State $State -PrevFindings $open -Notes (Get-StateNotes $State) -ReadBack $ReadBack
        if ($g.Deferred) { return $g.Deferred }
    }
    return ''
}

function Format-VerdictHead {
    # A reply that ended without a verdict is not a finding round: say so instead of showing "0/0/0".
    param($Rec)
    if ($Rec.verdict -eq 'NO_VERDICT') { return 'NO_VERDICT (reply ended without a verdict - rerun)' }
    return ("{0} (blocker {1}, should {2}, nit {3})" -f $Rec.verdict, $Rec.blockers, $Rec.should, $Rec.nits)
}

function Write-RunSummary {
    # The short block the conductor reads instead of the full output.
    param($Ctx, $State)
    $cur = [int]$State.round
    Write-Host ""
    Write-Host "=== SUMMARY ===" -ForegroundColor White
    Write-Host ("task {0} | repo {1} | status {2} | plan v{3} | branch {4}" -f $State.id, $State.repo, $State.status, $cur, $(if ($State.branch) { $State.branch } else { '-' }))
    if ($State.plan) { Write-Host ("plan:     v{0} by {1}:{2} [{3}] verdict={4} {5}" -f $cur, $State.plan.adapter, $State.plan.model, $State.plan.family, $State.plan.verdict, $State.plan.message) }
    if ($State.review) {
        $stale = if ($State.review.PSObject.Properties['round'] -and $State.review.round -ne $cur) { " (STALE: for v$($State.review.round))" } else { '' }
        Write-Host ("review:   {0} by {1}:{2}{3}" -f (Format-VerdictHead $State.review), $State.review.adapter, $State.review.model, $stale)
    }
    if ($State.PSObject.Properties['readback'] -and $State.readback) {
        $stale = if ($State.readback.round -ne $cur) { " (STALE: for v$($State.readback.round))" } else { '' }
        Write-Host ("readback: {0} by {1}:{2}{3}" -f (Format-VerdictHead $State.readback), $State.readback.adapter, $State.readback.model, $stale)
    }
    foreach ($p in @($State.phases | Where-Object { $_ })) {
        Write-Host ("phase {0}:  {1} by {2} | changed {3} | not in plan: {4} | violations: {5}" -f $p.phase, $p.verdict, $p.model, @($p.changed).Count, $(if (@($p.outsidePlan).Count) { @($p.outsidePlan) -join ', ' } else { 'none' }), $(if (@($p.violations).Count) { @($p.violations) -join '; ' } else { 'none' }))
    }
    if ($State.verify) { Write-Host ("verify:   {0} (gates {1}, model said {2})" -f $State.verify.verdict, $State.verify.gates, $State.verify.modelVerdict) }
    $id = $State.id
    $next = switch ($State.status) {
        'NeedsInput'       { "answer: revise $id `"<answer>`"  (or revise $id -PlanFile <fixed plan>)" }
        'AwaitingApproval' { "approve $id [`"notes`"]  |  revise $id -PlanFile <v$($cur + 1)>  |  revise $id `"notes`"" }
        'Approved'         { "implement $id 1" }
        'Implemented'      { "verify $id" }
        'Verified'         { "close by hand (commit? merge? delete branch?) or implement $id <next phase>" }
        'VerifyFailed'     { "fix, or implement $id <phase> again, then verify $id" }
        'Violation'        { "inspect git, fix by hand; then verify $id or implement $id <phase> -Force" }
        default            { "status $id" }
    }
    Write-Host "next:     $next"
    Write-Host "files:    $(Join-Path (Get-TaskDir $Ctx $id) 'artifacts')"
}

function Get-OpenCodeSessionUsage {
    # Ledger: opencode keeps each session's token totals; `opencode session export <id>` returns them in
    # info.tokens. Any failure returns $null - the ledger then shows "-" for that session.
    param([string]$SessionId, [string]$Cwd)
    try {
        $r = Invoke-External -Exe 'opencode' -ArgList @('session', 'export', $SessionId) -Cwd $Cwd -TimeoutSec 120
        if ($r.ExitCode -ne 0 -or $r.TimedOut -or -not $r.Stdout) { return $null }
        $j = $r.Stdout | ConvertFrom-Json
        $t = $j.info.tokens
        if (-not $t) { return $null }
        $read = if ($t.PSObject.Properties['cache'] -and $t.cache) { [long]$t.cache.read } else { 0 }
        $write = if ($t.PSObject.Properties['cache'] -and $t.cache -and $t.cache.PSObject.Properties['write']) { [long]$t.cache.write } else { 0 }
        # One "assistant" message per model request (step): the Go plan also caps requests, not only tokens.
        $requests = @($j.messages | Where-Object { $_.type -eq 'assistant' }).Count
        # Turns: each orchestrator call is one user message; the assistant messages after it, up to the next
        # user message, are that call's requests. Per turn: requests, tokens and wall time (user message ->
        # last completed answer), so the ledger can split a session that served several roles.
        $turns = @()
        $cur = $null
        foreach ($m in @($j.messages)) {
            if ($m.type -eq 'user') {
                if ($cur) { $turns += [pscustomobject]$cur }
                $cur = [ordered]@{ requests = 0L; input = 0L; output = 0L; reasoning = 0L; cacheRead = 0L; cacheWrite = 0L; start = [long]$m.time.created; end = [long]$m.time.created }
                continue
            }
            if ($m.type -ne 'assistant' -or -not $cur) { continue }
            $cur.requests++
            if ($m.tokens) {
                $cur.input += [long]$m.tokens.input; $cur.output += [long]$m.tokens.output; $cur.reasoning += [long]$m.tokens.reasoning
                if ($m.tokens.cache) { $cur.cacheRead += [long]$m.tokens.cache.read; $cur.cacheWrite += [long]$m.tokens.cache.write }
            }
            $done = if ($m.time.completed) { [long]$m.time.completed } elseif ($m.time.created) { [long]$m.time.created } else { 0L }
            if ($done -gt $cur.end) { $cur.end = $done }
        }
        if ($cur) { $turns += [pscustomobject]$cur }
        $turns = @($turns | ForEach-Object { $_ | Add-Member -NotePropertyName seconds -NotePropertyValue ([int][Math]::Round(($_.end - $_.start) / 1000.0)) -PassThru })
        return [pscustomobject]@{ requests = [long]$requests; input = [long]$t.input; output = [long]$t.output; reasoning = [long]$t.reasoning; cacheRead = $read; cacheWrite = $write; turns = $turns }
    }
    catch { return $null }
}

function Format-TokenCount {
    param([long]$N)
    if ($N -ge 1000000) { return ('{0:0.0}m' -f ($N / 1000000)) }
    if ($N -ge 1000) { return ('{0:0.0}k' -f ($N / 1000)) }
    return "$N"
}

function ConvertFrom-OpenCodeRateData {
    # opencode.ai/data/index.json -> @{ '<bare model>' = { input; output; cacheRead; cacheWrite } }, USD per 1M.
    # The site keys models without a provider (glm-5.3-flash) and has no cache-write price.
    param($Data)
    $rates = @{}
    foreach ($e in @($Data.tokenPricesUsdPer1M | Where-Object { $_ -and $_.model })) {
        $num = { param($v) if ($null -ne $v) { [double]$v } else { 0.0 } }
        $rates[[string]$e.model] = [pscustomobject]@{ input = (& $num $e.input); output = (& $num $e.output); cacheRead = (& $num $e.cachedInput); cacheWrite = 0.0 }
    }
    return $rates
}

function Get-LiveModelRates {
    # Prices from `modelRatesSource.url`, fetched at most once per maxAgeHours and cached in
    # runs/_model-rates.json. No network, a bad answer or an empty list falls back to the cache (any age),
    # then to nothing, so the caller uses the static modelRates. Never throws; memoised per invocation.
    # PROJECTA_ORCH_NO_RATE_FETCH=1 (the offline suite) reads the cache only.
    param($Ctx)
    if ($script:LiveRatesLoaded) { return $script:LiveRates }
    $script:LiveRatesLoaded = $true
    $script:LiveRates = $null
    $script:LiveRatesNote = $null
    try {
        $src = if ($Ctx.Config.PSObject.Properties['modelRatesSource']) { $Ctx.Config.modelRatesSource } else { $null }
        if (-not $src -or -not $src.url -or ($src.PSObject.Properties['enabled'] -and -not $src.enabled)) { return $null }
        $maxAge = if ($src.PSObject.Properties['maxAgeHours'] -and $src.maxAgeHours) { [double]$src.maxAgeHours } else { 24.0 }
        $timeout = if ($src.PSObject.Properties['timeoutSec'] -and $src.timeoutSec) { [int]$src.timeoutSec } else { 15 }
        $cacheFile = Join-Path $Ctx.RunsDir '_model-rates.json'
        $cached = $null
        if (Test-Path -LiteralPath $cacheFile) {
            try { $cached = Get-Content -LiteralPath $cacheFile -Raw -Encoding UTF8 | ConvertFrom-Json } catch { $cached = $null }
        }
        $fetchedAt = $null
        if ($cached -and $cached.fetchedAt) { try { $fetchedAt = [datetime]::Parse([string]$cached.fetchedAt, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind) } catch { $fetchedAt = $null } }
        $fresh = $fetchedAt -and (((Get-Date) - $fetchedAt).TotalHours -lt $maxAge)
        if (-not $fresh -and $env:PROJECTA_ORCH_NO_RATE_FETCH -ne '1') {
            try {
                [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
                $resp = Invoke-WebRequest -Uri $src.url -UseBasicParsing -TimeoutSec $timeout -ErrorAction Stop
                $data = $resp.Content | ConvertFrom-Json
                $parsed = ConvertFrom-OpenCodeRateData $data
                if ($parsed.Count) {
                    $obj = [ordered]@{ fetchedAt = (Get-Date).ToString('o'); source = [string]$src.url; updatedAt = [string]$data.updatedAt; rates = [pscustomobject]$parsed }
                    New-Item -ItemType Directory -Force -Path $Ctx.RunsDir | Out-Null
                    ([pscustomobject]$obj | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $cacheFile -Encoding UTF8
                    $cached = [pscustomobject]$obj
                    $fetchedAt = Get-Date
                }
            }
            catch { Write-Host "  [rates] live prices not fetched ($($_.Exception.Message)); using $(if ($cached) { 'the cached copy' } else { 'orchestrator.json modelRates' })" -ForegroundColor DarkYellow }
        }
        if (-not $cached -or -not $cached.rates) { return $null }
        $rates = @{}
        foreach ($p in $cached.rates.PSObject.Properties) { if ($p.Value -and $p.Value -isnot [string]) { $rates[$p.Name] = $p.Value } }
        if (-not $rates.Count) { return $null }
        $script:LiveRates = $rates
        $when = if ($fetchedAt) { $fetchedAt.ToString('yyyy-MM-dd HH:mm') } else { '?' }
        $script:LiveRatesNote = "opencode.ai/data prices fetched $when (orchestrator.json modelRates for models it does not list)"
        return $rates
    }
    catch { return $null }
}

function Get-RateSourceNote {
    # Where the costs came from, for the ledger line under the session table.
    param($Ctx)
    [void](Get-LiveModelRates $Ctx)
    if ($script:LiveRatesNote) { return $script:LiveRatesNote }
    # Plan 0006 D9: with a profile loaded, the static rates are its `modelRates`. Without one the text is
    # exactly as before, which is what T-L2 asserts.
    if ($Ctx.PSObject.Properties['ProfileId'] -and $Ctx.ProfileId) { return ("profile {0} modelRates" -f $Ctx.ProfileId) }
    return 'orchestrator.json modelRates'
}

function Write-ProfileVersionLine {
    # Plan 0006 D9: the run keeps the config it started on. When the profile has moved on since, say so in
    # one line and go on with the current version.
    param($Ctx, [string]$TaskId)
    if (-not $TaskId) { return }
    if (-not ($Ctx.PSObject.Properties['ProfileId']) -or -not $Ctx.ProfileId) { return }
    if (-not $Ctx.PSObject.Properties['ProfileVersion'] -or $null -eq $Ctx.ProfileVersion) { return }
    $file = Join-Path (Get-TaskDir $Ctx $TaskId) 'config.effective.json'
    if (-not (Test-Path -LiteralPath $file)) { return }
    try { $recorded = Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return }
    $start = $recorded.PSObject.Properties['profileVersion']
    if (-not $start -or $null -eq $start.Value) { return }
    if ([int]$start.Value -ne [int]$Ctx.ProfileVersion) {
        Write-Host ("profile {0} is v{1} now; this run started on v{2}" -f $Ctx.ProfileId, $Ctx.ProfileVersion, [int]$start.Value) -ForegroundColor Yellow
    }
}

function Get-ModelRate {
    # USD per 1,000,000 tokens. First the live opencode.ai/data price (Get-LiveModelRates), looked up by the
    # model name without its provider (opencode-go/glm-5.3-flash -> glm-5.3-flash); else orchestrator.json
    # `modelRates`, keyed by the exact model string the ledger writes. $null when neither has the model.
    param($Ctx, [string]$Model)
    if (-not $Model) { return $null }
    $live = Get-LiveModelRates $Ctx
    if ($live) {
        $bare = ($Model -split '/')[-1]
        if ($live.ContainsKey($Model)) { return $live[$Model] }
        if ($live.ContainsKey($bare)) { return $live[$bare] }
    }
    if (-not $Ctx.Config.PSObject.Properties['modelRates'] -or -not $Ctx.Config.modelRates) { return $null }
    $p = $Ctx.Config.modelRates.PSObject.Properties[$Model]
    if (-not $p -or -not $p.Value -or $p.Value -is [string]) { return $null }
    return $p.Value
}

function Get-UsageCost {
    # cost = input*in + (output + reasoning)*out + cacheRead*cacheRead + cacheWrite*cacheWrite, rates per 1M.
    # Reasoning tokens are billed at the output rate.
    param($Rate, [long]$In, [long]$Out, [long]$Reasoning, [long]$CacheRead, [long]$CacheWrite = 0)
    $r = { param($n) if ($Rate.PSObject.Properties[$n] -and $null -ne $Rate.$n) { [double]$Rate.$n } else { 0.0 } }
    return (($In * (& $r 'input')) + (($Out + $Reasoning) * (& $r 'output')) + ($CacheRead * (& $r 'cacheRead')) + ($CacheWrite * (& $r 'cacheWrite'))) / 1000000.0
}

function Format-Usd {
    param([double]$Usd)
    return '$' + $Usd.ToString('0.000', [Globalization.CultureInfo]::InvariantCulture)
}

function Format-CostTotal {
    # A total with unpriced models is a lower bound, never a silent 0.
    param([double]$Usd, [int]$Unpriced)
    if ($Unpriced -gt 0) { return ">= $(Format-Usd $Usd) ($Unpriced model(s) unpriced)" }
    return (Format-Usd $Usd)
}

function Format-TokenUnit {
    # The day block: M = million tokens, K = thousand tokens (never "8m", which reads as minutes).
    param([long]$N)
    $c = [Globalization.CultureInfo]::InvariantCulture
    if ($N -ge 1000000) { return ($N / 1000000.0).ToString('0.0', $c) + 'M' }
    if ($N -ge 1000) { return ($N / 1000.0).ToString('0.0', $c) + 'K' }
    return "$N"
}

function Get-RoleLabel {
    # A transcript name or a session's first role -> the stage the owner reads: review, read-back, implement, verify.
    param([string]$Name)
    switch -Wildcard ($Name) {
        'plan-reviewer*' { return 'review' }
        'readback*'      { return 'read-back' }
        'implementer*'   { return 'implement' }
        'verifier*'      { return 'verify' }
        'planner*'       { return 'plan' }
        default          { return $Name }
    }
}

function Get-SessionHopRoles {
    # The roles of the calls one session served, in call order: every hop's transcript names its role and
    # the session it ran in, and transcripts are written once per call, so their write time orders them.
    param($Ctx, $State, [string]$SessionId)
    $dir = Join-Path (Get-TaskDir $Ctx $State.id) 'transcript'
    if (-not (Test-Path -LiteralPath $dir)) { return @() }
    $hits = foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter '*.json' -File | Sort-Object LastWriteTimeUtc)) {
        try {
            $j = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($j.PSObject.Properties['SessionId'] -and [string]$j.SessionId -eq $SessionId) { Get-RoleLabel $f.BaseName }
        }
        catch { }
    }
    return @($hits)
}

function Get-RoleUsageRows {
    # One row per stage and model: sessions, calls (hops), requests, tokens, wall time and cost. A session
    # that served several stages (read-back -> implement, review -> verify) is split by its turns.
    param($Ctx, $State, [hashtable]$Usage)
    $rows = [ordered]@{}
    foreach ($sid in @($Usage.Keys)) {
        $rec = $State.sessions.$sid
        $u = $Usage[$sid]
        $roles = @(Get-SessionHopRoles -Ctx $Ctx -State $State -SessionId $sid)
        # @() outside the if: PowerShell 5.1 unrolls a one-turn array to a bare object, which has no .Count.
        $turns = @(if ($u.PSObject.Properties['turns']) { $u.turns })
        if (-not $turns.Count) {
            # No per-turn data: the whole session goes to its first role.
            $turns = @([pscustomobject]@{ requests = $u.requests; input = $u.input; output = $u.output; reasoning = $u.reasoning; cacheRead = $u.cacheRead; cacheWrite = $(if ($u.PSObject.Properties['cacheWrite']) { $u.cacheWrite } else { 0L }); seconds = -1; calls = [int]$rec.turns })
        }
        for ($i = 0; $i -lt $turns.Count; $i++) {
            $t = $turns[$i]
            $role = if ($i -lt $roles.Count) { $roles[$i] } else { Get-RoleLabel ([string]$rec.role) }
            $key = "$role|$($rec.model)"
            if (-not $rows.Contains($key)) {
                $rows[$key] = [ordered]@{ role = $role; model = [string]$rec.model; sessions = @{}; calls = 0; requests = 0L; input = 0L; output = 0L; reasoning = 0L; cacheRead = 0L; cacheWrite = 0L; seconds = 0; timed = $true }
            }
            $r = $rows[$key]
            $r.sessions[$sid] = $true
            $r.calls += $(if ($t.PSObject.Properties['calls']) { [int]$t.calls } else { 1 })
            foreach ($k in 'requests', 'input', 'output', 'reasoning', 'cacheRead', 'cacheWrite') { $r[$k] += [long]$t.$k }
            if ([int]$t.seconds -ge 0) { $r.seconds += [int]$t.seconds } else { $r.timed = $false }
        }
    }
    $order = @{ 'plan' = 0; 'review' = 1; 'read-back' = 2; 'implement' = 3; 'verify' = 4 }
    return @($rows.Values | ForEach-Object { [pscustomobject]$_ } | Sort-Object @{ e = { if ($order.ContainsKey($_.role)) { $order[$_.role] } else { 9 } } }, model)
}

function Format-RowCost {
    param($Ctx, $Row)
    $rate = Get-ModelRate $Ctx $Row.model
    if (-not $rate) { return $null }
    return (Get-UsageCost -Rate $rate -In $Row.input -Out $Row.output -Reasoning $Row.reasoning -CacheRead $Row.cacheRead -CacheWrite $Row.cacheWrite)
}

function Write-LedgerEntry {
    # Ledger: one block per verify and per close, appended to runs/_ledger.md: verdicts, models, calls and
    # opencode's token totals per session. Returns the per-session usage it read (session id -> usage), or
    # $null when the block could not be written.
    param($Ctx, $State, [string]$Event)
    try {
        $path = Join-Path $Ctx.RunsDir '_ledger.md'
        $stageText = ''
        $usage = @{}
        $totals = $null
        $cwd = Get-AgentCwd $Ctx $State.repo
        $sb = New-Object System.Text.StringBuilder
        $branch = if ($State.PSObject.Properties['branch'] -and $State.branch) { $State.branch } else { '-' }
        [void]$sb.AppendLine("## $((Get-Date).ToString('yyyy-MM-dd HH:mm')) - $($State.id) - $Event")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("repo $($State.repo) | status $($State.status) | plan v$([int]$State.round) | branch $branch")
        [void]$sb.AppendLine()
        if ($State.PSObject.Properties['review'] -and $State.review) { [void]$sb.AppendLine("- review: $(Format-VerdictHead $State.review) by $($State.review.adapter):$($State.review.model) (round $($State.review.round))") }
        if ($State.PSObject.Properties['readback'] -and $State.readback) { [void]$sb.AppendLine("- read-back: $(Format-VerdictHead $State.readback) by $($State.readback.adapter):$($State.readback.model) (round $($State.readback.round))") }
        foreach ($p in @($State.phases | Where-Object { $_ })) {
            [void]$sb.AppendLine("- phase $($p.phase): $($p.verdict) by $($p.model) | changed $(@($p.changed).Count) | not in plan $(@($p.outsidePlan).Count) | violations $(@($p.violations).Count)")
        }
        if ($State.PSObject.Properties['verify'] -and $State.verify) { [void]$sb.AppendLine("- verify: $($State.verify.verdict) (gates $($State.verify.gates), model said $($State.verify.modelVerdict)) by $($State.verify.adapter):$($State.verify.model)") }
        $ids = @()
        if ($State.PSObject.Properties['sessions'] -and $State.sessions) { $ids = @($State.sessions.PSObject.Properties.Name) }
        if ($ids.Count) {
            [void]$sb.AppendLine()
            [void]$sb.AppendLine('| session | first role | model | calls | requests | input | output | reasoning | cache read | cost |')
            [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|---|---|')
            $tot = @{ requests = 0L; input = 0L; output = 0L; reasoning = 0L; cacheRead = 0L; calls = 0 }
            $totals = $tot
            $costTotal = 0.0
            $unpriced = @{}
            $usage = @{}
            foreach ($sid in $ids) {
                $rec = $State.sessions.$sid
                $u = if ($rec.adapter -eq 'opencode') { Get-OpenCodeSessionUsage -SessionId $sid -Cwd $cwd } else { $null }
                $tot.calls += [int]$rec.turns
                if ($u) {
                    $usage[$sid] = $u
                    foreach ($k in 'requests', 'input', 'output', 'reasoning', 'cacheRead') { $tot[$k] += $u.$k }
                    $rate = Get-ModelRate $Ctx ([string]$rec.model)
                    $cw = if ($u.PSObject.Properties['cacheWrite']) { [long]$u.cacheWrite } else { 0L }
                    $costCell = '?'
                    if ($rate) {
                        $c = Get-UsageCost -Rate $rate -In $u.input -Out $u.output -Reasoning $u.reasoning -CacheRead $u.cacheRead -CacheWrite $cw
                        $costTotal += $c
                        $costCell = Format-Usd $c
                    }
                    else { $unpriced[[string]$rec.model] = $true }
                    [void]$sb.AppendLine("| $sid | $($rec.role) | $($rec.model) | $($rec.turns) | $($u.requests) | $(Format-TokenCount $u.input) | $(Format-TokenCount $u.output) | $(Format-TokenCount $u.reasoning) | $(Format-TokenCount $u.cacheRead) | $costCell |")
                }
                else { [void]$sb.AppendLine("| $sid | $($rec.role) | $($rec.model) | $($rec.turns) | - | - | - | - | - | - |") }
            }
            [void]$sb.AppendLine("| **total** | | | $($tot.calls) | $($tot.requests) | $(Format-TokenCount $tot.input) | $(Format-TokenCount $tot.output) | $(Format-TokenCount $tot.reasoning) | $(Format-TokenCount $tot.cacheRead) | $(Format-CostTotal $costTotal $unpriced.Count) |")
            [void]$sb.AppendLine()
            [void]$sb.AppendLine('Hops without a session (claude, codebuddy, manual) are not in this table. Token counts: m = million, k = thousand. Cost: USD from ' + (Get-RateSourceNote $Ctx) + '.')
            # By stage: how many sessions, calls and requests each stage took, on which model, how long, at what
            # cost. Its own try: a failure here must never cost the session table above.
            if ($usage.Count) { try {
                $stageFrom = $sb.Length
                $rows = @(Get-RoleUsageRows -Ctx $Ctx -State $State -Usage $usage)
                [void]$sb.AppendLine()
                [void]$sb.AppendLine('| stage | model | sessions | calls | requests | input | output | reasoning | cache read | time | cost |')
                [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|---|---|---|')
                $byModel = [ordered]@{}
                $allSecs = 0; $allTimed = $true
                foreach ($r in $rows) {
                    $c = Format-RowCost $Ctx $r
                    $time = if ($r.timed) { Format-Duration $r.seconds } else { '-' }
                    $allSecs += $r.seconds; if (-not $r.timed) { $allTimed = $false }
                    [void]$sb.AppendLine("| $($r.role) | $($r.model) | $($r.sessions.Count) | $($r.calls) | $($r.requests) | $(Format-TokenCount $r.input) | $(Format-TokenCount $r.output) | $(Format-TokenCount $r.reasoning) | $(Format-TokenCount $r.cacheRead) | $time | $(if ($null -ne $c) { Format-Usd $c } else { '?' }) |")
                    if (-not $byModel.Contains($r.model)) { $byModel[$r.model] = [ordered]@{ model = $r.model; input = 0L; output = 0L; reasoning = 0L; cacheRead = 0L; cacheWrite = 0L } }
                    foreach ($k in 'input', 'output', 'reasoning', 'cacheRead', 'cacheWrite') { $byModel[$r.model][$k] += $r.$k }
                }
                [void]$sb.AppendLine()
                [void]$sb.AppendLine('```')
                $sum = 0.0; $unp = 0
                foreach ($m in $byModel.Values) {
                    $mr = [pscustomobject]$m
                    $c = Format-RowCost $Ctx $mr
                    if ($null -ne $c) { $sum += $c } else { $unp++ }
                    [void]$sb.AppendLine(('{0,-34} in {1,-7} out {2,-7} reason {3,-7} cache {4,-7} {5}' -f $mr.model, (Format-TokenUnit $mr.input), (Format-TokenUnit $mr.output), (Format-TokenUnit $mr.reasoning), (Format-TokenUnit $mr.cacheRead), $(if ($null -ne $c) { Format-Usd $c } else { '?' })))
                }
                $wall = if ($allTimed) { "time $(Format-Duration $allSecs)  " } else { '' }
                [void]$sb.AppendLine(('{0,-34} {1}{2}' -f 'total', $wall, (Format-CostTotal $sum $unp)))
                [void]$sb.AppendLine('```')
                $stageText = $sb.ToString().Substring($stageFrom)
            }
            catch {
                $sb.Length = $stageFrom
                Write-Host "  [ledger] stage table skipped: $($_.Exception.Message)" -ForegroundColor DarkYellow
            } }
        }
        [void]$sb.AppendLine()
        Add-Content -LiteralPath $path -Value $sb.ToString() -Encoding UTF8
        if ($stageText) {
            Write-Host ""
            Write-Host "=== this run's usage by stage (opencode sessions; time = wall time of the calls) ===" -ForegroundColor White
            foreach ($line in ($stageText -split "`r?`n")) { if ($line.Trim() -and $line -ne '```') { Write-Host $line } }
        }
        Write-Host "ledger:   $path" -ForegroundColor DarkGray
        # Plan 0004 §6.1: the dashboard gets the same totals the ledger just wrote.
        Send-OrchestraEvent -Ctx $Ctx -State $State -Kind 'ledger.written' -Data ([ordered]@{ kind = $Event; usage = $totals })
        # The archive (close) reuses the usage just read, so opencode is asked once per session.
        return $usage
    }
    catch { Write-Host "  [ledger] not written: $($_.Exception.Message)" -ForegroundColor DarkYellow }
}

function Write-RunArchive {
    # close: one file per run, runs/_archive/<id>.<yyyyMMddHHmm>.json - task.json, every review / read-back text, the verify
    # text and the session usage - for Project Orchestra's dashboard until it records runs live. Plan
    # versions, prompts and transcripts are left out (the approved plan is filed in the repo's done/).
    # Config `archive.enabled` (default true). Never throws: a failure is one line and close goes on.
    param($Ctx, [string]$TaskId, $Usage)
    try {
        if (-not [bool](Get-CfgValue $Ctx 'archive' 'enabled' $true)) { return }
        $dir = Get-TaskDir $Ctx $TaskId
        # [IO.File] reads, not Get-Content: a Get-Content string carries PSPath/PSDrive/PSProvider note
        # properties, and ConvertTo-Json -Depth 30 walks into the provider objects and never finishes.
        $task = [IO.File]::ReadAllText((Join-Path $dir 'task.json')) | ConvertFrom-Json
        $texts = {
            param([string]$Folder, [string]$Filter)
            $out = @()
            if (Test-Path -LiteralPath $Folder) {
                foreach ($f in @(Get-ChildItem -LiteralPath $Folder -Filter $Filter -File | Sort-Object Name)) {
                    $m = [regex]::Match($f.Name, '\.v(\d+)\.md$')
                    $round = if ($m.Success) { [int]$m.Groups[1].Value } else { $null }
                    $out += [pscustomobject]@{ name = $f.Name; round = $round; text = [IO.File]::ReadAllText($f.FullName) }
                }
            }
            return , $out
        }
        $art = Join-Path $dir 'artifacts'
        # Plan 0006 D10: the config the run started with travels with the archive, so the dashboard can show
        # it for a closed run without the run folder.
        $effective = $null
        $effectiveFile = Join-Path $dir 'config.effective.json'
        if (Test-Path -LiteralPath $effectiveFile) {
            try {
                $recorded = [IO.File]::ReadAllText($effectiveFile) | ConvertFrom-Json
                if ($recorded.PSObject.Properties['config']) { $effective = $recorded.config }
            }
            catch { $effective = $null }
        }
        $u = [ordered]@{}
        # Anything else a callee wrote to the pipeline rides along with the ledger's return value; keep the map.
        if ($Usage -and $Usage -isnot [System.Collections.IDictionary]) { $Usage = @(@($Usage) | Where-Object { $_ -is [System.Collections.IDictionary] }) | Select-Object -Last 1 }
        if ($Usage) { foreach ($k in @($Usage.Keys | Sort-Object)) { $u[$k] = $Usage[$k] } }
        $obj = [ordered]@{
            schema     = 1
            archivedAt = (Get-Date).ToString('o')
            project    = (Split-Path -Leaf $Ctx.Root)
            task       = $task
            reviews    = (& $texts $art 'plan.findings.v*.md')
            readbacks  = (& $texts $art 'plan.readback.v*.md')
            verify     = (& $texts (Join-Path (Join-Path $dir 'outbox') 'verifier') 'verify.md')
            usage      = [pscustomobject]$u
            effectiveConfig = $effective
        }
        $archDir = Join-Path $Ctx.RunsDir '_archive'
        if (-not (Test-Path -LiteralPath $archDir -PathType Container)) { New-Item -ItemType Directory -Path $archDir -ErrorAction Stop | Out-Null }
        # A task id is reused after close, so the name carries the close time: each run keeps its own file.
        $file = Join-Path $archDir ("{0}.{1}.json" -f $TaskId, (Get-Date).ToString('yyyyMMddHHmm'))
        $tmp = "$file.tmp"
        [IO.File]::WriteAllText($tmp, ([pscustomobject]$obj | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding($false)))
        Move-Item -LiteralPath $tmp -Destination $file -Force
        Write-Host "archived: $file" -ForegroundColor DarkGray
    }
    catch { Write-Host "  [archive] not written: $($_.Exception.Message)" -ForegroundColor DarkYellow }
}

function Format-OpenCodeDayStats {
    # The day block from `opencode stats --json`: one line per model, tokens with their unit, and a cost
    # that opencode reports as 0 for Go-plan models recomputed from modelRates. Returns the lines.
    param($Ctx, $Stats)
    $lines = @()
    $lines += '=== opencode usage, ALL sessions on this machine, last 24h (not this run; this run is the ledger table above) ==='
    $lines += 'tokens: M = million, K = thousand | cost: USD'
    $fmt = '{0,-34} {1,9} {2,9} {3,9} {4,11} {5,9}'
    $lines += ($fmt -f 'model', 'in', 'out', 'reason', 'cache read', 'cost')
    $total = 0.0
    $unpriced = 0
    $computed = $false
    foreach ($m in @($Stats.models | Where-Object { $_ })) {
        $name = "$($m.model.providerID)/$($m.model.id)"
        $t = $m.tokens
        $read = if ($t.cache) { [long]$t.cache.read } else { 0L }
        $write = if ($t.cache -and $t.cache.PSObject.Properties['write']) { [long]$t.cache.write } else { 0L }
        $reported = if ($null -ne $m.cost) { [double]$m.cost } else { 0.0 }
        $rate = Get-ModelRate $Ctx $name
        if ($reported -gt 0) { $cost = $reported; $cell = Format-Usd $cost }
        elseif ($rate) {
            $cost = Get-UsageCost -Rate $rate -In ([long]$t.input) -Out ([long]$t.output) -Reasoning ([long]$t.reasoning) -CacheRead $read -CacheWrite $write
            $cell = (Format-Usd $cost) + '*'
            $computed = $true
        }
        else { $cost = 0.0; $cell = '?'; $unpriced++ }
        $total += $cost
        $lines += ($fmt -f $name, (Format-TokenUnit ([long]$t.input)), (Format-TokenUnit ([long]$t.output)), (Format-TokenUnit ([long]$t.reasoning)), (Format-TokenUnit $read), $cell)
    }
    $lines += ($fmt -f 'total', '', '', '', '', (Format-CostTotal $total $unpriced))
    if ($computed) { $lines += '* opencode reports $0.00 for Go-plan models; the cost shown is computed from ' + (Get-RateSourceNote $Ctx) + '.' }
    return $lines
}

function Write-OpenCodeStats {
    # The owner reads opencode's own usage after a verify or a close: the last day's tokens and cost by model
    # across EVERY session on the machine (--days 1, so a run that crossed midnight is still covered). It is
    # not this run's usage - the ledger table is - and the title says so.
    param($Ctx)
    # The offline suite (MOCK_LOG set) and a machine without opencode skip it: nothing real to report.
    if ($env:MOCK_LOG -or -not (Get-Command 'opencode' -ErrorAction SilentlyContinue)) { return }
    try {
        $r = Invoke-External -Exe 'opencode' -ArgList @('stats', '--days', '1', '--models', '--cost', '--json') -Cwd $Ctx.Root -TimeoutSec 60
        if ($r.ExitCode -ne 0) { Write-Host "  [opencode stats] exit $($r.ExitCode)" -ForegroundColor DarkYellow; return }
        $stats = [string]$r.Stdout | ConvertFrom-Json
        Write-Host ""
        $first = $true
        foreach ($line in (Format-OpenCodeDayStats -Ctx $Ctx -Stats $stats)) {
            if ($first) { Write-Host $line -ForegroundColor White; $first = $false } else { Write-Host $line }
        }
    }
    catch { Write-Host "  [opencode stats] failed: $($_.Exception.Message)" -ForegroundColor DarkYellow }
}

function Get-FamilyFromAuthor {
    # A model family from free text such as "Claude Opus 5.5 (planner seat)". $null when no known model
    # is named: a bare "Claude", a person, or the template placeholder.
    param([string]$Text)
    $t = ([string]$Text).ToLower()
    foreach ($m in @(
            @('opus', 'claude-opus'), @('sonnet', 'claude-sonnet'), @('haiku', 'claude-haiku'), @('fable', 'claude-fable'),
            @('deepseek', 'deepseek'), @('glm', 'glm'), @('mimo', 'mimo'), @('kimi', 'kimi'), @('qwen', 'qwen'),
            @('minimax', 'minimax'), @('gemini', 'gemini'), @('codex', 'gpt'), @('gpt', 'gpt'))) {
        if ($t -match ('\b' + [regex]::Escape($m[0]))) { return $m[1] }
    }
    return $null
}

function Resolve-PlanFamily {
    # Who wrote the plan is read from the plan itself - its `**Planned by:**` header line - not from
    # whichever model happens to run the conductor. Gate 2 excludes this family from the review.
    #  - header names a model: it wins; a different -PlanFamily is a small slip, fixed with a note;
    #  - no usable header, -PlanFamily given: used, with a warning to add the line;
    #  - no usable header, no flag, a previous conductor version: its family is kept, with a note;
    #  - none of these: stop - the planner has to put the author in the plan.
    param([string]$PlanText, [string]$Given, [bool]$GivenExplicit, [string]$Previous)
    $raw = ''
    $m = [regex]::Match([string]$PlanText, '(?im)^\s*\**\s*Planned by:?\s*\**\s*:?\s*(.+?)\s*$')
    if ($m.Success) { $raw = $m.Groups[1].Value.Trim() }
    $hdr = if ($raw) { Get-FamilyFromAuthor $raw } else { $null }
    if ($hdr) {
        if ($GivenExplicit -and $Given -and $Given.ToLower() -ne $hdr) {
            Write-Host "note: the plan says 'Planned by: $raw' -> family '$hdr'; -PlanFamily $Given ignored." -ForegroundColor DarkYellow
        }
        return $hdr
    }
    $seen = if ($raw) { "'$raw'" } else { 'none' }
    if ($GivenExplicit -and $Given) {
        Write-Host "warning: the plan names no known model in its **Planned by:** line (found: $seen); using -PlanFamily $Given. Add the author to the plan header." -ForegroundColor Yellow
        return $Given.ToLower()
    }
    if ($Previous) {
        Write-Host "note: this version names no known model in **Planned by:** (found: $seen); keeping the previous family '$Previous'." -ForegroundColor DarkYellow
        return $Previous
    }
    throw ("the plan does not say who wrote it: no **Planned by:** line naming a model (found: $seen). " +
        "Gate 2 must exclude the author's family from the review, so this run stops here. Planner: add " +
        "'**Planned by:** <model>, <date>' to the plan header (e.g. 'Claude Opus 5.5') and rerun; pass " +
        "-PlanFamily <family> only when the author is not a known model.")
}

function Cmd-New {
    param($Ctx, [string]$TaskId, [string]$Repo, [string]$TaskText, [string]$PlanFile, [string]$PlanFamily, [switch]$AutoRevise, [switch]$NoReadBack, [switch]$DryRun)
    if (-not $TaskId -or -not $Repo -or -not $TaskText) { throw 'usage: run.ps1 new <taskId> <repo> "<task>" [-PlanFile plan.md]' }
    if (-not (Test-RepoKnown $Ctx $Repo)) { throw "unknown repo '$Repo' (have: $($Ctx.Config.repos.PSObject.Properties.Name -join ', '))" }
    if (Test-Path -LiteralPath (Join-Path (Get-TaskDir $Ctx $TaskId) 'task.json')) { throw "run '$TaskId' already exists; use revise/status, or pick a new id" }
    if ($PlanFile -and -not (Test-Path -LiteralPath $PlanFile)) { throw "plan file not found: $PlanFile" }
    $repoPath = Get-RepoPath $Ctx $Repo

    if ($DryRun) {
        $state = New-TaskRun -Ctx $Ctx -TaskId $TaskId -Repo $Repo -Task $TaskText
        $planPrompt = New-RolePrompt -Ctx $Ctx -Role 'planner' -Repo $Repo -Task $TaskText -Extra (New-PlannerExtra $repoPath '' '' '')
        $reviewPrompt = New-RolePrompt -Ctx $Ctx -Role 'plan-reviewer' -Repo $Repo -Task $TaskText -Extra "The plan under review (version 1):`n`n(plan text goes here)"
        $rbPrompt = New-RolePrompt -Ctx $Ctx -Role 'implementer' -Protocol 'readback' -Repo $Repo -Task $TaskText -Extra "The plan you will implement later (version 1):`n`n(plan text goes here)"
        Save-Artifact -Ctx $Ctx -TaskId $TaskId -Name 'preview.plan.prompt' -Content $planPrompt | Out-Null
        Save-Artifact -Ctx $Ctx -TaskId $TaskId -Name 'preview.review.prompt' -Content $reviewPrompt | Out-Null
        Save-Artifact -Ctx $Ctx -TaskId $TaskId -Name 'preview.readback.prompt' -Content $rbPrompt | Out-Null
        Set-Prop $state 'status' 'DryRun'
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
        Write-Host "[dry-run] planner/reviewer/read-back prompts: $($planPrompt.Length) / $($reviewPrompt.Length) / $($rbPrompt.Length) chars" -ForegroundColor Yellow
        Write-Host "[dry-run] wrote artifacts under runs\$TaskId\artifacts\ (no repo files touched, no models called)."
        return
    }

    # Before the run exists: a plan that does not say who wrote it stops here and leaves nothing behind.
    $text = ''
    $family = ''
    if ($PlanFile) {
        $text = Get-Content -LiteralPath $PlanFile -Raw -Encoding UTF8
        $family = Resolve-PlanFamily -PlanText $text -Given $PlanFamily -GivenExplicit $script:PlanFamilyGiven -Previous ''
    }
    $state = New-TaskRun -Ctx $Ctx -TaskId $TaskId -Repo $Repo -Task $TaskText
    # Plan 0004 §6.1: the run exists on disk, so the dashboard hears about it before the first hop.
    Send-OrchestraEvent -Ctx $Ctx -State $state -Kind 'run.created' -Data ([ordered]@{
            repo = [string]$state.repo; task = [string]$state.task; status = [string]$state.status; planFamily = [string]$PlanFamily
        })
    $readBack = (-not $NoReadBack) -and [bool](Get-CfgValue $Ctx 'readback' 'enabled' $true)
    $deferred = ''
    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        if (Get-CfgValue $Ctx 'defaults' 'refreshGraph' $true) {
            # Only the target repo: the planner reads that graph, and a sibling checkout is not ours
            # to write into during this run. refresh-graph.ps1 never runs a full rebuild.
            Write-Host "refreshing code graph..." -ForegroundColor DarkGray
            & (Join-Path $OrchDir 'refresh-graph.ps1') -Config $Ctx.ConfigPath -Repos $Repo
        }
        if ($PlanFile) {
            # Conductor mode: the plan was written in the chat; gate 1 is already done.
            Set-Prop $state 'mode' 'conductor'
            [void](Add-PlanVersion -Ctx $Ctx -State $state -PlanText $text -Adapter 'conductor' -Model 'chat' -Family $family)
            Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
            $deferred = (Invoke-Gate2 -Ctx $Ctx -State $state -ReadBack $readBack).Deferred
            if ($AutoRevise -and -not $deferred) { $deferred = Invoke-ReviseLoop -Ctx $Ctx -State $state -MaxRounds ([int](Get-CfgValue $Ctx 'revise' 'maxAutoRounds' 2)) -ReadBack $readBack }
        }
        else {
            Set-Prop $state 'mode' 'auto'
            if (Invoke-PlannerStep -Ctx $Ctx -State $state) {
                $deferred = (Invoke-Gate2 -Ctx $Ctx -State $state -ReadBack $readBack).Deferred
                if (-not $deferred) { $deferred = Invoke-ReviseLoop -Ctx $Ctx -State $state -MaxRounds ([int](Get-CfgValue $Ctx 'revise' 'maxAutoRounds' 2)) -ReadBack $readBack }
            }
        }
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
        Write-RunSummary $Ctx $state
        # The review is recorded; only the human read-back is still open (exit 3).
        if ($deferred) { throw $deferred }
    }
    finally { Exit-TaskLock $lock }
}

function Cmd-Revise {
    param($Ctx, [string]$TaskId, [string]$Notes, [string]$PlanFile, [switch]$NoReadBack, [switch]$Retry)
    if (-not $TaskId) { throw 'usage: run.ps1 revise <taskId> ["notes"] [-PlanFile v2.md]' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    Write-ProfileVersionLine $Ctx $TaskId
    if (@('AwaitingApproval', 'NeedsInput') -notcontains $state.status) { throw "run '$TaskId' is '$($state.status)'; revise works before approval only" }
    if ($PlanFile -and -not (Test-Path -LiteralPath $PlanFile)) { throw "plan file not found: $PlanFile" }
    $readBack = (-not $NoReadBack) -and [bool](Get-CfgValue $Ctx 'readback' 'enabled' $true)
    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        if ($Notes) {
            $old = Get-StateNotes $state
            Set-Prop $state 'notes' ((@($old, $Notes) | Where-Object { $_ }) -join "`n`n")
        }
        $open = Get-OpenFindings $state
        $ok = $true
        if ($PlanFile) {
            $text = Get-Content -LiteralPath $PlanFile -Raw -Encoding UTF8
            $prev = if ($state.plan -and $state.plan.adapter -eq 'conductor') { [string]$state.plan.family } else { '' }
            $family = Resolve-PlanFamily -PlanText $text -Given $PlanFamily -GivenExplicit $script:PlanFamilyGiven -Previous $prev
            [void](Add-PlanVersion -Ctx $Ctx -State $state -PlanText $text -Adapter 'conductor' -Model 'chat' -Family $family)
        }
        else {
            $prevPlan = Get-Content -LiteralPath $state.plan.artifact -Raw -Encoding UTF8
            $ok = Invoke-PlannerStep -Ctx $Ctx -State $state -PrevPlan $prevPlan -PrevFindings $open -Notes (Get-StateNotes $state)
        }
        $deferred = ''
        if ($ok) {
            $deferred = (Invoke-Gate2 -Ctx $Ctx -State $state -PrevFindings $open -Notes (Get-StateNotes $state) -ReadBack $readBack -Retry:$Retry).Deferred
        }
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
        Write-RunSummary $Ctx $state
        if ($deferred) { throw $deferred }
    }
    finally { Exit-TaskLock $lock }
}

function Cmd-ReadBack {
    param($Ctx, [string]$TaskId, [switch]$Retry)
    if (-not $TaskId) { throw 'usage: run.ps1 readback <taskId>' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    Write-ProfileVersionLine $Ctx $TaskId
    if ($state.status -ne 'AwaitingApproval') { throw "run '$TaskId' is '$($state.status)'; read-back runs before approval" }
    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        Write-DirtyTreeWarning -Ctx $Ctx -Repo $state.repo
        [void](Invoke-ReadBack -Ctx $Ctx -State $state -Retry:$Retry)
        Write-RunSummary $Ctx $state
    }
    finally { Exit-TaskLock $lock }
}

function Format-Duration {
    param([int]$Seconds)
    if ($Seconds -lt 0) { return '0:00' }
    $h = [int][Math]::Floor($Seconds / 3600)
    $m = [int][Math]::Floor(($Seconds % 3600) / 60)
    $s = [int]($Seconds % 60)
    if ($h -gt 0) { return ('{0}:{1:00}:{2:00}' -f $h, $m, $s) }
    return ('{0}:{1:00}' -f $m, $s)
}

function Format-ProgressBlock {
    # One block of `run.ps1 watch`, from one progress.json.
    param($P)
    $sb = New-Object System.Text.StringBuilder
    # "opencode/glm-5.3-flash-free" under the opencode adapter reads better as "opencode:glm-5.3-flash-free".
    $model = [string]$P.model -replace ('^' + [regex]::Escape([string]$P.adapter) + '/'), ''
    [void]$sb.AppendLine(("{0}  {1} -> {2}  {3}:{4}" -f $P.taskId, $P.command, $P.role, $P.adapter, $model).Trim())
    $elapsed = [int]$P.elapsedSec
    $typical = $null
    if ($P.estimate -and $null -ne $P.estimate.typicalSec) { $typical = [int]$P.estimate.typicalSec }
    $line = "  elapsed $(Format-Duration $elapsed)"
    if ($null -ne $typical -and $typical -gt 0) {
        $line += "   typical ~= $(Format-Duration $typical) ($($P.estimate.basis))"
        $pct = [int][Math]::Round(100.0 * $elapsed / $typical)
        if ($P.state -eq 'running') {
            if ($pct -gt 99) { $pct = 99 }
            $line += "   ~$pct%"
        }
        elseif ($elapsed -gt $typical) { $line += "   over by $(Format-Duration ($elapsed - $typical))" }
        else { $line += "   ~$pct%" }
    }
    else { $line += '   typical ~= no history' }
    [void]$sb.AppendLine($line)
    $last = if ($P.lastLine) { $P.lastLine } else { '(no output yet)' }
    [void]$sb.AppendLine("  last output $([int]$P.lastOutputAgoSec) s ago: $last")
    # D10: a hop waiting for its pool says whom it is waiting for, not which phase it thinks it is in.
    if ($P.phase -eq 'queued') { [void]$sb.AppendLine(("  queued for pool {0} ({1})" -f $P.pool, $P.heldBy)) }
    elseif ($P.phase) { [void]$sb.AppendLine("  phase $($P.phase)") }
    if ($P.state -ne 'running' -and $P.state -ne 'queued') { [void]$sb.AppendLine("  state: $($P.state)") }
    return $sb.ToString().TrimEnd()
}

function Get-ResumeCommand {
    # The command that consumes an answer, by what the key is valid for (D7). `revise` cannot resume a
    # review: it creates a new version, so the key it then asks for is v<round+1> and the answer would be
    # orphaned. `review` re-runs gate 2 for the version already on disk.
    param([string]$Key)
    switch (($Key -split '\.')[0]) {
        'plan-reviewer' { return 'review' }
        'readback'      { return 'readback' }
        'verifier'      { return 'verify' }
        'implementer'   { return 'implement' }
        default         { return $null }
    }
}

function Get-ImplementPhase {
    # D8: an implementer manual key is implementer.phase-<n>; the resume command needs that <n>.
    param([string]$Key)
    if ($Key -like 'implementer.phase-*') { return $Key.Substring('implementer.phase-'.Length) }
    return ''
}

function Get-QueuedRuns {
    # D10: runs whose current hop is waiting for a pool slot, read from their progress.json. Filesystem
    # only - no model, no lock, nothing claimed.
    param($Ctx, [string]$TaskId)
    $out = @()
    if (-not (Test-Path -LiteralPath $Ctx.RunsDir)) { return $out }
    $dirs = if ($TaskId) { @(Join-Path $Ctx.RunsDir $TaskId) } else {
        @(Get-ChildItem -LiteralPath $Ctx.RunsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
    }
    foreach ($d in $dirs) {
        $p = Join-Path $d 'progress.json'
        if (-not (Test-Path -LiteralPath $p)) { continue }
        try { $j = Get-Content -LiteralPath $p -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
        if ($j.phase -ne 'queued') { continue }
        $out += [pscustomobject]@{
            TaskId = (Split-Path -Leaf $d)
            Pool   = [string]$j.pool
            HeldBy = [string]$j.heldBy
            Role   = [string]$j.role
        }
    }
    return $out
}

function Cmd-Queue {
    # FREE: what is waiting for a human, and the exact key to answer under. Filesystem only. A run waiting
    # for a quota slot is not a manual request, but it is why nothing moves, so it is listed here too.
    param($Ctx, [string]$TaskId)
    if ($TaskId -and -not (Test-Path -LiteralPath (Get-TaskDir $Ctx $TaskId))) { throw "no run '$TaskId'" }
    $rows = @(Get-PendingManual -Ctx $Ctx -TaskId $TaskId)
    $queued = @(Get-QueuedRuns -Ctx $Ctx -TaskId $TaskId)
    if (-not $rows.Count -and -not $queued.Count) {
        Write-Host 'nothing is waiting for a manual answer.' -ForegroundColor DarkGray
        return
    }
    foreach ($q in $queued) {
        Write-Host ("{0}  queued for pool {1} ({2})" -f $q.TaskId, $q.Pool, $q.HeldBy) -ForegroundColor DarkYellow
        Write-Host ("   role   : {0}   (run.ps1 pools shows every pool)" -f $q.Role) -ForegroundColor DarkGray
    }
    foreach ($r in $rows) {
        Write-Host ("{0}  {1}  [{2}]  state={3}  waiting {4}m" -f $r.TaskId, $r.Key, $r.Repo, $r.Status, $r.WaitMin) -ForegroundColor Yellow
        Write-Host ("   request: {0}" -f $r.Request) -ForegroundColor DarkGray
        Write-Host ("   answer : {0}" -f $r.Answer) -ForegroundColor DarkGray
        Write-Host ("   next   : run.ps1 answer {0} {1} <file>" -f $r.TaskId, $r.Key) -ForegroundColor DarkGray
        $resume = Get-ResumeCommand $r.Key
        if ($resume) {
            $phaseArg = if ($resume -eq 'implement') { ' ' + (Get-ImplementPhase $r.Key) } else { '' }
            Write-Host ("            then: run.ps1 {0} {1}{2}   (or watch {1} -ThenResume)" -f $resume, $r.TaskId, $phaseArg) -ForegroundColor DarkGray
        }
    }
    if ($rows.Count) { Write-Host ("{0} request(s) waiting." -f $rows.Count) -ForegroundColor Yellow }
}

function Cmd-Review {
    # Gate 2 again for the plan version already on disk. Unlike revise it writes no new version, so a
    # deferred plan-reviewer.v<round> answer is still the key this command asks for (D7 resume path).
    param($Ctx, [string]$TaskId, [switch]$NoReadBack, [switch]$Retry)
    if (-not $TaskId) { throw 'usage: run.ps1 review <taskId>' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    if (-not $state.plan) { throw "run '$TaskId' has no plan yet" }
    if (@('Approved', 'Implemented', 'Verified', 'VerifyFailed') -contains $state.status) {
        throw "run '$TaskId' is '$($state.status)'; its plan is already past review"
    }
    $readBack = (-not $NoReadBack) -and [bool](Get-CfgValue $Ctx 'readback' 'enabled' $true)
    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        # No -PrevFindings: this is the same version, so an earlier reply is not a round to check off.
        $deferred = (Invoke-Gate2 -Ctx $Ctx -State $state -PrevFindings '' -Notes (Get-StateNotes $state) -ReadBack $readBack -Retry:$Retry).Deferred
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
        Write-RunSummary $Ctx $state
        if ($deferred) { throw $deferred }
    }
    finally { Exit-TaskLock $lock }
}

function Wait-ManualAnswer {
    # Blocks until a complete answer exists for one pending request. Read-only: nothing is claimed or
    # changed here, so Ctrl+C leaves the run exactly as it was.
    param($Pending, [int]$PollSec = 5)
    if ($PollSec -lt 1) { $PollSec = 1 }
    Write-Host ("waiting for an answer to '{0}'" -f $Pending.Key) -ForegroundColor Cyan
    Write-Host ("  answer: {0}" -f $Pending.Answer) -ForegroundColor DarkGray
    Write-Host '  Ctrl+C is safe: nothing has been claimed or changed yet.' -ForegroundColor DarkGray
    $lastNote = $false
    while ($true) {
        $text = ''
        if (Test-Path -LiteralPath $Pending.Answer) {
            try { $text = Get-Content -LiteralPath $Pending.Answer -Raw -Encoding UTF8 } catch { $text = '' }
        }
        if ($text -and $text.Trim()) {
            # A file without a VERDICT: line is not an answer: it would be recorded as NO_VERDICT and
            # refused at approval. Keep waiting rather than resuming on a half-written file.
            if (Get-Verdict $text) { return $text }
            if (-not $lastNote) {
                Write-Host '  the file exists but has no VERDICT: line yet - still waiting' -ForegroundColor Yellow
                $lastNote = $true
            }
        }
        Start-Sleep -Seconds $PollSec
    }
}

function Cmd-Watch {
    # Read-only while it polls: nothing here starts, stops or changes a run, and no model is called.
    # -ThenResume is the one exception, and only after a complete answer is already on disk.
    param($Ctx, [string]$TaskId, [int]$Every = 90, [int]$PollSec = 5, [switch]$Once, [switch]$ThenResume, [switch]$NoReadBack)
    if ($ThenResume) {
        if (-not $TaskId) { throw 'usage: run.ps1 watch <taskId> -ThenResume' }
        if ($Once) { throw 'watch: -ThenResume cannot be combined with -Once (it has to wait for the answer)' }
        $waiting = @(Get-PendingManual -Ctx $Ctx -TaskId $TaskId)
        if (-not $waiting.Count) { throw "run '$TaskId' has no request waiting for an answer; nothing to resume" }
        if ($waiting.Count -gt 1) {
            throw ("run '{0}' has {1} requests waiting ({2}); answer them and rerun the commands by hand" -f $TaskId, $waiting.Count, (($waiting | ForEach-Object { $_.Key }) -join ', '))
        }
        $cmd = Get-ResumeCommand $waiting[0].Key
        if (-not $cmd) { throw "no command consumes a '$($waiting[0].Key)' answer; store it with 'answer' and continue by hand" }
        [void](Wait-ManualAnswer -Pending $waiting[0] -PollSec $PollSec)
        $resumeTarget = if ($cmd -eq 'implement') { "$TaskId $(Get-ImplementPhase $waiting[0].Key)" } else { $TaskId }
        Write-Host ("answer found: resuming with 'run.ps1 {0} {1}'" -f $cmd, $resumeTarget) -ForegroundColor Green
        switch ($cmd) {
            'review'   { Cmd-Review -Ctx $Ctx -TaskId $TaskId -NoReadBack:$NoReadBack }
            'readback' { Cmd-ReadBack -Ctx $Ctx -TaskId $TaskId }
            'verify'   { Cmd-Verify -Ctx $Ctx -TaskId $TaskId }
            'implement' { Cmd-Implement -Ctx $Ctx -TaskId $TaskId -Phase (Get-ImplementPhase $waiting[0].Key) }
        }
        return
    }
    if ($Every -lt 1) { $Every = 1 }
    while ($true) {
        if (-not $Once) { Clear-Host }
        $files = @()
        if ($TaskId) {
            $p = Get-TaskProgressFile $Ctx $TaskId
            if (Test-Path -LiteralPath $p) { $files = @($p) }
            else { Write-Host "no progress.json for '$TaskId' yet (a hop writes it when it starts)" -ForegroundColor Yellow }
        }
        else {
            if (Test-Path -LiteralPath $Ctx.RunsDir) {
                $files = @(Get-ChildItem -LiteralPath $Ctx.RunsDir -Directory -ErrorAction SilentlyContinue |
                    ForEach-Object { Join-Path $_.FullName 'progress.json' } | Where-Object { Test-Path -LiteralPath $_ })
            }
        }
        $blocks = 0
        $running = $false
        foreach ($f in $files) {
            $p = $null
            try { $p = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
            if ($p.state -eq 'running') { $running = $true }
            # Without an id only what is running is shown - including a run queued for its pool, which is
            # live; a finished run is history, not a live view.
            if (-not $TaskId -and $p.state -ne 'running' -and $p.phase -ne 'queued') { continue }
            Write-Host (Format-ProgressBlock $p)
            Write-Host ''
            $blocks++
        }
        if (-not $blocks) { Write-Host 'nothing is running.' -ForegroundColor DarkGray }
        if ($Once) { return }
        if ($TaskId -and -not $running) { return }
        Start-Sleep -Seconds $Every
    }
}

function Cmd-Answer {
    # Stores a human answer for a deferred manual hop. Nothing else reads answers/, so a refused file
    # never half-enters a run.
    param($Ctx, [string]$TaskId, [string]$Key, [string]$File, [switch]$Force)
    if (-not $TaskId -or -not $Key -or -not $File) { throw 'usage: run.ps1 answer <taskId> <key> <file>' }
    [void](Get-TaskState -Ctx $Ctx -TaskId $TaskId)
    if (-not (Test-Path -LiteralPath $File)) { throw "answer file not found: $File" }
    $text = Get-Content -LiteralPath $File -Raw -Encoding UTF8
    if (-not $text -or -not $text.Trim()) { throw "the answer file is empty: $File" }
    if (-not (Get-Verdict $text)) { throw "the answer has no VERDICT: line ($File); the orchestrator would record it as NO_VERDICT and refuse to approve" }
    $dir = Join-Path (Get-TaskDir $Ctx $TaskId) 'answers'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $target = Join-Path $dir "$Key.md"
    if ((Test-Path -LiteralPath $target) -and -not $Force) { throw "'$Key' was already answered ($target); pass -Force to replace it" }
    $tmp = "$target.tmp"
    Set-Content -LiteralPath $tmp -Value $text -Encoding UTF8
    Move-Item -LiteralPath $tmp -Destination $target -Force
    Write-Host "answer stored for '$Key': $target" -ForegroundColor Green
    Write-Host "review / readback / implement / verify now consumes it (revise would ask for a new version, so a new key)." -ForegroundColor DarkGray
}

function Cmd-Status {
    param($Ctx, [string]$TaskId)
    if (-not $TaskId) { throw 'usage: run.ps1 status <taskId>' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    $lockFile = Join-Path (Get-TaskDir $Ctx $TaskId) '.lock'
    if (Test-Path -LiteralPath $lockFile) { Write-Host "LOCKED: $(Get-Content -LiteralPath $lockFile -Raw)" -ForegroundColor Yellow }
    $queued = @(Get-QueuedRuns -Ctx $Ctx -TaskId $TaskId)
    if ($queued.Count) { Write-Host ("QUEUED: queued for pool {0} ({1})" -f $queued[0].Pool, $queued[0].HeldBy) -ForegroundColor Yellow }
    $state | ConvertTo-Json -Depth 12 | Write-Host
    Write-RunSummary $Ctx $state
}

function Cmd-Approve {
    param($Ctx, [string]$TaskId, [string]$Notes, [string]$Branch, [switch]$Force)
    if (-not $TaskId) { throw 'usage: run.ps1 approve <taskId> ["notes"] [-Branch name] [-Force]' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    Write-ProfileVersionLine $Ctx $TaskId
    if ($state.status -ne 'AwaitingApproval') { throw "run '$TaskId' is '$($state.status)', not AwaitingApproval" }
    $plannerFamily = if ($state.plan.PSObject.Properties['family']) { $state.plan.family } else { Get-HopFamily ([pscustomobject]@{ adapter = $state.plan.adapter; model = $state.plan.model }) }
    $reviewerFamily = if ($state.review.PSObject.Properties['family']) { $state.review.family } else { Get-HopFamily ([pscustomobject]@{ adapter = $state.review.adapter; model = $state.review.model }) }
    if (-not (Test-Gate2ModelDiffer -Ctx $Ctx -PlannerFamily $plannerFamily -ReviewerFamily $reviewerFamily)) { throw "gate 2 violated: reviewer family '$reviewerFamily' equals plan author family '$plannerFamily'" }
    $cur = [int]$state.round
    # NO_VERDICT is treated exactly like a version that was never reviewed: refuse unless -Force.
    $reviewed = ($state.review.PSObject.Properties['round'] -and $state.review.round -eq $cur -and $state.review.verdict -ne 'NO_VERDICT')
    if (-not $reviewed -and -not $Force) {
        $why = if ($state.review.verdict -eq 'NO_VERDICT' -and $state.review.round -eq $cur) { 'its review reply ended without a verdict' } else { "the last review is for v$($state.review.round)" }
        throw "plan v$cur has not been reviewed ($why). revise/review first, or -Force."
    }
    $problems = @()
    if ($state.review.verdict -ne 'PASS') { $problems += "review $($state.review.verdict)" }
    $rb = if ($state.PSObject.Properties['readback']) { $state.readback } else { $null }
    if ($rb -and $rb.round -eq $cur -and $rb.verdict -ne 'PASS') { $problems += "read-back $($rb.verdict)" }
    if ($problems.Count -and -not $Notes -and -not $Force) {
        throw "open findings ($($problems -join ', ')). Either revise, approve with notes that settle them, or approve -Force to accept the plan as is."
    }
    if (-not $rb -or $rb.round -ne $cur) { Write-Host "note: no read-back for v$cur (run.ps1 readback $TaskId to get one)" -ForegroundColor Yellow }

    $repoPath = Get-RepoPath $Ctx $state.repo
    $planText = Get-Content -LiteralPath $state.plan.artifact -Raw -Encoding UTF8
    $approved = $planText
    $allNotes = (@((Get-StateNotes $state), $Notes) | Where-Object { $_ }) -join "`n`n"
    if ($allNotes) { $approved += "`n`n## APPROVED AMENDMENTS (gate 2, user-approved; these override the plan above)`n`n$allNotes`n" }
    if (-not $Branch) { $Branch = Get-PlanBranch -PlanText $approved -GitUser (Get-GitUser $repoPath) -TaskId $TaskId }
    $approvedFile = Save-Artifact -Ctx $Ctx -TaskId $TaskId -Name 'plan.approved' -Content $approved
    $sha = (Get-FileHash -LiteralPath $approvedFile -Algorithm SHA256).Hash

    Set-Prop $state 'approved' ([pscustomobject]@{ artifact = $approvedFile; sha256 = $sha; at = (Get-Date).ToString('s'); version = $cur; notes = $allNotes; openFindings = ($problems -join ', '); forced = [bool]$Force })
    Set-Prop $state 'branch' $Branch
    Set-Prop $state 'status' 'Approved'
    Add-History -Ctx $Ctx -State $state -Event 'approve' -Data @{ version = $cur; branch = $Branch; notes = [bool]$allNotes; forced = [bool]$Force }
    Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state
    Write-Host "run '$TaskId' approved (gate 2). frozen plan: $approvedFile" -ForegroundColor Green
    Write-RunSummary $Ctx $state
}

function Get-ApprovedPlan {
    param($State)
    if (-not $State.approved) { throw "run has no approved plan" }
    $f = $State.approved.artifact
    if ((Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash -ne $State.approved.sha256) { throw "plan.approved.md changed after approval ($f). Re-approve instead of editing it." }
    Get-Content -LiteralPath $f -Raw -Encoding UTF8
}

function Test-PlanDone {
    # Plan 0007 D4: a plan is closed by its Status line, not by its folder. The header is the first 15
    # lines; `**Status:** Done`, `Status: **Done**` and an unbolded `Status: Done` all count.
    param([string]$Path)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return $false }
    foreach ($line in @(Get-Content -LiteralPath $Path -TotalCount 15 -Encoding UTF8)) {
        if ($line -match '^\s*\*{0,2}Status:?\*{0,2}\s*\*{0,2}Done') { return $true }
    }
    return $false
}

function Cmd-Close {
    # The durable record is the plan filed in its repo's done folder. So a run is deleted (its folder and its
    # _drafts/<id>.* files) only once that filed copy exists, or with -Force. Before that, its ledger block is
    # appended and runs/_archive/<id>.<yyyyMMddHHmm>.json is written: a temporary data copy for the dashboard.
    param($Ctx, [string]$TaskId, [switch]$Force)
    if (-not $TaskId) { throw 'usage: run.ps1 close <taskId> [-Force]' }
    $dir = Get-TaskDir $Ctx $TaskId
    if (-not (Test-Path -LiteralPath $dir)) { throw "no run '$TaskId' under $($Ctx.RunsDir)" }
    if (Test-Path -LiteralPath (Join-Path $dir '.lock')) { throw "run '$TaskId' is locked: a command is running on it" }
    Write-ProfileVersionLine $Ctx $TaskId
    if (-not $Force) {
        $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
        $planText = if ($state.PSObject.Properties['approved'] -and $state.approved -and (Test-Path -LiteralPath $state.approved.artifact)) { Get-Content -LiteralPath $state.approved.artifact -Raw -Encoding UTF8 } else { '' }
        $branch = if ($state.PSObject.Properties['branch']) { $state.branch } else { '' }
        # D3: how plans are filed is a project setting. `default` is Project B's number filing, unchanged;
        # `slug` is Project A's block, unchanged. Get-CfgValue cannot read a top-level scalar (its name is
        # [string]), so the switch value is read from the config's own property.
        $planFiling = 'number'
        $pf = $Ctx.Config.PSObject.Properties['planFiling']
        if ($pf -and $pf.Value) { $planFiling = [string]$pf.Value }
        # Plan 0007 D3: a project that sets a top-level `plansDir` keeps every plan of a repo in one folder
        # for its whole life, and `**Status:** Done` is what files it - there is no done/ folder. Without
        # `plansDir` the switch below is today's code, unchanged.
        $planDir = $null
        $plansDirProp = $Ctx.Config.PSObject.Properties['plansDir']
        if ($plansDirProp -and $plansDirProp.Value) {
            if (-not $state.repo) { throw "run '$TaskId' has no repo in task.json, so its plan folder is unknown. close -Force to delete it anyway." }
            $planDir = Resolve-WorkflowPath $Ctx ([string]$plansDirProp.Value) $state.repo
        }
        if ($planDir) {
            $candidates = @()
            if ($planFiling -eq 'slug') {
                $slug = Get-PlanSlug -Branch $branch -PlanText $planText
                if (-not $slug) { throw "run '$TaskId' names no plan branch (branch '$branch'), so its filed copy cannot be checked. close -Force to delete it anyway." }
                $exact = Join-Path $planDir "$slug.md"
                if (Test-Path -LiteralPath $exact) { $candidates += @(Get-Item -LiteralPath $exact) }
                $candidates += @(Get-ChildItem -LiteralPath $planDir -Filter "*-$slug.md" -File -ErrorAction SilentlyContinue)
                if ($branch) {
                    $candidates += @(Get-ChildItem -LiteralPath $planDir -Filter '*.md' -File -ErrorAction SilentlyContinue | Where-Object {
                            $t = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
                            $t -and $t.Contains($branch)
                        })
                }
                $planLabel = "'$slug'"
            }
            else {
                $num = Get-PlanNumber -Branch $branch -PlanText $planText
                if (-not $num) { throw "run '$TaskId' names no plan number (branch '$branch'), so its filed copy cannot be checked. close -Force to delete it anyway." }
                $candidates += @(Get-ChildItem -LiteralPath $planDir -Filter "$num-*.md" -File -ErrorAction SilentlyContinue)
                $planLabel = $num
            }
            # Every match is a candidate and only then filtered on Done, so the run's own open plan (which
            # also names its branch) never hides a Done copy.
            $candidates = @($candidates | Sort-Object -Property FullName -Unique)
            $filed = @($candidates | Where-Object { Test-PlanDone $_.FullName })
            if (-not $candidates.Count) { throw "plan $planLabel is not in $planDir. Write it there first (gate 1), or close -Force to delete the run anyway." }
            if (-not $filed.Count) { throw "plan $planLabel is in $planDir but its Status is not Done yet. Close the plan first (the close-plan checklist), or close -Force to delete the run anyway." }
            Write-Host "plan $planLabel is filed: $($filed[0].FullName)" -ForegroundColor DarkGray
        }
        else {
        switch ($planFiling) {
            'slug' {
        # Project A plans are filed by slug (`done/<slug>.md`), never by number. Find the filed copy by the
        # branch's topic, then by a `-<slug>` suffix, and finally by a done file that names this branch.
        $slug = Get-PlanSlug -Branch $branch -PlanText $planText
        if (-not $slug) { throw "run '$TaskId' names no plan branch (branch '$branch'), so its filed copy cannot be checked. close -Force to delete it anyway." }
        $doneDir = Join-Path (Get-RepoPath $Ctx $state.repo) (Get-CfgValue $Ctx 'defaults' 'plansDoneDir' '.ai/plans/done')
        $filed = @()
        $exact = Join-Path $doneDir "$slug.md"
        if (Test-Path -LiteralPath $exact) { $filed = @(Get-Item -LiteralPath $exact) }
        if (-not $filed.Count) { $filed = @(Get-ChildItem -LiteralPath $doneDir -Filter "*-$slug.md" -File -ErrorAction SilentlyContinue) }
        if (-not $filed.Count -and $branch) {
            $filed = @(Get-ChildItem -LiteralPath $doneDir -Filter '*.md' -File -ErrorAction SilentlyContinue | Where-Object {
                    $t = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
                    $t -and $t.Contains($branch)
                })
        }
        if (-not $filed.Count) { throw "plan '$slug' is not filed in $doneDir yet. File it first (the close-plan checklist), or close -Force to delete the run anyway." }
        Write-Host "plan '$slug' is filed: $($filed[0].FullName)" -ForegroundColor DarkGray
            }
            default {
        $num = Get-PlanNumber -Branch $branch -PlanText $planText
        if (-not $num) { throw "run '$TaskId' names no plan number (branch '$branch'), so its filed copy cannot be checked. close -Force to delete it anyway." }
        $doneDir = Join-Path (Get-RepoPath $Ctx $state.repo) (Get-CfgValue $Ctx 'defaults' 'plansDoneDir' '.ai/plans/done')
        $filed = @(Get-ChildItem -LiteralPath $doneDir -Filter "$num-*.md" -File -ErrorAction SilentlyContinue)
        if (-not $filed.Count) { throw "plan $num is not filed in $doneDir yet. File it first (the close-plan checklist), or close -Force to delete the run anyway." }
        Write-Host "plan $num is filed: $($filed[0].FullName)" -ForegroundColor DarkGray
            }
        }
        }
    }
    # Ledger: the run's summary and token totals are kept in runs/_ledger.md before its folder goes.
    $ledgerState = $null
    try { $ledgerState = Get-TaskState -Ctx $Ctx -TaskId $TaskId } catch { Write-Host "  [ledger] task.json unreadable; no close entry" -ForegroundColor DarkYellow }
    if ($ledgerState) {
        $usage = Write-LedgerEntry $Ctx $ledgerState 'close'
        Write-RunArchive -Ctx $Ctx -TaskId $TaskId -Usage $usage
        # Plan 0004 §6.1: reported while the state can still be read - the folder goes on the next line.
        Send-OrchestraEvent -Ctx $Ctx -State $ledgerState -Kind 'run.closed' -Data ([ordered]@{ status = [string]$ledgerState.status })
    }
    $draftsDir = Join-Path $Ctx.RunsDir '_drafts'
    $drafts = @(Get-ChildItem -LiteralPath $draftsDir -Filter "$TaskId.*" -File -ErrorAction SilentlyContinue)
    Remove-Item -LiteralPath $dir -Recurse -Force
    foreach ($f in $drafts) { Remove-Item -LiteralPath $f.FullName -Force }
    Write-Host "run '$TaskId' closed: deleted $dir and $($drafts.Count) draft file(s)." -ForegroundColor Green
    Write-OpenCodeStats $Ctx
}

function Cmd-Implement {
    param($Ctx, [string]$TaskId, [string]$Phase, [switch]$Force, [switch]$Retry)
    if (-not $TaskId) { throw 'usage: run.ps1 implement <taskId> [phase]' }
    if (-not $Phase) { $Phase = '1' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    Write-ProfileVersionLine $Ctx $TaskId
    $okStates = @('Approved', 'Implemented', 'Verified', 'VerifyFailed')
    if ($Force) { $okStates += 'Violation' }
    if ($okStates -notcontains $state.status) { throw "run '$TaskId' is '$($state.status)'; implement needs one of: $($okStates -join ', ')" }
    $repoPath = Get-RepoPath $Ctx $state.repo
    if (-not (Test-IsGitRepo $repoPath)) { throw "repo '$($state.repo)' at $repoPath is not a git repo" }
    $planText = Get-ApprovedPlan $state

    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        $current = Get-GitBranch $repoPath
        if (-not $state.baseSha) {
            # Gate 3 in code: clean tree, then the orchestrator itself branches before any agent edit.
            # D11: records under cleanTree.ignore (plans, session logs) never block; they are remembered
            # so the post-checks and the verifier do not mistake them for this run's work.
            $ignore = @()
            if ($Ctx.Config.PSObject.Properties['cleanTree'] -and $Ctx.Config.cleanTree.PSObject.Properties['ignore']) { $ignore = @($Ctx.Config.cleanTree.ignore) }
            $dp = Get-DirtyPaths -RepoPath $repoPath -Ignore $ignore
            if (@($dp.Blocking).Count) { throw "gate 3: $($state.repo) has uncommitted changes; the first phase needs a clean tree:`n$(@($dp.Blocking) -join "`n")" }
            Set-Prop $state 'preexistingDirty' @($dp.Ignored)
            if (@($dp.Ignored).Count) { Write-Host "gate 3: ignoring pre-existing uncommitted records: $(@($dp.Ignored) -join ', ')" -ForegroundColor DarkGray }
            if (Test-GitBranchExists $repoPath $state.branch) { throw "gate 3: branch '$($state.branch)' already exists. approve again with -Branch <new name>." }
            $base = Get-GitHead $repoPath
            $sw = Invoke-Git $repoPath @('switch', '-c', $state.branch)
            if (-not $sw.Ok) { throw "git switch -c $($state.branch) failed: $($sw.Stderr)" }
            Set-Prop $state 'baseSha' $base
            Set-Prop $state 'baseBranch' $current
            Write-Host "gate 3: created branch '$($state.branch)' from $current @ $($base.Substring(0,7))" -ForegroundColor DarkYellow
        }
        elseif ($current -ne $state.branch) {
            throw "gate 3: repo is on '$current' but this run works on '$($state.branch)'. Switch back first (git -C `"$repoPath`" switch $($state.branch))."
        }
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state

        $before = Get-WorkspaceFingerprint $Ctx
        $phaseLines = "Phase to execute: $Phase`nBranch (already created and checked out by the orchestrator): $($state.branch)`nRepo root: $repoPath`nYour cwd is the workspace root. Run all git commands against the repo root above, e.g. git -C `"$repoPath`" <args>."
        $extra = "Approved plan (frozen; follow it, including the APPROVED AMENDMENTS):`n`n$planText`n`n$phaseLines"
        $prompt = New-RolePrompt -Ctx $Ctx -Role 'implementer' -Repo $state.repo -Task $state.task -Extra $extra -Branch $state.branch
        Write-Host "gate 3: implementing phase $Phase..." -ForegroundColor Cyan
        # D7: continue this run's implement session, else the read-back or review session that ran on the
        # implementer's own model (it saw the approved version or an earlier one). From the read-back a short
        # follow-up is enough: that session already has the implementer's instructions, and an earlier version
        # gets the diff. A review session has the reviewer's instructions, so it gets the full implement
        # prompt as the next turn: the role changes, the files it read stay cached.
        $continue = $null
        $contId = ''
        $from = ''
        $fromRound = 0
        if ($state.PSObject.Properties['implementSession'] -and $state.implementSession) { $contId = [string]$state.implementSession }
        else {
            $pick = Find-StageSession -State $state -Stages @('readback', 'review') -SameFamily (Get-FirstHopFamily $Ctx 'implementer')
            if ($pick) { $contId = $pick.Id; $from = $pick.Stage; $fromRound = $pick.Round }
        }
        if ($contId -and (Test-SessionCanContinue -Ctx $Ctx -State $state -SessionId $contId) -and $from -eq 'review') {
            $rec = $state.sessions.$contId
            $head = "# ORCHESTRATOR CALL - role: implementer (same session, new role)`n`nEarlier in this session you reviewed version $fromRound of this plan. Version $([int]$state.approved.version) is now approved and you implement it. Your review role ends here; the full implementer instructions follow and replace it.`n`n"
            $continue = @{ Adapter = $rec.adapter; Model = $rec.model; SessionId = $contId; Prompt = ($head + $prompt) }
        }
        elseif ($contId -and (Test-SessionCanContinue -Ctx $Ctx -State $state -SessionId $contId)) {
            $rec = $state.sessions.$contId
            $approvedV = [int]$state.approved.version
            $rbV = if ($from -eq 'readback') { $fromRound } else { $approvedV }
            if ($rbV -lt $approvedV) {
                $taskDir = Get-TaskDir $Ctx $TaskId
                $pdiff = Get-PlanDiffText -OldFile (Join-Path $taskDir "artifacts\plan.v$rbV.md") -NewFile (Join-Path $taskDir "artifacts\plan.v$approvedV.md")
                $body = "You read back version $rbV; version $approvedV is now approved. Changes from version $rbV to version $approvedV (unified diff):`n`n$pdiff`n`n"
            }
            else { $body = "The plan you read back (version $approvedV) is now approved.`n`n" }
            $amend = Get-ApprovedAmendments $planText
            if ($amend) { $body += "$amend`n`n" }
            $body += $phaseLines
            $continue = @{ Adapter = $rec.adapter; Model = $rec.model; SessionId = $contId; Prompt = (New-FollowUpPrompt -Role 'implementer' -Body $body -Branch $state.branch) }
        }
        $impl = Invoke-Role -Ctx $Ctx -Role 'implementer' -Prompt $prompt -Cwd (Get-AgentCwd $Ctx $state.repo) -TimeoutSec (Get-Timeout $Ctx) -Repo $state.repo -ProgressFile (Get-TaskProgressFile $Ctx $TaskId) -TaskId $TaskId -Phase $Phase -ManualKey "implementer.phase-$Phase" -Continue $continue -Retry:$Retry
        # D8: a human answered this phase, so the tree was meant to change. The baseline for the sibling
        # check is the workspace snapshot taken when the prompt was written, not the one captured above
        # (which already includes the human's edits).
        $wsFile = Join-Path (Get-TaskDir $Ctx $TaskId) "inbox\implementer.phase-$Phase.workspace.json"
        if ((Get-AdapterTransport -Config $Ctx.Config -Adapter $impl.Adapter) -eq 'inbox' -and (Test-Path -LiteralPath $wsFile)) {
            $snap = Get-Content -LiteralPath $wsFile -Raw -Encoding UTF8 | ConvertFrom-Json
            $before = @{}
            foreach ($p in $snap.PSObject.Properties) { $before[$p.Name] = [string]$p.Value }
        }
        Save-Outbox -Ctx $Ctx -TaskId $TaskId -Role 'implementer' -Name "phase-$Phase" -Content $impl.Result.Stdout | Out-Null
        Save-Transcript -Ctx $Ctx -TaskId $TaskId -Name "implementer.phase-$Phase" -Data $impl | Out-Null

        # Post-checks the agent cannot talk its way past.
        $after = Get-WorkspaceFingerprint $Ctx
        $violations = @()
        $siblings = @(Compare-WorkspaceFingerprint $before $after | Where-Object { $_ -ne $state.repo })
        if ($siblings.Count) {
            # Deliberately a note, never a violation: a sibling checkout is another run's work (the two
            # repos are meant to run in parallel) or the owner's own commit between phases. This worker's
            # own edits are already policed in its own repo - branch switched, commits made, files
            # outside the plan - and a stray edit in the sibling shows up there as a dirty tree the
            # sibling's own run refuses to start on.
            Write-Host "  note: $($siblings -join ', ') changed meanwhile (not this worker)" -ForegroundColor Yellow
        }
        $now = Get-GitBranch $repoPath
        if ($now -ne $state.branch) { $violations += "branch switched to '$now'" }
        $commits = (Invoke-Git $repoPath @('rev-list', '--count', "$($state.baseSha)..HEAD")).Stdout
        if ([int]$commits -gt 0) { $violations += "$commits commit(s) made" }
        $changed = @(Get-GitChangedFiles $repoPath $state.baseSha)
        # D11: a record already uncommitted before this run is not this run's work; drop it from both lists.
        $pre = @()
        if ($state.PSObject.Properties['preexistingDirty']) { $pre = @($state.preexistingDirty | Where-Object { $_ }) }
        if ($pre.Count) { $changed = @($changed | Where-Object { $pre -notcontains $_ }) }
        $outside = @(Get-FilesOutsidePlan -Changed $changed -Mentions (Get-PlanPathMentions $planText $state.repo))
        if ($pre.Count) { $outside = @($outside | Where-Object { $pre -notcontains $_ }) }
        $iv = Get-Verdict $impl.Result.Stdout
        $ik = if ($iv) { $iv.Kind } else { 'NONE' }

        $phaseRec = [pscustomobject]@{ phase = $Phase; adapter = $impl.Adapter; model = "$($impl.Adapter):$($impl.Model)"; family = $impl.Family; verdict = $ik; message = $(if ($iv) { $iv.Message } else { '' }); changed = $changed; outsidePlan = $outside; violations = $violations; at = (Get-Date).ToString('s') }
        Add-SessionUse -State $state -SessionId $impl.SessionId -Adapter $impl.Adapter -Model $impl.Model -Role 'implementer'
        Set-Prop $state 'implementSession' $impl.SessionId
        Set-Prop $state 'implement' $phaseRec
        Set-Prop $state 'phases' (@($state.phases | Where-Object { $_ }) + $phaseRec)
        Add-History -Ctx $Ctx -State $state -Event 'implement' -Data @{ phase = $Phase; verdict = $ik; violations = $violations.Count }
        $status = if ($violations.Count) { 'Violation' } else { 'Implemented' }
        Set-Prop $state 'status' $status
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state

        Write-Host ""
        Write-Host "===== IMPLEMENT phase $Phase ($($impl.Adapter) / $($impl.Model)) - verdict=$ik =====" -ForegroundColor Cyan
        Write-Host $impl.Result.Stdout
        Write-Host ""
        Write-Host "changed files: $($changed -join ', ')" -ForegroundColor DarkGray
        if ($outside.Count) { Write-Host "WARNING not mentioned in the plan: $($outside -join ', ')" -ForegroundColor Yellow }
        if ($ik -ne 'PASS') { Write-Host "implementer did not report PASS ($ik): $($phaseRec.message)" -ForegroundColor Yellow }
        if ($violations.Count) {
            Write-Host "VIOLATION: $($violations -join '; ')" -ForegroundColor Red
            Write-Host "fix by hand, then: verify $TaskId (or implement $TaskId <phase> -Force)" -ForegroundColor Yellow
        }
        else { Write-Host "next: run.ps1 verify $TaskId   (nothing was committed)" -ForegroundColor Green }
        Write-RunSummary $Ctx $state
    }
    finally { Exit-TaskLock $lock }
}

function Cmd-Verify {
    param($Ctx, [string]$TaskId, [switch]$Retry)
    if (-not $TaskId) { throw 'usage: run.ps1 verify <taskId>' }
    $state = Get-TaskState -Ctx $Ctx -TaskId $TaskId
    Write-ProfileVersionLine $Ctx $TaskId
    if (@('Implemented', 'Verified', 'VerifyFailed', 'Violation') -notcontains $state.status) { throw "run '$TaskId' is '$($state.status)'; nothing to verify" }
    $repoPath = Get-RepoPath $Ctx $state.repo
    $planText = Get-ApprovedPlan $state
    $lock = Enter-TaskLock $Ctx $TaskId
    try {
        $gateOut = New-Object System.Text.StringBuilder
        $cmds = @()
        if ($Ctx.Config.PSObject.Properties['verifyCommands'] -and $Ctx.Config.verifyCommands.PSObject.Properties[$state.repo]) { $cmds = @($Ctx.Config.verifyCommands.($state.repo)) }
        $gatesFailed = @()
        foreach ($c in $cmds) {
            Write-Host "gate: $c" -ForegroundColor DarkGray
            $r = Invoke-External -Exe 'powershell' -ArgList @('-NoProfile', '-Command', $c) -Cwd $repoPath -TimeoutSec (Get-Timeout $Ctx)
            if ($r.ExitCode -ne 0 -or $r.TimedOut) { $gatesFailed += $c }
            [void]$gateOut.AppendLine("### $c  (exit $($r.ExitCode), timedOut=$($r.TimedOut))")
            [void]$gateOut.AppendLine((Get-Tail ($r.Stdout + "`n" + $r.Stderr) 6000))
        }
        $gateText = if ($cmds.Count) { $gateOut.ToString() } else { "No verifyCommands configured for this repo; nothing was executed. Verify by reading the diff and the repo gate doc only." }
        $diff = Get-GitDiff $repoPath $state.baseSha ([int](Get-CfgValue $Ctx 'defaults' 'maxDiffChars' 60000))
        $last = $state.implement
        $checks = "Orchestrator checks: changed files = $(@($last.changed) -join ', '); not mentioned in plan = $(@($last.outsidePlan) -join ', '); violations = $(@($last.violations) -join '; ')"
        # A plan is implemented one phase per user go, and verify runs after each one: without its scope the
        # verifier judged phase 1 against the whole plan's definition of done and failed it for phase 2's absence.
        $done = @($state.phases | Where-Object { $_ } | ForEach-Object { [string]$_.phase } | Select-Object -Unique)
        $scope = "Phases implemented so far: $($done -join ', ') (the last one is phase $($last.phase)). Verify ONLY these phases: their files, their specification and THEIR OWN phase gate (e.g. that phase's test-count target). Work that belongs to a later phase being absent is expected and is NOT a finding. Apply the plan's whole definition of done only if the phases above are all of the plan's phases."
        # D11: records that were already uncommitted before this run are not its work; tell the verifier.
        $preLine = ''
        if ($state.PSObject.Properties['preexistingDirty'] -and @($state.preexistingDirty | Where-Object { $_ }).Count) {
            $preLine = "Pre-existing uncommitted paths (not this run's work; ignore them in the diff): $(@($state.preexistingDirty) -join ', ')`n`n"
        }
        $tail = "$scope`n`n$checks`n`nGate command results (run by the orchestrator, not by you):`n`n$gateText`n`n" + $preLine + "Diff against the base commit $($state.baseSha):`n`n$diff"
        $extra = "Plan (approved, frozen):`n`n$planText`n`n$tail"
        $prompt = New-RolePrompt -Ctx $Ctx -Role 'verifier' -Repo $state.repo -Task $state.task -Extra $extra
        Write-Host "gate 4: verifier..." -ForegroundColor Cyan
        # D7: continue this run's verify session, else the review session (it reviewed the approved version
        # or an earlier one, AND its family differs from the last implementer's, so gate-2 independence
        # still holds). An earlier version gets the plan diff from there to the approved one.
        # A read-back session has the implementer's instructions, not the verifier's: it gets the full verify
        # prompt as its next turn (new role, cached files kept).
        $continue = $null
        $contId = ''
        $fromReview = $false
        $fromReadBack = $false
        $fromRound = 0
        if ($state.PSObject.Properties['verifySession'] -and $state.verifySession) { $contId = [string]$state.verifySession }
        else {
            $lastFamily = if ($state.implement -and $state.implement.PSObject.Properties['family']) { [string]$state.implement.family } else { '' }
            $pick = Find-StageSession -State $state -Stages @('review', 'readback') -OtherFamily $lastFamily
            if ($pick) { $contId = $pick.Id; $fromRound = $pick.Round; $fromReview = $pick.Stage -eq 'review'; $fromReadBack = $pick.Stage -eq 'readback' }
        }
        if ($contId -and $fromReadBack -and (Test-SessionCanContinue -Ctx $Ctx -State $state -SessionId $contId)) {
            $rec = $state.sessions.$contId
            $head = "# ORCHESTRATOR CALL - role: verifier (same session, new role)`n`nEarlier in this session you read back version $fromRound of this plan as its implementer. Version $([int]$state.approved.version) was approved and another model has implemented it. You now verify that work independently. Your read-back role ends here; the full verifier instructions follow and replace it.`n`n"
            $continue = @{ Adapter = $rec.adapter; Model = $rec.model; SessionId = $contId; Prompt = ($head + $prompt) }
        }
        elseif ($contId -and (Test-SessionCanContinue -Ctx $Ctx -State $state -SessionId $contId)) {
            $rec = $state.sessions.$contId
            $approvedV = [int]$state.approved.version
            $revV = if ($fromReview) { [int]$state.review.round } else { $approvedV }
            if ($revV -lt $approvedV) {
                $taskDir = Get-TaskDir $Ctx $TaskId
                $pdiff = Get-PlanDiffText -OldFile (Join-Path $taskDir "artifacts\plan.v$revV.md") -NewFile (Join-Path $taskDir "artifacts\plan.v$approvedV.md")
                $body = "You reviewed version $revV; version $approvedV was approved. Plan changes from version $revV to version $approvedV (unified diff):`n`n$pdiff`n`n"
            }
            else { $body = "The plan you reviewed (version $approvedV) was approved.`n`n" }
            $amend = Get-ApprovedAmendments $planText
            if ($amend) { $body += "$amend`n`n" }
            $body += $tail
            $continue = @{ Adapter = $rec.adapter; Model = $rec.model; SessionId = $contId; Prompt = (New-FollowUpPrompt -Role 'verifier' -Body $body) }
        }
        # The key is what the answer is valid for: the gate results and the diff the verifier is given.
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $vHash = [BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($gateText + $diff))).Replace('-', '').ToLower()
        $v = Invoke-Role -Ctx $Ctx -Role 'verifier' -Prompt $prompt -Cwd (Get-AgentCwd $Ctx $state.repo) -TimeoutSec (Get-Timeout $Ctx) -Repo $state.repo -ProgressFile (Get-TaskProgressFile $Ctx $TaskId) -TaskId $TaskId -ManualKey ("verifier." + $vHash.Substring(0, 12)) -Continue $continue -Retry:$Retry
        Save-Outbox -Ctx $Ctx -TaskId $TaskId -Role 'verifier' -Name 'verify' -Content $v.Result.Stdout | Out-Null
        Save-Transcript -Ctx $Ctx -TaskId $TaskId -Name "verifier" -Data $v | Out-Null

        $vv = Get-Verdict $v.Result.Stdout
        $vk = if ($vv) { $vv.Kind } else { 'NO_VERDICT' }
        # Deterministic rules first: red gates or a violation fail regardless of what the model says.
        $final = if ($gatesFailed.Count) { 'FAIL' } elseif (@($last.violations).Count) { 'FAIL' } elseif ($vk -eq 'PASS') { 'PASS' } else { 'FAIL' }
        $gatesSummary = if (-not $cmds.Count) { 'none configured' } elseif ($gatesFailed.Count) { "red: $($gatesFailed -join '; ')" } else { 'green' }
        Add-SessionUse -State $state -SessionId $v.SessionId -Adapter $v.Adapter -Model $v.Model -Role 'verifier'
        Set-Prop $state 'verifySession' $v.SessionId
        Set-Prop $state 'verify' ([pscustomobject]@{ adapter = $v.Adapter; model = $v.Model; modelVerdict = $vk; verdict = $final; gates = $gatesSummary; at = (Get-Date).ToString('s') })
        Add-History -Ctx $Ctx -State $state -Event 'verify' -Data @{ verdict = $final; model = $vk; gates = $gatesSummary }
        $status = if ($state.status -eq 'Violation') { 'Violation' } elseif ($final -eq 'PASS') { 'Verified' } else { 'VerifyFailed' }
        Set-Prop $state 'status' $status
        Save-TaskState -Ctx $Ctx -TaskId $TaskId -State $state

        Write-Host "===== VERIFY ($($v.Adapter) / $($v.Model)) - model=$vk gates=$gatesSummary => $final =====" -ForegroundColor Cyan
        Write-Host $v.Result.Stdout
        Write-Host ""
        if ($final -eq 'PASS') { Write-Host "CLOSE by hand, one at a time: commit? merge? delete the branch '$($state.branch)'? Once the plan is filed: close $TaskId. Or: implement $TaskId <next phase>." -ForegroundColor Green }
        else { Write-Host "not passed. fix by hand, or: implement $TaskId <phase> again, then verify." -ForegroundColor Yellow }
        Write-RunSummary $Ctx $state
        [void](Write-LedgerEntry $Ctx $state 'verify')
        Write-OpenCodeStats $Ctx
    }
    finally { Exit-TaskLock $lock }
}

$cfgPath = if ($Config) { $Config } else { Join-Path $OrchDir 'orchestrator.json' }
# What progress.json reports as `command`; the hop functions read it from this scope.
$script:OrchCommand = $Command.ToLower()

try {
    # Inside the try: a config that must not load (D7) is reported as one line, not as a stack trace.
    # help is the exception to D7: without a loadable config it still prints the usage, saying so (D5).
    $ctx = $null
    if ($Command.ToLower() -eq 'help') { try { $ctx = Import-OrchConfig $cfgPath } catch { $ctx = $null } }
    else { $ctx = Import-OrchConfig $cfgPath }
    # Plan 0004 §6.6: the hooks that have no $Ctx of their own (progress, a slot being released) read it
    # here. With no report block every one of them is a no-op.
    $script:ReportCtx = $ctx
    switch ($Command.ToLower()) {
        'help'      { Show-Usage -Ctx $ctx }
        'agents'    { Cmd-Agents -Ctx $ctx }
        'graph'     { Cmd-Graph -Ctx $ctx -Repo $Arg1 -Check:$Check }
        'pools'     { Cmd-Pools -Ctx $ctx }
        'health'    { exit (Cmd-Health -Ctx $ctx -TimeoutSec ([int]$ctx.Config.preflight.timeoutSec)) }
        'new'       { Cmd-New -Ctx $ctx -TaskId $Arg1 -Repo $Arg2 -TaskText $Arg3 -PlanFile $PlanFile -PlanFamily $PlanFamily -AutoRevise:$AutoRevise -NoReadBack:$NoReadBack -DryRun:$DryRun }
        'revise'    { Cmd-Revise -Ctx $ctx -TaskId $Arg1 -Notes $Arg2 -PlanFile $PlanFile -NoReadBack:$NoReadBack -Retry:$Retry }
        'readback'  { Cmd-ReadBack -Ctx $ctx -TaskId $Arg1 -Retry:$Retry }
        'status'    { Cmd-Status -Ctx $ctx -TaskId $Arg1 }
        'queue'     { Cmd-Queue -Ctx $ctx -TaskId $Arg1 }
        'review'    { Cmd-Review -Ctx $ctx -TaskId $Arg1 -NoReadBack:$NoReadBack -Retry:$Retry }
        'answer'    { Cmd-Answer -Ctx $ctx -TaskId $Arg1 -Key $Arg2 -File $Arg3 -Force:$Force }
        'close'     { Cmd-Close -Ctx $ctx -TaskId $Arg1 -Force:$Force }
        'watch'     { Cmd-Watch -Ctx $ctx -TaskId $Arg1 -Every $Every -PollSec $PollSec -Once:$Once -ThenResume:$ThenResume -NoReadBack:$NoReadBack }
        'approve'   { Cmd-Approve -Ctx $ctx -TaskId $Arg1 -Notes $Arg2 -Branch $Branch -Force:$Force }
        'implement' { Cmd-Implement -Ctx $ctx -TaskId $Arg1 -Phase $Arg2 -Force:$Force -Retry:$Retry }
        'verify'    { Cmd-Verify -Ctx $ctx -TaskId $Arg1 -Retry:$Retry }
        default     { Show-Usage -Ctx $ctx; exit 2 }
    }
}
catch {
    # A deferred manual hop is not a failure: the run is left exactly where it was (exit 3).
    if ($_.Exception.Message -like 'DEFERRED:*') {
        Write-Host $_.Exception.Message -ForegroundColor Cyan
        Write-Host 'nothing failed: what ran is recorded, and only the human answer is open.' -ForegroundColor Cyan
        $resume = $null
        if ($_.Exception.Message -match 'answers[\\/]([^\\/]+)\.md') { $resume = Get-ResumeCommand $Matches[1] }
        Write-Host ("  the request (its first lines say where the answer goes): run.ps1 queue {0}" -f $Arg1) -ForegroundColor Cyan
        if ($resume) {
            $phaseArg = if ($resume -eq 'implement') { ' ' + (Get-ImplementPhase $Matches[1]) } else { '' }
            Write-Host ("  then: run.ps1 {0} {1}{2}   (or: run.ps1 watch {1} -ThenResume)" -f $resume, $Arg1, $phaseArg) -ForegroundColor Cyan
        }
        else { Write-Host ("  then rerun the same command (or store it first: run.ps1 answer {0} <key> <file>)" -f $Arg1) -ForegroundColor Cyan }
        exit 3
    }
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
