// For more information see https://aka.ms/fsharp-console-apps
open System

[<EntryPoint>]
let main args =
  Console.Title <- "Armor Packer for MH Rise"

  let r =
    try
      args
      |> InputProcessingWorkflow.getInput
      |> CompressWorkflow.execute

      0
    with
    | _ as e ->
      printfn "Error:\n%s" e.Message
      Console.ReadKey() |> ignore
      1

  r
