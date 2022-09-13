@echo off
set version=1.0
set fileName=Armor Packer
set zipExe="C:\Program Files\7-Zip\7z.exe"
set releaseFile=".\%fileName% %version%.zip"
del %releaseFile%

%zipExe% a -t7z %releaseFile% "..\ArmorPacker\bin\Release\net6.0\*" -spf2 -mx=9
%zipExe% a -t7z %releaseFile% "..\ArmorPacker\pack.bat" -spf2 -mx=9
