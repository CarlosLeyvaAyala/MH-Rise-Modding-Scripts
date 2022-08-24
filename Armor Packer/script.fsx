let zipExe = @"C:\Program Files\7-Zip\7z.exe"

let inDir = @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\"
let relDir = @"natives\STM\player\mod\f\pl080"
let outFileName = "EBB Nargacuga - Options"

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions

let modinfo = "modinfo.ini"
let getDir (path: string) = Path.GetDirectoryName(path)
let getExt (path: string) = Path.GetExtension(path)
let getFileName (path: string) = Path.GetFileName(path)
let trim (s: string) = s.Trim()
let surroundWith str surround = surround + str + surround
let foldNl acc s = acc + s + "\n"
let toStrWithNl = Array.fold foldNl ""

let fileLines fileName =
    File.ReadAllText(fileName).Split("\n")
    |> Array.map (fun s -> s.TrimEnd())

let removeDrive (path: string) =
    let m = Regex.Match(path, @".*:\\(.*)")

    if m.Success then
        m.Groups[1].Value
    else
        path

removeDrive @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\Leg Option - Mesh pantsu\modinfo.ini"

let transformIfNot transform condition x =
    if not (condition x) then
        transform x
    else
        x

module Domain =
    type ConfigData = { RelDir: string; Extensions: string }

    type QuotedStr = private QuotedStr of string

    module QuotedStr =
        let private ensureFirstQuote s =
            transformIfNot (fun s -> "\"" + s) (fun (s: string) -> s.StartsWith('"')) s

        let private ensureTrailQuote s =
            transformIfNot (fun s -> s + "\"") (fun (s: string) -> s.EndsWith('"')) s

        let create (fileName: string) =
            QuotedStr(fileName |> ensureFirstQuote |> ensureTrailQuote)

        let value (QuotedStr fileName) = fileName

        let unquote (QuotedStr fileName) = fileName[1..]

    type ExeName = private ExeName of QuotedStr

    module ExeName =
        let create fileName = fileName |> QuotedStr.create |> ExeName
        let value (ExeName fileName) = fileName |> QuotedStr.value

    type ZipFile = private ZipFile of QuotedStr

    module ZipFile =
        let hasExtension ext (fileName: string) =
            let fn = fileName.Replace("\"", "") |> trim |> getExt
            ext = fn

        let create (fileName: string) =
            if not (hasExtension ".7z" fileName) then
                failwith "File must have the 7z extension"
            else
                ZipFile(QuotedStr.create fileName)

        let value (ZipFile fileName) = fileName |> QuotedStr.value

open Domain

let jsonPathToWinPath (json: string) =
    json.Replace('/', Path.DirectorySeparatorChar)

let executeApp (fileName: ExeName) (args: string) =
    Process
        .Start(fileName = $"{ExeName.value fileName}", arguments = args)
        .WaitForExit()

let batExecuteApp (fileName: ExeName) (args: string) = $"{ExeName.value fileName} {args}"

let zipExecute = batExecuteApp (ExeName.create zipExe)

let zipCreate (fileName: ZipFile) =
    [| zipExecute $"d {ZipFile.value fileName}" |]

let zipAdd (zipFile: ZipFile) relDir (fileName: QuotedStr) inArr =
    let zf = ZipFile.value zipFile
    let fn = QuotedStr.value fileName
    let add = $"a -t7z {zf} {fn} -spf2"

    let quote str =
        str |> QuotedStr.create |> QuotedStr.value

    let oldFn =
        fileName
        |> QuotedStr.unquote
        |> removeDrive
        |> quote

    let newFn =
        fileName
        |> QuotedStr.unquote
        |> getFileName
        |> (fun s -> Path.Combine(relDir, s))
        |> quote

    let rename = $"rn {zf} {oldFn} {newFn}"
    Array.append inArr [| zipExecute add; zipExecute rename |]

let baseDir = getDir (Path.TrimEndingDirectorySeparator(inDir))

let outFile =
    Path.Combine(baseDir, outFileName + ".7z")
    |> ZipFile.create

let t =
    QuotedStr.create
        @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\Leg Option - Mesh pantsu\modinfo.ini"

