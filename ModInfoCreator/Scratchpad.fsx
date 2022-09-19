#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "..\DMLib\Result.fs"
#load "Domain\Common.fs"
#load "Domain\InfoGathering.fs"

open Domain.InfoGathering
open FSharpx.Collections

match DirInfoContents.create @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\modinfo.csv" with
| Error e -> printfn "%A" e
| Ok c ->
  c
  |> DirInfoContents.value
  |> NonEmptyList.toList
  |> List.map (Result.mapError toErrorMsg)
  |> List.map (Result.map DirModInfo.stringValue)
  |> List.iter (printfn "%A")

IniTemplateContents.create @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\template.ini"
