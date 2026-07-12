@echo off
REM Launch Brennen Blender OUTSIDE any agent/shell Job Object via Task Scheduler.
REM Double-click this anytime. Safe to re-run (opens another Blender if one is open).

set "TASK=GrokLaunchBrennenBlender"
set "BLENDER=C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
set "BLEND=%~dp0brennen_combat_rig.blend"

if not exist "%BLENDER%" (
  echo Missing Blender: %BLENDER%
  pause
  exit /b 1
)
if not exist "%BLEND%" (
  echo Missing blend: %BLEND%
  pause
  exit /b 1
)

schtasks /Delete /TN "%TASK%" /F >nul 2>&1
schtasks /Create /TN "%TASK%" /TR "\"%BLENDER%\" \"%BLEND%\"" /SC ONCE /ST 00:00 /F /RL LIMITED /IT >nul
schtasks /Run /TN "%TASK%"
if errorlevel 1 (
  echo Scheduled task failed — falling back to direct start...
  start "" "%BLENDER%" "%BLEND%"
)

echo.
echo Requested Blender start for:
echo   %BLEND%
echo.
echo If no window appears, open Steam -^> Blender, then File -^> Open that .blend path.
echo This console can be closed; Blender should keep running.
timeout /t 3 >nul
exit /b 0
