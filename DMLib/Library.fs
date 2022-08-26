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

    let combineArray a = Path.Combine(a)
    let combine2 p1 p2 = Path.Combine(p1, p2)
    let combine3 p1 p2 p3 = Path.Combine(p1, p2, p3)
    let combine4 p1 p2 p3 p4 = Path.Combine(p1, p2, p3, p4)

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

  let fork f g x = (f x), (g x)

  let join2 aTuple =
    match fst aTuple with
    | Error e -> Error e
    | Ok v1 ->
      match snd aTuple with
      | Error e -> Error e
      | Ok v2 -> Ok(v1, v2)
