module InputProcessingWorkflow

open Domain
open Domain.InputProcessing

let private getErrorMsg e =
  match e with
  | InvalidFile e -> $"Invalid file error: {e}"
  | IncompleteParams e -> $"Invalid arguments error: {e}"

let processInput: ProcessInput =
  fun args ->
    result {
      let! input = InputArgs.create args
      let (IniTemplate template, DirProperties dirInfo) = InputArgs.value input

      return
        { Template = FileName.value template
          DirInfo = FileName.value dirInfo }
    }
    |> Result.mapError getErrorMsg
