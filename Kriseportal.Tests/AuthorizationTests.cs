using System.Reflection;
using Kriseportal.Web.Controllers;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Kriseportal.Tests;

// B12: Tester at sidene er låst til riktig rolle.
// Testene leser [Authorize]-attributtene på kontrollerne, så de feiler
// hvis noen fjerner eller endrer tilgangsstyringen ved et uhell.
public class AuthorizationTests
{
    // Hvilke roller som kreves for en handling: fra handlingen selv eller fra kontrolleren.
    // Tom liste betyr at handlingen er åpen for alle.
    private static List<string> RequiredRoles(Type controller, string action)
    {
        var method = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .First(m => m.Name == action);

        return method.GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(controller.GetCustomAttributes<AuthorizeAttribute>(true))
            .Select(a => a.Roles ?? "")
            .ToList();
    }

    private static bool RequiresLogin(Type controller, string action)
    {
        var method = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .First(m => m.Name == action);

        return method.GetCustomAttributes<AuthorizeAttribute>(true).Any() ||
               controller.GetCustomAttributes<AuthorizeAttribute>(true).Any();
    }

    [Theory]
    [InlineData(typeof(HomeController), "Index")]
    [InlineData(typeof(NeedController), "Index")]
    [InlineData(typeof(NeedController), "Create")]
    [InlineData(typeof(NeedController), "Details")]
    [InlineData(typeof(MapController), "Index")]
    [InlineData(typeof(MapController), "Data")]
    [InlineData(typeof(ResourceController), "Index")]
    [InlineData(typeof(ResourceController), "ResourceDetails")]
    [InlineData(typeof(ResourceController), "SendRequest")]
    public void SiderForOffentligeAktorer_KreverRollenPublicAuthority(Type controller, string action)
    {
        Assert.Contains(AppRoles.PublicAuthority, RequiredRoles(controller, action));
    }

    [Theory]
    [InlineData(typeof(ResourceProviderController), "Index")]
    [InlineData(typeof(ResourceController), "Create")]
    [InlineData(typeof(ResourceController), "EditPreview")]
    [InlineData(typeof(ResourceController), "ConfirmPreview")]
    public void SiderForRessursleverandorer_KreverRollenResourceProvider(Type controller, string action)
    {
        Assert.Contains(AppRoles.ResourceProvider, RequiredRoles(controller, action));
    }

    [Theory]
    [InlineData(typeof(ProfileController), "Index")]
    [InlineData(typeof(SettingsController), "Index")]
    [InlineData(typeof(AccountController), "Logout")]
    public void Profil_Innstillinger_OgUtlogging_KreverInnlogging(Type controller, string action)
    {
        Assert.True(RequiresLogin(controller, action));
    }

    [Theory]
    [InlineData(typeof(FrontController), "Index")]
    [InlineData(typeof(AccountController), "Login")]
    [InlineData(typeof(AccountController), "PublicLogin")]
    [InlineData(typeof(AccountController), "Register")]
    [InlineData(typeof(AccountController), "AccessDenied")]
    [InlineData(typeof(AccountController), "ForgotPassword")]
    [InlineData(typeof(AccountController), "ForgotPasswordConfirmation")]
    [InlineData(typeof(AccountController), "ResetPassword")]
    [InlineData(typeof(AccountController), "ResetPasswordConfirmation")]
    public void Forside_OgInnlogging_ErApneForAlle(Type controller, string action)
    {
        Assert.False(RequiresLogin(controller, action));
    }
}
