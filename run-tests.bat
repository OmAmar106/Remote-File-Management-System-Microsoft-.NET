@echo off
setlocal
echo ==============================================================
echo   Running Remote File Management Integration Test Suite
echo ==============================================================

set "SERVER_EXE=%~dp0RemoteFileManagement.Server\bin\Debug\RemoteFileManagement.Server.exe"
set "TEST_EXE=%~dp0RemoteFileManagement.Tests\bin\Debug\RemoteFileManagement.Tests.exe"

if not exist "%TEST_EXE%" (
    echo [ERROR] Test executable not found. Running build.bat first...
    call "%~dp0build.bat"
)

echo Starting Server process in background...
start /b "" "%SERVER_EXE%"

rem Wait for server to bind port
ping 127.0.0.1 -n 3 >nul 2>&1

echo.
echo Running integration tests...
"%TEST_EXE%"
set TEST_STATUS=%ERRORLEVEL%

echo.
echo Stopping test server process...
taskkill /IM RemoteFileManagement.Server.exe /F >nul 2>&1

echo.
if %TEST_STATUS% EQU 0 (
    echo ==============================================================
    echo   ALL TESTS PASSED SUCCESSFULLY!
    echo ==============================================================
) else (
    echo ==============================================================
    echo   TESTS ENCOUNTERED FAILURES! (Return Code: %TEST_STATUS%)
    echo ==============================================================
)

exit /b %TEST_STATUS%
