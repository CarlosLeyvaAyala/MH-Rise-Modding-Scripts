module Execution

open Domain.Execution
open System.IO
open DMLib.IO.Path
open DMLib.Combinators

let execute (cmd: Command) =
  match cmd with
  | Error e -> printfn "%A" e
  | Ok v ->
    let m = "modinfo.ini"
    let p = Path.Combine(v.Dir, m)
    File.WriteAllText(p, v.Contents)
    printfn "Success:\t %s" (v.Dir |> getFileName |> swap combine2 m)

let processData: ProcessData =

  fun v ->
    let dirs = DirList.create v.BaseDir

    v.InfoList
    |> List.map (DirModInfo.create dirs v.TemplateContents)
    |> List.iter execute
