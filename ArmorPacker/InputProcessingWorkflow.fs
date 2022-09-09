module InputProcessingWorkflow

open Domain
open Domain.InputProcessingWorkflow
open System
open System.IO
open DMLib.IO.Path

let private askForOutputFile () =
  printfn "What will be the name of the release file?"

  match Console.ReadLine().Trim() with
  | "" ->
    "You need to define a name for your release file."
    |> NoOutput
    |> Error
  | s -> Ok s

let invalidInputHelp =
  let sep = "".PadRight(100, '=')

  let head =
    $"\n{sep}\n\tARMOR PACKER\t::\tEasily pack your MH Rise armor mods for distribution.\n{sep}\n"

  head
  + """
ERROR: Invalid input directory.

Either you drag and drop the folder you want to distribute to this app or use 
command line arguments.

    Command line :: ArmorPacker input output

           input :: Directory containing a config.ini file and all 
                    subfolders with the options you want to distribute.

                    Due to some peculiarities of the .net Framework, 
                    THIS NAME SHOULD NEVER END WITH THE \ CHARACTER,
                    otherwise this app will never work. 

                    Examples:
                      ArmorPacker "C:\invalid dir\" "My 7z file"
                      ArmorPacker "C:\valid dir\." "My 7z file"
                      ArmorPacker "C:\another valid dir" "My 7z file"
          
          output :: Name of both the *.7z and *.bat files that will be generated.

SUGGESTED USAGE: Create a *.bat file next to "x:\dir to pack\config.ini" with
the following contents:

    ArmorPacker "%~dp0." "My 7z file"

It will pack your mod each time you double click that file and both *.bat and *.7z files
will be created inside "x:\dir to pack\".
"""

let private getCmdArgType: GetCmdArgType =
  fun args ->
    match Array.toList args with
    | [ IsDir dir ] -> DirOnly dir
    | dir :: file :: _ -> DirAndFile(dir, file)
    | []
    | [ _ ] -> InvalidInput invalidInputHelp

let private cmdTypeToParams: CmdTypeToParams =
  fun inputType ->
    match inputType with
    | InvalidInput e -> e |> NoInput |> Error
    | DirOnly d -> Ok { InputDir = d; OutFile = None }
    | DirAndFile (d, f) -> Ok { InputDir = d; OutFile = Some f }

let private getZipExe =
  let jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")
  ExeName.create jsonPath @"C:\Program Files\7-Zip\7z.exe"

let private paramErrorToMsg err =
  match err with
  | NoInput e -> e
  | NoOutput e -> e
  | NoZipExe e -> e
  |> ErrorMsg

let getInput args =
  //let t = getFromCmd args
  result {
    let! t = args |> getCmdArgType |> cmdTypeToParams

    let! r =
      match t.OutFile with
      | Some o ->
        Ok
          { FullParams.InputDir = t.InputDir
            OutFile = o
            ZipExe = getZipExe }
      | None ->
        result {
          let! outFile = askForOutputFile ()

          let rr =
            { InputDir = t.InputDir
              OutFile = outFile
              ZipExe = getZipExe }

          return rr
        }

    return r
  }
  |> Result.mapError paramErrorToMsg
