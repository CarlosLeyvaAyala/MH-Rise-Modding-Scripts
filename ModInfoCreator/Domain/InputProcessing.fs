module Domain.InputProcessing

open System.IO

type CmdArgs = string array
type FileName = private FileName of string
type IniTemplate = IniTemplate of FileName
type DirProperties = DirProperties of FileName

type InputArgs =
  private
    { Template: IniTemplate
      Dirs: DirProperties }

type InputError =
  | InvalidFile of string
  | IncompleteParams of string

type ProcessInput = CmdArgs -> Result<FilesToProcess, ErrorMessage>

module FileName =
  let create (fileName: string) =
    if File.Exists(fileName) then
      FileName fileName |> Ok
    else
      InvalidFile $"File \"{fileName}\" does not exist."
      |> Error

  let value (FileName fileName) = fileName

module InputArgs =
  let invalidInputHelp =
    let sep = "".PadRight(100, '=')

    let head =
      $"\n\n{sep}\n\tMODINFO.INI CREATOR\t::\tCreate modinfo.ini files for your MH Rise mods.\n{sep}\n"

    head
    + """
This program expects these command line arguments:

    Command line    :: ModInfoCreator iniFileTemplate dirInfo

    iniFileTemplate :: Path to the file that will be used as a template to
                       generate all modinfo.ini files.

    dirInfo         :: Path to the file containing data for each dir that will
                       get a modinfo.ini file.
"""

  let create (args: string array) =
    result {
      let! r =
        match args |> Array.toList with
        | []
        | [ _ ] -> IncompleteParams invalidInputHelp |> Error
        | template :: dirInfo :: _ ->
          result {
            let! temp = FileName.create template
            let! dirs = FileName.create dirInfo

            return
              { Template = temp |> IniTemplate
                Dirs = dirs |> DirProperties }
          }

      return r
    }

  let value input = (input.Template, input.Dirs)
