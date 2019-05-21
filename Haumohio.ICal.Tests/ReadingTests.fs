module ICal.ReadingTests

open System
open Xunit
open Haumohio.ICal
open System.IO

let ANON_PRODID  = {company = "Abc Co."; product="Xyz"; version=Version(1,2,3,4); language="en"}
let ANON_VEVENT = 
    {
        VEvent.summary = "Sumarry"
        uid = "1qazxsw23edc"
        sequence = 0
        status = CONFIRMED
        cls = PUBLIC
        transp = TRANSPARENT
        dtStart = DateTime(2019,1,1,8,0,0)
        dtEnd = DateTime(2019,1,1,9,30,0)
        dtStamp = DateTime(2019,1,1,9,31,0)
        description = "Desc."
        lastModified = DateTime(2019,1,2,0,0,0)
        location = "The location"
        url = Uri("http://test.com")
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
    "DTSTART:20190101T080000\n" +
    "DTEND:20190101T093000\n" +
    "DTSTAMP:20190101T093100\n" +
    "LAST-MODIFIED:20190102T120000\n" +
    "DESCRIPTION:Desc.\n" +
    "LOCATION:The location\n" +
    "URL:http://test.com/\n"
let END_VEVENT = "END:VEVENT\n"


[<Fact>]
let ``can read cal header`` () =
    let result = 
        RESULT_VCAL + END_VCAL
        |> fun s -> new StringReader(s)
        |> parse
    Assert.Equal(2.0m, result.version)
    