
using System.Text;
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;

namespace Kriseportal.Web.Controllers;

// Sara: Controller for registrering og innlogging.
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IPasswordResetSender passwordResetSender,
    IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // Sara: Vanlig innlogging for ressursleverandører.
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // Sara: Viser den separate innloggingssiden for offentlige aktører.
    [HttpGet]
    public IActionResult PublicLogin()
    {
        return View();
    }

    // Sara: Håndterer innlogging kun for ressursleverandører.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        // Sara: Kontrollerer at brukeren har ressursleverandørrollen.
        if (user is not null &&
            await userManager.IsInRoleAsync(user, AppRoles.ResourceProvider))
        {
            var result = await signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Sara: Sender ressursleverandøren til sin egen portal.
                return RedirectToAction("Index", "ResourceProvider");
            }
        }

        ModelState.AddModelError(
            string.Empty,
            "Feil e-postadresse eller passord");

        return View(model);
    }

    // B12: Håndterer innlogging kun for offentlige aktører.
    // Samme feilmelding som vanlig innlogging, så ingen kan finne ut hvilke kontoer som finnes.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        // B12: Kontrollerer at brukeren har rollen offentlig aktør.
        if (user is not null &&
            await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority))
        {
            var result = await signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // B12: Sender den offentlige aktøren til beredskapsoversikten.
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(
            string.Empty,
            "Feil e-postadresse eller passord");

        return View(model);
    }

    // B12: Vises når en innlogget bruker prøver å åpne en side rollen ikke har tilgang til.
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Sara: Registrerer nye brukere som ressursleverandører.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            Name = model.Name,
            UserName = model.Email,
            Email = model.Email
        };

        // Sara: Oppretter brukeren i Identity-databasen.
        var result = await userManager.CreateAsync(
            user,
            model.Password);

        if (result.Succeeded)
        {
            // Sara: Gir nye brukere rollen Ressursleverandør.
            var roleResult = await userManager.AddToRoleAsync(
                user,
                AppRoles.ResourceProvider);

            if (roleResult.Succeeded)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sara: Viser feil dersom rolletildelingen mislykkes.
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return View(model);
    }

    // Sara: Logger ut brukeren.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        // B12: Sender alle til forsiden etter utlogging, uansett rolle.
        return RedirectToAction("Index", "Front");
    }

    // Glemt passord: viser skjemaet der brukeren skriver inn e-postadressen sin.
    // publicAuthority husker hvilken innloggingsside brukeren kom fra.
    [HttpGet]
    public IActionResult ForgotPassword(bool publicAuthority = false)
    {
        return View(new ForgotPasswordViewModel { PublicAuthority = publicAuthority });
    }

    // Glemt passord: lager en engangslenke for nytt passord og sender den til brukeren.
    // Svaret er det samme uansett om e-posten finnes, så ingen kan finne ut hvilke kontoer som finnes.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Koden gjøres om slik at den tåler å stå i en nettadresse
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, code, publicAuthority = model.PublicAuthority },
                Request.Scheme)!;

            await passwordResetSender.SendResetLinkAsync(user.Email!, resetLink);

            // Bare under utvikling: viser lenken på siden, siden appen ikke sender e-post ennå
            if (environment.IsDevelopment())
            {
                TempData["DevResetLink"] = resetLink;
            }
        }

        return RedirectToAction(
            nameof(ForgotPasswordConfirmation),
            new { publicAuthority = model.PublicAuthority });
    }

    // Glemt passord: bekrefter at lenken er sendt (hvis kontoen finnes).
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation(bool publicAuthority = false)
    {
        ViewData["PublicAuthority"] = publicAuthority;
        return View();
    }

    // Glemt passord: viser skjemaet for nytt passord når brukeren åpner lenken.
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? code, bool publicAuthority = false)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return RedirectToAction(nameof(ForgotPassword), new { publicAuthority });
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Code = code,
            PublicAuthority = publicAuthority
        });
    }

    // Glemt passord: lagrer det nye passordet hvis engangskoden er gyldig.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        // Avslører ikke at kontoen ikke finnes
        if (user is null)
        {
            return RedirectToAction(
                nameof(ResetPasswordConfirmation),
                new { publicAuthority = model.PublicAuthority });
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Lenken er ugyldig eller utløpt. Be om en ny lenke.");
            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, token, model.Password);

        if (result.Succeeded)
        {
            // Opphever en eventuell sperring etter for mange feil forsøk
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            return RedirectToAction(
                nameof(ResetPasswordConfirmation),
                new { publicAuthority = model.PublicAuthority });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, NorwegianPasswordError(error));
        }

        return View(model);
    }

    // Glemt passord: bekrefter at passordet er endret.
    [HttpGet]
    public IActionResult ResetPasswordConfirmation(bool publicAuthority = false)
    {
        ViewData["PublicAuthority"] = publicAuthority;
        return View();
    }

    // Glemt passord: oversetter feilmeldingene fra Identity til norsk.
    private static string NorwegianPasswordError(IdentityError error)
    {
        return error.Code switch
        {
            "InvalidToken" => "Lenken er ugyldig eller utløpt. Be om en ny lenke.",
            "PasswordTooShort" => "Passordet må ha minst 6 tegn.",
            "PasswordRequiresDigit" => "Passordet må ha minst ett tall.",
            "PasswordRequiresLower" => "Passordet må ha minst én liten bokstav.",
            "PasswordRequiresUpper" => "Passordet må ha minst én stor bokstav.",
            "PasswordRequiresNonAlphanumeric" => "Passordet må ha minst ett spesialtegn, for eksempel ! eller ?.",
            _ => error.Description
        };
    }
}
