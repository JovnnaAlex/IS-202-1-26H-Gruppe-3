using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    // Glemt passord: skjemaet der brukeren velger nytt passord fra lenken.
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Du må oppgi e-postadresse.")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
        public string Email { get; set; } = string.Empty;

        // Engangskoden fra lenken. Identity kontrollerer den, og den varer i én time.
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du må oppgi nytt passord.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du må gjenta passordet.")]
        [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool PublicAuthority { get; set; }
    }
}
