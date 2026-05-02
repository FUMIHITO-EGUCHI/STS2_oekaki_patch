param(
  [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

$ErrorActionPreference = 'Stop'
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'

if (-not (Test-Path $GameDir)) {
  throw "Game directory not found: $GameDir — check -GameDir"
}

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$modDll = Join-Path $repo 'src\EraserMod\bin\Release\EraserMod.dll'
$manifest = Join-Path $repo 'src\EraserMod\manifest.json'

Push-Location $repo
try {
  dotnet build 'src\EraserMod' -c Release
  if ($LASTEXITCODE -ne 0) { throw "EraserMod build failed (exit $LASTEXITCODE)" }
} finally {
  Pop-Location
}

if (-not (Test-Path $modDll)) { throw "Build EraserMod first ($modDll missing)" }
if (-not (Test-Path $manifest)) { throw "manifest.json missing ($manifest)" }

# 1. legacy cleanup: restore sts2.dll if a previous Injector-based install left a backup
$sts2 = Join-Path $dataDir 'sts2.dll'
$backup = Join-Path $dataDir 'sts2.dll.orig'
if (Test-Path $backup) {
  Copy-Item $backup $sts2 -Force
  Remove-Item $backup
  Write-Host "Restored sts2.dll from previous Injector install."
}
$legacyModDll = Join-Path $dataDir 'EraserMod.dll'
if (Test-Path $legacyModDll) {
  Remove-Item $legacyModDll
  Write-Host "Removed legacy EraserMod.dll from data_sts2_windows_x86_64."
}

# 2. install via the official ModManager loader: <game>/mods/EraserMod/
$modDir = Join-Path $GameDir 'mods\EraserMod'
New-Item -ItemType Directory -Force $modDir | Out-Null
try {
  Copy-Item $modDll (Join-Path $modDir 'EraserMod.dll') -Force
  Copy-Item $manifest (Join-Path $modDir 'manifest.json') -Force
} catch {
  throw "Failed to copy mod files. Close Slay the Spire 2 and run install.ps1 again. $($_.Exception.Message)"
}
Write-Host "Installed EraserMod -> $modDir"

Write-Host ""
Write-Host "Install complete."
Write-Host "Enable mods in the in-game settings (Mods: ON) before launching a run."
Write-Host ""
Write-Host "Hotkeys (while on the map screen):"
Write-Host "  [   shrink eraser"
Write-Host "  ]   grow eraser"
Write-Host "  \   reset to 1.0x"
Write-Host "  Ctrl+Shift+E   toggle toolbar"
Write-Host "Config: %LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.json"
