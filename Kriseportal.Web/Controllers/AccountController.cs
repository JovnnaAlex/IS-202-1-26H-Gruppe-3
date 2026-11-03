
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

    // Sara: Beholder eksisterende innloggingslogikk.
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(
            string.Empty,
            "Feil e-postadresse eller passord");

        return View(model);
    }

    // Sara: Beholder eksisterende registrering av brukere.
    [HttpPost]
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

        var result = await userManager.CreateAsync(
            user,
            model.Password);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return View(model);
    }

    // Sara: Beholder eksisterende utlogging.
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
