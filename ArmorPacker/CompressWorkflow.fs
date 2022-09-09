module CompressWorkflow

open Domain
open DMLib.IO.Path
open DMLib.String
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics
open FSharpx.Collections

let private outFileName dir fileName ext = Path.Combine(dir, $"{fileName}.{ext}")

let private batHeader zipExe outFile =
  let o = ZipFile.value outFile

  [| "@echo off"
     $"set zipExe={ExeName.value zipExe}"
     $"set releaseFile={o}"
     "del %releaseFile%"
     "" |]

module ArmorOption =
  let private getFiles compress fileToBeCompressed extensions dirName =
    let files =
      Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories)
      |> Array.map (ArmorFile.create compress fileToBeCompressed extensions)
      |> Array.catOptions
      |> Array.toList

    match files with
    | [] ->
      Error(
        Extensions.getNoFilesError extensions
        |> NoFilesToPack
      )
    | head :: tail -> Ok(NonEmptyList.create head tail)

  let private getOptional modInfoFile dir compress fileToBeCompressed (getter: GetModInfoVariable) fileType =
    match getter modInfoFile dir with
    | Error _ -> None
    | Ok v ->
      let optional = compress (fileToBeCompressed v) |> fileType
      Some optional

  let private validateScreenshot (screenshot: Screenshot option) =
    match screenshot with
    | None -> Ok None
    | Some v ->
      let (Screenshot v') = v
      let fileName = v'.PathOnDisk |> QuotedStr.unquote

      if not (System.IO.File.Exists(fileName)) then
        Error(
          $"Screenshot file \"{fileName}\" does not exist.\nCheck the modinfo.ini file for that armor option and make sure that screenshot file exists on disk."
          |> NonExistentFile
        )
      else
        Ok(Some v)

  let create (d: ArmorOptionCreationData) =
    let modInfoFile = d.ModInfoFile
    let dir = d.Dir

    result {
      let! optionName =
        d.Getters.Name modInfoFile dir
        |> Result.mapError UndefinedVariable

      let optionName' = ArmorZipPath.create d.Config.OptionsPrefix optionName

      let compressToBase = FileToBeCompressed.create (ArmorZipPath.value optionName')

      let compressToNatives =
        FileToBeCompressed.create (Path.Combine(ArmorZipPath.value optionName', d.Config.RelDir))

      let fileToBeCompressed = combine2 dir

      let getOptional' = getOptional modInfoFile dir compressToBase fileToBeCompressed

      let zippedModInfo =
        compressToBase (fileToBeCompressed modInfoFile)
        |> ModInfoIni

      let! zippedScreenshot =
        getOptional' d.Getters.Screenshot Screenshot
        |> validateScreenshot

      let! files = getFiles compressToNatives fileToBeCompressed d.Config.Extensions dir

      return
        { ModInfo = zippedModInfo
          Screenshot = zippedScreenshot
          Name = optionName'
          Files = files }
    }

  let toStr (armorOption: ArmorOption) =
    let header str =
      let sep = "".PadRight(30, ':')
      $"{sep}\n:: {str}\n{sep}"

    let getOptionalValue head toStr v =
      v
      |> Option.map toStr
      |> Option.map (fun s -> $"{header head}\n{s}\n")
      |> Option.defaultValue ""

    let files =
      armorOption.Files
      |> NonEmptyList.toArray
      |> Array.map ArmorFile.toStr
      |> toStrWithNl

    [| header "ModInfo"
       ModInfoIni.toStr armorOption.ModInfo
       getOptionalValue "Screenshot file" Screenshot.toStr armorOption.Screenshot
       header "Armor files"
       files |]
    |> toStrWithNl

/// Gets a list of subfolders. Each subfolder is a different armor option.
let private getArmorOptions: GetArmorOptions =
  fun cfg inDir modInfoFile ->
    let toArmorOption dir =
      let getters =
        { ArmorOptionValues.Name = Config.ModInfo.getName
          Screenshot = Config.ModInfo.getScreenShot }

      let d =
        { ArmorOptionCreationData.Dir = dir
          ModInfoFile = modInfoFile
          Config = cfg
          Getters = getters }

      ArmorOption.create d

    let validateList lst =
      match lst with
      | [] ->
        Error(
          NoArmorOptions $"No armor options were found. Put a {modInfoFile} file inside each folder you want to pack."
        )
      | head :: tail -> Ok(NonEmptyList.create head tail)

    result {
      let! o =
        Directory.GetFiles(inDir, modInfoFile, SearchOption.AllDirectories)
        |> Array.map getDir
        |> Array.map toArmorOption
        |> Array.toList
        |> Result.sequence

      let! lst = validateList o
      return lst
    }

let private armorOptionsErrorToMsg err =
  match err with
  | NoArmorOptions x -> x
  | NoFilesToPack x -> x
  | UndefinedVariable x -> x
  | NonExistentFile x -> x
  |> ErrorMsg

let execute args =
  let modinfoFile = "modinfo.ini"

  let inputDir =
    args.InputDir
    |> CleanedInputDir.create
    |> CleanedInputDir.value

  let outDir = inputDir

  let newFile = outFileName outDir args.OutFile
  let outFile = newFile "7z" |> ZipFile.create
  let tempBat = newFile "bat"

  let beautify str =
    Regex(@"\n{3,}").Replace(str, "\n\n") |> trim

  result {
    let! cfg = Config.get inputDir

    let! armorOptions =
      getArmorOptions cfg inputDir modinfoFile
      |> Result.mapError armorOptionsErrorToMsg

    armorOptions
    |> NonEmptyList.toArray
    |> Array.map ArmorOption.toStr
    |> Array.insertManyAt 0 (batHeader args.ZipExe outFile)
    |> toStrWithNl
    |> (fun s -> s + "pause")
    |> beautify
    |> (fun s -> File.WriteAllText(tempBat, s))

    Process.Start(tempBat) |> ignore
  }
