namespace Domain

type IniTemplatePath = string
type DirInfoPath = string
type ErrorMessage = string

type FilesToProcess =
  { Template: IniTemplatePath
    DirInfo: DirInfoPath }

type DirInfo =
  { DirName: string
    OptionName: string
    Description: string }

type DirInfoToProcess = Result<DirInfo, ErrorMessage>

type ProcessingInfo =
  { InfoList: DirInfoToProcess list
    TemplateContents: string }
