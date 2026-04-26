param(
  [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

$ErrorActionPreference = 'Stop'
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'

if (-not (Test-Path (Join-Path $dataDir 'sts2.dll'))) {
  throw "sts2.dll not found in $dataDir — check -GameDir"
}

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$modDll = Join-Path $repo 'src\EraserMod\bin\Release\EraserMod.dll'
$injector = Join-Path $repo 'src\Injector\bin\Release\net8.0\EraserMod.Injector.dll'

if (-not (Test-Path $modDll)) { throw "Build EraserMod first ($modDll missing)" }
if (-not (Test-Path $injector)) { throw "Build Injector first ($injector missing)" }

# 1. drop EraserMod.dll into game data dir
Copy-Item $modDll (Join-Path $dataDir 'EraserMod.dll') -Force
Write-Host "Copied EraserMod.dll -> $dataDir"

# 2. patch sts2.dll
& dotnet $injector $dataDir
if ($LASTEXITCODE -ne 0) { throw "Injector failed (exit $LASTEXITCODE)" }

Write-Host ""
Write-Host "Install complete. Hotkeys (while on the map screen):"
Write-Host "  [   shrink eraser"
Write-Host "  ]   grow eraser"
Write-Host "  \   reset to 1.0x"
Write-Host "Config: %LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.txt"
