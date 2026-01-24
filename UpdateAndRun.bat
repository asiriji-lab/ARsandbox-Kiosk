@echo off
title AR Sandbox Kiosk Wrapper
color 0A

:: Kiosk Loop
:loop
cls
echo ====================================================
echo   AR SANDBOX MUSEUM LAUNCHER
echo ====================================================
echo.

:: 1. Attempt Auto-Update
echo [1/3] Checking for updates...
git pull origin main
if %ERRORLEVEL% NEQ 0 (
    echo    ! Update failed or offline. Skipping...
) else (
    echo    + Update successful.
)

:: 2. Launch Application
echo.
echo [2/3] Launching Sandbox...
echo    (Press CTRL+C to stop the loop)
echo.

:: IMPORTANT: Change "ARSandbox.exe" to your actual build name!
start /wait ARSandbox.exe

:: 3. Crash Recovery / Restart
echo.
echo [3/3] Application closed. Restarting in 5 seconds...
timeout /t 5 >nul
goto loop
