namespace DMLib

open System.IO
open System.Text.RegularExpressions
open System.Text.Json

module Combinators =
  let tap f x =
    f
    x

  let tee f x =
    f x
    x

  let fork f g x = (f x), (g x)

  let swap f x y = f y x

  let join2 aTuple =
    match fst aTuple with
    | Error e -> Error e
    | Ok v1 ->
      match snd aTuple with
      | Error e -> Error e
      | Ok v2 -> Ok(v1, v2)


module IO =
  module Path =
    let getDir (path) = Path.GetDirectoryName(path: string)
    let getExt path = Path.GetExtension(path: string)
    let getFileName path = Path.GetFileName(path: string)

    let getRelativeDir relPath dir =
      Path.GetFullPath(Path.Combine(dir, relPath))

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

    let (|IsDir|_|) path =
      match File.GetAttributes(path: string) with
      | FileAttributes.Directory -> Some path
      | _ -> None

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
  let removeLastChars n (s: string) = s[.. s.Length - (n + 1)]

  type NonEmptyString = private NonEmptyString of string

  module NonEmptyString =
    let create str =
      if str = "" then
        Error "This string can not be empty"
      else
        NonEmptyString str |> Ok

    let value (NonEmptyString str) = str

    let apply f (NonEmptyString e) = f e

    let map f e = apply f e |> create


module OutputAccumulator =
  type InputOutputPair<'i, 'o> = 'i * 'o array

  let private append args v =
    fst args, Array.append (snd args) [| v |]

  let bind f (args: InputOutputPair<'i, 'o>) =
    match f (fst args) with
    | Ok v -> Ok(append args v)
    | Error e -> Error e

  let map f (args: InputOutputPair<'i, 'o>) = append args (args |> fst |> f)

  let start x = x, [||]


[<RequireQualifiedAccess>]
module Json =
  let get<'a> path =
    File.ReadAllText(path)
    |> JsonSerializer.Deserialize<'a>

  let deserialize = get
  let read = get

  let serialize indented obj =
    JsonSerializer.Serialize(obj, JsonSerializerOptions(WriteIndented = indented))

  let write = serialize
