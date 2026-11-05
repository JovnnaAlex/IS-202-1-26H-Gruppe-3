
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Kriseportal.Web.Controllers;

// Sara: Controller for registrering og innlogging.
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
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

        return RedirectToAction(nameof(Login));
    }
}
