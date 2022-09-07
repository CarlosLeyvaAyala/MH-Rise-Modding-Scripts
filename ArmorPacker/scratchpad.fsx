#r "nuget: FSharpx.Collections"
#load "..\DMLib\Library.fs"
#load "Domain\GlobalTypes.fs"
#load "Domain\Config.fs"
#load "Domain\CompressWorkflow.fs"
#load "Config.fs"

open Domain

let cfg =
  match Config.get @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\342 Storge" with
  | Ok v -> v
  | Error _ ->
    { RelDir = @"natives\STM\player\mod\f\pl342"
      Extensions = "mdf2|mesh|chain" |> Extensions.create
      OptionsPrefix = "sick gains 342" }

let getters =
  { ArmorOptionValues.Name = Config.ModInfo.getName
    Screenshot = Config.ModInfo.getScreenShot }

let d =
  { ArmorOptionCreationData.Dir = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\342 Storge\00 Base"
    ModInfoFile = "modinfo.ini"
    Config = cfg
    Getters = getters }

ArmorOption.create d |> printfn "%A"

/////////////////////////////////////

open System.IO

let xxx = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\"

let k = Path.Combine(xxx, @"..\EBB Distributable Textures")
printfn "%A" (Path.GetFullPath(k))
