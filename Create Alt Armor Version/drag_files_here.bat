@echo off
set script=%~dp0%script.fsx
dotnet fsi "%script%" "" %*
pause