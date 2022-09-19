module InfoGathering

open Domain.InfoGathering

let gather: GatherInfo =
  fun input ->
    result {
      let! c =
        input.DirInfo
        |> DirInfoContents.create
        |> Result.mapError toErrorMsg

      let infoList =
        c
        |> DirInfoContents.value
        |> List.map (Result.mapError toErrorMsg)
        |> List.map (Result.map DirModInfo.toDirInfo)

      let! t =
        input.Template
        |> IniTemplateContents.create
        |> Result.mapError toErrorMsg

      return
        { InfoList = infoList
          TemplateContents = t |> IniTemplateContents.value }
    }
