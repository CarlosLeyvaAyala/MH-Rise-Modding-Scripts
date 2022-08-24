namespace Domain

/// Full parameters needed to start processing files.
type FullParams = { InputDir: string; OutFile: string }

/// String surrounded by quotes.
type QuotedStr = private QuotedStr of string

module QuotedStr =
  let private transformIfNot transform condition x =
    if not (condition x) then
      transform x
    else
      x

  let private ensureFirstQuote s =
    transformIfNot (fun s -> "\"" + s) (fun (s: string) -> s.StartsWith('"')) s

  let private ensureTrailQuote s =
    transformIfNot (fun s -> s + "\"") (fun (s: string) -> s.EndsWith('"')) s

  let create (fileName: string) =
    QuotedStr(fileName |> ensureFirstQuote |> ensureTrailQuote)

  let value (QuotedStr fileName) = fileName

  let unquote (QuotedStr fileName) = fileName[1..]
