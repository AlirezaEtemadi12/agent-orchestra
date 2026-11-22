function Get-RepoPath {
    param($Ctx, [string]$Repo)
    Join-Path $Ctx.Root $Ctx.Config.repos.$Repo
}

function Get-AgentCwd {
    param($Ctx, [string]$Repo)
    $mode = $Ctx.Config.defaults.agentCwd
    if ($mode -eq 'repo') { return (Get-RepoPath $Ctx $Repo) }
    return $Ctx.Root
}

function Test-RepoKnown {
    param($Ctx, [string]$Repo)
    return ($null -ne $Ctx.Config.repos.PSObject.Properties[$Repo])
}

function Invoke-Git {
    param([string]$RepoPath, [string[]]$GitArgs)
    $res = Invoke-External -Exe 'git' -ArgList (@('-C', $RepoPath) + $GitArgs) -TimeoutSec 60
    [pscustomobject]@{ Ok = ($res.ExitCode -eq 0); Stdout = $res.Stdout.Trim(); Stderr = $res.Stderr.Trim() }
}

function Test-IsGitRepo { param([string]$RepoPath) return (Invoke-Git $RepoPath @('rev-parse', '--is-inside-work-tree')).Ok }
function Get-GitBranch { param([string]$RepoPath) return (Invoke-Git $RepoPath @('rev-parse', '--abbrev-ref', 'HEAD')).Stdout }
function Get-GitHead { param([string]$RepoPath) return (Invoke-Git $RepoPath @('rev-parse', 'HEAD')).Stdout }
function Get-GitUser { param([string]$RepoPath) return (Invoke-Git $RepoPath @('config', 'user.name')).Stdout }
function Get-GitStatus { param([string]$RepoPath) return (Invoke-Git $RepoPath @('status', '--short', '--branch')).Stdout }
function Get-GitPorcelain { param([string]$RepoPath) return (Invoke-Git $RepoPath @('status', '--porcelain', '--untracked-files=all')).Stdout }
function Test-GitBranchExists { param([string]$RepoPath, [string]$Branch) return (Invoke-Git $RepoPath @('rev-parse', '--verify', '--quiet', "refs/heads/$Branch")).Ok }

function Get-GitChangedFiles {
    # Files that differ from $Base: tracked changes (committed or not) plus untracked files.
    param([string]$RepoPath, [string]$Base)
    $tracked = (Invoke-Git $RepoPath @('diff', '--name-only', $Base)).Stdout
    $untracked = (Invoke-Git $RepoPath @('ls-files', '--others', '--exclude-standard')).Stdout
    return @(($tracked -split "`n") + ($untracked -split "`n") | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Sort-Object -Unique)
}

function Get-DirtyPaths {
    # D11: the uncommitted paths from porcelain, split into Blocking (gate 3 refuses on these) and
    # Ignored (records under a cleanTree.ignore prefix that never block). Repo-relative, forward slashes.
    param([string]$RepoPath, [string[]]$Ignore = @())
    $blocking = @(); $ignored = @()
    foreach ($l in @((Get-GitPorcelain $RepoPath) -split "`n" | Where-Object { $_.Trim() })) {
        # Invoke-Git trims the output, so the first line may have lost its leading status space.
        $rel = ($l -replace '^[ MADRCU?!]{1,2} ', '').Trim().Trim('"')
        if ($rel -match ' -> ') { $rel = ($rel -split ' -> ')[1] }
        if (-not $rel) { continue }
        $isIgnored = $false
        foreach ($p in $Ignore) { if ($p -and $rel.StartsWith($p)) { $isIgnored = $true; break } }
        if ($isIgnored) { $ignored += $rel } else { $blocking += $rel }
    }
    return [pscustomobject]@{ Blocking = $blocking; Ignored = $ignored }
}

function Write-DirtyTreeWarning {
    # Review and read-back run on a dirty tree; only gate 3 (implement) refuses one. Say so up front, so
    # the owner can keep testing or close the previous plan while gate 2 runs, and is not surprised later.
    param($Ctx, [string]$Repo)
    try {
        $ignore = @()
        if ($Ctx.Config.PSObject.Properties['cleanTree'] -and $Ctx.Config.cleanTree.PSObject.Properties['ignore']) { $ignore = @($Ctx.Config.cleanTree.ignore) }
        $dp = Get-DirtyPaths -RepoPath (Get-RepoPath $Ctx $Repo) -Ignore $ignore
        $n = @($dp.Blocking).Count
        if ($n) {
            $shown = @($dp.Blocking | Select-Object -First 5) -join ', '
            $more = if ($n -gt 5) { ", ... (+$($n - 5))" } else { '' }
            Write-Host "warning: $Repo has $n uncommitted file(s) ($shown$more). Review/read-back run anyway; implement will refuse until the tree is clean." -ForegroundColor Yellow
        }
    }
    catch { }
}

