// For more information see https://aka.ms/fsharp-console-apps
open System
open DMLib
open Domain

let returnError msg =
  printfn "Error:\n%s" msg
  Console.ReadKey() |> ignore
  1

let processData args =
  result {
    let! inputs = args |> InputProcessingWorkflow.getInput
    let! r = inputs |> CompressWorkflow.execute

    return r
  }

[<EntryPoint>]
let main args =
  Console.Title <- "Armor Packer for MH Rise"

  let r =
    try
      match processData args with
      | Ok _ -> 0
      | Error e -> returnError e
    with
    | _ as e -> returnError e.Message

  r
