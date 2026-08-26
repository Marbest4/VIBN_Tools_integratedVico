@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Configure-VIBN-Tools.ps1"
if errorlevel 1 (
  echo.
  echo Die Konfiguration konnte nicht abgeschlossen werden.
  pause
)
endlocal
