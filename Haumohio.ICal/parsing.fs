module Haumohio.ICal.Parsing

open System
open System.IO
open Microsoft.FSharp.Reflection

let toString (x:'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

let fromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None


let private readLine (stream:TextReader) =
    stream.ReadLine()
        |> (fun x -> x.Split(':'))
        |> (fun parts -> (parts.[0], parts.[1]))

let private parseProduct (s:string) =
    let parts = s.Split([|"//"|], StringSplitOptions.RemoveEmptyEntries)
    let prodSplitAt = parts.[2].LastIndexOf(' ')
    {
        company = parts.[1]
        product = parts.[2].Substring(0, prodSplitAt)
        version = Version.Parse(parts.[2].Substring(prodSplitAt + 1))
        language = parts.[3]
    }


let parse (stream:TextReader) =
    let beginCal = stream |> readLine
    let cal = {
        version = stream |> readLine |> snd |> Decimal.Parse
        prodid = stream |> readLine |> snd |> parseProduct
        calScale = stream |> readLine |> snd |> fromString<CalScale> |> Option.defaultValue CalScale.GREGORIAN
        method = stream |> readLine |> snd |> fromString<CalMethod> |> Option.defaultValue CalMethod.PUBLISH
        events = []
    }
    cal
