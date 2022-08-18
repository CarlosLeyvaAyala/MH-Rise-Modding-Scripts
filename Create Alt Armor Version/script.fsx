open System.Text.RegularExpressions
open System.IO
open System
open System.Diagnostics

/// <summary>
/// Base dir where all mods are.
/// </summary>
/// <remarks>
/// <para>This is used so directory structures are correctly managed by
/// this script.</para>
///
/// <para>By default, it expects mods to be worked on to be inside a folder named <c>"mods"</c>,
/// like this:</para>
///
/// <code>
/// x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor\natives\STM\player\mod\f\pl352\f_body352.mesh.2109148288
/// x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor\natives\STM\player\mod\f\pl352\f_leg352.mesh.2109148288
/// </code>
///
/// <para>With this value being <c>"mods"</c>, <see cref="getFilePaths">getFilePaths</see> will
/// recognize this structure:</para>
/// <code>
/// BasePath = x:\MH Rise\Mod.Organizer\mods\
/// ModName = EBB Malzeno Armor
/// RelPath = natives\STM\player\mod\f\pl352\
/// FileName = f_leg352.mesh.2109148288
/// </code>
///
/// <para>And then files will be copied where they are expected to be.</para>
///
/// <para>You can change this value to your liking so it correctly deals with whatevere setup
/// you are dealing at the moment.</para>
/// </remarks>
let defaultBasePath = "mods"

/// Suffix added in case the user didn't provide one.
let defaultModSuffix = " - Naked"

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

/// Gets a simplified mod name.
let cleanModName modName =
    let m = Regex.Match(modName, @"(.*?)-.*")

    if m.Success then
        m.Groups[ 1 ].Value.Trim()
    else
        modName

let getFilePaths filePath =
    let rx =
        @"(.*\\"
        + defaultBasePath
        + @"\\)(.*?)\\(.*)\\(.*)"

    let m = Regex.Match(filePath, rx)

    if m.Success then
        { OriginalFile = filePath
          BasePath = m.Groups[1].Value
          ModName = cleanModName m.Groups[2].Value
          RelPath = m.Groups[3].Value
          FileName = m.Groups[4].Value }
    else
        blankInfo

let getNewFilePaths fi =
    let modPath = Path.Combine(fi.ModName + defaultModSuffix, fi.RelPath)
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

let mockCopyNewFile _ (newFile: string) _ =
    let p = Path.GetFileName(newFile)
    printfn "Mock copy\t%s" p

let openDir (dirPath: string) =
    Process.Start(fileName = "explorer.exe", arguments = dirPath)
    |> ignore

    dirPath

if fsi.CommandLineArgs.Length > 1 then
    fsi.CommandLineArgs[1..]
    |> Array.toList
    |> List.map getFilePaths
    |> List.map (processFilePaths mockCopyNewFile)
    |> List.distinct
    |> List.map openDir
    |> List.iter (printfn "Files were copied to: %s")
else
    printfn "You need to drag and drop at least one file"
    printf "Press ENTER to continue..."
    Console.Read() |> ignore
