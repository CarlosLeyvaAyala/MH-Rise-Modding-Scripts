// Extensions are separated by the | character. You can put whatever extensions you need.
let ext = "mesh|chain|mdf2|albd"

open System
open System.IO
open System.Text.RegularExpressions

let armors =
    [| ("Aelucanth", "241")
       ("Aknosom S", "203")
       ("Aknosom X", "302")
       ("Aknosom", "203")
       ("Akuma", "276")
       ("Alloy", "004")
       ("Almudron X", "307")
       ("Almudron", "208")
       ("Anjanath X", "332")
       ("Anjanath", "020")
       ("Arc", "342")
       ("Archfiend Armor", "355")
       ("Arthur", "251")
       ("Arzuros", "224")
       ("Astalos", "344")
       ("Auroracanth", "348")
       ("Azure Age", "293")
       ("Azure", "361")
       ("Baggi", "223")
       ("Barbania", "383")
       ("Barioth X", "319")
       ("Barioth", "079")
       ("Barroth X", "316")
       ("Barroth", "012")
       ("Basarios X", "324")
       ("Basarios", "233")
       ("Base Commander", "381")
       ("Bazelgeuse X", "333")
       ("Bazelgeuse", "042")
       ("Benevolent Bandage", "288")
       ("Bishaten X", "301")
       ("Bishaten", "202")
       ("Black Belt S", "433")
       ("Black Belt", "280")
       ("Black Leather", "255")
       ("Blessed Feather", "507")
       ("Blossom", "291")
       ("Bnahabra", "217")
       ("Bombadgy Mask", "268")
       ("Bone", "003")
       ("Bow Necklace", "285")
       ("Brigade", "036")
       ("Bullfango Mask", "218")
       ("Bunny Dango Earrings", "267")
       ("Canyne Mask", "260")
       ("Canyne Tail", "260")
       ("Ceanataur", "338")
       ("Chainmail", "014")
       ("Channeler", "259")
       ("Chaos", "243")
       ("Charité", "374")
       ("Chrome Metal", "252")
       ("Cohoot Mask", "389")
       ("Commission", "380")
       ("Crimson Valstrax", "237")
       ("Cunning Specs", "060")
       ("Damascus X", "314")
       ("Damascus", "043")
       ("Death Stench", "026")
       ("Diablos X", "323")
       ("Diablos", "034")
       ("Dignified", "369")
       ("Diver", "277")
       ("Dober", "044")
       ("Dragonsbane", "394")
       ("Droth", "220")
       ("Edel", "244")
       ("Elgado", "508")
       ("Espinas", "356")
       ("Ethereal Diadem", "391")
       ("Feather of Mastery", "037")
       ("Felyne Ears", "261")
       ("Felyne Stealth Hood", "270")
       ("Felyne Tail", "261")
       ("Fiorayne", "407")
       ("Five Element", "385")
       ("Flame Seal", "061")
       ("Floral", "265")
       ("Formal Dragon", "375")
       ("Fox Mask", "264")
       ("Frilled Choker", "287")
       ("Gala Suit", "290")
       ("Garangolm", "354")
       ("Gargwa Mask", "213")
       ("Golden Lune", "335")
       ("Golden", "235")
       ("Gore Magala", "341")
       ("Gorgeous Earrings", "282")
       ("Goss Harag X", "306")
       ("Goss Harag", "207")
       ("Grand Chaos", "243")
       ("Grand Divine Ire", "339")
       ("Grand God's Peer", "326")
       ("Grand Mizuha", "312")
       ("Guardian", "386")
       ("Guild Bard", "376")
       ("Guild Cross", "279")
       ("Guild Palace", "387")
       ("Harp Crown", "382")
       ("Hawk", "378")
       ("Heavy Knight", "371")
       ("Hermitaur", "337")
       ("Hornetaur", "360")
       ("Hunter", "002")
       ("Ibushi - Pure", "308")
       ("Ibushi", "209")
       ("Ingot", "031")
       ("Izuchi X", "310")
       ("Izuchi", "211")
       ("Jaggi Mask", "257")
       ("Jaggi", "216")
       ("Jelly", "249")
       ("Jyuratodus X", "330")
       ("Jyuratodus", "011")
       ("Kaiser X", "313")
       ("Kaiser", "046")
       ("Kamura Cloak", "271")
       ("Kamura", "200")
       ("Kamurai", "272")
       ("Khezu X", "317")
       ("Khezu", "229")
       ("Knight Squire", "372")
       ("Kulu-Ya-Ku X", "328")
       ("Kulu-Ya-Ku", "009")
       ("Kunai Earrings", "281")
       ("Kushala Daora", "047")
       ("Kushala X", "311")
       ("Lagombi", "225")
       ("Leather Choker", "286")
       ("Leather", "001")
       ("Lecturer", "378")
       ("Lien", "363")
       ("Lucent Narga", "340")
       ("Lunagaron", "353")
       ("Magmadron", "350")
       ("Makluva", "239")
       ("Malzeno", "352")
       ("Medium", "246")
       ("Melahoa", "238")
       ("Mighty Bow Feather", "037")
       ("Mizuha", "212")
       ("Mizutsune X", "334")
       ("Mizutsune", "228")
       ("Monksnail Hat", "390")
       ("Mosgharl", "240")
       ("Nargacuga", "080")
       ("Narwa - Pure", "309")
       ("Narwa", "210")
       ("Orangaten", "347")
       ("Origin", "278")
       ("Orion", "289")
       ("Pride", "351")
       ("Professor", "374")
       ("Pukei-Pukei X", "329")
       ("Pukei-Pukei", "010")
       ("Pyre-Kadaki", "349")
       ("Qurio Crown", "400")
       ("Rakna-Kadaki X", "305")
       ("Rakna-Kadaki", "206")
       ("Rathalos X", "321")
       ("Rathalos", "033")
       ("Rathian X", "320")
       ("Rathian", "021")
       ("Relunea", "392")
       ("Remobra", "219")
       ("Reverent Wrap", "269")
       ("Rhenoplos", "215")
       ("Rhopessa", "241")
       ("Rider", "254")
       ("Royal Artillery Corps", "373")
       ("Royal Ludroth X", "315")
       ("Royal Ludroth", "227")
       ("Sailor", "377")
       ("Scholar", "376")
       ("Scholarly", "379")
       ("Seregios", "343")
       ("Shadow Shades", "062")
       ("Shell-Studded", "248")
       ("Silver Sol", "336")
       ("Sinister Demon", "300")
       ("Sinister Grudge", "346")
       ("Sinister Seal", "250")
       ("Sinister", "201")
       ("Skalda", "245")
       ("Skull", "054")
       ("Slagtoth", "214")
       ("Snowshear", "384")
       ("Soaring Feather", "507")
       ("Somnacanth X", "304")
       ("Somnacanth", "205")
       ("Sonic Wear", "292")
       ("Spio", "245")
       ("Spiribird Earrings", "283")
       ("Storge", "342")
       ("Summer", "395")
       ("Swallow", "266")
       ("Tetranadon X", "303")
       ("Tetranadon", "204")
       ("Theater Wig", "262")
       ("Tigrex X", "322")
       ("Tigrex", "083")
       ("Tobi-Kadachi X", "331")
       ("Tobi-Kadachi", "013")
       ("Uroktor", "221")
       ("Utsushi (Hidden)", "259")
       ("Utsushi (Visible)", "246")
       ("Utsushi", "408")
       ("Vaik", "242")
       ("Velociprey", "358")
       ("Vespoid", "359")
       ("Volvidon", "226")
       ("Woofpurr Earrings", "284")
       ("Wroggi", "222")
       ("Wyverian Earrings", "263")
       ("Yukumo Sky", "388")
       ("Zinogre X", "325")
       ("Zinogre", "234") |]

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

module FileFilter =
    let private createFilter (sex: string) (armorId: string) =
        let rx = $"(?i).*[{sex}]\\\\pl{armorId}.*({ext})"
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
        |> Array.iteri (fun i t -> printEntry (i + 1) (fst t))

        match tryParseInt (Console.ReadLine()) with
        | (Some i) when (i >= 1 && i <= l.Length) -> snd l[i - 1]
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
            |> Array.filter (fun t -> (fst t).ToLower().Contains(armorName))
            |> Array.sort

        match possibleArmors with
        | [||] ->
            printf "No armor was found. No files will be extracted."
            fun _ -> false
        | [| x |] -> createFilter (snd x)
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
// extract a[1] @"Extract Armor Files\mhrisePC.list"
