open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open System.Text.Json

// let serializeJson indented obj =
//     JsonSerializer.Serialize(obj, JsonSerializerOptions(WriteIndented = indented))

// type WarningList = string list

// type FileInfo =
//     { OriginalFile: string
//       BasePath: string
//       ModName: string
//       RelPath: string
//       FileName: string }

// let blankInfo =
//     { OriginalFile = ""
//       BasePath = ""
//       ModName = ""
//       RelPath = ""
//       FileName = "" }

// let strReplace oldStr newStr (str: string) =
//     str.Replace(oldValue = oldStr, newValue = newStr)

// module Cfg =
//     let private filePath = Path.Combine(__SOURCE_DIRECTORY__, "config.json")

//     type Config =
//         { basePath: string
//           defaultModSuffix: string }

//     type ConfigData = { Data: Config; Warnings: WarningList }

//     let private defaultCfg =
//         { basePath = "Mod.Organizer-2.4.4/mods"
//           defaultModSuffix = " - Naked" }

//     type ConfigFileContent =
//         | FileContent of string
//         | FileNotFound of string
//         | UnknownFileError of string

//     let private readCfgFile () =
//         try
//             let f = File.ReadAllText(filePath)
//             FileContent f
//         with
//         | :? FileNotFoundException as e -> FileNotFound e.Message
//         | _ as e -> UnknownFileError e.Message

//     let private showUnexpectedError lst error =
//         lst
//         @ [ error
//             "Getting default configuration values due to unexpected error."
//             "" ]

//     let private ensureFileContent content =
//         let d = serializeJson true defaultCfg

//         match content with
//         | FileNotFound error ->
//             let newCfgMsg = "A new configuration file with default values was created."
//             File.WriteAllText(path = filePath, contents = d)
//             ([ error; newCfgMsg; "" ], d)
//         | UnknownFileError e -> (showUnexpectedError [] e, d)
//         | FileContent c -> ([], c)

//     let private strToConfig (c: WarningList * string) =
//         try
//             let s = snd c
//             let o = JsonSerializer.Deserialize<Config> s
//             { Data = o; Warnings = fst c }
//         with
//         | _ as e ->
//             { Data = defaultCfg
//               Warnings = showUnexpectedError (fst c) e.Message }

//     let value =
//         ()
//         |> readCfgFile
//         |> ensureFileContent
//         |> strToConfig

// module ProcessFiles =
//     let cfg = Cfg.value.Data

//     module private RegexOps =
//         let normalizePath (path: string) =
//             let join s1 s2 = s1 + s2

//             Path.TrimEndingDirectorySeparator(path) |> join
//             <| "/"
//             |> strReplace "/" @"\\"

//         let n = normalizePath cfg.basePath
//         let rx = $"(.*{n})" + @"(.*?)\\(.*)\\(.*)"

//         /// Gets a simplified mod name.
//         let cleanModName modName =
//             let m = Regex.Match(modName, @"(.*?)-.*")

//             if m.Success then
//                 m.Groups[ 1 ].Value.Trim()
//             else
//                 modName

//     let getFilePaths filePath =
//         let m = Regex.Match(filePath, RegexOps.rx)

//         if m.Success then
//             { OriginalFile = filePath
//               BasePath = m.Groups[1].Value
//               ModName = RegexOps.cleanModName m.Groups[2].Value
//               RelPath = m.Groups[3].Value
//               FileName = m.Groups[4].Value }
//         else
//             blankInfo

//     let getNewFilePaths fi =
//         let modPath = Path.Combine(fi.ModName + cfg.defaultModSuffix, fi.RelPath)
//         let newDir = Path.Combine(fi.BasePath, modPath)
//         let newFile = Path.Combine(newDir, fi.FileName)
//         (newFile, newDir)

//     let processFilePaths operation fi =
//         let (newFile, newDir) = getNewFilePaths fi
//         operation newDir newFile fi.OriginalFile
//         newDir

//     let copyNewFile newDir newFile originalFile =
//         Directory.CreateDirectory(newDir) |> ignore
//         File.Delete(newFile)
//         File.Copy(sourceFileName = originalFile, destFileName = newFile)

//     let openDir (dirPath: string) =
//         Process.Start(fileName = "explorer.exe", arguments = dirPath)
//         |> ignore

//         dirPath

// module Mock =
//     let openDir dirPath =
//         printfn "Mock open dir\t%s" dirPath
//         dirPath

//     let copyNewFile _ (newFile: string) _ =
//         let p = Path.GetFileName(newFile)
//         printfn "Mock copy\t%s" p

// open ProcessFiles

// let run copyNewFile openDir (args: string array) =
//     if args.Length > 1 then
//         args[1..]
//         |> Array.toList
//         |> List.map getFilePaths
//         |> List.map (processFilePaths copyNewFile)
//         |> List.distinct
//         |> List.map openDir
//         |> List.iter (printfn "Files were copied to: %s")
//     else
//         printfn "You need to drag and drop at least one file"
//         printf "Press ENTER to continue..."
//         Console.Read() |> ignore

// let validPaths =
//     [| "Dummy path"
//        @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\f_body352.mesh.2109148288"
//        @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\f_leg235.mesh.2109148288" |]

// run fsi.CommandLineArgs
// let runMock = run Mock.copyNewFile Mock.openDir
// runMock validPaths

let testPath =
    @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\f_body235.mesh.2109148288"

let natives = "natives"
let getDirName (path: string) = Path.GetDirectoryName(path)
let getFileName (path: string) = Path.GetFileName(path)
let trimEndingDirSeparator (path: string) = Path.TrimEndingDirectorySeparator(path)
let toLower (s: string) = s.ToLower()

