# Offline mock agent for the orchestrator test. Reads the prompt from stdin, detects the role from the
# "# ORCHESTRATOR CALL - role: <name>" header, and prints a canned reply ending in VERDICT.
# $env:MOCK_MODE drives the misbehaviours the guards must catch; $env:MOCK_LOG records every call.
param([string]$Model = 'mock', [string]$Session = '')

$prompt = [Console]::In.ReadToEnd()

function Get-PromptField {
    param([string]$Text, [string]$Pattern)
    $m = [regex]::Match($Text, $Pattern)
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return ''
}

$role = Get-PromptField $prompt '(?m)^# ORCHESTRATOR CALL - role:\s*(\S+)'
$repo = Get-PromptField $prompt '(?m)^Repo root on disk:\s*(.+?)\s*$'
$phase = Get-PromptField $prompt '(?m)^Phase to execute:\s*(\d+)'
if (-not $phase) { $phase = '1' }
$isReadBack = ($prompt -match 'READ-BACK ONLY')
$mode = $env:MOCK_MODE

# D5: a session id. -Session <id> continues (reuse it); otherwise mint a fresh one. It is reported on
# stderr (ORCH-SESSION:) and appended as a sixth MOCK_LOG column; existing readers split on tab and read
# fixed columns 0-4, so they are unaffected.
$sessionId = if ($Session) { $Session } else { 'ses_mock_' + ([guid]::NewGuid().ToString('N').Substring(0, 8)) }
[Console]::Error.WriteLine("ORCH-SESSION: $sessionId")

if ($env:MOCK_LOG) {
    # Column 4 is the call time in ticks: the quota-pool cases read WHEN a call happened ("the second
    # caller's first call comes after the first caller's end"). Column 5 is the session id (D5). Both
    # appended, so every existing reader - which splits on tab and reads [2] - is untouched.
    Add-Content -LiteralPath $env:MOCK_LOG -Value ("{0}`t{1}`t{2}`t{3}`t{4}`t{5}" -f $role, $Model, $isReadBack, $mode, (Get-Date).Ticks, $sessionId)
}

# MOCK_PROMPT_DIR (opt-in, D7 tests): save every call's prompt, in order, as <n>-<sessionId>-<role>.md so
# a case can read what a hop was actually sent (follow-up vs full, diff, amendments). The session id (no
# hyphens) comes before the role (which may contain hyphens, e.g. plan-reviewer). A counter in the dir keeps
# the order across roles and runs; nothing is written unless the var is set, so other cases are untouched.
if ($env:MOCK_PROMPT_DIR) {
    $pd = $env:MOCK_PROMPT_DIR
    if (-not (Test-Path -LiteralPath $pd)) { New-Item -ItemType Directory -Force -Path $pd | Out-Null }
    $counter = Join-Path $pd 'counter.txt'
    $n = 0
    if (Test-Path -LiteralPath $counter) { try { $n = [int]((Get-Content -LiteralPath $counter -Raw -Encoding UTF8).Trim()) } catch { $n = 0 } }
    $n++
    Set-Content -LiteralPath $counter -Value ([string]$n) -Encoding ASCII -NoNewline
    Set-Content -LiteralPath (Join-Path $pd ('{0:d4}-{1}-{2}.md' -f $n, $sessionId, $role)) -Value $prompt -Encoding UTF8
}

# MOCK_RATE_LIMIT_TIMES=<n>: the first n calls print "429 Too Many Requests" and exit 1, so a
# rate-limited attempt can be tested without a model. The count lives beside the log, so two concurrent
# callers share one budget and the case controls exactly how many 429s happen.
if ($env:MOCK_RATE_LIMIT_TIMES -and $env:MOCK_LOG) {
    $limit = [int]$env:MOCK_RATE_LIMIT_TIMES
    $counter = "$($env:MOCK_LOG).429"
    $n = 0
    if (Test-Path -LiteralPath $counter) { try { $n = [int]((Get-Content -LiteralPath $counter -Raw -Encoding UTF8).Trim()) } catch { $n = 0 } }
    $n++
    Set-Content -LiteralPath $counter -Value ([string]$n) -Encoding ASCII -NoNewline
    if ($n -le $limit) {
        [Console]::Error.WriteLine('429 Too Many Requests')
        exit 1
    }
}

