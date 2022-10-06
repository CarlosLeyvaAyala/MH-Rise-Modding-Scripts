#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "..\DMLib\Result.fs"
#load "Domain\GlobalTypes.fs"
#load "Domain\Config.fs"
#load "Domain\CompressWorkflow.fs"
#load "Domain\InputProcessingWorkflow.fs"
#load "Config.fs"
#load "CompressWorkflow.fs"

open Domain
open System
open System.IO
open System.Text.Json
open Domain.InputProcessingWorkflow
open DMLib

let narwa = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\210 Narwa"
let inner = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\500 Inner"

let args =
  { FullParams.InputDir = DirToProcess inner
    OutFile = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\500 Inner\Pepe"
    ZipExe = ExeName.createForDebugging "C:/Program Files/7-Zip/7z.exe"
    RarExe = None }

let nArgs = { args with InputDir = narwa }

Config.getFolders args.InputDir
Config.getFolders nArgs.InputDir
Config.getFolders @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\EBB Common"

CompressWorkflow.execute args
CompressWorkflow.execute nArgs

/////////////////////////////////////

//open System.IO

//let xxx = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\"

//let k = Path.Combine(xxx, @"..\EBB Distributable Textures")
//printfn "%A" (Path.GetFullPath(k))
