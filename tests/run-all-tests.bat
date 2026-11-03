@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-all-tests.ps1" %*
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" (
  echo.
  echo PRE-DEPLOYMENT CHECK FAILED. Deployment must not continue.
) else (
  echo.
  echo ALL TEST SUITES PASSED. Deployment may proceed.
)
exit /b %RESULT%
