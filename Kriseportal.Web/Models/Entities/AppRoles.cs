namespace Kriseportal.Web.Models.Entities;

/// <summary>
/// Navn på rollene i Identity. Bruk konstantene i [Authorize(Roles = AppRoles.PublicAuthority)]
/// så slipper vi skrivefeil i strenger.
/// </summary>
public static class AppRoles
{
    public const string PublicAuthority = "PublicAuthority";     // vises som «Offentlig aktør»
    public const string ResourceProvider = "ResourceProvider";   // vises som «Ressursleverandør»

    public static readonly string[] All = { PublicAuthority, ResourceProvider };

    // Norsk visningsnavn til skjermen
    public static string DisplayName(string role) => role switch
    {
        PublicAuthority => "Offentlig aktør",
        ResourceProvider => "Ressursleverandør",
        _ => role
    };
}
