@echo off
setlocal
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

if not exist "%GAME%\" (
    echo ERROR: Game directory not found.
    echo   %GAME%
    echo.
    echo Usage:
    echo   - Drag the game folder onto uninstall.bat, OR
    echo   - Double-click ^(uses the default Steam path above^)
    echo.
    pause
    exit /b 1
)

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
    if errorlevel 1 (
        echo [legacy] WARNING: Could not delete sts2.dll.orig — delete it manually.
    ) else (
        echo [legacy] Restored sts2.dll from sts2.dll.orig.
    )
)
if exist "%DATADIR%\EraserMod.dll" (
    del "%DATADIR%\EraserMod.dll"
    if errorlevel 1 (
        echo [legacy] WARNING: Could not delete EraserMod.dll — delete it manually.
    ) else (
        echo [legacy] Removed EraserMod.dll from data dir.
    )
)

echo.
echo Uninstall complete.
echo.
pause
