module Haumohio.ICal.Parsing

open System
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.Reflection
open System.Collections.Generic
open System.Globalization

type Section = {
    name: string;
    values: (string * string) list
}

let toString (x:'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

let fromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None


let private readLine (stream:TextReader) =
    let line = stream.ReadLine()
    match line with
    | null -> None
    | _ ->
        let colonAt = line.IndexOf(':')
        (line.Substring(0, colonAt), line.Substring(colonAt + 1)) |> Some

let rec private readIntoSection stream section =
    let line = readLine stream
    match line with 
    | None -> None
    | Some line ->
        match fst line, section.values.Length with 
        | beginner, 0 when beginner.StartsWith("BEGIN") -> 
            // printfn "\n%A is my beginner" line
            readIntoSection stream {section with name = snd line}
        | beginner, _ when beginner.StartsWith("BEGIN") -> 
            // printfn "\n%A is a beginner" line
            Some section
        | ender, 0 when ender.StartsWith("END") -> 
            printfn "%A is an ender of a container" line
            None
        | ender, _ when ender.StartsWith("END") -> 
            // printfn "%A is an ender" line
            Some {section with name = snd line}
        | _, _ -> 
            // printfn "%A is a value" line
            let updated = {section with values = section.values @ [(fst line, snd line)]}
            readIntoSection stream updated
        

let private readSection (stream: TextReader) =
    {name = ""; values=[]}
    |> readIntoSection stream

let private parseProduct (s:string) =
    let parts = s.Split([|"//"|], StringSplitOptions.RemoveEmptyEntries)
    let prodSplitAt = parts.[2].LastIndexOf(' ')
    {
        company = parts.[1]
        product = parts.[2].Substring(0, prodSplitAt)
        version = Version.Parse(parts.[2].Substring(prodSplitAt + 1))
        language = parts.[3]
    }

let private parseTime (s:string) =
    match s with
    | x when x.EndsWith("Z") ->
        DateTime.ParseExact( x, [|"yyyyMMddTHHmmssZ"|], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
    | x ->
        DateTime.ParseExact( x, [|"yyyyMMddTHHmmss"; "yyyyMMddTHHmmsszzz"|], CultureInfo.InvariantCulture, DateTimeStyles.None)

let private parseEvent (section:Section) =
    let values = section.values |> dict
    {
        summary = values.["SUMMARY"] 
        uid  = values.["UID"] 
        sequence = values.["SEQUENCE"] |> Int32.Parse
        status = values.["STATUS"] |> fromString<EventStatus> |> Option.defaultValue EventStatus.CONFIRMED
        cls = values.["CLASS"] |> fromString<EventClass> |> Option.defaultValue EventClass.PUBLIC
        transp = values.["TRANSP"] |> fromString<EventTransp> |> Option.defaultValue EventTransp.TRANSPARENT
        dtStart = values.["DTSTART"] |> parseTime
        dtEnd = values.["DTEND"] |> parseTime
        dtStamp = values.["DTSTAMP"] |> parseTime
        lastModified = values.["LAST-MODIFIED"] |> parseTime
        description = values.["DESCRIPTION"] 
        location = values.["LOCATION"] 
        url = Uri(values.["URL"], UriKind.Absolute)
    }

let parse (stream:TextReader) =
    let section = readSection stream
    match section with
    | None -> failwith "Calendar not found in stream."
    | Some section ->
        let values = section.values |> dict
        let events = 
            stream
            |> Seq.unfold 
                (fun stream' -> 
                    match readSection stream' with
                    | None -> None
                    | Some e -> (e |> parseEvent, stream') |> Some
                )

        {
            version = values.["VERSION"] |> Decimal.Parse
            prodid = values.["PRODID"] |> parseProduct
            calScale = values.["CALSCALE"] |> fromString<CalScale> |> Option.defaultValue CalScale.GREGORIAN
            method =values.["METHOD"] |> fromString<CalMethod> |> Option.defaultValue CalMethod.PUBLISH
            events = events |> Seq.toList
        }
