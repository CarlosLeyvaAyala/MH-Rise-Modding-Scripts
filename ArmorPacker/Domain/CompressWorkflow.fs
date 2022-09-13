namespace Domain

open DMLib.IO.Path
open DMLib.String
open FSharpx.Collections
open Domain
open System.Text.RegularExpressions
open System.IO

type PathContainingModinfoIni = string

type CleanedInputDir = private CleanedInputDir of string

module CleanedInputDir =
  let create dir =
    dir
    |> trim
    |> fun s ->
         if s.EndsWith(".") then
           removeLastChars 1 s
         else
           s
    |> trimEndingDirectorySeparator
    |> CleanedInputDir

  let value (CleanedInputDir dir) = dir

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

  let toStr (zippedFile: FileToBeCompressed) =
    let disk = QuotedStr.value zippedFile.PathOnDisk
    let inZip = DrivelessPath.value zippedFile.AddedZipPath
    let renamed = DrivelessPath.value zippedFile.RenamedZipPath

    [| $"%%zipExe%% a -t7z %%releaseFile%% {disk} -spf2"
       $"%%zipExe%% rn %%releaseFile%% {inZip} {renamed}" |]
    |> toStrWithNl

type ModInfoIni = ModInfoIni of FileToBeCompressed
type Screenshot = Screenshot of FileToBeCompressed
type ArmorFile = private ArmorFile of FileToBeCompressed

module ModInfoIni =
  let toStr (ModInfoIni x) = x |> FileToBeCompressed.toStr

module Screenshot =
  let toStr (Screenshot x) = x |> FileToBeCompressed.toStr

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

  let toStr (ArmorFile x) = x |> FileToBeCompressed.toStr

/// Armor option <c>name</c> taken from modinfo.ini
type ArmorZipPath = private ArmorZipPath of string

module ArmorZipPath =
  let create prefix (optionName: string) =
    let n = prefix + " " + optionName

    let s =
      n
        .Replace(",", " ")
        .Replace("-", " ")
        .Replace("_", " ")
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

type ArmorOptions = NonEmptyList<ArmorOption>

type ArmorOptionsError =
  | NoArmorOptions of string
  | NoFilesToPack of string
  | UndefinedVariable of string
  | NonExistentFile of string

type GetArmorOptions = ConfigData -> DirToProcess -> ModInfoFileName -> Result<ArmorOptions, ArmorOptionsError>
