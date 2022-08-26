namespace DomainTests

open System
open Xunit
open Domain
open FsUnit.Xunit
open FSharpx.Collections

module ``Domain Validation Tests`` =
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

  [<Fact>]
  let ``Driveless path has no drive`` () =
    "Papitas\Frescas"
    |> QuotedStr.create
    |> QuotedStr.value
    |> should
         equal
         ("C:\Papitas\Frescas"
          |> DrivelessPath.create
          |> DrivelessPath.value)

  let legsArmorPath = @"x:\My Armors\Kamura\Skimpy\legs.mesh.443953485"

  let compressedFile =
    FileToBeCompressed.create "Super Skimpy lololol\pl\pl999" legsArmorPath

  [<Fact>]
  let ``Compressed file full path`` () =
    compressedFile.PathOnDisk
    |> should equal (QuotedStr.create legsArmorPath)

  [<Fact>]
  let ``Compressed file full name when recently added`` () =
    compressedFile.AddedZipPath
    |> should equal (DrivelessPath.create legsArmorPath)

  [<Fact>]
  let ``Compressed file complete name ready to distribute`` () =
    compressedFile.RenamedZipPath
    |> should not' (equal "lololol")
