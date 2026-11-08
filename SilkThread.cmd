@echo off
cd /d "%~dp0"
start "" "%~dp0dist\SilkThread\SilkThread.App.exe" %*
exit /b 0
