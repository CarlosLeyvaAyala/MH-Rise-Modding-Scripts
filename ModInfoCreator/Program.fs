open System
open Domain
open InputProcessingWorkflow
open InfoGathering
open Execution

let processData args =
  try
    result {
      let! i = processInput args
      let! g = gather i
      return processData g
    }
  with
  | _ as e -> ErrorMessage e.Message |> Error

[<EntryPoint>]
let main args =
  Console.Title <- "modinfo.ini Creator for MH Rise"

  match processData args with
  | Ok _ ->
    Console.ReadKey() |> ignore
    0
  | Error e ->
    printfn "%s" e
    Console.ReadKey() |> ignore
    1
