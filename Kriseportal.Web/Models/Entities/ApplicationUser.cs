
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Kriseportal.Web.Models.Entities;

/// <summary>
/// Brukeren i systemet. Arver fra IdentityUser, som allerede har Id (GUID som tekst),
/// e-post, telefon og passordhash. Vi legger bare til feltene som modell 1.2 har ut over det.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Settes bare for offentlige aktører (rollen PublicAuthority).
    public Agency? Agency { get; set; }

    // Settes bare for ressursleverandører (rollen ResourceProvider).
    public ProviderType? ProviderType { get; set; }

    // Sara: Viser om ressursleverandøren har deaktivert kontoen midlertidig.
    public bool IsDeactivated { get; set; } = false;

    // Sara: Valgfri beskrivelse av ressursleverandøren eller virksomheten.
    [MaxLength(2000)]
    public string? Description { get; set; }

    // Sara: Adresse til ressursleverandøren.
    [MaxLength(200)]
    public string? Address { get; set; }

    // Sara: Tidspunktet kontoen ble deaktivert.
    public DateTime? DeactivatedAt { get; set; }
}
