namespace Domain

open DMLib.IO.Path
open DMLib.String

/// Executable file name.
type ExeName = private ExeName of QuotedStr

module ExeName =
  let create fileName = fileName |> QuotedStr.create |> ExeName
  let value (ExeName fileName) = fileName |> QuotedStr.value

/// Name for the compressed distributable file.
type ZipFile = private ZipFile of QuotedStr

module ZipFile =
  let private hasExtension ext (fileName: string) =
    let fn = fileName.Replace("\"", "") |> trim |> getExt
    ext = fn

  let create (fileName: string) =
    if not (hasExtension ".7z" fileName) then
      failwith "File must have the 7z extension"
    else
      ZipFile(QuotedStr.create fileName)

  let value (ZipFile fileName) = fileName |> QuotedStr.value

  let unquote (ZipFile fileName) = fileName |> QuotedStr.unquote

type DrivelessPath = DrivelessPath of QuotedStr

type FileToBeCompressed =
  { /// Full path from where it will be compressed.
    PathOnDisk: QuotedStr
    /// Path created by 7zip once it was added to a file.
    AddedZipPath: DrivelessPath
    /// Actual path needed to be inside the zip file.
    RenamedZipPath: DrivelessPath }

type ModInfoIni = ModInfoIni of FileToBeCompressed
type Screenshot = Screenshot of FileToBeCompressed option
type ArmorFile = ArmorFile of FileToBeCompressed
type OptionInternalZipPath = OptionInternalZipPath of string

type ArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot
    Name: OptionInternalZipPath }

type SingleArmorOption =
  { ModInfo: ModInfoIni
    Screenshot: Screenshot }
