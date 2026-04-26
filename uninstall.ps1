param(
  [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)
$ErrorActionPreference = 'Stop'
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'
$sts2 = Join-Path $dataDir 'sts2.dll'
$backup = Join-Path $dataDir 'sts2.dll.orig'
$modDll = Join-Path $dataDir 'EraserMod.dll'

if (Test-Path $backup) {
  Copy-Item $backup $sts2 -Force
  Remove-Item $backup
  Write-Host "Restored sts2.dll from backup."
} else {
  Write-Host "No backup found — sts2.dll left untouched. Verify integrity via Steam if needed."
}

if (Test-Path $modDll) { Remove-Item $modDll; Write-Host "Removed EraserMod.dll." }
Write-Host "Uninstall complete."
