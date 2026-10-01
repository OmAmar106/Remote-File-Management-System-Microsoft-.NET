@echo off
setlocal
echo ==============================================================
echo   Building Remote File Management System (.NET Remoting)
echo ==============================================================

rem Check Visual Studio MSBuild first
set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%MSBUILD_PATH%" goto :FOUND

set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%MSBUILD_PATH%" goto :FOUND

set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%MSBUILD_PATH%" goto :FOUND

rem Fallback to .NET Framework 4.0 MSBuild
set "MSBUILD_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if exist "%MSBUILD_PATH%" goto :FOUND

set "MSBUILD_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if exist "%MSBUILD_PATH%" goto :FOUND

echo [ERROR] MSBuild.exe was not found on this system.
pause
exit /b 1

:FOUND
echo Using MSBuild: "%MSBUILD_PATH%"
echo.
"%MSBUILD_PATH%" "%~dp0RemoteFileManagementSystem.sln" /t:Clean,Build /p:Configuration=Debug
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed! Check output above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ==============================================================
echo   BUILD SUCCEEDED!
echo   Binaries located in:
echo   - Server: RemoteFileManagement.Server\bin\Debug\RemoteFileManagement.Server.exe
echo   - Client: RemoteFileManagement.Client\bin\Debug\RemoteFileManagement.Client.exe
echo ==============================================================
echo.
pause
