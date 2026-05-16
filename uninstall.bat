@echo off

:: --- keep-open trick ---
:: If double-clicked, %CMDCMDLINE% contains "cmd /c ...". Re-launch under
:: `cmd /k` once so the console stays open even on parse / runtime errors
:: (pause inside the script cannot catch parse errors). KEEPOPEN guards
:: against infinite recursion.
if "%KEEPOPEN%"=="1" goto :body
echo %CMDCMDLINE% | findstr /i /c:"/c " > nul
if errorlevel 1 goto :body
set "KEEPOPEN=1"
cmd /k "%~f0" %*
exit /b

:body
setlocal
chcp 65001 > nul

:: If a folder was dragged onto this .bat, use it; otherwise fall back to the default Steam path.
:: NOTE: We avoid `if (...) else (...)` blocks throughout this script because expanding
::       %GAME% (which contains the literal `(x86)`) inside a parenthesised block makes
::       cmd treat the inner `)` as the block terminator. See: poison characters in batch.
set "GAME=%~1"
if not defined GAME set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"

echo === EraserMod Uninstaller ===
echo Game dir: %GAME%
echo.

if not exist "%GAME%\" goto :no_game_dir

set "DATADIR=%GAME%\data_sts2_windows_x86_64"
set "MODDIR=%GAME%\mods\EraserMod"

:: Remove ModManager-based install
if not exist "%MODDIR%\" goto :no_moddir
rd /s /q "%MODDIR%"
echo Removed: %MODDIR%
goto :after_moddir

:no_moddir
echo No ModManager install found at: %MODDIR%

:after_moddir

:: Legacy Injector cleanup (only relevant for users who installed v0.0.1 via the old injector)
if exist "%DATADIR%\sts2.dll.orig" call :restore_legacy_sts2
if exist "%DATADIR%\EraserMod.dll" call :remove_legacy_mod_dll

echo.
echo Uninstall complete.
echo.
pause
exit /b 0

:restore_legacy_sts2
copy /y "%DATADIR%\sts2.dll.orig" "%DATADIR%\sts2.dll" > nul
del "%DATADIR%\sts2.dll.orig"
if errorlevel 1 echo [legacy] WARNING: Could not delete sts2.dll.orig — delete it manually.
if not errorlevel 1 echo [legacy] Restored sts2.dll from sts2.dll.orig.
exit /b 0

:remove_legacy_mod_dll
del "%DATADIR%\EraserMod.dll"
if errorlevel 1 echo [legacy] WARNING: Could not delete EraserMod.dll — delete it manually.
if not errorlevel 1 echo [legacy] Removed EraserMod.dll from data dir.
exit /b 0

:no_game_dir
echo ERROR: Game directory not found.
echo   %GAME%
echo.
echo Usage:
echo   - Drag the game folder onto uninstall.bat, OR
echo   - Double-click ^(uses the default Steam path above^)
echo.
pause
exit /b 1
