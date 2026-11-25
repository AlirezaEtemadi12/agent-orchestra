# Calls the shared engine in Orchestra with this project's orchestrator.json.
function Get-EnginePath {
    $local = Join-Path $PSScriptRoot 'orchestrator.local.json'
    if (-not (Test-Path -LiteralPath $local)) { return $null }
    $path = (Get-Content -LiteralPath $local -Raw -Encoding UTF8 | ConvertFrom-Json).enginePath
    if ($path -and (Test-Path -LiteralPath (Join-Path $path 'engine\run.ps1'))) { return $path }
    return $null
}
$engine = Get-EnginePath
if (-not $engine) {
    Write-Host 'Open Orchestra and run "Check setup" for this project: enginePath in .orchestra\orchestrator.local.json is missing or wrong.' -ForegroundColor Red
    exit 1
}
if ($args -contains '-Config') {
    Write-Host 'Do not pass -Config: the shim always uses this project''s orchestrator.json.' -ForegroundColor Red
    exit 1
}
& (Join-Path $engine 'engine\run.ps1') -Config (Join-Path $PSScriptRoot 'orchestrator.json') @args
exit $LASTEXITCODE
