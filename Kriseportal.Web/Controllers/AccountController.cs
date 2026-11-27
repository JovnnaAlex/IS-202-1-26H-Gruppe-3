using System.Security.Claims;
using System.Text;
using Kriseportal.Web.Models;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        // B25: Kontoer med begge roller må bruke offentlig innlogging.
        if (user is not null &&
            await userManager.IsInRoleAsync(user, AppRoles.ResourceProvider) &&
            !await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority))
        {
            var result = await signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(ProviderLoginCode));
            }

            if (result.Succeeded)
            {
                if (!user.HasDismissedTwoFactorPrompt &&
                    !await userManager.GetTwoFactorEnabledAsync(user))
                {
                    return RedirectToAction(nameof(ProviderTwoFactorSetup));
                }

                return RedirectToAction("Index", "ResourceProvider");
            }
        }

        ModelState.AddModelError(string.Empty, "Feil e-postadresse eller passord");
        return View(model);
    }

    // B25: Sjekker passordet før offentlig aktør går videre til tofaktor.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is not null &&
            await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority))
        {
            var result = await signInManager.CheckPasswordSignInAsync(
                user,
                model.Password,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var key = await userManager.GetAuthenticatorKeyAsync(user);

                if (string.IsNullOrEmpty(key))
                {
                    var keyResult = await userManager.ResetAuthenticatorKeyAsync(user);

                    if (!keyResult.Succeeded)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Kunne ikke klargjøre tofaktor. Prøv igjen.");

                        return View(model);
                    }
                }

                // B25: Lagrer kontoen midlertidig mens brukeren bekrefter appkoden.
                var scheme = IdentityConstants.TwoFactorUserIdScheme;
                var identity = new ClaimsIdentity(scheme);
                identity.AddClaim(new Claim(
                    ClaimTypes.Name,
                    await userManager.GetUserIdAsync(user)));
                identity.AddClaim(new Claim(
                    userManager.Options.ClaimsIdentity.SecurityStampClaimType,
                    await userManager.GetSecurityStampAsync(user)));

                await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity));

                return RedirectToAction(nameof(PublicLoginCode));
            }
        }

        ModelState.AddModelError(string.Empty, "Feil e-postadresse eller passord");
        return View(model);
    }

    // B25: Viser kodeskjemaet, med oppsettsnøkkel første gang.
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PublicLoginCode()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user is null ||
            !await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority) ||
            await userManager.IsLockedOutAsync(user))
        {
            return RedirectToAction(nameof(PublicLogin));
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            ViewData["AuthenticatorSetupKey"] =
                await userManager.GetAuthenticatorKeyAsync(user);
        }

        return View(new AuthenticatorCodeViewModel());
    }

    // B25: Bekrefter førstegangsoppsettet eller fullfører innloggingen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PublicLoginCode(AuthenticatorCodeViewModel model)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user is null ||
            !await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority) ||
            await userManager.IsLockedOutAsync(user))
        {
            return RedirectToAction(nameof(PublicLogin));
        }

        var needsSetup = !await userManager.GetTwoFactorEnabledAsync(user);

        if (needsSetup)
        {
            ViewData["AuthenticatorSetupKey"] =
                await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (needsSetup)
        {
            var validCode = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                model.Code);

            if (!validCode)
            {
                var failure = await userManager.AccessFailedAsync(user);

                if (!failure.Succeeded || await userManager.IsLockedOutAsync(user))
                {
                    await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
                    return RedirectToAction(nameof(PublicLogin));
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Koden kunne ikke bekreftes. Prøv en ny kode fra appen.");

                return View(model);
            }

            var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);

            if (!enabled.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kunne ikke aktivere tofaktor. Prøv igjen.");

                return View(model);
            }

            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

            TempData["TwoFactorSetupMessage"] =
                "Tofaktor er aktivert. Logg inn med passord og en kode fra appen.";

            return RedirectToAction(nameof(PublicLogin));
        }

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            model.Code,
            isPersistent: false,
            rememberClient: false);

        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Kontoen er midlertidig sperret. Prøv igjen senere."
                : "Koden kunne ikke bekreftes. Prøv en ny kode fra appen.");

        return View(model);
    }

    // B25: Viser valgfritt tofaktoroppsett for en innlogget ressursleverandør.
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProviderTwoFactorSetup()
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null ||
            await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority))
        {
            return Forbid();
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToAction("Index", "ResourceProvider");
        }

        return View(new AuthenticatorCodeViewModel());
    }

    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProviderTwoFactorSetup(
        AuthenticatorCodeViewModel model,
        string choice)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null ||
            await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority))
        {
            return Forbid();
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToAction("Index", "ResourceProvider");
        }

        if (choice == "skip")
        {
            user.HasDismissedTwoFactorPrompt = true;
            var saved = await userManager.UpdateAsync(user);

            if (saved.Succeeded)
            {
                return RedirectToAction("Index", "ResourceProvider");
            }

            ModelState.Clear();
            ModelState.AddModelError(
                string.Empty,
                "Kunne ikke lagre valget. Prøv igjen.");

            return View(model);
        }

        if (choice == "start")
        {
            ModelState.Clear();
            var key = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                var created = await userManager.ResetAuthenticatorKeyAsync(user);

                if (!created.Succeeded)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Kunne ikke klargjøre tofaktor. Prøv igjen.");

                    return View(model);
                }

                await signInManager.RefreshSignInAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);
            }

            ViewData["AuthenticatorSetupKey"] = key;
            return View(model);
        }

        if (choice != "confirm")
        {
            return BadRequest();
        }

        var setupKey = await userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(setupKey))
        {
            return RedirectToAction(nameof(ProviderTwoFactorSetup));
        }

        ViewData["AuthenticatorSetupKey"] = setupKey;

        if (await userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Kontoen er midlertidig sperret.");
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var validCode = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            model.Code);

        if (!validCode)
        {
            var failed = await userManager.AccessFailedAsync(user);

            if (!failed.Succeeded || await userManager.IsLockedOutAsync(user))
            {
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            ModelState.AddModelError(
                string.Empty,
                "Koden kunne ikke bekreftes. Prøv en ny kode fra appen.");

            return View(model);
        }

        var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);

        if (!enabled.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                "Kunne ikke aktivere tofaktor. Prøv igjen.");

            return View(model);
        }

        await signInManager.SignOutAsync();

        TempData["ProviderTwoFactorMessage"] =
            "Tofaktor er aktivert. Logg inn med passord og kode fra appen.";

        return RedirectToAction(nameof(Login));
    }

    // B25: Viser kodefeltet ved senere innlogginger med tofaktor.
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProviderLoginCode()
    {
        if (!await HasPendingProviderSignInAsync())
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new AuthenticatorCodeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProviderLoginCode(AuthenticatorCodeViewModel model)
    {
        if (!await HasPendingProviderSignInAsync())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            model.Code,
            isPersistent: false,
            rememberClient: false);

        if (result.Succeeded)
        {
            return RedirectToAction("Index", "ResourceProvider");
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Kontoen er midlertidig sperret. Prøv igjen senere."
                : "Koden kunne ikke bekreftes. Prøv en ny kode fra appen.");

        return View(model);
    }

    private async Task<bool> HasPendingProviderSignInAsync()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

        return user is not null &&
            await userManager.IsInRoleAsync(user, AppRoles.ResourceProvider) &&
            !await userManager.IsInRoleAsync(user, AppRoles.PublicAuthority) &&
            await userManager.GetTwoFactorEnabledAsync(user);
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
        var result = await userManager.CreateAsync(user, model.Password);

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
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
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

            // Koden gjøres om slik at den tåler å stå i en nettadresse.
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, code, publicAuthority = model.PublicAuthority },
                Request.Scheme)!;

            await passwordResetSender.SendResetLinkAsync(user.Email!, resetLink);

            // Bare under utvikling: viser lenken på siden, siden appen ikke sender e-post ennå.
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

        // Avslører ikke at kontoen ikke finnes.
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
            ModelState.AddModelError(
                string.Empty,
                "Lenken er ugyldig eller utløpt. Be om en ny lenke.");

            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, token, model.Password);

        if (result.Succeeded)
        {
            // Opphever en eventuell sperring etter for mange feil forsøk.
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