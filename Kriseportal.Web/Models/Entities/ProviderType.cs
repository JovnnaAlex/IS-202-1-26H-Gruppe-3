using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models.Entities;

// Hva slags ressursleverandør en bruker er.
// Kodenavn er uten æøå; norsk tekst til skjermen ligger i Display(Name).
public enum ProviderType
{
    [Display(Name = "Privatperson")] Privatperson,
    [Display(Name = "Bedrift")] Bedrift,
    [Display(Name = "Bonde")] Bonde,
    [Display(Name = "Entreprenør")] Entreprenor,
    [Display(Name = "Frivillig organisasjon")] FrivilligOrganisasjon,
    [Display(Name = "Droneoperatør")] Droneoperator
}
