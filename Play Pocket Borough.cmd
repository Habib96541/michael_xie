@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Start-Preview.ps1"
if errorlevel 1 pause
