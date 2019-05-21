namespace Haumohio.ICal

open System


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



