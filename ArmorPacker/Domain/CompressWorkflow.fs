namespace Domain

open DMLib.IO.Path
open DMLib.String
open FSharpx.Collections
open Domain

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
type Screenshot = Screenshot of FileToBeCompressed option
type ArmorFile = ArmorFile of FileToBeCompressed

/// Armor option <c>name</c> taken from modinfo.ini
type ArmorZipPath = ArmorZipPath of string

type ArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot
    Name: ArmorZipPath
    Files: NonEmptyList<ArmorFile> }

type SingleArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot
    Files: NonEmptyList<ArmorFile> }

type ManyArmorOptions = NonEmptyList<ArmorOption>

type ArmorOptionsInFile =
  | ManyArmorOptions
  | SingleArmorOption

type ArmorOptionsError =
  | NoArmorOptions of string
  | NoFilesToPack of string

type GetArmorOptions = string -> string -> Result<ArmorOptionsInFile, ArmorOptionsError>
