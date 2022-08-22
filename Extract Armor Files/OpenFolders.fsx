// let inPath = @"F:\SteamLibrary\steamapps\common\MonsterHunterRise\_exe\RE Tool\"
let inPath = fsi.CommandLineArgs[ 1 ].Replace("\"", "")

open System
open System.IO
open System.Diagnostics

let split (s: string) = s.Split("\n")
let trim (s: string) = s.Trim()
let dir (path: string) = Path.GetDirectoryName(path)

let absDir (path: string) =
    Path.Combine(inPath, "re_chunk_000", path)

let openDir (dir: string) =
    Process.Start(fileName = "explorer.exe", arguments = dir)
    |> ignore

File.ReadAllText(Path.Combine(inPath, "mhrisePC.list"))
|> trim
|> split
|> Array.map dir
|> Array.distinct
|> Array.map absDir
|> Array.iter openDir
