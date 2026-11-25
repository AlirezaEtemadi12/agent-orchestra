# lib/plans.ps1 - plan 0013: a plan is a folder (`plans/NNNN-<slug>/plan.md`) or the old flat file, and its
# OWN header is what the engine reads. Nothing here calls a model, starts a run or touches git: it reads and
# rewrites one Markdown header.

function Read-PlanHeader {
    <#
      D1-D3, D8: { Path, Number, Slug, Title, Repos[], Status } from the plan's own header.
        Number, Slug  the folder name `NNNN-slug`, else the file name.
        Title         the `# Plan NNNN — <title>` line.
        Repos         `**Repos:** Web, Ui` (comma-separated, trimmed); empty when the line is absent.
        Status        the first word after `**Status:**`.
      Everything is '' or @() when the plan does not say it, so an old flat plan keeps working (rule 0.3).
    #>
    param([string]$Path)
    $header = [pscustomobject]@{ Path = $Path; Number = ''; Slug = ''; Title = ''; Repos = @(); Status = '' }
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return $header }

    $leaf = Split-Path -Leaf $Path
    $stem = if ($leaf -match '(?i)\.md$') { $leaf.Substring(0, $leaf.Length - 3) } else { $leaf }
    $parent = Split-Path -Leaf (Split-Path -Parent $Path)
    $name = if ($parent -match '^\d{4}-') { $parent } else { $stem }
    $m = [regex]::Match($name, '^(\d{4})-(\S+)$')
    if ($m.Success) {
        $header.Number = $m.Groups[1].Value
        $header.Slug = $m.Groups[2].Value
    }

    foreach ($line in @(Get-Content -LiteralPath $Path -TotalCount 25 -Encoding UTF8)) {
        if (-not $header.Title) {
            # The dash is an em dash, an en dash or a hyphen; \u escapes keep this file pure ASCII.
            $t = [regex]::Match($line, '^#\s*Plan\s+(\d{4})\s*[\u2014\u2013-]\s*(.+?)\s*$')
            if ($t.Success) {
                if (-not $header.Number) { $header.Number = $t.Groups[1].Value }
                $header.Title = $t.Groups[2].Value
                continue
            }
        }
        if (-not $header.Status) {
            $s = [regex]::Match($line, '^\s*\**\s*Status:?\s*\**\s*:?\s*\**\s*([A-Za-z]+)')
            if ($s.Success) { $header.Status = $s.Groups[1].Value; continue }
        }
        if (-not @($header.Repos).Count) {
            $r = [regex]::Match($line, '^\s*\**\s*Repos:?\s*\**\s*:?\s*(.+)$')
            if ($r.Success) {
                $header.Repos = @($r.Groups[1].Value -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
                continue
            }
        }
    }

    return $header
}

function Set-PlanStatus {
    <#
      D8: replaces the first word after `**Status:**` on that one line and touches nothing else - the BOM
      stays (or does not), the line endings stay, and a plan with no Status line is left alone.
    #>
    param([string]$Path, [string]$Word)
    if (-not $Path -or -not $Word) { return $false }
    if (-not (Test-Path -LiteralPath $Path)) { return $false }

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom -and $text.Length -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }

    $m = [regex]::Match($text, '(?m)^[ \t]*\**\**\s*Status:?\s*\**\s*:?\s*\**\s*([A-Za-z]+)')
    if (-not $m.Success) { return $false }

    $group = $m.Groups[1]
    $updated = $text.Substring(0, $group.Index) + $Word + $text.Substring($group.Index + $group.Length)
    [System.IO.File]::WriteAllText($Path, $updated, (New-Object System.Text.UTF8Encoding($hasBom)))
    return $true
}

function Get-NextPlanNumber {
    # D2: four digits, per workspace: the highest folder or flat-file number in plans/ plus one.
    param([string]$PlansDir)
    $max = 0
    if ($PlansDir -and (Test-Path -LiteralPath $PlansDir)) {
        foreach ($item in @(Get-ChildItem -LiteralPath $PlansDir -ErrorAction SilentlyContinue)) {
            $name = if ($item.PSIsContainer) { $item.Name } else { $item.Name -replace '(?i)\.md$', '' }
            $m = [regex]::Match($name, '^(\d{4})-')
            if ($m.Success) {
                $n = [int]$m.Groups[1].Value
                if ($n -gt $max) { $max = $n }
            }
        }
    }
    return ('{0:0000}' -f ($max + 1))
}
