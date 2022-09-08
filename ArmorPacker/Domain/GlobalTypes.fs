namespace Domain

open System.IO
open System

/// Path of the dir the user wants to process
type DirToProcess = string

/// modinfo.ini
type ModInfoFileName = string

/// String surrounded by quotes. Used to send command line instructions to compress files.
type QuotedStr = private QuotedStr of string

module QuotedStr =
  let private transformIfNot transform condition x =
    if not (condition x) then
      transform x
    else
      x

  let private ensureFirstQuote s =
    transformIfNot (fun s -> "\"" + s) (fun (s: string) -> s.StartsWith('"')) s

  let private ensureTrailQuote s =
    transformIfNot (fun s -> s + "\"") (fun (s: string) -> s.EndsWith('"')) s

  let create (fileName: string) =
    QuotedStr(fileName |> ensureFirstQuote |> ensureTrailQuote)

  let value (QuotedStr fileName) = fileName

  let unquote (QuotedStr fileName) = fileName[.. fileName.Length - 2][1..]

  let modify fn (fileName: QuotedStr) = fileName |> unquote |> fn |> create

type ErrorMsg = string

module ErrorMsg =
  let map errorExtractor x =
    match x with
    | Ok v -> Ok v
    | Error e -> e |> errorExtractor |> Error

/// Executable file name.
type ExeName = private ExeName of QuotedStr

module ExeName =
  let private checkFileExists jsonPath q fileName =
    if not (File.Exists(fileName)) then
      failwith
        $"7zip executable {QuotedStr.value q} does not exist. If you have installed it somewhere else, make sure to modify {QuotedStr.value jsonPath}."

    fileName

  let private checkExe q (fileName: string) =
    let exe = "7z.exe"

    if not (fileName.ToLower().EndsWith(exe)) then
      failwith
        $"Your 7zip executable must be named \"{exe}\" (last tested with 7zip v22.01, which is guaranteed to have a file named like that)."

    q

  let create jsonPath fileName =
    let q = fileName |> QuotedStr.create
    let jsonPath' = jsonPath |> QuotedStr.create

    fileName
    |> checkFileExists jsonPath' q
    |> checkExe q
    |> ExeName

  let value (ExeName fileName) = fileName |> QuotedStr.value

/// Full parameters needed to start processing files.
type FullParams =
  { InputDir: DirToProcess
    OutFile: string
    ZipExe: ExeName }
