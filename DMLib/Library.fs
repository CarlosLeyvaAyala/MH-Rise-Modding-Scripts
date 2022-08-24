namespace DMLib

open System.IO
open System.Text.RegularExpressions

module IO =
  module Path =
    let getDir (path) = Path.GetDirectoryName(path: string)
    let getExt path = Path.GetExtension(path: string)
    let getFileName path = Path.GetFileName(path: string)

    let removeDrive (path) =
      let m = Regex.Match(path, @".*:\\(.*)")

      if m.Success then
        m.Groups[1].Value
      else
        path

    let trimEndingDirectorySeparator path =
      Path.TrimEndingDirectorySeparator(path: string)

  module File =
    let fileLines fileName =
      File.ReadAllText(fileName).Split("\n")
      |> Array.map (fun s -> s.TrimEnd())

  module Directory =
    let getFilesOption (option: SearchOption) searchPattern path =
      Directory.GetFiles(path, searchPattern, option)

module String =
  let foldNl acc s = acc + s + "\n"

  /// Converts a string array to a string separated by newlines.
  let toStrWithNl = Array.fold foldNl ""
  let trim (s: string) = s.Trim()

module Combinators =
  let tap f x =
    f
    x
