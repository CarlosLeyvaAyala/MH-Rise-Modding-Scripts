module Config

open System.IO
open System.Text.RegularExpressions
open Domain
open FSharpx.Collections
open DMLib.IO.Path
open DMLib.String

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
  let rx = "(?i)^" + varName + @"\s*=\s*(.*)"
  let regex = Regex(rx)

  let (|VarValue|_|) s =
    let m = regex.Match(s)

    if m.Success then
      let v = m.Groups[1].Value
      if v.Trim() = "" then None else Some(v)
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
    | Error _ -> Extensions.defaultValue
    | Ok s -> s
  )

let private getModInternalPath =
  let error =
    "You need to define the \"modInternalPath\" variable in your ini file, otherwise where would your mod files be installed by Fluffy?"

  getValue (NoConfigValueError error) "modInternalPath"

let private getOptionsPrefix =
  let error =
    "You need to define the \"optionsPrefix\" variable in your ini file, otherwise you may get name conflicts inside Fluffy."

  getValue (NoConfigValueError error) "optionsPrefix"

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

      let getRequiredValue f =
        contents |> f |> Result.mapError ValueError

      let! relPath = getRequiredValue getModInternalPath
      let! optionsPrefix = getRequiredValue getOptionsPrefix
      let ext = contents |> getExtensions

      return
        { RelDir = relPath
          Extensions = ext
          OptionsPrefix = optionsPrefix }
    }
    |> ErrorMsg.map extractErrorMsg

let getFolders inDir =
  let dirs =
    Directory.GetFiles(inDir, configFileName, SearchOption.AllDirectories)
    |> Array.map (fun s -> Path.GetDirectoryName(s))

  match dirs |> Array.toList with
  | [] ->
    ErrorMsg $"Your input folder must contain at least one {encloseQuotes configFileName} file."
    |> Error
  | h :: t -> NonEmptyList.create h t |> Ok


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

  let getName: GetModInfoVariable =
    fun fileName dir -> getValue $"A mod option must have a name defined in {fileName}" fileName dir "name"

  let getScreenShot: GetModInfoVariable =
    fun fileName dir -> getValue "Screenshots are optional. Ignore this error." fileName dir "screenshot"
