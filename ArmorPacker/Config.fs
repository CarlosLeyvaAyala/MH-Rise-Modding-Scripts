module Config

open System.IO
open System.Text.RegularExpressions
open Domain
open FSharpx.Collections
open DMLib.IO.Path
open DMLib.Combinators

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

// TODO Delete
//let private getRawConfig fileName dir =
//  let f = Path.Combine(dir, fileName)
//  if not (File.Exists(f)) then
//    failwith $"File \"{f}\" must exist before we can continue."
//  else
//    DMLib.IO.File.fileLines f

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

// TODO: Delete
//let private getConfigValue errorMsg cfg varName : IniValue =
//  let rx = "(?i)" + varName + @"\s*=\s*(.*)"
//  let regex = Regex(rx)
//  match cfg
//        |> Array.filter (fun s -> regex.Match(s).Success)
//    with
//  | [||] -> Error(NoValue errorMsg)
//  | [| l |] -> Ok(regex.Match(l).Groups[1].Value)
//  | _ ->
//    Error(ManyVariables $"Your config.ini file has many variables named {varName}. You should have only one of those.")

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

let private createCfgData (aTuple: IniValue * Extensions) =
  match fst aTuple with
  | Error e -> Error e
  | Ok r -> Ok { RelDir = r; Extensions = snd aTuple }

let get: GetConfigData =
  fun inDir ->
    getIniFileContents (combine2 inDir "config.ini")
    |> fork (Result.map getModInternalPath) (Result.map getExtensions)
    |> join2
    |> Result.mapError NoFileError
    |> Result.bind (createCfgData >> Result.mapError ValueError)

module ModInfo =
  let private getValue fileName dir varName =
    getConfigValue "" (getRawConfig fileName dir) varName

  let getName fileName dir : IniValue = getValue fileName dir "name"

  let getScreenShot fileName dir : IniValue = getValue fileName dir "screenshot"
