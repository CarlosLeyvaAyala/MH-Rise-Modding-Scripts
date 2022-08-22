@echo off

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Set here the path to re_chunk_000.pak
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
set re_chunk_path=""

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: IGNORE
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
set script=%~dp0%script.fsx
set bat=%~dp1extract-rise-pak.bat

echo Opening FSI. Please wait...
echo ---------------------------

dotnet fsi "%script%" %1 "%~dp1mhrisePC.list"

echo Executing RE Tool
echo -----------------

if %re_chunk_path% == "" (
    echo Now you can drag and drop re_chunk_000.pak to extract-rise-pak.bat,
    echo but if you really want to automate the process, open drag_full_PClist_here.bat
    echo with any Notepad application and set re_chunk_path="your full chunk path here".
) else (
    "%bat%" "%re_chunk_path%"
)
pause 