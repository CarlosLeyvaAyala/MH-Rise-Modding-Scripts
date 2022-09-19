open System
open Domain
open InputProcessingWorkflow
open InfoGathering

let processData args =
  try
    result {
      let! i = processInput args
      let! g = gather i
      return g
    }
  with
  | _ as e -> ErrorMessage e.Message |> Error

[<EntryPoint>]
let main args =
  Console.Title <- "modinfo.ini Creator for MH Rise"

  match processData args with
  | Ok _ -> 0
  | Error e ->
    printfn "%s" e
    Console.ReadKey() |> ignore
    1
