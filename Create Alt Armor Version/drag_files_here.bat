@echo off
set script=%~dp0%script.fsx

echo Opening FSI. Please wait...
dotnet fsi "%script%" "" %*
pause