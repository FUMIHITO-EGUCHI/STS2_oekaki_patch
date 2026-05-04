@echo off
setlocal EnableDelayedExpansion
chcp 65001 > nul

:: If a folder was dragged onto this .bat, use it; otherwise fall back to the default Steam path.
if not "%~1"=="" (
    set "GAME=%~1"
) else (
    set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

echo === EraserMod Uninstaller ===
echo Game dir: %GAME%
echo.

set "DATADIR=%GAME%\data_sts2_windows_x86_64"
set "MODDIR=%GAME%\mods\EraserMod"

:: Remove ModManager-based install
if exist "%MODDIR%\" (
    rd /s /q "%MODDIR%"
    echo Removed: %MODDIR%
) else (
    echo No ModManager install found at: %MODDIR%
)

:: Legacy Injector cleanup (only relevant for users who installed v0.0.1 via the old injector)
if exist "%DATADIR%\sts2.dll.orig" (
    copy /y "%DATADIR%\sts2.dll.orig" "%DATADIR%\sts2.dll" > nul
    del "%DATADIR%\sts2.dll.orig"
    echo [legacy] Restored sts2.dll from sts2.dll.orig.
)
if exist "%DATADIR%\EraserMod.dll" (
    del "%DATADIR%\EraserMod.dll"
    echo [legacy] Removed EraserMod.dll from data dir.
)

echo.
echo Uninstall complete.
echo.
pause
