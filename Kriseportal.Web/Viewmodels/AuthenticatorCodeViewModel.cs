using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models;

public class AuthenticatorCodeViewModel
{
    [Required(ErrorMessage = "Skriv inn koden fra autentiseringsappen.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Koden må bestå av seks sifre.")]
    [Display(Name = "Kode fra autentiseringsappen")]
    public string Code { get; set; } = string.Empty;
}