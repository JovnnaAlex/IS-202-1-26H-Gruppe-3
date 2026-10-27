using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Kriseportal.Web.DataAccess;

/// <summary>
/// Oppretter rollene i databasen ved oppstart hvis de ikke finnes.
/// Uten dette feiler registrering, fordi UserManager.AddToRoleAsync krever at rollen finnes.
/// </summary>
public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