function Get-GitDiff {
    param([string]$RepoPath, [string]$Base, [int]$MaxChars = 60000)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('## git status --short')
    [void]$sb.AppendLine((Invoke-Git $RepoPath @('status', '--short')).Stdout)
    $diffArgs = if ($Base) { @('diff', $Base) } else { @('diff') }
    [void]$sb.AppendLine("## git $($diffArgs -join ' ') (tracked changes)")
    [void]$sb.AppendLine((Invoke-Git $RepoPath $diffArgs).Stdout)
    $untracked = (Invoke-Git $RepoPath @('ls-files', '--others', '--exclude-standard')).Stdout
    foreach ($f in @($untracked -split "`n" | Where-Object { $_.Trim() })) {
        $rel = $f.Trim()
        $full = Join-Path $RepoPath $rel
        [void]$sb.AppendLine("## new file (untracked): $rel")
        if (-not (Test-Path -LiteralPath $full)) { continue }
        $len = (Get-Item -LiteralPath $full).Length
        if ($len -gt 200KB) { [void]$sb.AppendLine("(skipped: $len bytes)"); continue }
        $text = Get-Content -LiteralPath $full -Raw -Encoding UTF8
        if ($text -match "`0") { [void]$sb.AppendLine('(skipped: binary)'); continue }
        [void]$sb.AppendLine($text)
    }
    $out = $sb.ToString()
    if ($out.Length -gt $MaxChars) {
        $out = $out.Substring(0, $MaxChars) + "`n`n(diff truncated at $MaxChars chars; read the files on disk for the rest)"
    }
    return $out
}

function Get-RepoState {
    param([string]$RepoPath)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("branch: $((Invoke-Git $RepoPath @('branch', '--show-current')).Stdout)")
    [void]$sb.AppendLine("user.name: $((Invoke-Git $RepoPath @('config', 'user.name')).Stdout)")
    [void]$sb.AppendLine('local branches:')
    [void]$sb.AppendLine((Invoke-Git $RepoPath @('branch', '--list', '--format=%(refname:short)')).Stdout)
    [void]$sb.AppendLine('status --short:')
    [void]$sb.AppendLine((Invoke-Git $RepoPath @('status', '--short')).Stdout)
    [void]$sb.AppendLine('recent commits:')
    [void]$sb.AppendLine((Invoke-Git $RepoPath @('log', '--oneline', '-8')).Stdout)
    [void]$sb.AppendLine('worktrees:')
    [void]$sb.AppendLine((Invoke-Git $RepoPath @('worktree', 'list')).Stdout)
    return $sb.ToString()
}

function Test-FingerprintIgnoredLine {
    # True when a porcelain line names a path under one of $Ignore (cleanTree.ignore prefixes).
    param([string]$Line, [string[]]$Ignore)
    if (-not $Ignore -or -not $Ignore.Count -or $Line.Length -lt 4) { return $false }
    $path = $Line.Substring(3).Trim()
    if ($path -like '* -> *') { $path = ($path -split ' -> ')[-1].Trim() }   # rename/copy: the new path
    $path = $path.Trim('"') -replace '\\', '/'
    foreach ($i in $Ignore) { if ($path.StartsWith($i, [System.StringComparison]::OrdinalIgnoreCase)) { return $true } }
    return $false
}

