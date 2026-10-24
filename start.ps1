# Starts StudySpace: imports the content packs, runs the API on 127.0.0.1:5080 and the web app on http://localhost:5173.
# Run from anywhere:  .\start.cmd   (or: powershell -File start.ps1).  Press Ctrl+C to stop both.
param([switch]$NoBrowser)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$db = Join-Path $root 'studyspace.db'

foreach ($tool in 'dotnet', 'node', 'npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "'$tool' was not found on PATH. Install it first." }
}

if (-not (Test-Path (Join-Path $root 'frontend\node_modules'))) {
    Write-Host 'Installing frontend packages (first run only)...'
    npm --prefix (Join-Path $root 'frontend') ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
}

# Importing is safe to repeat: a pack version that is already imported is skipped.
$packs = Get-ChildItem (Join-Path $root 'content') -Directory |
    Where-Object { $_.Name -notlike '_*' } |
    ForEach-Object { Get-ChildItem $_.FullName -Directory } |
    ForEach-Object { Get-ChildItem $_.FullName -Directory } |
    Sort-Object FullName
foreach ($pack in $packs) {
    Write-Host "Importing $($pack.FullName.Substring($root.Length + 1))"
    dotnet run --project (Join-Path $root 'tools\StudySpace.Import') -- import $pack.FullName --db $db
    if ($LASTEXITCODE -ne 0) { throw "Import failed for $($pack.FullName)." }
}

# --no-launch-profile: the launch profile would pick port 5018, but the web app talks to 5080.
$env:ConnectionStrings__Default = "Data Source=$db"
$api = Start-Process dotnet -PassThru -ArgumentList 'run', '--project', "`"$(Join-Path $root 'backend\src\StudySpace.Api')`"", '--no-launch-profile'
try {
    Write-Host 'Starting the web app. Press Ctrl+C to stop.'
    $viteArgs = @('--prefix', (Join-Path $root 'frontend'), 'run', 'dev')
    if (-not $NoBrowser) { $viteArgs += @('--', '--open') }
    npm @viteArgs
}
finally {
    # dotnet run spawns the API as a child process, so stop the whole tree.
    if ($api -and -not $api.HasExited) { taskkill /PID $api.Id /T /F | Out-Null }
}
