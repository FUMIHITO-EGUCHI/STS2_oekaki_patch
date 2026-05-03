param(
  [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)
$ErrorActionPreference = 'Stop'
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'

# 1. remove ModManager-based install: <game>/mods/EraserMod/
$modDir = Join-Path $GameDir 'mods\EraserMod'
if (Test-Path $modDir) {
  Remove-Item -Recurse -Force $modDir
  Write-Host "Removed $modDir."
} else {
  Write-Host "No ModManager install found at $modDir."
}

# 2. legacy cleanup: restore sts2.dll if a previous Injector-based install left a backup
$sts2 = Join-Path $dataDir 'sts2.dll'
$backup = Join-Path $dataDir 'sts2.dll.orig'
if (Test-Path $backup) {
  Copy-Item $backup $sts2 -Force
  Remove-Item $backup
  Write-Host "Restored sts2.dll from previous Injector install backup."
}
$legacyModDll = Join-Path $dataDir 'EraserMod.dll'
if (Test-Path $legacyModDll) {
  Remove-Item $legacyModDll
  Write-Host "Removed legacy EraserMod.dll from data_sts2_windows_x86_64."
}

Write-Host "Uninstall complete."
