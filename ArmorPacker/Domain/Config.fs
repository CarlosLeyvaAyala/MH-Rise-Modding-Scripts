namespace Domain

open System.Text.RegularExpressions

/// <summary>Extensions used for searching which files will be compressed.</summary>
/// <remarks>These are internally a regular expression.</remarks>
type Extensions = private Extensions of string

module Extensions =
  let create str =
    Regex
      .Replace(str, @"\s*,\s*", "|")
      .Replace(".", @"\.")
    |> Extensions

  let value (Extensions ext) = ext

/// <summary>Data read from the propietary <c>config.ini</c> file.</summary>
type ConfigData = { RelDir: string; Extensions: Extensions }
