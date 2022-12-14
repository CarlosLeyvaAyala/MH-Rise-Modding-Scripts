namespace Domain

open System.Text.RegularExpressions

type IniVariableValue = string

/// <summary>Extensions used for searching which files will be compressed.</summary>
/// <remarks>Internally, these are a regular expression.</remarks>
type Extensions = private Extensions of string

module Extensions =
  let create str =
    Regex
      .Replace(str, @"\s*,\s*", "|")
      .Replace(".", @"\.")
    |> Extensions

  let value (Extensions ext) : IniVariableValue = ext

  let defaultValue = "mdf2,mesh,chain"

  /// Regular expression used to see if some file name matches the extension list.
  let fileFilterRegex (Extensions ext) = @"(?i).*\.(" + ext + @")\.\d+.*"

  let getNoFilesError (Extensions ext) =
    let t =
      ext.Split("|")
      |> Array.map (fun e -> $"*.{e}.bunchOfWeirdNumbers")
      |> DMLib.String.toStrWithNl

    $"No file with any of these extensions was found:\n\n{t}\nMake sure you either add those files to your mod or set what extensions to search for in config.ini"
    |> ErrorMsg

/// Variables inside an ini file.
type IniFileContents = IniFileContents of string array

type InexistentIniFileError = InexistentIniFileError of string

type IniFilePath = string

/// <summary>Gets the contents fron an ini file.</summary>
type GetIniFileContents = IniFilePath -> Result<IniFileContents, InexistentIniFileError>

type NoConfigValueError = NoConfigValueError of string

type IniValueError =
  | NoValue of NoConfigValueError
  | ManyVariables of string

/// Value read from an ini file
type IniValue = Result<IniVariableValue, IniValueError>

type IniVariableName = string
type GetConfigValue = NoConfigValueError -> IniVariableName -> IniFileContents -> IniValue

type ConfigError =
  | ValueError of IniValueError
  | NoFileError of InexistentIniFileError

/// <summary>Data read from the propietary <c>config.ini</c> file.</summary>
type ConfigData =
  { RelDir: string
    Extensions: Extensions
    OptionsPrefix: string }

type GetConfigData = DirToProcess -> Result<ConfigData, ErrorMsg>

/// Get variable value from modinfo.ini
type GetModInfoVariable = ModInfoFileName -> IniFilePath -> Result<IniVariableValue, ErrorMsg>
