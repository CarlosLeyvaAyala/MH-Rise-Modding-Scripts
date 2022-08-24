module CompressWorkflow

open Domain
open DMLib.IO.Path
open DMLib.String
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics

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
  let zipExecute args = $"%%zipExe%% {args}"

  let zipAdd (zipFile: ZipFile) relDir (fileName: QuotedStr) inArr =
    let zf = ZipFile.value zipFile
    let fn = QuotedStr.value fileName
    let add = $"a -t7z %%releaseFile%% {fn} -spf2"

    let quote str =
      str |> QuotedStr.create |> QuotedStr.value

    let oldFn =
      fileName
      |> QuotedStr.unquote
      |> removeDrive
      |> quote

    let newFn =
      fileName
      |> QuotedStr.unquote
      |> getFileName
      |> (fun s -> Path.Combine(relDir, s))
      |> quote

    let rename = $"rn %%releaseFile%% {oldFn} {newFn}"
    Array.append inArr [| zipExecute add; zipExecute rename |]

  let qStr str = QuotedStr.create str

  /// Gets the MH Rise files that will be shipped for the mod.
  let getFiles cfg outFile dirName =
    let toZip fileName =
      zipAdd outFile cfg.RelDir (qStr fileName) [||]

    let rx =
      @"(?i).*\.("
      + Extensions.value cfg.Extensions
      + @")\.\d+.*"

    Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories)
    |> Array.filter (fun s -> Regex.Match(s, rx).Success)
    |> Array.map toZip
    |> Array.collect (fun a -> a)

  /// Adds an optional file if it was defined.
  let optional outFile dirName opt results =
    match opt with
    | Error _ -> results
    | Ok v ->
      let q = qStr (Path.Combine(dirName, v))
      zipAdd outFile "" q results

  /// Generates the strings needed to compress a whole dir as an armor option
  let armorOption cfg modinfoFile outFile dirName =
    let screenshot = Config.ModInfo.getScreenShot modinfoFile dirName
    let modinfo = qStr (Path.Combine(dirName, modinfoFile))
    let riseFiles = getFiles cfg outFile dirName
    let optional' = optional outFile dirName

    [||]
    |> zipAdd outFile "" modinfo
    |> optional' screenshot
    |> fun a -> Array.append a riseFiles
    |> toStrWithNl

let execute args =
  let modinfoFile = "modinfo.ini"
  let cfg = Config.get args.InputDir

  let baseDir =
    args.InputDir
    |> trimEndingDirectorySeparator
    |> getDir

  let armorOptions = getArmorOptions args.InputDir modinfoFile
  let newFile = outFileName baseDir args.OutFile
  let outFile = newFile "7z" |> ZipFile.create
  let tempBat = newFile "bat"
  let zipExe = ExeName.create @"C:\Program Files\7-Zip\7z.exe"

  armorOptions
  |> Array.map (Compression.armorOption cfg modinfoFile outFile)
  |> Array.insertManyAt 0 (batHeader zipExe outFile)
  |> toStrWithNl
  |> (fun s -> s + "pause")
  |> trim
  |> (fun s -> File.WriteAllText(tempBat, s))

  Process.Start(tempBat) |> ignore
