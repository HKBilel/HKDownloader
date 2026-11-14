@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0HKDownloader.exe" (
  echo Building HKDownloader v1.0.0...
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0source\build.ps1"
  if errorlevel 1 (
    echo.
    echo ERROR: Could not build the application.
    pause
    exit /b 1
  )
)
start "" "%~dp0HKDownloader.exe"
endlocal