let tt =
    QuotedStr.create
        @"C:\Users\Osrail\Documents\GitHub\MH-Rise-EBB-Armors\080 Nargacuga\Leg Option - Mesh pantsu\f_leg080.mesh.2109148288"

let tempBat = Path.Combine(baseDir, $"{outFileName}.bat")

let testZip () =
    outFile
    |> zipCreate
    |> zipAdd outFile "" t
    |> zipAdd outFile relDir tt
    |> Array.insertAt 0 "@echo off"
    |> Array.insertAt 1 $"del {ZipFile.value outFile}"
    |> Array.fold (fun acc s -> acc + s + "\n") ""
    |> (fun s -> s + "pause")
    |> trim
    |> (fun s -> File.WriteAllText(tempBat, s))

// () |> testZip

module Config =
    let private getRawConfig fileName dir =
        let f = Path.Combine(dir, fileName)

        if not (File.Exists(f)) then
            failwith $"File \"{f}\" must exist before we can continue."
        else
            fileLines f

    let private getConfigValue errorMsg cfg varName =
        let rx = "(?i)" + varName + @"\s*=\s*(.*)"
        let regex = Regex(rx)

        match cfg
              |> Array.filter (fun s -> regex.Match(s).Success)
            with
        | [||] -> Error errorMsg
        | [| l |] -> Ok(regex.Match(l).Groups[1].Value)
        | _ -> Error $"Your config.ini file has many variables named {varName}. You should have only one of those."

    let getExtensions cfg =
        let replace s =
            Regex
                .Replace(s, @"\s*,\s*", "|")
                .Replace(".", @"\.")

        match getConfigValue "" cfg "extensions" with
        | Error _ -> @"mdf2\.|mesh\.|chain\."
        | Ok s -> replace s

    let get () =
        let cfg = getRawConfig "config.ini" inDir

        let error =
            "You need to define the modInternalPath variable in your ini file, otherwise where your mod files would be installed by Fluffy?"

        let rel = getConfigValue error cfg "modInternalPath"

        match rel with
        | Ok r ->
            { RelDir = r
              Extensions = getExtensions cfg }
        | Error e -> failwith e

    module ModInfo =
        let getScreenShot fileName dir =
            getConfigValue "" (getRawConfig fileName dir) "screenshot"

// Para cada opción, agregar archivos al rar de salida
let compressOption cfg outFile dirName =
    let qStr str = QuotedStr.create str

    let getFiles () =
        let toZip fileName =
            zipAdd outFile cfg.RelDir (qStr fileName) [||]

        let rx = @"(?i).*\.(" + cfg.Extensions + @")\.\d+.*"

        Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories)
        |> Array.filter (fun s -> Regex.Match(s, rx).Success)
        |> Array.map toZip
        |> Array.collect (fun a -> a)

    let optional opt results =
        match opt with
        | Error _ -> results
        | Ok v ->
            let q = qStr (Path.Combine(dirName, v))
            zipAdd outFile "" q results

    let screenshot = Config.ModInfo.getScreenShot modinfo dirName
    let modinfo' = qStr (Path.Combine(dirName, modinfo))

    [||]
    |> zipAdd outFile "" modinfo'
    |> optional screenshot
    |> fun a -> Array.append a (getFiles ())
    |> toStrWithNl

let pack () =
    let cfg = Config.get ()

    let options =
        Directory.GetFiles(inDir, modinfo, SearchOption.AllDirectories)
        |> Array.map getDir

    if options.Length < 1 then
        failwith $"Put a {modinfo} file inside each folder you want to pack."

    let outFile =
        Path.Combine(baseDir, outFileName + ".7z")
        |> ZipFile.create

    options
    |> Array.map (compressOption cfg outFile)
    |> Array.insertAt 0 "@echo off"
    |> Array.insertAt 1 $"del {ZipFile.value outFile}"
    |> toStrWithNl
    |> (fun s -> s + "pause")
    |> trim
    |> (fun s -> File.WriteAllText(tempBat, s))
// Array.iter (fun s -> printfn "%s" s) options

pack ()
