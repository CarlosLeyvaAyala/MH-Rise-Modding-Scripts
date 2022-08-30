module InputProcessingWorkflow

open Domain
open System
open System.IO
open DMLib.IO.Path

type CmdParams =
  { InputDir: string
    OutFile: string option }

let private askForOutputFile () =
  printfn "What will be the name of the release file?"
  Console.ReadLine()

let private getFromCmd args =
  match Array.toList args with
  | [] -> failwith "You must drag and drop a folder (or file) to this app."
  | [ IsDir dir ] ->
    { CmdParams.InputDir = dir
      OutFile = None }
  | [ file ] ->
    { InputDir = Path.GetDirectoryName(file)
      OutFile = Some(Path.GetFileNameWithoutExtension(file)) }
  | dir :: file :: _ -> { InputDir = dir; OutFile = Some file }

let private getZipExe =
  let jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")
  ExeName.create jsonPath @"C:\Program Files\7-Zip\7z.exe"

let getInput args =
  let t = getFromCmd args

  match t.OutFile with
  | Some o ->
    { FullParams.InputDir = t.InputDir
      OutFile = o
      ZipExe = getZipExe }
  | None ->
    { InputDir = t.InputDir
      OutFile = askForOutputFile ()
      ZipExe = getZipExe }
