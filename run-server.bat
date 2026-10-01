@echo off
setlocal
title Remote File Management Server
cd /d "%~dp0RemoteFileManagement.Server\bin\Debug"

if not exist "RemoteFileManagement.Server.exe" (
    echo [ERROR] Server executable not found. Running build.bat first...
    call "%~dp0build.bat"
)

echo Starting Remote File Management Server (.NET Remoting)...
"RemoteFileManagement.Server.exe" %*
