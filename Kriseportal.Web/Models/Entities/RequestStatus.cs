namespace Kriseportal.Web.Models.Entities;

// Statusflyten for en forespørsel: sendt → bekreftet eller avslått.
// Vises på norsk i grensesnittet: «Venter på svar», «Bekreftet», «Avslått».
public enum RequestStatus
{
    Proposed,
    Confirmed,
    Rejected
}