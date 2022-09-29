#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "..\DMLib\Result.fs"
#load "Domain\GlobalTypes.fs"
#load "Domain\Config.fs"
#load "Domain\CompressWorkflow.fs"
#load "Domain\InputProcessingWorkflow.fs"
#load "Config.fs"

open Domain
open System
open System.IO
open System.Text.Json
open Domain.InputProcessingWorkflow
open DMLib

let jsonPath = Path.Combine(__SOURCE_DIRECTORY__, "config.json")
let cfg = Json.get<ConfigJson> jsonPath
let exe = ExeName.create jsonPath cfg.``7zipPath``
//let exe = ExeName.create jsonPath "c:/Program Files/7-Zip/7zG.exe"
let value = exe |> Result.map ExeName.value

match value with
| Ok v -> printfn "Value %A" (v)
| Error e -> printfn "Error: %A" e

let rar = RarExeName.create jsonPath ""

//printfn "***************************************"
//ArmorOption.create d |> printfn "%A"
//printfn "***************************************"

/////////////////////////////////////

open System.IO

let xxx = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\"

let k = Path.Combine(xxx, @"..\EBB Distributable Textures")
printfn "%A" (Path.GetFullPath(k))
