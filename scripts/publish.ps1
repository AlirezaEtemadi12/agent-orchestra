# Plan 0012 D2b: builds the app once and puts everything it needs beside the exe.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$app = Join-Path $dist 'Orchestra-win-x64'
$zip = Join-Path $dist 'Orchestra-win-x64.zip'

Push-Location $root

try {
    if (-not (Test-Path (Join-Path $root 'ui\node_modules'))) {
        Write-Host '1/6 npm ci'
        npm --prefix ui ci
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    Write-Host '2/6 npm run build'
    npm --prefix ui run build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host '3/6 dotnet publish'
    dotnet publish api/src/Orchestra.Desktop -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $app
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host '4/6 wwwroot, engine and appsettings.json'
    $wwwroot = Join-Path $app 'wwwroot'
    if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
    Copy-Item -Recurse (Join-Path $root 'ui\out') $wwwroot

    $engine = Join-Path $app 'engine'
    if (Test-Path $engine) { Remove-Item -Recurse -Force $engine }
    Copy-Item -Recurse (Join-Path $root 'engine') $engine
    Remove-Item -Recurse -Force (Join-Path $engine 'tests') -ErrorAction SilentlyContinue

    # Without it Kestrel takes its own port, and the window opens on nothing.
    Copy-Item -Force (Join-Path $root 'api\src\Orchestra.Api\appsettings.json') $app

    Write-Host '5/6 zip'
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path $app -DestinationPath $zip -CompressionLevel Optimal

    Write-Host "6/6 done: $zip"
}
finally {
    Pop-Location
}
