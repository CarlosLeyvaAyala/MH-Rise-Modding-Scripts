// For more information see https://aka.ms/fsharp-console-apps
open System
open DMLib

let returnError msg =
  printfn "Error:\n%s" msg
  Console.ReadKey() |> ignore
  1

let processData args =
  result {
    let! r =
      args
      |> InputProcessingWorkflow.getInput
      |> Combinators.tee (printfn "%A")
      |> CompressWorkflow.execute

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
