#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "..\DMLib\Result.fs"
#load "Domain\Common.fs"
#load "Domain\InfoGathering.fs"
#load "Domain\Execution.fs"
#load "InfoGathering.fs"

open Domain.Execution
open Domain
open InfoGathering
open FSharpx.Collections

let dirs =
  DirList.create @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded"

let i =
  { Template = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\template.ini"
    DirInfo = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\248 Shell Studded\modinfo.csv" }

let execute (cmd: Command) =
  match cmd with
  | Error e -> printfn "%A" e
  | Ok v ->
    let p = System.IO.Path.Combine(v.Dir, "modinfo.ini")
    System.IO.File.WriteAllText(p, v.Contents)

match gather i with
| Error _ -> ()
| Ok v ->
  v.InfoList
  |> List.map (Domain.Execution.DirModInfo.create dirs v.TemplateContents)
  |> List.iter execute
  |> printfn "%A"

printfn "****************************"
