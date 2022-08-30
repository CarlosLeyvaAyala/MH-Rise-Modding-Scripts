// For more information see https://aka.ms/fsharp-console-apps
open System

let returnError msg =
  printfn "Error:\n%s" msg
  Console.ReadKey() |> ignore
  1

[<EntryPoint>]
let main args =
  Console.Title <- "Armor Packer for MH Rise"

  let r =
    try
      let r =
        args
        |> InputProcessingWorkflow.getInput
        |> CompressWorkflow.execute

      match r with
      | Ok _ -> 0
      | Error e -> returnError e
    with
    | _ as e -> returnError e.Message

  r
