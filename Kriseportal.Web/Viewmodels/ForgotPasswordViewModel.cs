using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    // Glemt passord: skjemaet der brukeren skriver inn e-postadressen sin.
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Du må oppgi e-postadresse.")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
        public string Email { get; set; } = string.Empty;

        // Husker om brukeren kom fra innloggingen for offentlige aktører,
        // slik at «Tilbake til innlogging» går til riktig side.
        public bool PublicAuthority { get; set; }
    }
}
