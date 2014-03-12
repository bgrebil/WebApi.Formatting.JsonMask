@echo off
echo.

setlocal EnableDelayedExpansion
set toolsDir=%~dp0tools

%toolsDir%\nuget\nuget.exe restore
