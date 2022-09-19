module Domain.Execution

open Domain
open System.IO

type DirPath = string
type DirList = private DirList of DirPath array
type ModInfoContents = string

type DirModInfo =
  { Dir: DirPath
    Contents: ModInfoContents }

type Command = Result<DirModInfo, ErrorMessage>
type Commands = Command list

module DirList =
  let create baseDir =
    Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories)
    |> DirList

  let apply f (DirList list) = f list

module DirModInfo =
  let private validateMatch noMatch tooManyMatches possibleMatches =
    match possibleMatches with
    | [] -> Error noMatch
    | [ v ] -> Ok v
    | _ -> Error tooManyMatches

  let private matchErrorMsg msg dirName = $"Directory \"{dirName}\" {msg}."

  let private getOutputDir possibleDirs (info: DirInfo) =
    let getThis (dir: string) =
      let name = info.DirName.ToLower()
      dir.ToLower().EndsWith(name)

    let possibleMatches =
      possibleDirs
      |> DirList.apply (Array.filter getThis)
      |> Array.toList

    let noMatch =
      matchErrorMsg "was not found. Did you change some directory name?" info.DirName

    let tooMany =
      matchErrorMsg "is repeated. Make sure that all yout directories have a different name." info.DirName

    possibleMatches |> validateMatch noMatch tooMany

  let create possibleDirs (template: TemplateContents) (dirInfo: DirInfoToProcess) =
    result {
      let! info = dirInfo
      let! dir = getOutputDir possibleDirs info

      let t =
        template
          .Replace("%name%", info.OptionName)
          .Replace("%desc%", info.Description)

      return { Dir = dir; Contents = t }
    }
