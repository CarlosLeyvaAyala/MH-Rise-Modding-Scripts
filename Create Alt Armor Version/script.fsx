open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open System.Text.Json

let serializeJson indented obj =
    JsonSerializer.Serialize(obj, JsonSerializerOptions(WriteIndented = indented))

type WarningList = string list

type FileInfo =
    { OriginalFile: string
      BasePath: string
      ModName: string
      RelPath: string
      FileName: string }

let blankInfo =
    { OriginalFile = ""
      BasePath = ""
      ModName = ""
      RelPath = ""
      FileName = "" }

let strReplace oldStr newStr (str: string) =
    str.Replace(oldValue = oldStr, newValue = newStr)

module Cfg =
    let private filePath = Path.Combine(__SOURCE_DIRECTORY__, "config.json")

    type Config =
        { basePath: string
          defaultModSuffix: string }

    type ConfigData = { Data: Config; Warnings: WarningList }

    let private defaultCfg =
        { basePath = "Mod.Organizer-2.4.4/mods"
          defaultModSuffix = " - Naked" }

    type ConfigFileContent =
        | FileContent of string
        | FileNotFound of string
        | UnknownFileError of string

    let private readCfgFile () =
        try
            let f = File.ReadAllText(filePath)
            FileContent f
        with
        | :? FileNotFoundException as e -> FileNotFound e.Message
        | _ as e -> UnknownFileError e.Message

    let private showUnexpectedError lst error =
        lst
        @ [ error
            "Getting default configuration values due to unexpected error."
            "" ]

    let private ensureFileContent content =
        let d = serializeJson true defaultCfg

        match content with
        | FileNotFound error ->
            let newCfgMsg = "A new configuration file with default values was created."
            File.WriteAllText(path = filePath, contents = d)
            ([ error; newCfgMsg; "" ], d)
        | UnknownFileError e -> (showUnexpectedError [] e, d)
        | FileContent c -> ([], c)

    let private strToConfig (c: WarningList * string) =
        try
            let s = snd c
            let o = JsonSerializer.Deserialize<Config> s
            { Data = o; Warnings = fst c }
        with
        | _ as e ->
            { Data = defaultCfg
              Warnings = showUnexpectedError (fst c) e.Message }

    let value =
        ()
        |> readCfgFile
        |> ensureFileContent
        |> strToConfig

module ProcessFiles =
    let cfg = Cfg.value.Data

    module private RegexOps =
        let normalizePath (path: string) =
            let join s1 s2 = s1 + s2

            Path.TrimEndingDirectorySeparator(path) |> join
            <| "/"
            |> strReplace "/" @"\\"

        let n = normalizePath cfg.basePath
        let rx = $"(.*{n})" + @"(.*?)\\(.*)\\(.*)"

        /// Gets a simplified mod name.
        let cleanModName modName =
            let m = Regex.Match(modName, @"(.*?)-.*")

            if m.Success then
                m.Groups[ 1 ].Value.Trim()
            else
                modName

    let getFilePaths filePath =
        let m = Regex.Match(filePath, RegexOps.rx)

        if m.Success then
            { OriginalFile = filePath
              BasePath = m.Groups[1].Value
              ModName = RegexOps.cleanModName m.Groups[2].Value
              RelPath = m.Groups[3].Value
              FileName = m.Groups[4].Value }
        else
            blankInfo

    /// Suffix added in case the user didn't provide one.
    let defaultModSuffix = " - Naked"

    let getNewFilePaths fi =
        let modPath = Path.Combine(fi.ModName + cfg.defaultModSuffix, fi.RelPath)
        let newDir = Path.Combine(fi.BasePath, modPath)
        let newFile = Path.Combine(newDir, fi.FileName)
        (newFile, newDir)


    let processFilePaths operation fi =
        let (newFile, newDir) = getNewFilePaths fi
        operation newDir newFile fi.OriginalFile
        newDir

    let copyNewFile newDir newFile originalFile =
        Directory.CreateDirectory(newDir) |> ignore
        File.Delete(newFile)
        File.Copy(sourceFileName = originalFile, destFileName = newFile)

    let openDir (dirPath: string) =
        Process.Start(fileName = "explorer.exe", arguments = dirPath)
        |> ignore

        dirPath

module Mock =
    let openDir dirPath =
        printfn "Mock open dir\t%s" dirPath
        dirPath

    let copyNewFile _ (newFile: string) _ =
        let p = Path.GetFileName(newFile)
        printfn "Mock copy\t%s" p

open ProcessFiles

let run copyNewFile openDir (args: string array) =
    if args.Length > 1 then
        args[1..]
        |> Array.toList
        |> List.map getFilePaths
        |> List.map (processFilePaths copyNewFile)
        |> List.distinct
        |> List.map openDir
        |> List.iter (printfn "Files were copied to: %s")
    else
        printfn "You need to drag and drop at least one file"
        printf "Press ENTER to continue..."
        Console.Read() |> ignore

let validPaths =
    [| "Dummy path"
       @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\f_body352.mesh.2109148288"
       @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\f_leg235.mesh.2109148288" |]

// run fsi.CommandLineArgs
let runMock = run Mock.copyNewFile Mock.openDir
runMock validPaths
