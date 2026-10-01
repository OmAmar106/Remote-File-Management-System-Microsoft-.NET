@echo off
setlocal
title Remote File Management Client
cd /d "%~dp0RemoteFileManagement.Client\bin\Debug"

if not exist "RemoteFileManagement.Client.exe" (
    echo [ERROR] Client executable not found. Running build.bat first...
    call "%~dp0build.bat"
)

echo Starting Remote File Management Client (Windows Forms)...
start "" "RemoteFileManagement.Client.exe"
