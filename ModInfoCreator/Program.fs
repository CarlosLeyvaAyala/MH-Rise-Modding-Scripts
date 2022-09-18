open System
open Domain
open InputProcessingWorkflow

let processData args =
  try
    result {
      let! i = processInput args
      return i
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