function Get-RepoFingerprint {
    # HEAD + branch + porcelain + content hash of the tracked diff + size/mtime of untracked files.
    # Two fingerprints differ iff something in the repo changed (commit, branch switch, edit, new file).
    # Paths under $Ignore (cleanTree.ignore: plans and session logs) are left out on purpose: gate 3
    # already ignores them, and another session writing its own plan file must not look like a
    # read-only role editing the repo - which is exactly what made read-back hops fail.
    param([string]$RepoPath, [string[]]$Ignore = @())
    if (-not (Test-Path -LiteralPath $RepoPath)) { return '(missing)' }
    $head = (Invoke-Git $RepoPath @('rev-parse', 'HEAD')).Stdout
    $branch = (Invoke-Git $RepoPath @('branch', '--show-current')).Stdout
    $porcelain = @((Get-GitPorcelain $RepoPath) -split "`n" | Where-Object { $_ -and -not (Test-FingerprintIgnoredLine -Line $_ -Ignore $Ignore) })
    $pathspec = @('diff', 'HEAD', '--')
    foreach ($i in $Ignore) { $pathspec += ":(exclude,glob)$i**" }
    $diff = (Invoke-Git $RepoPath $pathspec).Stdout
    $untracked = foreach ($l in @($porcelain | Where-Object { $_ -like '`?`? *' })) {
        $rel = $l.Substring(3).Trim('"')
        $p = Join-Path $RepoPath $rel
        if (Test-Path -LiteralPath $p) { $i = Get-Item -LiteralPath $p; "$rel|$($i.Length)|$($i.LastWriteTimeUtc.Ticks)" }
    }
    $raw = "$head`n$branch`n$($porcelain -join "`n")`n$diff`n$($untracked -join "`n")"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return [BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($raw))).Replace('-', '')
}

function Get-WorkspaceFingerprint {
    # -Repos narrows the snapshot to those repos; empty means every configured one.
    param($Ctx, [string[]]$Repos = @())
    $names = if ($Repos -and $Repos.Count) { $Repos } else { @($Ctx.Config.repos.PSObject.Properties.Name) }
    $ignore = @()
    if ($Ctx.Config.PSObject.Properties['cleanTree'] -and $Ctx.Config.cleanTree.PSObject.Properties['ignore']) { $ignore = @($Ctx.Config.cleanTree.ignore) }
    $fp = @{}
    foreach ($r in $names) {
        $fp[$r] = Get-RepoFingerprint (Get-RepoPath $Ctx $r) -Ignore $ignore
    }
    return $fp
}

function Compare-WorkspaceFingerprint {
    param([hashtable]$Before, [hashtable]$After)
    $changed = @()
    foreach ($k in $Before.Keys) { if ($Before[$k] -ne $After[$k]) { $changed += $k } }
    return $changed
}

function Get-PlanPathMentions {
    # Every backticked token in the plan that looks like a path (has a slash or a file extension).
    param([string]$PlanText, [string]$Repo)
    $out = @()
    foreach ($m in [regex]::Matches($PlanText, '`([^`\s]+)`')) {
        $t = $m.Groups[1].Value -replace '\\', '/' -replace '^\./', ''
        $t = $t -replace ('^' + [regex]::Escape($Repo) + '/'), ''
        $t = $t -replace ':\d+(-\d+)?$', ''
        if ($t -match '/' -or $t -match '\.[A-Za-z0-9]{1,6}$') { $out += $t }
    }
    return @($out | Sort-Object -Unique)
}

function Get-FilesOutsidePlan {
    # Heuristic manifest check: a changed file the plan never mentions (by full path or file name).
    # A mention elided at the front (`…Presentation/Pages/Shell/ShellText.cs` or `...Presentation/...`) is
    # a plain path suffix: it may start mid-segment ("ProjectB.Agent.Presentation"), so no "/" is required
    # before it. Plan 0153's manifest wrote rows that way and three in-plan files were flagged.
    param([string[]]$Changed, [string[]]$Mentions)
    $ellipsis = [string][char]0x2026
    $outside = @()
    foreach ($f in $Changed) {
        $hit = $false
        foreach ($m in $Mentions) {
            $tail = $null
            if ($m.StartsWith($ellipsis)) { $tail = $m.Substring(1) }
            elseif ($m.StartsWith('...')) { $tail = $m.Substring(3) }
            if ($tail) {
                if ($tail -match '[/.]' -and $f.EndsWith($tail)) { $hit = $true; break }
                continue
            }
            if ($f -eq $m -or $f.EndsWith('/' + $m) -or ($m.EndsWith('/') -and $f.StartsWith($m))) { $hit = $true; break }
        }
        if (-not $hit) { $outside += $f }
    }
    return $outside
}

