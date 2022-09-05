module CompressWorkflow

open Domain
open DMLib.IO.Path
open DMLib.String
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics
open DMLib.ResultComputationExpression

let private outFileName dir fileName ext = Path.Combine(dir, $"{fileName}.{ext}")

/// Gets a list of subfolders. Each subfolder is a different armor option.
let private getArmorOptions inDir modInfoFile =
  let o =
    Directory.GetFiles(inDir, modInfoFile, SearchOption.AllDirectories)
    |> Array.map getDir

  if o.Length < 1 then
    failwith $"Put a {modInfoFile} file inside each folder you want to pack."

  o

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

let execute args =
  result {
    let modinfoFile = "modinfo.ini"
    let! cfg = Config.get args.InputDir

    let baseDir =
      args.InputDir
      |> trimEndingDirectorySeparator
      |> getDir

    let armorOptions = getArmorOptions args.InputDir modinfoFile
    let newFile = outFileName baseDir args.OutFile
    let outFile = newFile "7z" |> ZipFile.create
    let tempBat = newFile "bat"

    let processArmorOption =
      Compression.armorOption (armorOptions.Length = 1) cfg modinfoFile outFile

    armorOptions
    |> Array.map processArmorOption
    |> Array.insertManyAt 0 (batHeader args.ZipExe outFile)
    |> toStrWithNl
    |> (fun s -> s + "pause")
    |> trim
    |> (fun s -> File.WriteAllText(tempBat, s))

    Process.Start(tempBat) |> ignore
  }
