module InputProcessingWorkflow

open Domain
open System
open System.IO

let (|IsDir|_|) path =
  match File.GetAttributes(path: string) with
  | FileAttributes.Directory -> Some path
  | _ -> None

type EntryParams =
  { InputDir: string
    OutFile: string option }

let private askForOutputFile () =
  printfn "What will be the name of the release file?"
  Console.ReadLine()

let getInput args =
  let t =
    match Array.toList args with
    | [] -> failwith "You must drag and drop a folder (or file) to this app."
    | [ IsDir dir ] ->
      { EntryParams.InputDir = dir
        OutFile = None }
    | [ file ] ->
      { InputDir = Path.GetDirectoryName(file)
        OutFile = Some(Path.GetFileNameWithoutExtension(file)) }
    | dir :: file :: _ -> { InputDir = dir; OutFile = Some file }

  match t.OutFile with
  | Some o ->
    { FullParams.InputDir = t.InputDir
      OutFile = o }
  | None ->
    { InputDir = t.InputDir
      OutFile = askForOutputFile () }