module Domain =
    module NameProcessing =
        type DesiredOutputDir = DesiredOutputDir of string option
        type SourceDir = SourceDir of string
        type OutputDir = OutputDir of string
        type UnvalidatedFilenameList = UnvalidatedFilenameList of string list
        type ValidatedFilenameList = private ValidatedFilenameList of string list

        module ValidatedFilenameList =
            let create (lst: string list) =
                if lst.Length = 0 then
                    failwith "You didn't pass any files."
                elif (lst |> List.map getDirName |> List.distinct)
                    .Length > 1 then
                    failwith "This script can't process files from different folders."
                elif not (lst[ 0 ].ToLower().Contains(@"natives\")) then
                    failwith "Files must be inside a \"natives\" folder."
                else
                    ValidatedFilenameList(lst |> List.map toLower)

            let value (ValidatedFilenameList lst) = lst

        type ValidatedFilesAndSourceDir =
            { Files: ValidatedFilenameList
              SourceDir: SourceDir }

        type FilesValidationResult = ValidatedFilesAndSourceDir

        type CopyFilesPetition =
            { Files: UnvalidatedFilenameList
              OutputDir: DesiredOutputDir }

        type CopyFilesData =
            { Files: ValidatedFilenameList
              SourceDir: SourceDir
              OutputDir: OutputDir }

        type FileProcessingResult = CopyFilesData

        type private ExecuteCmd = string array -> FileProcessingResult

        type InGateValidation = CopyFilesPetition

        // Commands
        type GateIn = string array -> InGateValidation

        let private gateIn: GateIn =
            fun a ->
                let outDir =
                    match a[1] with
                    | "" -> None
                    | _ as v -> Some(v)

                let files = UnvalidatedFilenameList(a[2..] |> Array.toList)

                { Files = files
                  OutputDir = (DesiredOutputDir outDir) }

        // sub steps
        type ValidateNames = UnvalidatedFilenameList -> FilesValidationResult

        let private validateNames: ValidateNames =
            fun (UnvalidatedFilenameList lst) ->
                let validLst = ValidatedFilenameList(lst |> List.map getFileName)

                { Files = validLst
                  SourceDir = SourceDir(getDirName lst[0]) }

        module private OutFolderProcess =
            let extractPaths (p: string) =
                let relPart = p[p.IndexOf(natives) ..]
                let basePart = trimEndingDirSeparator (p[0 .. p.IndexOf(natives) - 1])

                let internalPath = relPart
                let basePath = getDirName basePart
                let modName = getFileName basePart
                (basePath, modName, internalPath)

            let generateNewModName (desiredName: string option) modName =
                let template =
                    match desiredName with
                    | None -> "$o - Naked"
                    | Some s -> s

                template.Replace("$o", modName)

        let private getOutputFolder (SourceDir s) (DesiredOutputDir d) =
            let (b, m, i) = OutFolderProcess.extractPaths s
            let newMod = OutFolderProcess.generateNewModName d m
            Path.Combine(b, newMod, i)

        let execute: ExecuteCmd =
            fun a ->
                let petition = a |> gateIn
                let v = validateNames petition.Files
                let o = getOutputFolder v.SourceDir petition.OutputDir

                { Files = v.Files
                  SourceDir = v.SourceDir
                  OutputDir = OutputDir o }

    module FileCopying =
        open NameProcessing

        type private ExecuteCmd = CopyFilesData -> unit

        let getProcessList (data: CopyFilesData) =
            let (SourceDir s) = data.SourceDir
            let (OutputDir o) = data.OutputDir
            let l = ValidatedFilenameList.value data.Files
            let prepend (str: string) = fun s -> Path.Combine(str, s)
            let sl = l |> List.map (prepend s)
            let ol = l |> List.map (prepend o)
            List.zip sl ol

        let copyFileBase onFileExists onCanCopy (files: string * string) =
            let fromF = fst files
            let toF = snd files

            if File.Exists(toF) then
                onFileExists fromF toF
            else
                onCanCopy fromF toF

        module private Mock =
            let copyFile (files: string * string) =
                let print s = fun _ tf -> printfn s tf
                copyFileBase (print "File already exists:\t%s") (print "File copied:\t%s") files

        let copyFile (files: string * string) =
            copyFileBase
                (fun _ tf -> printfn "File already exists:\t%s" tf)
                (fun ff tf -> File.Copy(sourceFileName = ff, destFileName = tf))
                files

        let ensureOutputDir (OutputDir o) = Directory.CreateDirectory(o) |> ignore

        let openDir (OutputDir o) =
            Process.Start(fileName = "explorer.exe", arguments = o)
            |> ignore

        let execute: ExecuteCmd =
            fun d ->
                ensureOutputDir d.OutputDir
                let processList = getProcessList d
                processList |> List.iter copyFile
                openDir d.OutputDir
                ()
//     let copyNewFile newDir newFile originalFile =
//         Directory.CreateDirectory(newDir) |> ignore
//         File.Delete(newFile)
//         File.Copy(sourceFileName = originalFile, destFileName = newFile)

let mockInput =
    [| "work path"
       ""
       testPath
       @"F:\MH Rise\Mod.Organizer-2.4.4\mods\EBB Rajang Armor\natives\STM\player\mod\f\pl235\full\f_leg235.mesh.2109148288" |]

open Domain

// mockInput
// |> NameProcessing.execute
// |> FileCopying.execute

fsi.CommandLineArgs
|> NameProcessing.execute
|> FileCopying.execute
