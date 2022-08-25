namespace DomainTests

open System
open Xunit
open Domain
open FsUnit.Xunit
open Domain.ZipFile

module ``Compressing Workflow`` =
  let testStr = "Test" |> QuotedStr.create

  [<Fact>]
  let ``Quoted string creation`` () =
    "\"Test\""
    |> should equal (QuotedStr.value testStr)

  [<Fact>]
  let ``Quoted string unquoting`` () =
    "Test" |> should equal (QuotedStr.unquote testStr)

  [<Fact>]
  let ``Zip file name ends with zip`` () =
    let fn = "pepe" |> ZipFile.create |> ZipFile.unquote
    "pepe.7z" |> should equal fn