function Test-GitBranchName {
    # git check-ref-format's rules for the shapes a plan can name: no "..", no leading or trailing "/",
    # no "//", no "@{", no trailing ".", no component starting with "." or ending with ".lock".
    param([string]$Name)
    if (-not $Name) { return $false }
    if ($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._/-]*$') { return $false }
    if ($Name -match '\.\.|//|@\{|\.lock$|\.$|/\.|/$') { return $false }
    return $true
}

function Get-PlanBranch {
    # The branch the plan itself says to use (`git switch -c <name>` / `git checkout -b <name>`, code blocks
    # included), else the backticked `<user>/<topic>` or `plan-NNNN-<topic>`, else `<user>/<task id>`.
    param([string]$PlanText, [string]$GitUser, [string]$TaskId)
    $user = ($GitUser -replace '\s', '').ToLower()
    foreach ($m in [regex]::Matches($PlanText, 'git\s+(?:switch\s+-c|checkout\s+-b)\s+(?:-\S+\s+)*[`"'']?([^\s`"'']+)')) {
        $name = $m.Groups[1].Value.Trim('`', '"', "'", ',', '.', ')', ';')
        if (Test-GitBranchName $name) { return $name }
    }
    # A plan may name its branch on its own `**Branch:**` line instead of in a git command (Project A does).
    $m = [regex]::Match($PlanText, '(?m)^\*\*Branch:\*\*\s*`([^`]+)`')
    if ($m.Success -and (Test-GitBranchName $m.Groups[1].Value)) { return $m.Groups[1].Value }
    if ($user) {
        $m = [regex]::Match($PlanText, '`(' + [regex]::Escape($user) + '/[a-z0-9][a-z0-9._-]*)`')
        if ($m.Success) { return $m.Groups[1].Value }
    }
    $m = [regex]::Match($PlanText, '`(plan-\d{4}-[a-z0-9][a-z0-9-]*)`')
    if ($m.Success) { return $m.Groups[1].Value }
    $who = if ($user) { $user } else { 'orch' }
    return "$who/$($TaskId.ToLower())"
}

function Get-PlanNumber {
    # The plan's four-digit number: from its branch (`plan-NNNN-<topic>`), else from its title (`# Plan NNNN`).
    # Empty when neither names one - `close` then cannot find the filed copy and needs -Force.
    param([string]$Branch, [string]$PlanText)
    $m = [regex]::Match([string]$Branch, '(?:^|/)plan-(\d{4})-')
    if ($m.Success) { return $m.Groups[1].Value }
    $m = [regex]::Match([string]$PlanText, '(?m)^#\s*Plan\s+(\d{4})\b')
    if ($m.Success) { return $m.Groups[1].Value }
    return ''
}

function Get-PlanSlug {
    # Project A files plans by slug, never by number: `.ai/plans/done/<slug>.md` and `<person>/<topic>`
    # branches. The slug is the branch's topic, else the topic on the plan's own **Branch:** line.
    # Empty when neither names one - `close` then cannot find the filed copy and needs -Force.
    param([string]$Branch, [string]$PlanText)
    if ([string]$Branch -match '^[^/]+/(.+)$') { return $Matches[1] }
    $m = [regex]::Match([string]$PlanText, '(?m)^\*\*Branch:\*\*\s*`([^`]+)`')
    if ($m.Success -and $m.Groups[1].Value -match '^[^/]+/(.+)$') { return $Matches[1] }
    return ''
}

function Test-Gate2ModelDiffer {
    param($Ctx, [string]$PlannerFamily, [string]$ReviewerFamily)
    if (-not $Ctx.Config.rules.reviewerModelMustDifferFromPlanner) { return $true }
    return ($PlannerFamily -ne $ReviewerFamily)
}

function Get-SemanticIds {
    # Ids of LLM-extracted nodes ("_origin": "semantic"). Regex over the raw text: parsing a 20+ MB
    # graph with ConvertFrom-Json in PowerShell 5.1 is far slower. Used by the opt-in root merge.
    param([string]$GraphPath)
    if (-not (Test-Path -LiteralPath $GraphPath)) { return @() }
    $t = Get-Content -LiteralPath $GraphPath -Raw -Encoding UTF8
    @([regex]::Matches($t, '\{[^{}]*"_origin"\s*:\s*"semantic"[^{}]*\}') | ForEach-Object { ([regex]::Match($_.Value, '"id"\s*:\s*"([^"]+)"')).Groups[1].Value } | Where-Object { $_ })
}

