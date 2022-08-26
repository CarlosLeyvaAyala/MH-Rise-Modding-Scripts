#r "nuget: FSharpx.Collections"

open System.Text.RegularExpressions

let bind switchFn twoTrackInput =
  match twoTrackInput with
  | Ok success -> switchFn success
  | Error fail -> Error fail

let map f aResult =
  match aResult with
  | Ok success -> Ok(f success)
  | Error failure -> Error failure

let fork f g x = (f x), (g x)

let mapError f aResult =
  match aResult with
  | Ok success -> Ok success
  | Error failure -> Error(f failure)
