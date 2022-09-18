namespace Domain

type IniTemplatePath = string
type DirInfoPath = string
type ErrorMessage = string

type FilesToProcess =
  { Template: IniTemplatePath
    DirInfo: DirInfoPath }