function Test-GraphHasSemanticNodes {
    # LLM-extracted nodes carry _origin: "semantic". A raw text test is enough and avoids parsing a
    # 19 MB graph in PowerShell 5.1.
    param([string]$GraphPath)
    if (-not (Test-Path -LiteralPath $GraphPath)) { return $false }
    return ((Get-Content -LiteralPath $GraphPath -Raw -Encoding UTF8) -match '"_origin"\s*:\s*"semantic"')
}

function Format-Age {
    param($Span)
    if ($null -eq $Span) { return '-' }
    if ($Span.TotalDays -ge 1) { return ('{0:n1}d' -f $Span.TotalDays) }
    if ($Span.TotalHours -ge 1) { return ('{0:n1}h' -f $Span.TotalHours) }
    return ('{0:n0}m' -f $Span.TotalMinutes)
}

function Get-GraphStatus {
    # Read-only. A graph is stale when the code it describes changed after it was built:
    #  - HEAD differs from the commit recorded at the last refresh (.orch-head; covers a checkout of
    #    an older commit, which a commit-time check misses), or HEAD's commit is newer;
    #  - an uncommitted or untracked file was modified after the graph (the in-progress phase case).
    param([string]$RepoPath)
    $graph = Join-Path $RepoPath 'graphify-out\graph.json'
    $sidecar = Join-Path $RepoPath 'graphify-out\.orch-head'
    $line = (Invoke-Git $RepoPath @('log', '-1', '--format=%H %ct')).Stdout
    $sha = ''; $commitAt = $null
    if ($line) {
        $p = @($line -split '\s+'); $sha = $p[0]
        if ($p.Count -ge 2) { $commitAt = [DateTimeOffset]::FromUnixTimeSeconds([long]$p[1]).LocalDateTime }
    }
    $short = if ($sha) { $sha.Substring(0, 7) } else { '-' }
    if (-not (Test-Path -LiteralPath $graph)) {
        return [pscustomobject]@{ State = 'missing'; Reason = 'no graph.json'; Head = $short; GraphAt = $null; Behind = $null; Semantic = $false }
    }
    $graphAt = (Get-Item -LiteralPath $graph).LastWriteTime
    $reasons = @(); $behind = $null
    $recorded = if (Test-Path -LiteralPath $sidecar) { (Get-Content -LiteralPath $sidecar -Raw).Trim() } else { '' }
    # The sidecar wins when present: `graphify update` leaves graph.json untouched when no code file
    # changed, so its mtime can be older than a commit that the refresh did cover.
    if ($recorded) {
        if ($sha -and $recorded -ne $sha) {
            $reasons += "HEAD moved since refresh ($($recorded.Substring(0, [Math]::Min(7, $recorded.Length))) -> $short)"
        }
    }
    elseif ($commitAt -and $graphAt -lt $commitAt) { $reasons += 'commit newer than graph'; $behind = $commitAt - $graphAt }
    $newer = @()
    foreach ($l in @((Get-GitPorcelain $RepoPath) -split "`n" | Where-Object { $_.Trim() })) {
        # Invoke-Git trims the output, so the first line may have lost its leading status space.
        $rel = ($l -replace '^[ MADRCU?!]{1,2} ', '').Trim().Trim('"')
        if ($rel -match ' -> ') { $rel = ($rel -split ' -> ')[1] }
        # Docs never stale the graph: `graphify update` does not rewrite graph.json for a .md, so counting
        # them would make the check un-clearable (the .orch-head sidecar stays authoritative for code).
        if ($rel -match '\.md$') { continue }
        if ($rel -like 'graphify-out/*') { continue }
        $f = Join-Path $RepoPath $rel
        if ((Test-Path -LiteralPath $f -PathType Leaf) -and (Get-Item -LiteralPath $f).LastWriteTime -gt $graphAt) { $newer += $rel }
    }
    if ($newer.Count) { $reasons += "$($newer.Count) uncommitted file(s) newer than graph" }
    [pscustomobject]@{
        State    = $(if ($reasons.Count) { 'stale' } else { 'fresh' })
        Reason   = $(if ($reasons.Count) { $reasons -join '; ' } else { '' })
        Head     = $short
        GraphAt  = $graphAt
        Behind   = $behind
        Semantic = (Test-GraphHasSemanticNodes $graph)
    }
}
