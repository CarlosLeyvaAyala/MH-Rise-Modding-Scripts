namespace Domain.InputProcessingWorkflow

open Domain

/// Parameters extracted from whatever input from the command line
type CmdParams =
  { InputDir: DirToProcess
    OutFile: BatFileName option }

type ParamError =
  | NoInput of string
  | NoOutput of string
  | NoZipExe of string

type InputType =
  | InvalidInput of string
  | DirOnly of string
  | TextFileName of string
  | ConfigIni of string
  | DirAndFile of string * string

type CmdLineArgs = string array

type GetCmdArgType = CmdLineArgs -> InputType

type CmdTypeToParams = InputType -> Result<CmdParams, ParamError>
