@echo off
setlocal
chcp 65001 > nul

:: If a folder was dragged onto this .bat, use it; otherwise fall back to the default Steam path.
if not "%~1"=="" (
    set "GAME=%~1"
) else (
    set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

echo === EraserMod Installer ===
echo Game dir: %GAME%
echo.

if not exist "%GAME%\" (
    echo ERROR: Game directory not found.
    echo   %GAME%
    echo.
    echo Usage:
    echo   - Drag the game folder onto install.bat, OR
    echo   - Double-click ^(uses the default Steam path above^)
    echo.
    pause
    exit /b 1
)

set "HERE=%~dp0"
set "DATADIR=%GAME%\data_sts2_windows_x86_64"
set "MODDIR=%GAME%\mods\EraserMod"

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

:: Install
if not exist "%MODDIR%\" mkdir "%MODDIR%"
copy /y "%HERE%EraserMod.dll"  "%MODDIR%\" > nul
if errorlevel 1 goto :copyerr
copy /y "%HERE%manifest.json"  "%MODDIR%\" > nul
if errorlevel 1 goto :copyerr

echo.
echo Installed: %MODDIR%
echo.
echo Next step: Enable Mods in the in-game settings ^(first launch shows a confirmation dialog^).
echo.
echo Hotkeys ^(map screen^):
echo   [  /  ]         eraser width
echo   Shift+[  /  ]   pencil width
echo   \               reset eraser to 1.0x
echo   Ctrl+Z          undo last stroke
echo   Ctrl+Shift+E    toggle toolbar
echo.
echo Config: %%LOCALAPPDATA%%\MegaCrit\SlayTheSpire2\EraserMod\config.json
echo.
pause
exit /b 0

:copyerr
echo.
echo ERROR: Could not copy files.
echo Close Slay the Spire 2 and run install.bat again.
echo.
pause
exit /b 1
