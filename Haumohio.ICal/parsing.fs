module Haumohio.ICal.Parsing

open System
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.Reflection
open System.Globalization


type IndexedSection = {
    name: string;
    values: IDictionary<string,string>
    children: IDictionary<string,IndexedSection list>
}

type private Section = {
    values: (string * string) list
    children: IndexedSection list
}

let toString (x:'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

let fromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None

let lookupWithDefault<'a, 'b> (theDefault: 'b) (key:'a) (dic:IDictionary<'a, 'b>) : 'b =
    match dic.ContainsKey(key) with
    | true -> dic.[key]
    | false -> theDefault

let private index (section:Section) =
    let name = section.values |> Seq.find(fun x -> (fst x) = "BEGIN") |> snd
    let values = section.values |> List.filter (fun x -> (fst x) <> "BEGIN" && (fst x) <> "END") |> dict
    let children = section.children |> List.groupBy (fun s -> s.name) |> dict
    {
        name = name
        values = values
        children = children
    }


let private readLine (stream:TextReader) =
    let line = stream.ReadLine()
    match line with
    | null -> None
    | _ ->
        let colonAt = line.IndexOf(':')
        (line.Substring(0, colonAt), line.Substring(colonAt + 1)) |> Some

let rec private readIntoSection stream (section:Section) : IndexedSection =
    let line = readLine stream
    match line with 
    | None -> section |> index
    | Some line ->
        match fst line, section.values.Length with 
        | beginner, 0 when beginner.StartsWith("BEGIN") -> 
            // printfn "\n%A is my beginner" line
            let updated = {section with values = section.values @ [line]}
            readIntoSection stream updated
        | beginner, _ when beginner.StartsWith("BEGIN") -> 
            // printfn "\n%A is a beginner" line
            let subsection = readIntoSection stream {values=[line]; children = []}
            let updated = {section with children = section.children @ [subsection]}
            readIntoSection stream updated
        | ender, 0 when ender.StartsWith("END") -> 
            // printfn "%A is a container ender" line
            section |> index
        | ender, _ when ender.StartsWith("END") -> 
            // printfn "%A is an ender" line
            section |> index
        | _, _ -> 
            // printfn "%A is a value" line
            let updated = {section with values = section.values @ [line]}
            readIntoSection stream updated
        

let private readSection (stream: TextReader) =
    {values=[]; children=[]}
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

let private parseEvent (section:IndexedSection) =
    {
        summary = section.values.["SUMMARY"] 
        uid  = section.values.["UID"] 
        sequence = section.values.["SEQUENCE"] |> Int32.Parse
        status = section.values.["STATUS"] |> fromString<EventStatus> |> Option.defaultValue EventStatus.CONFIRMED
        cls = section.values.["CLASS"] |> fromString<EventClass> |> Option.defaultValue EventClass.PUBLIC
        transp = section.values.["TRANSP"] |> fromString<EventTransp> |> Option.defaultValue EventTransp.TRANSPARENT
        dtStart = section.values.["DTSTART"] |> parseTime
        dtEnd = section.values.["DTEND"] |> parseTime
        dtStamp = section.values.["DTSTAMP"] |> parseTime
        lastModified = section.values.["LAST-MODIFIED"] |> parseTime
        description = section.values.["DESCRIPTION"] 
        location = section.values.["LOCATION"] 
        url = Uri(section.values.["URL"], UriKind.Absolute)
    }

let parse (stream:TextReader) =
    let cal = readSection stream
    let events = 
        cal.children 
        |> lookupWithDefault [] "VEVENT"
        |> List.map parseEvent

    {
        version = cal.values.["VERSION"] |> Decimal.Parse
        prodid = cal.values.["PRODID"] |> parseProduct
        calScale = cal.values.["CALSCALE"] |> fromString<CalScale> |> Option.defaultValue CalScale.GREGORIAN
        method = cal.values.["METHOD"] |> fromString<CalMethod> |> Option.defaultValue CalMethod.PUBLISH
        events = events
    }
