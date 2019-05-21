module Haumohio.ICal

open System
open System.IO
open Microsoft.FSharp.Reflection

[<Literal>]
let DATE_FORMAT = "yyyyMMddThhmmss"

type CalScale = | GREGORIAN
type CalMethod = | PUBLISH
type CalProduct = 
    {
        company: string
        product: string
        version: Version
        language: string
    }
    with override this.ToString() = sprintf "-//%s//%s %A//%s" this.company this.product this.version (this.language.ToUpper())

type EventStatus = | CONFIRMED | TENTATIVE
type EventTransp = | TRANSPARENT | OPAQUE
type EventClass = | PUBLIC | PRIVATE | CONFIDENTIAL

type VEvent = {
        summary: string
        uid : string
        sequence: int
        status: EventStatus
        cls: EventClass
        transp: EventTransp
        dtStart: DateTime
        dtEnd: DateTime
        dtStamp: DateTime
        lastModified: DateTime
        description: string
        location: string
        url: Uri
    }

type VCalendar = {
    version: decimal
    prodid: CalProduct
    calScale: CalScale
    method: CalMethod
    events: VEvent list
}

let private publishValue (a, b) (stream: TextWriter) =
    let s = sprintf "%s:%s" (a.ToString().ToUpperInvariant()) (b.ToString())
    s |> stream.WriteLine
    stream

let publishEvent (evt: VEvent) (stream: TextWriter) =
    stream 
    |> publishValue ("BEGIN", "VEVENT")
    |> publishValue ("SUMMARY", evt.summary)
    |> publishValue ("UID", evt.uid)
    |> publishValue ("SEQUENCE", evt.sequence)
    |> publishValue ("STATUS", evt.status)
    |> publishValue ("CLASS", evt.cls)
    |> publishValue ("TRANSP", evt.transp)
    |> publishValue ("DTSTART", evt.dtStart.ToString(DATE_FORMAT))
    |> publishValue ("DTEND", evt.dtEnd.ToString("yyyyMMddThhmmss"))
    |> publishValue ("DTSTAMP", evt.dtStamp.ToString("yyyyMMddThhmmss"))
    |> publishValue ("LAST-MODIFIED", evt.lastModified.ToString("yyyyMMddThhmmss"))
    |> publishValue ("DESCRIPTION", evt.description)
    |> publishValue ("LOCATION", evt.location)
    |> publishValue ("URL", evt.url)
    |> publishValue ("END", "VEVENT")


let publishCalendar (cal: VCalendar) (stream: TextWriter) =
    stream 
    |> publishValue ("BEGIN", "VCALENDAR")
    |> publishValue ("VERSION", cal.version)
    |> publishValue ("PRODID", cal.prodid)
    |> publishValue ("CALSCALE", cal.calScale)
    |> publishValue ("METHOD", cal.method)
    |> fun s ->  cal.events |> List.fold (fun str evt -> publishEvent evt str ) s
    |> publishValue ("END", "VCALENDAR")


let publish (cal: VCalendar) =
    new StringWriter()
    |> publishCalendar cal
    |> fun x -> x.ToString()


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
    {
        version = stream |> readLine |> snd |> Decimal.Parse
        prodid = stream |> readLine |> snd |> parseProduct
        calScale = stream |> readLine |> snd |> fromString<CalScale> |> Option.defaultValue CalScale.GREGORIAN
        method = stream |> readLine |> snd |> fromString<CalMethod> |> Option.defaultValue CalMethod.PUBLISH
        events = []
    }