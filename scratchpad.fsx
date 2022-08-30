#r "nuget: FSharpx.Collections"

open System.Text.RegularExpressions
open System
open System.IO

let fileLines fileName =
  File.ReadAllText(fileName).Split("\n")
  |> Array.map (fun s -> s.TrimEnd())
// let (>>=) m f =
//   printfn "expression is %A" m
//   f m

// let loggingWorkflow = 1 >>= (+) 2 >>= (*) 42 >>= id

let divideBy bottom top =
  if bottom = 0 then
    None
  else
    Some(top / bottom)

type MaybeBuilder() =
  member this.Bind(m, f) = Option.bind f m
  member this.Return(x) = Some x

let maybe = MaybeBuilder()

type ResultBuilder() =
  member ___.Bind(x, f) = Result.bind f x
  member ___.Return(x) = Ok x
  member ___.ReturnFrom(x) = x
  member this.Zero() = this.Return()

let result = ResultBuilder()

let divideByWorkflow x y w z =
  maybe {
    let! a = x |> divideBy y
    let! b = a |> divideBy w
    let! c = b |> divideBy z
    return c + a
  }

// // test
// let good = divideByWorkflow 60 6 2 1
// let bad = divideByWorkflow 12 3 0 1

let strToInt s =
  try
    s |> int |> Ok
  with
  | :? FormatException -> Error $"\"{s}\" is not a valid integer."

strToInt "3"
strToInt "a"

let stringAddWorkflow x y z =
  result {
    let! a = strToInt x
    let! b = strToInt y
    let! c = strToInt z
    return a + b + c
  }

let good = stringAddWorkflow "12" "3" "2"
let bad = stringAddWorkflow "12" "xyz" "2"

let strAdd str i =
  match str |> strToInt with
  | Ok x -> Ok(i + x)
  | Error e -> Error e

let (>>=) m f = Result.bind f m

let good1 = strToInt "1" >>= strAdd "2" >>= strAdd "3"
let bad1 = strToInt "1" >>= strAdd "xyz" >>= strAdd "3"

let mmmm x y z =
  result {
    let! a = strToInt x
    let! b = strAdd y a
    let! c = strAdd z b
    return c
  }

let good2 = mmmm "12" "3" "2"
let bad2 = mmmm "12" "xyz" "2"

type ErrorMsg = ErrorMsg of string

type IniVariableValue = string

type Extensions = private Extensions of string

module Extensions =
  let create str =
    Regex
      .Replace(str, @"\s*,\s*", "|")
      .Replace(".", @"\.")
    |> Extensions

  let value (Extensions ext) : IniVariableValue = ext

type ConfigData =
  { RelDir: string
    Extensions: Extensions }

AppDomain.CurrentDomain.BaseDirectory