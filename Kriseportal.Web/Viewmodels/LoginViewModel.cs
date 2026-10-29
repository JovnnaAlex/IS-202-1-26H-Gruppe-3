using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Du må oppgi e-postadresse.")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du må oppgi passord.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}


