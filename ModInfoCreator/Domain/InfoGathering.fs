module Domain.InfoGathering

open FSharpx.Collections
open DMLib.String
open System.IO
open System
open System.Text.RegularExpressions

type DirName = DirName of NonEmptyString
type OptionName = OptionName of NonEmptyString
type Description = string
type DirNameStr = string
type OptionNameStr = string

type DirModInfo =
  private
    { Dir: DirName
      OptionName: OptionName
      Description: Description }

type RawModInfo = (DirNameStr * OptionNameStr * Description)
type DirModInfoToRawStr = DirModInfo -> RawModInfo


type GatheringError =
  //| NonExistentDir of string // Some dir in dir info points to a non existent dir. MOVE TO EXECUTION DOMAIN
  | EmptyTemplate of string // Template file is empty
  | InvalidTemplate of string // Template has no "name" variable
  | EmptyDirInfo of string // DirInfo file is blank
  | InvalidDirInfo of string // Incomplete dir info

type IniTemplateContents = private IniTemplateContents of NonEmptyString
type DirInfoContents = private DirInfoContents of NonEmptyList<Result<DirModInfo, GatheringError>>

let toErrorMsg e =
  "ERROR "
  + match e with
    | EmptyTemplate m -> $"with Template file: {m}"
    | InvalidTemplate m -> $"in Template file contents: {m}"
    | EmptyDirInfo m -> $"with DirInfo file: {m}"
    | InvalidDirInfo m -> $"in DirInfo contents: {m}"
  |> ErrorMessage


module DirModInfo =
  let private validSeparators = [| '\t'; '|' |]

  let private trySplit (str: string) separator =
    let r = str.Split(separator: char)

    if r.Length < 3 then
      InvalidDirInfo
        $"Expected \"Dir name{separator}Option name{separator}Option description\" but got \"{str}\". Did you properly separated values with tabs or the | symbol?."
      |> Error
    else
      Ok r

  let private split separators str =
    let s = separators |> Array.map (trySplit str)
    let valid = s |> Array.filter Result.isOk

    if valid.Length > 0 then
      valid[0]
    else
      s[s.Length - 1]

  let private noDirName line _ =
    $"Error on \"{line}\". Row has no \"Dir name\" to work on."
    |> InvalidDirInfo

  let private noOptionName line _ =
    $"Error on \"{line}\". Row has no \"Option name\"."
    |> InvalidDirInfo

  let create (str: string) =
    result {
      let! strings = split validSeparators str

      let validateStr errorMsg index =
        strings[ index ].Trim()
        |> NonEmptyString.create
        |> Result.mapError (errorMsg str)

      let! dirName = validateStr noDirName 0
      let! optionName = validateStr noOptionName 1

      return
        { Dir = dirName |> DirName
          OptionName = optionName |> OptionName
          Description = strings[2] }
    }

  let value dirInfo =
    (dirInfo.Dir, dirInfo.OptionName, dirInfo.Description)

  let stringValue: DirModInfoToRawStr =
    fun dirInfo ->
      let (DirName dir, OptionName option, desc) = value dirInfo
      (NonEmptyString.value dir, NonEmptyString.value option, desc)


module internal FileOps =
  let readAllLinesTrimmed path =
    let r =
      File.ReadAllLines(path)
      |> Array.filter (fun s -> not (s.Trim() = String.Empty))
      |> Array.toList

    match r with
    | [] -> None
    | head :: tail -> NonEmptyList.create head tail |> Some

  let emptyPathMsg path = $"File \"{path}\" is empty."

  let returnError errorType path =
    path |> emptyPathMsg |> errorType |> Error

open FileOps
open DMLib.String.NonEmptyString
open DMLib.String.NonEmptyString


module DirInfoContents =
  let create (path: DirInfoPath) =
    match FileOps.readAllLinesTrimmed path with
    | None -> FileOps.returnError EmptyDirInfo path
    | Some list ->
      list
      |> NonEmptyList.map DirModInfo.create
      |> DirInfoContents
      |> Ok

  let value (DirInfoContents contents) = contents


module IniTemplateContents =
  let private validate arr =
    let rx = Regex("^name=")

    match arr |> Array.filter (fun s -> rx.Match(s).Success) with
    | [||] ->
      InvalidTemplate "Your template file NEEDS to define a \"name\" variable."
      |> Error
    | _ -> arr |> toStrWithNl |> Ok

  let private emptyPathError path _ =
    path |> FileOps.emptyPathMsg |> EmptyTemplate

  let create path =
    let toEmptyError = Result.mapError (emptyPathError path)

    result {
      let! content =
        File.ReadAllText(path)
        |> trim
        |> NonEmptyString.create
        |> toEmptyError

      let! validated =
        content
        |> NonEmptyString.apply (fun s -> s.Split("\n"))
        |> Array.map trim
        |> validate

      let! r = NonEmptyString.create validated |> toEmptyError

      return r
    }
