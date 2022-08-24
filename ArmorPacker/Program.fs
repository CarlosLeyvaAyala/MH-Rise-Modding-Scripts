// For more information see https://aka.ms/fsharp-console-apps
open System
open Domain

type EntryParams =
  { InputDir: string
    OutFile: string option }

let askForOutputFile () =
  printfn "What will be the name of the release file?"
  Console.ReadLine()

let getInput args =
  let t =
    match Array.toList args with
    | [] -> failwith "You must drag and drop a folder to this app."
    | [ dir ] ->
      { EntryParams.InputDir = dir
        OutFile = None }
    | dir :: file :: _ -> { InputDir = dir; OutFile = Some file }

  match t.OutFile with
  | Some o ->
    { FullParams.InputDir = t.InputDir
      OutFile = o }
  | None ->
    { InputDir = t.InputDir
      OutFile = askForOutputFile () }

[<EntryPoint>]
let main args =
  let r =
    try
      let input = getInput args
      CompressWorkflow.execute input
      0
    with
    | _ as e ->
      printfn "Error:\n%s" e.Message
      Console.ReadKey() |> ignore
      1

  r
