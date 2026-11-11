namespace Kriseportal.Web.Models.Entities;

// Reglene for statusflyten til et behov: ny → vurderes → tildelt → løst.
// Et behov kan bare flyttes ett steg fram om gangen, og et løst behov kan ikke endres.
// Reglene ligger her (og ikke i kontrolleren) slik at de kan testes uten database.
public static class NeedStatusFlow
{
    // Gir neste status i flyten, eller null hvis behovet allerede er løst.
    public static NeedStatus? NextStatus(NeedStatus current)
    {
        return current switch
        {
            NeedStatus.New => NeedStatus.UnderReview,
            NeedStatus.UnderReview => NeedStatus.Assigned,
            NeedStatus.Assigned => NeedStatus.Resolved,
            _ => null
        };
    }

    // Sant bare hvis «to» er neste steg etter «from».
    public static bool CanChange(NeedStatus from, NeedStatus to)
    {
        return NextStatus(from) == to;
    }

    // Norsk tekst til skjermen.
    public static string DisplayName(NeedStatus status)
    {
        return status switch
        {
            NeedStatus.New => "Ny",
            NeedStatus.UnderReview => "Vurderes",
            NeedStatus.Assigned => "Tildelt",
            NeedStatus.Resolved => "Løst",
            _ => status.ToString()
        };
    }
}
