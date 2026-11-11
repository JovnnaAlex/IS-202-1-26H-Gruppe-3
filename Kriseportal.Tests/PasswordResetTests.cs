using System.ComponentModel.DataAnnotations;
using Kriseportal.Web.Models;

namespace Kriseportal.Tests;

// Glemt passord: tester valideringen i skjemaene for glemt og nytt passord.
public class PasswordResetTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    [Fact]
    public void ForgotPassword_UtenEpost_Avvises()
    {
        var model = new ForgotPasswordViewModel { Email = "" };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ForgotPasswordViewModel.Email)));
    }

    [Fact]
    public void ForgotPassword_UgyldigEpost_Avvises()
    {
        var model = new ForgotPasswordViewModel { Email = "ikke-en-epost" };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ForgotPasswordViewModel.Email)));
    }

    [Fact]
    public void ForgotPassword_GyldigEpost_Godtas()
    {
        var model = new ForgotPasswordViewModel { Email = "ola@kriseportal.test" };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void ResetPassword_UlikePassord_Avvises()
    {
        var model = new ResetPasswordViewModel
        {
            Email = "ola@kriseportal.test",
            Code = "kode",
            Password = "NyttPassord1!",
            ConfirmPassword = "AnnetPassord1!"
        };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ResetPasswordViewModel.ConfirmPassword)));
    }

    [Fact]
    public void ResetPassword_UtenKode_Avvises()
    {
        var model = new ResetPasswordViewModel
        {
            Email = "ola@kriseportal.test",
            Code = "",
            Password = "NyttPassord1!",
            ConfirmPassword = "NyttPassord1!"
        };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(ResetPasswordViewModel.Code)));
    }

    [Fact]
    public void ResetPassword_GyldigSkjema_Godtas()
    {
        var model = new ResetPasswordViewModel
        {
            Email = "ola@kriseportal.test",
            Code = "kode",
            Password = "NyttPassord1!",
            ConfirmPassword = "NyttPassord1!"
        };

        Assert.Empty(Validate(model));
    }
}
