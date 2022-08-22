// Extensions are separated by the | character. You can put whatever extensions you need.
let ext = "mesh|chain|mdf2|albd"

open System
open System.IO
open System.Text.RegularExpressions
open System.Text.Json

let serializeJson indented obj =
    JsonSerializer.Serialize(obj, JsonSerializerOptions(WriteIndented = indented))

let tryParseInt s =
    try
        s |> int |> Some
    with
    | :? FormatException -> None

let tap f x =
    f
    x

let readAndFilterLargeFile fileName doSomething =
    use largeFile = File.OpenText(fileName)
    let mutable valid = true
    let mutable result: string list = []

    while (valid) do
        let line = largeFile.ReadLine()

        if (line = null) then
            valid <- false
        else if doSomething line then
            result <- result @ [ line ]

    result

let writeAllText path contents = File.WriteAllText(path, contents)

type ArmorInfo = { Name: string; Id: string }
type ArmorInfoArray = ArmorInfo array

let armors =
    File.ReadAllText("armors.json")
    |> JsonSerializer.Deserialize<ArmorInfoArray>

module FileFilter =
    let private createFilter (sex: string) (armorId: string) =
        let baseRx = @"natives\\STM\\player\\mod\\"
        let rx = $"(?i){baseRx}[{sex}]\\\\pl{armorId}.*({ext})"
        let regex = Regex(rx, RegexOptions.Compiled)
        fun line -> regex.Match(line).Success

    let private getSex () =
        let s = [ "(F)emale"; "(M)ale"; "(B)oth" ]
        printfn "Which sex do you want to extract the armor for?"
        s |> List.iter (printfn "\t%s")

        let (|StartsWith|_|) (prefix: string) (s: string) =
            if s.ToLower().StartsWith(prefix) then
                Some StartsWith
            else
                None

        match Console.ReadLine() with
        | StartsWith "m" -> "m"
        | StartsWith "b" -> "fm"
        | StartsWith "f" -> "f"
        | _ ->
            printfn "\nUnknown selection. I will assume you want to extract both man and woman armor.\n"
            "fm"

    let private menu l =
        printfn "These armors were found. Select one by entering a number:"
        let printEntry = printfn "\t%d. %s"

        l
        |> Array.iteri (fun i t -> printEntry (i + 1) t.Name)

        match tryParseInt (Console.ReadLine()) with
        | (Some i) when (i >= 1 && i <= l.Length) -> l[i - 1].Id
        | _ -> ""

    let private loopMenu l =
        let mutable continueLoop = true
        let mutable selection = ""

        while continueLoop do
            selection <- menu l
            continueLoop <- selection = ""

        selection

    let private getFilterFromName createFilter (armorName: string) =
        let armorName = armorName.ToLower()

        let possibleArmors =
            armors
            |> Array.filter (fun t -> t.Name.ToLower().Contains(armorName))
            |> Array.sort

        match possibleArmors with
        | [||] ->
            printf "No armor was found. No files will be extracted."
            fun _ -> false
        | [| x |] -> createFilter x.Id
        | l -> createFilter (loopMenu l)

    let private getArmorId createFilter =
        printf "Enter the armor name or number you want to extract: "
        let desiredArmorId = Console.ReadLine()

        match tryParseInt desiredArmorId with
        | Some id -> createFilter (id.ToString())
        | None -> getFilterFromName createFilter (desiredArmorId)

    let get () =
        () |> getSex |> createFilter |> getArmorId

let toStr lst =
    (lst, "")
    ||> List.foldBack (fun acc s -> acc + "\n" + s)

let printFiltering (fileName: string) =
    printfn "\nFiltering files from %s, please wait...\n" (Path.GetFileName(fileName))

let extract fullPcList outputPcList =
    FileFilter.get ()
    |> tap (printFiltering fullPcList)
    |> readAndFilterLargeFile fullPcList
    |> toStr
    |> tap (File.Delete(outputPcList))
    |> writeAllText outputPcList

let a = fsi.CommandLineArgs
extract a[1] a[2]
