namespace Domain

open DMLib.IO.Path
open DMLib.String
open FSharpx.Collections
open Domain
open DMLib.ResultComputationExpression
open System.Text.RegularExpressions
open System.IO

type PathContainingModinfoIni = string

/// Name for the compressed distributable file.
type ZipFile = private ZipFile of QuotedStr

module ZipFile =
  let private fileExtension = ".7z"

  let private hasExtension ext (fileName: string) =
    let fn = fileName.Replace("\"", "") |> trim |> getExt
    ext = fn

  let private ensureExt fileName =

    if not (hasExtension fileExtension fileName) then
      fileName + fileExtension
    else
      fileName

  let create (fileName: string) =
    ZipFile(QuotedStr.create (fileName |> ensureExt))

  let value (ZipFile fileName) = fileName |> QuotedStr.value

  let unquote (ZipFile fileName) = fileName |> QuotedStr.unquote

/// Paths created inside compressed files when 7zip adds them with the -spf2 flag.
type DrivelessPath = private DrivelessPath of QuotedStr

module DrivelessPath =
  let create fileName =
    fileName
    |> removeDrive
    |> QuotedStr.create
    |> DrivelessPath

  let value (DrivelessPath path) = path |> QuotedStr.value

type FileToBeCompressed =
  { /// Full path from where it will be compressed.
    PathOnDisk: QuotedStr
    /// Path created by 7zip once it was added to a file.
    AddedZipPath: DrivelessPath
    /// Actual path needed to be inside the zip file.
    RenamedZipPath: DrivelessPath }

module FileToBeCompressed =
  let create pathWhenCompressed fileName =
    { PathOnDisk = QuotedStr.create fileName
      AddedZipPath = DrivelessPath.create fileName
      RenamedZipPath =
        fileName
        |> getFileName
        |> combine2 pathWhenCompressed
        |> DrivelessPath.create }

type ModInfoIni = ModInfoIni of FileToBeCompressed
type Screenshot = Screenshot of FileToBeCompressed
type ArmorFile = ArmorFile of FileToBeCompressed

module ArmorFile =
  let create compress fileToBeCompressed extensions fileName =
    if not (File.Exists(fileName)) then
      None
    else
      let fn = Path.GetFileName(fileName)
      let rx = Extensions.fileFilterRegex extensions

      if Regex(rx).Match(fn).Success then
        let zippedFile =
          compress (fileToBeCompressed fileName)
          |> ArmorFile

        Some zippedFile
      else
        None

/// Armor option <c>name</c> taken from modinfo.ini
type ArmorZipPath = private ArmorZipPath of string

module ArmorZipPath =
  let create prefix (optionName: string) =
    let n = prefix + " " + optionName

    let s =
      n
        .Replace(",", " ")
        .Replace("-", " ")
        .ToLower()
        .Trim()

    Regex(@"\s+").Replace(s, "_") |> ArmorZipPath

  let value (ArmorZipPath path) = path

type ArmorOptionValues =
  { Name: GetModInfoVariable
    Screenshot: GetModInfoVariable }

type ArmorOptionCreationData =
  { Config: ConfigData
    Getters: ArmorOptionValues
    ModInfoFile: ModInfoFileName
    Dir: PathContainingModinfoIni }

type ArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot option
    Name: ArmorZipPath
    Files: NonEmptyList<ArmorFile> }

module ArmorOption =
  let private getFiles compress fileToBeCompressed extensions dirName =
    let files =
      Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories)
      |> Array.map (ArmorFile.create compress fileToBeCompressed extensions)
      |> Array.catOptions
      |> Array.toList

    match files with
    | [] -> Error(Extensions.getNoFilesError extensions)
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
          |> ErrorMsg
        )
      else
        Ok(Some v)

  let create (d: ArmorOptionCreationData) =
    let modInfoFile = d.ModInfoFile
    let dir = d.Dir

    result {
      let! optionName = d.Getters.Name modInfoFile dir

      let optionName' = ArmorZipPath.create d.Config.OptionsPrefix optionName

      let compress = FileToBeCompressed.create (ArmorZipPath.value optionName')
      let fileToBeCompressed = combine2 dir

      let getOptional' = getOptional modInfoFile dir compress fileToBeCompressed

      let zippedModInfo =
        compress (fileToBeCompressed modInfoFile)
        |> ModInfoIni

      let! zippedScreenshot =
        getOptional' d.Getters.Screenshot Screenshot
        |> validateScreenshot

      let! files = getFiles compress fileToBeCompressed d.Config.Extensions dir

      return
        { ModInfo = zippedModInfo
          Screenshot = zippedScreenshot
          Name = optionName'
          Files = files }
    }

type SingleArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot option
    Files: NonEmptyList<ArmorFile> }

module SingleArmorOption =
  let getScreenshot (getter: GetModInfoVariable) modInfoFile dir =
    match getter modInfoFile dir with
    | Error _ -> None
    | Ok v ->
      let screen =
        FileToBeCompressed.create "" (combine2 dir modInfoFile)
        |> Screenshot

      Some screen

  let getModInfo modInfoFile dir =
    FileToBeCompressed.create "" (combine2 dir modInfoFile)
    |> ModInfoIni

  /// Creates a single armor option from a given dir.
  let create (getters: ArmorOptionValues) modInfoFile dir =
    let modInfo = FileToBeCompressed.create "" (combine2 dir modInfoFile)
    let screenshot = getters.Screenshot modInfoFile dir
    modInfo

type ManyArmorOptions = NonEmptyList<ArmorOption>

type ArmorOptionsInFile =
  | ManyArmorOptions
  | SingleArmorOption

type ArmorOptionsError =
  | NoArmorOptions of string
  | NoFilesToPack of string

type GetArmorOptions = string -> string -> Result<ArmorOptionsInFile, ArmorOptionsError>
