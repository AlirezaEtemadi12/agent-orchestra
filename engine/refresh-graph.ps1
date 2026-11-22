# Rebuild the per-repo code graphs (AST only, no LLM, free). Run before writing a plan; run.ps1
# `new` calls it for the target repo. Failures are reported, never fatal.
#
# Rules this script enforces:
#  1. The repo list comes from orchestrator.json - never a hardcoded one.
#  2. Only `graphify update`; never a full rebuild (`graphify extract`), which would drop the graph's
#     LLM-extracted ("semantic") nodes. A graph holding such nodes is backed up first, and only the
#     newest -KeepBackups (2) backups are kept - these files are 2-20 MB each.
#  3. After a successful update the repo's HEAD is recorded in graphify-out/.orch-head, so
#     `run.ps1 graph -Check` can see that HEAD moved even when graphify did not rewrite graph.json.
#     The sidecar is written only when graphify-out/ is git-ignored in that repo, so this can never
#     dirty a checkout.
#  4. The root merge stays opt-in (-MergeRoot): no root graph is used in this workspace. When asked
#     for, it is refused if the existing root holds a semantic node id that no repo graph has, since
#     the re-merge would drop it.
#
# Usage: refresh-graph.ps1 -Repos Ui            (what run.ps1 new does)
#        refresh-graph.ps1 -All                 (every repo in the config)
#        refresh-graph.ps1 -Repos Ui,Web -MergeRoot   (opt-in cross-repo graph at the root)
[CmdletBinding()]
param(
    [string]$Config,
    [string[]]$Repos = @(),
    [switch]$All,
    [switch]$MergeRoot,
    [switch]$ForceRoot,
    [int]$KeepBackups = 2
)

$ErrorActionPreference = 'Stop'
$OrchDir = $PSScriptRoot
. (Join-Path $OrchDir 'lib\agents.ps1')
. (Join-Path $OrchDir 'lib\gates.ps1')

function Invoke-Graphify {
    # graphify writes progress and warnings to stderr and still exits 0 (e.g. "community set changed
    # since labeling ...", or a missing tree-sitter grammar). Under PS 5.1 with
    # ErrorActionPreference=Stop a native command's stderr becomes a terminating error, which used to
    # abort the whole refresh after the first repo. Run it with stderr as data, judge it by exit code.
    param([string[]]$ArgList)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& graphify @ArgList 2>&1 | ForEach-Object { "$_" })
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $code; Output = $output }
}

$cfgPath = if ($Config) { $Config } else { Join-Path $OrchDir 'orchestrator.json' }
$ctx = Import-OrchConfig $cfgPath
$known = @($ctx.Config.repos.PSObject.Properties.Name)

if ($All) { $targets = $known }
elseif ($Repos.Count) {
    foreach ($r in $Repos) {
        if ($known -notcontains $r) { throw "unknown repo '$r' (have: $($known -join ', '))" }
    }
    $targets = @($Repos | Select-Object -Unique)
}
else {
    throw "nothing to refresh: pass -Repos <name...> or -All (repos: $($known -join ', '))"
}

$failed = @()
foreach ($r in $targets) {
    $repoPath = Join-Path $ctx.Root $ctx.Config.repos.$r
    if (-not (Test-Path -LiteralPath $repoPath)) {
        Write-Host "[$r] not on disk at $repoPath; skipped" -ForegroundColor Yellow
        $failed += $r
        continue
    }
    $graph = Join-Path $repoPath 'graphify-out\graph.json'
    if (Test-GraphHasSemanticNodes $graph) {
        $bak = "$graph.bak-$((Get-Date).ToString('yyyyMMdd-HHmmss'))"
        Copy-Item -LiteralPath $graph -Destination $bak -Force
        # Names are yyyyMMdd-HHmmss, so name order is time order.
        Get-ChildItem -LiteralPath (Split-Path $graph) -Filter 'graph.json.bak-*' |
            Sort-Object Name -Descending | Select-Object -Skip ([Math]::Max(1, $KeepBackups)) |
            Remove-Item -Force
        Write-Host "[$r] graph holds LLM-extracted nodes; backed up to $(Split-Path $bak -Leaf)" -ForegroundColor DarkGray
    }
    $res = Invoke-Graphify @('update', $repoPath)
    if ($res.ExitCode -ne 0) {
        $failed += $r
        Write-Host "[$r] graphify update failed (exit $($res.ExitCode)): $(($res.Output | Select-Object -Last 3) -join ' ')" -ForegroundColor Yellow
        continue
    }
    $warning = @($res.Output | Where-Object { $_ -match '(?i)warning' } | Select-Object -First 1)
    if ($warning.Count) { Write-Host "[$r] graphify: $($warning[0].Trim())" -ForegroundColor DarkYellow }

    $goDir = Join-Path $repoPath 'graphify-out'
    if (Test-Path -LiteralPath $goDir) {
        # The sidecar is a write inside the repo: only safe when git ignores that directory.
        $ignored = (Invoke-Git $repoPath @('check-ignore', '-q', 'graphify-out/graph.json')).Ok
        if ($ignored) {
            $head = Get-GitHead $repoPath
            if ($head) { Set-Content -LiteralPath (Join-Path $goDir '.orch-head') -Value $head -Encoding ASCII }
        }
        else {
            Write-Host "[$r] graphify-out/ is not git-ignored; .orch-head not written" -ForegroundColor Yellow
        }
    }
    Write-Host "[$r] graph updated (AST only)" -ForegroundColor DarkGray
}

if ($MergeRoot) {
    $rootGraph = Join-Path $ctx.Root 'graphify-out\graph.json'
    $graphs = @($known | ForEach-Object {
            Join-Path (Get-RepoPath $ctx $_) 'graphify-out\graph.json'
        } | Where-Object { Test-Path -LiteralPath $_ })
    if ($graphs.Count -eq 0) {
        Write-Host 'no repo graphs; root graph not rebuilt' -ForegroundColor Yellow
    }
    else {
        $lost = @()
        if (Test-Path -LiteralPath $rootGraph) {
            # Refuse only when the root holds a semantic node that no repo graph can supply; a node
            # that a repo graph also has survives the merge, so it is not a reason to refuse.
            $repoIds = @{}
            foreach ($g in $graphs) { foreach ($id in (Get-SemanticIds $g)) { $repoIds[$id] = $true } }
            $lost = @(Get-SemanticIds $rootGraph | Where-Object { -not $repoIds.ContainsKey($_) })
        }
        if ($lost.Count -and -not $ForceRoot) {
            Write-Host "root graph has $($lost.Count) semantic node(s) found in no repo graph (e.g. $($lost[0])); refusing to re-merge (-ForceRoot to override)" -ForegroundColor Yellow
        }
        else {
            New-Item -ItemType Directory -Force -Path (Split-Path $rootGraph) | Out-Null
            $res = Invoke-Graphify (@('merge-graphs') + $graphs + @('--out', $rootGraph))
            if ($res.ExitCode -ne 0) {
                Write-Host "graphify merge-graphs failed (exit $($res.ExitCode)): $(($res.Output | Select-Object -Last 3) -join ' ')" -ForegroundColor Yellow
            }
            elseif ($failed.Count) {
                Write-Host "root graph rebuilt, but $($failed -join ', ') may be stale" -ForegroundColor Yellow
            }
            else {
                Write-Host 'root graph rebuilt' -ForegroundColor DarkGray
            }
        }
    }
}
