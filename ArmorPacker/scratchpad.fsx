#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "Domain\GlobalTypes.fs"
#load "Domain\CompressWorkflow.fs"

open Domain

SingleArmorOption.create
  "modinfo.ini"
  @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\Leg Option - Mesh pantsu"

open System.IO

let xxx = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\"

let k = Path.Combine(xxx, @"..\EBB Distributable Textures")
printfn "%A" (Path.GetFullPath(k))
