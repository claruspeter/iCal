module ICal.ReadingTests

open System
open System.IO
open Xunit
open Haumohio.ICal
open Haumohio.ICal.Parsing

let ANON_PRODID  = {company = "Abc Co."; product="Xyz"; version=Version(1,2,3,4); language="en"}
let ANON_VEVENT = 
    {
        VEvent.summary = "Sumarry"
        uid = "1qazxsw23edc"
        sequence = 0
        status = CONFIRMED
        cls = PUBLIC
        transp = TRANSPARENT
        dtStart = DateTime(2019,1,1,8,0,0, DateTimeKind.Utc)
        dtEnd = DateTime(2019,1,1,9,30,0, DateTimeKind.Utc)
        dtStamp = DateTime(2019,1,1,9,31,0, DateTimeKind.Utc)
        lastModified = DateTime(2019,1,2,12,0,0, DateTimeKind.Utc)
        description = "Desc."
        location = "The location"
        url = Uri("http://test.com/")
    }
let ANON_VCAL = {
        version = 2.0m
        prodid = ANON_PRODID
        calScale = CalScale.GREGORIAN
        method = CalMethod.PUBLISH
        events = []
    }


let RESULT_VCAL = "BEGIN:VCALENDAR\nVERSION:2.0\nPRODID:-//Abc Co.//Xyz 1.2.3.4//EN\nCALSCALE:GREGORIAN\nMETHOD:PUBLISH\n"
let END_VCAL = "END:VCALENDAR\n"

let RESULT_VEVENT = 
    "BEGIN:VEVENT\n" +
    "SUMMARY:Sumarry\n" +
    "UID:1qazxsw23edc\n" +
    "SEQUENCE:0\n" +
    "STATUS:CONFIRMED\n" +
    "CLASS:PUBLIC\n" +
    "TRANSP:TRANSPARENT\n" +
    "DTSTART:20190101T080000Z\n" +
    "DTEND:20190101T093000Z\n" +
    "DTSTAMP:20190101T093100Z\n" +
    "LAST-MODIFIED:20190102T120000Z\n" +
    "DESCRIPTION:Desc.\n" +
    "LOCATION:The location\n" +
    "URL:http://test.com\n"
let END_VEVENT = "END:VEVENT\n"

let makeEvent i =
    (RESULT_VEVENT + END_VEVENT).Replace("SEQUENCE:0", "SEQUENCE:" + i.ToString())

[<Fact>]
let ``can read just cal header`` () =
    let result = 
        RESULT_VCAL + END_VCAL
        |> fun s -> new StringReader(s)
        |> parse
    Assert.Equal(2.0m, result.version)
    Assert.Equal("Abc Co.", result.prodid.company)
    Assert.Equal("Xyz", result.prodid.product)
    Assert.Equal(Version(1, 2, 3, 4), result.prodid.version)
    Assert.Equal("EN", result.prodid.language)
    Assert.Equal(CalMethod.PUBLISH, result.method)
    Assert.Equal(CalScale.GREGORIAN, result.calScale)
    

[<Fact>]
let ``can read the right number of cal events`` () =
    let result = 
        RESULT_VCAL + makeEvent(12) + makeEvent(14) + makeEvent(16) + END_VCAL
        |> fun s -> new StringReader(s)
        |> parse
    Assert.Equal(3, result.events.Length)
    Assert.Equal(12,  result.events.[0].sequence)
    Assert.Equal(14,  result.events.[1].sequence)
    Assert.Equal(16,  result.events.[2].sequence)

[<Fact>]
let ``can read event values`` () =
    let result = 
        RESULT_VCAL + makeEvent(0) + END_VCAL
        |> fun s -> new StringReader(s)
        |> parse
    let evt = result.events.[0]
    Assert.Equal(ANON_VEVENT, evt)
