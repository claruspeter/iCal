module Tests.ICal

open System
open Xunit
open Haumohio.ICal
open Haumohio.ICal.Publishing

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
let ``can print ProdId`` () =
    Assert.Equal("-//Abc Co.//Xyz 1.2.3.4//EN", ANON_PRODID.ToString())

[<Fact>]
let ``can print empty calendar`` () =
    let stream = new System.IO.StringWriter()
    let published =
        stream 
        |>  publishCalendar ANON_VCAL

    let result = published.ToString()
    Assert.Equal(RESULT_VCAL + END_VCAL, result)


[<Fact>]
let ``can print vEvent`` () =
    let stream = new System.IO.StringWriter()
    let published =
        stream 
        |>  publishEvent ANON_VEVENT

    let result = published.ToString()
    Assert.Equal(RESULT_VEVENT + END_VEVENT, result)


[<Fact>]
let ``can print calendar with one event`` () =
    let cal = {ANON_VCAL with events = [ANON_VEVENT]}
    let stream = new System.IO.StringWriter()
    let published =
        stream 
        |>  publishCalendar cal

    let result = published.ToString()
    Assert.Equal(RESULT_VCAL + RESULT_VEVENT + END_VEVENT + END_VCAL, result)

[<Fact>]
let ``can print calendar with many events`` () =
    let cal = {ANON_VCAL with events = [ANON_VEVENT; ANON_VEVENT; ANON_VEVENT]}
    let stream = new System.IO.StringWriter()
    let published =
        stream 
        |>  publishCalendar cal

    let result = published.ToString()
    Assert.Equal(
        (
            RESULT_VCAL + 
            RESULT_VEVENT + END_VEVENT + 
            RESULT_VEVENT + END_VEVENT + 
            RESULT_VEVENT + END_VEVENT + 
            END_VCAL
        ),
        result)