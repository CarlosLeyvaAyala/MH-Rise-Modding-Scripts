#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "..\DMLib\Result.fs"
#load "Domain\Common.fs"
#load "Domain\InfoGathering.fs"
#load "InfoGathering.fs"

open Domain.InfoGathering
open Domain
open InfoGathering
open FSharpx.Collections

let i =
  { Template = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\template.ini"
    DirInfo = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\modinfo.csv" }

gather i
