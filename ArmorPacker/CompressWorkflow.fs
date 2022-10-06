module CompressWorkflow

open Domain
open DMLib.IO.Path
open DMLib.String
open System.IO
open System.Text.RegularExpressions
open FSharpx.Collections
open DMLib.Combinators
open System.Diagnostics

let private sectionSeparator = ":: ".PadRight(100, '#')

let private outFileName dir fileName ext = Path.Combine(dir, $"{fileName}.{ext}")

let private batHeader rarHeader zipExe outFile =
  let o = ZipFile.value outFile

  [| "@echo off"
     ""
     $"set zipExe={ExeName.value zipExe}"
     rarHeader
     $"set releaseFile={o}"
     "del %releaseFile%"
     "" |]

let private rarHeader rarExe tempFolder releaseFileRar =
  match rarExe with
  | None -> ""
  | Some v ->
    [| $"set rar={RarExeName.value v}"
       $"set tempFolder={encloseQuotes tempFolder}"
       $"set releaseFileRar={Path.GetFileName(releaseFileRar: string)
                             |> encloseQuotes}"
       "del %releaseFileRar%" |]
    |> toStrWithNl

let private createRarInstructions rarExe =
  match rarExe with
  | None -> [||]
  | Some _ ->
    [| sectionSeparator
       ":: Rar conversion"
       "%zipExe% x %releaseFile% -o%tempFolder% -r"
       "%rar% m %releaseFileRar% %tempFolder% -ep1"
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

    [| sectionSeparator
       $":: {armorOption.Name |> ArmorZipPath.value}\n\n"
       header "ModInfo"
       ModInfoIni.toStr armorOption.ModInfo
       getOptionalValue "Screenshot file" Screenshot.toStr armorOption.Screenshot
       header "Armor files"
       files |]
    |> toStrWithNl

/// Gets a list of subfolders. Each subfolder is a different armor option.
let private getArmorOptions: GetArmorOptions =
  fun inDir modInfoFile cfg ->
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

let convertSingleArmor armorDir = ""

let private getFileRelatedInfo args inputDir =
  let outDir = inputDir
  let newFile = outFileName outDir args.OutFile
  let outFile = newFile "7z" |> ZipFile.create
  let tempBat = newFile "bat"

  let rarH = rarHeader args.RarExe args.OutFile (newFile "rar")
  let header = batHeader rarH args.ZipExe outFile

  let rarInstructions = createRarInstructions args.RarExe

  (header, rarInstructions, tempBat)

let private getBatContents inputDir =
  let modinfoFile = "modinfo.ini"

  let validateConfigs workingDirs =
    workingDirs
    |> List.map Config.get
    |> Result.sequence

  let generateArmorOptions cfgs =
    cfgs
    |> List.map (fun t -> getArmorOptions (fst t) modinfoFile (snd t))
    |> List.map (Result.mapError armorOptionsErrorToMsg)
    |> Result.sequence

  result {
    let! workingDirs = Config.getFolders inputDir

    let workingDirs' =
      NonEmptyList.toList workingDirs
      |> List.map DirToProcess

    let! cfgData = validateConfigs workingDirs'
    let dirAndCfg = List.zip workingDirs' cfgData
    let! armorOptions = generateArmorOptions dirAndCfg

    return
      armorOptions
      |> List.map (NonEmptyList.toList >> List.map ArmorOption.toStr)
      |> List.map (List.fold foldNl "")
      |> List.toArray
  }

let execute args =
  let modinfoFile = "modinfo.ini"
  let inputDir = CleanedInputDir.clean args.InputDir
  let (header, rarInstructions, tempBat) = getFileRelatedInfo args inputDir

  let beautify str =
    Regex(@"\n{3,}").Replace(str, "\n\n") |> trim

  result {
    let! packingFiles = getBatContents inputDir

    packingFiles
    |> (fun x -> Array.append x rarInstructions)
    |> Array.insertManyAt 0 header
    |> toStrWithNl
    |> (fun s -> s + "pause")
    |> beautify
    |> tee (printfn "%A")
    |> (fun s -> File.WriteAllText(tempBat, s))

    Process.Start(tempBat) |> ignore
  }
