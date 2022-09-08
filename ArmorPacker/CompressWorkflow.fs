module CompressWorkflow

open Domain
open DMLib.IO.Path
open DMLib.String
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics
open FSharpx.Collections
open Config

let private outFileName dir fileName ext = Path.Combine(dir, $"{fileName}.{ext}")

let private batHeader zipExe outFile =
  let o = ZipFile.value outFile

  [| "@echo off"
     $"set zipExe={ExeName.value zipExe}"
     $"set releaseFile={o}"
     "del %releaseFile%" |]

module private Compression =
  let private zipExecute args = $"%%zipExe%% {args}"

  let private zipAdd (zipFile: ZipFile) relDir (fileName: QuotedStr) inArr =
    let zf = ZipFile.value zipFile
    let fn = QuotedStr.value fileName
    let add = $"a -t7z %%releaseFile%% {fn} -spf2"

    let oldFn =
      fileName
      |> QuotedStr.modify removeDrive
      |> QuotedStr.value

    let newFn =
      fileName
      |> QuotedStr.modify (fun s -> s |> getFileName |> combine2 relDir)
      |> QuotedStr.value

    let rename = $"rn %%releaseFile%% {oldFn} {newFn}"
    Array.append inArr [| zipExecute add; zipExecute rename |]

  let qStr str = QuotedStr.create str

  /// Gets the MH Rise files that will be shipped for the mod.
  let private getFiles cfg outFile armorOptionName dirName =
    let relPath = Path.Combine(armorOptionName, cfg.RelDir)

    let toZip fileName =
      zipAdd outFile relPath (qStr fileName) [||]

    let rx =
      @"(?i).*\.("
      + Extensions.value cfg.Extensions
      + @")\.\d+.*"

    Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories)
    |> Array.filter (fun s -> Regex.Match(s, rx).Success)
    |> Array.map toZip
    |> Array.collect (fun a -> a)

  /// Adds an optional file if it was defined.
  let private optional outFile armorOptionName dirName opt results =
    match opt with
    | Error _ -> results
    | Ok v ->
      let q = qStr (Path.Combine(dirName, v))
      zipAdd outFile armorOptionName q results

  // TODO: Move to Domain
  let private getArmorOptionName isSingleArmorOption modinfoFile dirName =
    if isSingleArmorOption then
      ""
    else
      match Config.ModInfo.getName modinfoFile dirName with
      | Error _ -> failwith $"A \"name\" is required inside {Path.Combine(dirName, modinfoFile)}"
      | Ok v -> v

  let normalizeName prefix (optionName: string) =
    let n = prefix + " " + optionName

    let s =
      n
        .Replace(",", " ")
        .Replace("-", " ")
        .ToLower()
        .Trim()

    Regex(@"\s+").Replace(s, "_")

  /// Generates the strings needed to compress a whole dir as an armor option
  let armorOption isSingleArmorOption cfg modinfoFile outFile dirName =
    let armorOptionName =
      getArmorOptionName isSingleArmorOption modinfoFile dirName
      |> normalizeName cfg.OptionsPrefix

    let screenshot = Config.ModInfo.getScreenShot modinfoFile dirName
    let modinfo = qStr (Path.Combine(dirName, modinfoFile))
    let riseFiles = getFiles cfg outFile armorOptionName dirName
    let optional' = optional outFile armorOptionName dirName

    [||]
    |> zipAdd outFile armorOptionName modinfo
    |> optional' screenshot
    |> fun a -> Array.append a riseFiles
    |> toStrWithNl

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
    let compressedFileToStr convert v = convert v |> FileToBeCompressed.toStr

    let getOptionalValue convert v =
      v
      |> Option.map (fun z -> compressedFileToStr convert z)
      |> Option.defaultValue ""

    let files =
      armorOption.Files
      |> NonEmptyList.toArray
      |> Array.map (compressedFileToStr (fun z -> let (ArmorFile x) = z in x))
      |> toStrWithNl

    [| compressedFileToStr (fun z -> let (ModInfoIni x) = z in x) armorOption.ModInfo
       getOptionalValue (fun z -> let (Screenshot x) = z in x) armorOption.Screenshot
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

let armorOptionsErrorToMsg err =
  match err with
  | NoArmorOptions x -> x
  | NoFilesToPack x -> x
  | UndefinedVariable x -> x
  | NonExistentFile x -> x
  |> ErrorMsg

let execute args =
  let modinfoFile = "modinfo.ini"

  let baseDir =
    args.InputDir
    |> trimEndingDirectorySeparator
    |> getDir

  let newFile = outFileName baseDir args.OutFile
  let outFile = newFile "7z" |> ZipFile.create
  let tempBat = newFile "bat"

  result {
    let! cfg = Config.get args.InputDir

    let! armorOptions =
      getArmorOptions cfg args.InputDir modinfoFile
      |> Result.mapError armorOptionsErrorToMsg

    armorOptions
    |> NonEmptyList.toArray
    |> Array.map ArmorOption.toStr
    |> Array.insertManyAt 0 (batHeader args.ZipExe outFile)
    |> toStrWithNl
    |> (fun s -> s + "pause")
    |> trim
    |> (fun s -> File.WriteAllText(tempBat, s))

    Process.Start(tempBat) |> ignore
  }
