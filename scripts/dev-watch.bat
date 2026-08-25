@echo off
REM Development watch script - auto-rebuild and restart on file changes
REM This runs the PowerShell file watcher

powershell -ExecutionPolicy Bypass -File "%~dp0dev-watch.ps1"
pause
