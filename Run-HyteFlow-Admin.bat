@echo off
cd /d "%~dp0"
net session >nul 2>&1
if %errorLevel% == 0 (
    start "" "%~dp0HyteFlow.exe"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dp0HyteFlow.exe' -Verb RunAs -WorkingDirectory '%~dp0'"
)
