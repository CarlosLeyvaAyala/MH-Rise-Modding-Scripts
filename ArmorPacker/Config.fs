﻿module Config

open System.IO
open System.Text.RegularExpressions
open Domain
open FSharpx.Collections
open DMLib.IO.Path
open DMLib

let private configFileName = "config.ini"

let private getIniFileContents: GetIniFileContents =
  fun iniPath ->
    if not (File.Exists(iniPath)) then
      $"File \"{iniPath}\" must exist before we can continue."
      |> InexistentIniFileError
      |> Error
    else
      iniPath
      |> DMLib.IO.File.fileLines
      |> IniFileContents
      |> Ok

let private getConfigValues varName (IniFileContents cfg) =
  let rx = "(?i)" + varName + @"\s*=\s*(.*)"
  let regex = Regex(rx)

  let (|VarValue|_|) s =
    let m = regex.Match(s)

    if m.Success then
      Some(m.Groups[1].Value)
    else
      None

  cfg
  |> Array.map (fun s ->
    match s with
    | VarValue v -> Some v
    | _ -> None)
  |> Array.catOptions

let private getValue: GetConfigValue =
  fun error varName cfg ->
    match getConfigValues varName cfg with
    | [||] -> Error(NoValue error)
    | [| l |] -> Ok l
    | _ ->
      Error(
        ManyVariables
          $"Your config.ini file has many variables named {varName}. If you only have one of those, tell the programmer to stop being a noob."
      )

let private getExtensions cfg =
  Extensions.create (
    match getValue (NoConfigValueError "") "extensions" cfg with
    | Error _ -> "mdf2,mesh,chain"
    | Ok s -> s
  )

let private getModInternalPath =
  let error =
    "You need to define the modInternalPath variable in your ini file, otherwise where would your mod files be installed by Fluffy?"

  getValue (NoConfigValueError error) "modInternalPath"

/// Common operations when reading file contents.
let internal getIniContentsForVarReading dir fileName =
  (combine2 dir fileName)
  |> getIniFileContents
  |> Result.mapError NoFileError

/// Extracts the string from a ConfigError.
let internal extractErrorMsg =
  function
  | NoFileError (InexistentIniFileError e) -> e
  | ValueError v ->
    match v with
    | NoValue (NoConfigValueError n) -> n
    | ManyVariables v -> v

let get: GetConfigData =
  fun inDir ->
    result {
      let! contents = getIniContentsForVarReading inDir configFileName

      let! relPath =
        contents
        |> getModInternalPath
        |> Result.mapError ValueError

      let ext = contents |> getExtensions
      return { RelDir = relPath; Extensions = ext }
    }
    |> ErrorMsg.map extractErrorMsg

module ModInfo =
  let private getValue error fileName dir varName =
    result {
      let! contents = getIniContentsForVarReading dir fileName

      let! value =
        getValue (NoConfigValueError error) varName contents
        |> Result.mapError ValueError

      return value
    }
    |> ErrorMsg.map extractErrorMsg

  let getName fileName dir =
    getValue $"A mod option must have a name defined in {fileName}" fileName dir "name"

  let getScreenShot fileName dir =
    getValue "Screenshots are optional. Ignore this error." fileName dir "screenshot"