# MOCK_QUOTA_TIMES=<n>: the first n calls print "Usage limit reached" and exit 1, so a quota-exhausted
# hop can be tested without a model. Own counter beside the log, mirroring the 429 budget (D2).
if ($env:MOCK_QUOTA_TIMES -and $env:MOCK_LOG) {
    $limit = [int]$env:MOCK_QUOTA_TIMES
    $counter = "$($env:MOCK_LOG).quota"
    $n = 0
    if (Test-Path -LiteralPath $counter) { try { $n = [int]((Get-Content -LiteralPath $counter -Raw -Encoding UTF8).Trim()) } catch { $n = 0 } }
    $n++
    Set-Content -LiteralPath $counter -Value ([string]$n) -Encoding ASCII -NoNewline
    if ($n -le $limit) {
        [Console]::Error.WriteLine('Usage limit reached')
        exit 1
    }
}

# D1/D8 test behaviours, driven by env vars so the same script also covers them:
#   MOCK_STREAM_MS (+ MOCK_STREAM_INTERVAL_MS) prints a line every N ms for M ms (a hop that streams),
#   MOCK_SLEEP_MS sleeps silently for M ms (a hop that says nothing while it works).
if ($env:MOCK_STREAM_MS) {
    $ms = [int]$env:MOCK_STREAM_MS
    $every = if ($env:MOCK_STREAM_INTERVAL_MS) { [int]$env:MOCK_STREAM_INTERVAL_MS } else { 200 }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $i = 0
    while ($sw.ElapsedMilliseconds -lt $ms) {
        $i++
        [Console]::Out.WriteLine("mock progress line $i")
        [Console]::Out.Flush()
        Start-Sleep -Milliseconds $every
    }
    exit 0
}
if ($env:MOCK_SLEEP_MS) {
    Start-Sleep -Milliseconds ([int]$env:MOCK_SLEEP_MS)
    exit 0
}

function Write-RepoFile {
    param([string]$Name, [string]$Text = 'demo')
    if (-not $repo) { return }
    $path = Join-Path $repo $Name
    $dir = Split-Path -Parent $path
    if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    Set-Content -LiteralPath $path -Value $Text -Encoding UTF8
}

switch ($role) {
    { $mode -eq 'touch-sibling-any' } {
        # A read-only hop during which a DIFFERENT configured repo moves (D2 / T-W1).
        $sibling = Join-Path (Split-Path -Parent $repo) 'Web'
        if (Test-Path -LiteralPath $sibling) { Set-Content -LiteralPath (Join-Path $sibling 'sibling.txt') -Value 'touched' -Encoding UTF8 }
        Write-Output 'VERDICT: PASS'
        break
    }
    'planner' {
        Write-Output 'PLAN from mock planner.'
        Write-Output 'VERDICT: PASS'
    }
    'plan-reviewer' {
        if ($mode -eq 'write-on-review') { Write-RepoFile 'review-wrote.txt' }
        if ($mode -eq 'no-verdict') {
            # A reply that stops mid-way: exit 0, output, no VERDICT line (D4).
            Write-Output 'I read the plan and the files it names, and then the reply stopped.'
        }
        elseif ($mode -eq 'findings') {
            Write-Output 'FINDING [should] File manifest: the phase 2 file has no shape (mock finding)'
            Write-Output 'VERDICT: FINDINGS'
        }
        else {
            Write-Output 'VERDICT: PASS'
        }
    }
    'implementer' {
        if ($isReadBack) {
            if ($mode -eq 'readback-findings') {
                Write-Output 'FINDING [blocker] Phase 1: the exact file content is not stated (mock finding)'
                Write-Output 'VERDICT: FINDINGS'
            }
            else {
                Write-Output 'VERDICT: PASS'
            }
        }
        else {
            switch ($mode) {
                'edit-then-fail' {
                    Write-RepoFile "phase$phase.txt"
                    Write-Output 'I edited the tree and then failed.'
                    exit 1
                }
                'touch-sibling' {
                    $sibling = Join-Path (Split-Path -Parent $repo) 'Web'
                    Set-Content -LiteralPath (Join-Path $sibling 'sibling.txt') -Value 'touched' -Encoding UTF8
                    Write-Output 'VERDICT: PASS'
                }
                'commit' {
                    Write-RepoFile "phase$phase.txt"
                    & git -C $repo add -A 2>&1 | Out-Null
                    & git -C $repo commit -q -m 'mock commit' 2>&1 | Out-Null
                    Write-Output 'VERDICT: PASS'
                }
                default {
                    Write-RepoFile "phase$phase.txt"
                    Write-Output 'VERDICT: PASS'
                }
            }
        }
    }
    'verifier' {
        Write-Output 'VERDICT: PASS'
    }
    default {
        Write-Output 'VERDICT: PASS'
    }
}
