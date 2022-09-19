namespace Domain

type IniTemplatePath = string
type DirInfoPath = string
type ErrorMessage = string

type FilesToProcess =
  { Template: IniTemplatePath
    DirInfo: DirInfoPath }

type BaseSubdirPath = string

type DirInfo =
  { DirName: string
    OptionName: string
    Description: string }

type DirInfoToProcess = Result<DirInfo, ErrorMessage>
type TemplateContents = string

type ProcessingInfo =
  { InfoList: DirInfoToProcess list
    TemplateContents: TemplateContents
    BaseDir: BaseSubdirPath }
