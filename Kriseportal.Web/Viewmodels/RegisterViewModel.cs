using System.ComponentModel.DataAnnotations;
namespace Kriseportal.Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Write your name")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Write your password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Write your email")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}
