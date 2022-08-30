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

[<AutoOpen>]
module ResultComputationExpression =

  type ResultBuilder() =
    member __.Return(x) = Ok x
    member __.Bind(x, f) = Result.bind f x

    member __.ReturnFrom(x) = x
    member this.Zero() = this.Return()

    member __.Delay(f) = f
    member __.Run(f) = f ()

    member this.While(guard, body) =
      if not (guard ()) then
        this.Zero()
      else
        this.Bind(body (), (fun () -> this.While(guard, body)))

    member this.TryWith(body, handler) =
      try
        this.ReturnFrom(body ())
      with
      | e -> handler e

    member this.TryFinally(body, compensation) =
      try
        this.ReturnFrom(body ())
      finally
        compensation ()

    member this.Using(disposable: #System.IDisposable, body) =
      let body' = fun () -> body disposable

      this.TryFinally(
        body',
        fun () ->
          match disposable with
          | null -> ()
          | disp -> disp.Dispose()
      )

    member this.For(sequence: seq<_>, body) =
      this.Using(
        sequence.GetEnumerator(),
        fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current))
      )

    member this.Combine(a, b) = this.Bind(a, (fun () -> b ()))

  let result = new ResultBuilder()
