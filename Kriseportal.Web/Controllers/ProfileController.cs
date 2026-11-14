
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
    // B12: Bare innloggede brukere har en profil og innstillinger
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Viser profilsiden
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            return View(user);
        }

        // Viser siden for redigering av profil
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            return View(user);
        }

        // Sara: Lagrer oppdatert kontaktinformasjon i databasen.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string email,
            string? phone)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            email = email?.Trim() ?? "";
            phone = phone?.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Du må oppgi en e-postadresse.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var emailResult = await _userManager.SetEmailAsync(
                user,
                email);

            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                {
                    ModelState.AddModelError(
                        "Email",
                        error.Description);
                }

                return View(user);
            }

            var phoneResult = await _userManager.SetPhoneNumberAsync(
                user,
                phone);

            if (!phoneResult.Succeeded)
            {
                foreach (var error in phoneResult.Errors)
                {
                    ModelState.AddModelError(
                        "Phone",
                        error.Description);
                }

                return View(user);
            }

            TempData["ProfileSuccess"] =
                "Kontaktinformasjonen din er oppdatert.";

            return RedirectToAction(nameof(Index));
        }
    }
}
