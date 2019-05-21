module Haumohio.ICal.Publishing

open System.IO

[<Literal>]
let DATE_FORMAT = "yyyyMMddThhmmss"


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
