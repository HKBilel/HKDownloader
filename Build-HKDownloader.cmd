@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0source\build.ps1"
if errorlevel 1 (
  echo.
  echo Build failed.
  pause
  exit /b 1
)
echo.
echo Executable built: HKDownloader.exe
pause
