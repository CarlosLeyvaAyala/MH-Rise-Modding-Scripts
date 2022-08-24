module Config

open System.IO
open System.Text.RegularExpressions
open Domain

let private getRawConfig fileName dir =
  let f = Path.Combine(dir, fileName)

  if not (File.Exists(f)) then
    failwith $"File \"{f}\" must exist before we can continue."
  else
    DMLib.IO.File.fileLines f

let private getConfigValue errorMsg cfg varName =
  let rx = "(?i)" + varName + @"\s*=\s*(.*)"
  let regex = Regex(rx)

  match cfg
        |> Array.filter (fun s -> regex.Match(s).Success)
    with
  | [||] -> Error errorMsg
  | [| l |] -> Ok(regex.Match(l).Groups[1].Value)
  | _ -> Error $"Your config.ini file has many variables named {varName}. You should have only one of those."

let getExtensions cfg =
  Extensions.create (
    match getConfigValue "" cfg "extensions" with
    | Error _ -> "mdf2,mesh,chain"
    | Ok s -> s
  )

let get inDir =
  let cfg = getRawConfig "config.ini" inDir

  let error =
    "You need to define the modInternalPath variable in your ini file, otherwise where would your mod files be installed by Fluffy?"

  let rel = getConfigValue error cfg "modInternalPath"

  match rel with
  | Ok r ->
    { RelDir = r
      Extensions = getExtensions cfg }
  | Error e -> failwith e

module ModInfo =
  let getScreenShot fileName dir =
    getConfigValue "" (getRawConfig fileName dir) "screenshot"
