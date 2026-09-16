@echo off
setlocal

REM Client-only CatosInventorySorter multiplayer test endpoint; same Dedicated world as CatosChestViewer.
set "REPO_DIR=%~dp0.."
for %%I in ("%REPO_DIR%") do set "REPO_DIR=%%~fI"
if not defined CIS_SERVER_INSTALL set "CIS_SERVER_INSTALL=C:\PROGRA~2\Steam\steamapps\common\Valheim dedicated server"
set "CIS_CLIENT_PROFILE=C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosInventorySorter"
if not defined CIS_WORLD_DIR set "CIS_WORLD_DIR=C:\Users\magni\Downloads\Dedicated"
if not defined CIS_SAVE_DIR set "CIS_SAVE_DIR=C:\Users\magni\Downloads"
set "CIS_WORLD_MOUNT=%CIS_SAVE_DIR%\worlds_local\Dedicated"
set "CIS_DLL=%REPO_DIR%\src\CatosInventorySorter\bin\Release\net48\net48\CatosInventorySorter.dll"
set "CIS_CONFIG=%REPO_DIR%\TEST_SERVER\com.catosaur.catosinventorysorter.cfg"

if not exist "%CIS_SERVER_INSTALL%\valheim_server.exe" goto :no_server
if not exist "%CIS_SERVER_INSTALL%\valheim_server_Data\Managed\assembly_valheim.dll" goto :no_game_refs
if not exist "%CIS_CLIENT_PROFILE%\BepInEx" goto :no_client
if not exist "%REPO_DIR%\TEST_SERVER\adminlist.txt" goto :no_adminlist
if not exist "%CIS_WORLD_DIR%\*.db2" goto :no_world
if not exist "%CIS_WORLD_DIR%\*.fwl2" goto :no_world
if not exist "%CIS_CONFIG%" goto :no_config
tasklist /FI "IMAGENAME eq valheim.exe" 2>nul | find /I "valheim.exe" >nul && goto :client_running

echo Building CatosInventorySorter Release DLL...
powershell -NoProfile -ExecutionPolicy Bypass -File "%REPO_DIR%\scripts\build.ps1"
if errorlevel 1 goto :build_failed
if not exist "%CIS_DLL%" goto :no_mod
for /f %%A in ('powershell -NoProfile -Command "$server=(Get-Item ''%CIS_SERVER_INSTALL%\valheim_server_Data\Managed\assembly_valheim.dll'').LastWriteTimeUtc; $reference=(Get-Item ''%REPO_DIR%\lib\assembly_valheim.dll'').LastWriteTimeUtc; if($server -lt $reference){''STALE''}"') do if "%%A"=="STALE" goto :stale_game

if not exist "%CIS_SAVE_DIR%\worlds_local" mkdir "%CIS_SAVE_DIR%\worlds_local"
if not exist "%CIS_WORLD_MOUNT%" mklink /J "%CIS_WORLD_MOUNT%" "%CIS_WORLD_DIR%" >nul
if errorlevel 1 goto :world_mount_failed
copy /Y "%REPO_DIR%\TEST_SERVER\adminlist.txt" "%CIS_SAVE_DIR%\adminlist.txt" >nul
if errorlevel 1 goto :admin_copy_failed
if not exist "%CIS_CLIENT_PROFILE%\BepInEx\plugins" mkdir "%CIS_CLIENT_PROFILE%\BepInEx\plugins"
echo Deploying newest DLL to %CIS_CLIENT_PROFILE%\BepInEx\plugins
copy /Y "%CIS_DLL%" "%CIS_CLIENT_PROFILE%\BepInEx\plugins\CatosInventorySorter.dll" >nul
if errorlevel 1 goto :client_copy_failed
if not exist "%CIS_CLIENT_PROFILE%\BepInEx\config" mkdir "%CIS_CLIENT_PROFILE%\BepInEx\config"
if not exist "%CIS_CLIENT_PROFILE%\BepInEx\config\com.catosaur.catosinventorysorter.cfg" copy /Y "%CIS_CONFIG%" "%CIS_CLIENT_PROFILE%\BepInEx\config\com.catosaur.catosinventorysorter.cfg" >nul

set "SteamAppId=892970"
echo CatosInventorySorter local test server - world Dedicated - port 2462
echo Client DLL: %CIS_CLIENT_PROFILE%\BepInEx\plugins\CatosInventorySorter.dll
echo Server: test endpoint only; no CatosInventorySorter.dll is deployed
echo Save root: %CIS_SAVE_DIR%   Admin list: %CIS_SAVE_DIR%\adminlist.txt
cd /d "%CIS_SERVER_INSTALL%"
"%CIS_SERVER_INSTALL%\valheim_server.exe" -name "CatosInventorySorter Test" -port 2462 -world "Dedicated" -password "696969" -savedir "%CIS_SAVE_DIR%" -public 0
goto :eof

:no_server
echo ERROR: dedicated server executable not found: %CIS_SERVER_INSTALL%
goto :fail
:no_game_refs
echo ERROR: dedicated server game assemblies are missing.
goto :fail
:no_client
echo ERROR: CatosInventorySorter r2modman client profile not found: %CIS_CLIENT_PROFILE%
echo Create a clean profile with BepInExPack Valheim and retry.
goto :fail
:no_adminlist
echo ERROR: TEST_SERVER\adminlist.txt is missing.
goto :fail
:no_world
echo ERROR: existing world .db2/.fwl2 files not found at %CIS_WORLD_DIR%
goto :fail
:no_config
echo ERROR: TEST_SERVER\com.catosaur.catosinventorysorter.cfg is missing.
goto :fail
:client_running
echo ERROR: Valheim is running and may lock the client DLL. Close it before deployment.
goto :fail
:build_failed
echo ERROR: release build failed.
goto :fail
:no_mod
echo ERROR: expected CatosInventorySorter.dll was not produced.
goto :fail
:stale_game
echo ERROR: dedicated server assembly is older than the refreshed local reference.
goto :fail
:world_mount_failed
echo ERROR: could not mount %CIS_WORLD_DIR% at %CIS_WORLD_MOUNT%
goto :fail
:admin_copy_failed
echo ERROR: could not copy adminlist into the active save directory.
goto :fail
:client_copy_failed
echo ERROR: could not deploy the DLL to the client profile.
goto :fail
:fail
pause
exit /b 1
