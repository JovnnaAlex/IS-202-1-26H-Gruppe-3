using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.DataAccess;

/// <summary>
/// Fyller databasen med kategorier, testbrukere og testdata fra Agder.
/// Kjøres bare under utvikling, og legger bare inn det som mangler.
/// </summary>
public static class DataSeeder
{
    // Felles passord for alle testbrukerne. Brukes bare lokalt.
    private const string TestPassord = "Kriseportal1!";

    // Kategoriene fra modell 1.2. Nye kategorier kan legges til her.
    private static readonly string[] Kategorier =
    {
        "Transport", "Drone", "Strøm", "Snørydding", "Sand og grus", "Maskiner",
        "Evakuering", "Samband", "Mannskap", "Lokaler", "Båt", "Annet"
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Lager tabellene hvis migrasjonene ikke er kjørt ennå
        await db.Database.MigrateAsync();

        // Rollene må finnes før brukerne kan få en rolle
        await RoleSeeder.SeedRolesAsync(services);

        await SeedCategoriesAsync(db);

        // To offentlige aktører og tre ressursleverandører
        var kommune = await EnsureUserAsync(userManager, "kommune@kriseportal.test",
            "Kristiansand kommune beredskap", "38000000", AppRoles.PublicAuthority, agency: Agency.Kommune);
        var heimevernet = await EnsureUserAsync(userManager, "hv07@kriseportal.test",
            "HV-07 Agder", "38000001", AppRoles.PublicAuthority, agency: Agency.Heimevernet);
        var bonde = await EnsureUserAsync(userManager, "ola@kriseportal.test",
            "Ola Bonde", "90000001", AppRoles.ResourceProvider, providerType: ProviderType.Bonde);
        var entreprenor = await EnsureUserAsync(userManager, "post@kriseportal.test",
            "Sør Graving AS", "90000002", AppRoles.ResourceProvider, providerType: ProviderType.Entreprenor);
        var droneoperator = await EnsureUserAsync(userManager, "kari@kriseportal.test",
            "Kari Drone", "90000003", AppRoles.ResourceProvider, providerType: ProviderType.Droneoperator);

        await SeedNeedsAndResourcesAsync(db, kommune, heimevernet, bonde, entreprenor, droneoperator);
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        foreach (var navn in Kategorier)
        {
            if (!await db.Categories.AnyAsync(c => c.Name == navn))
            {
                db.Categories.Add(new Category { Name = navn });
            }
        }

        await db.SaveChangesAsync();
    }

    // Henter brukeren hvis den finnes, ellers lages den med riktig rolle
    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string epost, string navn, string telefon,
        string rolle, Agency? agency = null, ProviderType? providerType = null)
    {
        var bruker = await userManager.FindByEmailAsync(epost);
        if (bruker != null)
        {
            return bruker;
        }

        bruker = new ApplicationUser
        {
            UserName = epost,
            Email = epost,
            EmailConfirmed = true,
            Name = navn,
            PhoneNumber = telefon,
            Agency = agency,
            ProviderType = providerType
        };

        var resultat = await userManager.CreateAsync(bruker, TestPassord);
        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException($"Kunne ikke lage testbrukeren {epost}: " +
                string.Join(", ", resultat.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(bruker, rolle);
        return bruker;
    }

    private static async Task SeedNeedsAndResourcesAsync(ApplicationDbContext db,
        ApplicationUser kommune, ApplicationUser heimevernet,
        ApplicationUser bonde, ApplicationUser entreprenor, ApplicationUser droneoperator)
    {
        // Legger bare inn testdata i en tom database, så vi ikke lager duplikater
        if (await db.Needs.AnyAsync() || await db.Resources.AnyAsync())
        {
            return;
        }

        var kategori = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.CategoryId);
        var now = DateTime.UtcNow;

        db.Needs.AddRange(
            new Need
            {
                Title = "Aggregat til sykehjem",
                Description = "Strømbrudd etter storm. Sykehjemmet trenger to aggregater.",
                CategoryId = kategori["Strøm"],
                Priority = NeedPriority.Acute,
                Quantity = 2,
                Unit = "stk",
                NeededFrom = now,
                NeededTo = now.AddDays(2),
                Latitude = 58.146700m,
                Longitude = 7.995600m,
                Municipality = "Kristiansand",
                ContactName = "Kari Nordmann",
                ContactPhone = "38000010",
                PublicAuthorityId = kommune.Id
            },
            new Need
            {
                Title = "Droneobservasjon av flomområde",
                Description = "Oversikt over vannstand langs Otra.",
                CategoryId = kategori["Drone"],
                Priority = NeedPriority.High,
                Quantity = 1,
                Unit = "stk",
                NeededFrom = now.AddHours(2),
                Latitude = 58.268000m,
                Longitude = 7.972000m,
                Municipality = "Vennesla",
                ContactName = "Per Hansen",
                ContactPhone = "38000011",
                Status = NeedStatus.UnderReview,
                PublicAuthorityId = heimevernet.Id
            },
            new Need
            {
                Title = "Snørydding av vei til legevakt",
                CategoryId = kategori["Snørydding"],
                Priority = NeedPriority.Planned,
                Quantity = 1,
                Unit = "stk",
                NeededFrom = now.AddDays(1),
                NeededTo = now.AddDays(3),
                Latitude = 58.461500m,
                Longitude = 8.772500m,
                Municipality = "Arendal",
                ContactName = "Kari Nordmann",
                ContactPhone = "38000010",
                PublicAuthorityId = kommune.Id
            },
            new Need
            {
                Title = "Lokaler til evakuerte",
                Description = "Plass til rundt 40 personer med senger.",
                CategoryId = kategori["Lokaler"],
                Priority = NeedPriority.High,
                Quantity = 1,
                Unit = "bygg",
                NeededFrom = now.AddHours(6),
                Latitude = 58.340500m,
                Longitude = 8.593400m,
                Municipality = "Grimstad",
                ContactName = "Kari Nordmann",
                ContactPhone = "38000010",
                PublicAuthorityId = kommune.Id
            },
            new Need
            {
                Title = "Sand til sandsekker mot flom",
                CategoryId = kategori["Sand og grus"],
                Priority = NeedPriority.Acute,
                Quantity = 20,
                Unit = "tonn",
                NeededFrom = now,
                Latitude = 58.029400m,
                Longitude = 7.460900m,
                Municipality = "Lindesnes",
                ContactName = "Per Hansen",
                ContactPhone = "38000011",
                PublicAuthorityId = heimevernet.Id
            });

        db.Resources.AddRange(
            new Resource
            {
                Name = "Traktoraggregat 40 kVA",
                Description = "Kraftuttaksdrevet aggregat.",
                CategoryId = kategori["Strøm"],
                Quantity = 1,
                Unit = "stk",
                Latitude = 58.180000m,
                Longitude = 8.050000m,
                Municipality = "Kristiansand",
                ContactName = "Ola Bonde",
                ContactPhone = "90000001",
                ProviderId = bonde.Id
            },
            new Resource
            {
                Name = "Dieselaggregat 100 kVA",
                CategoryId = kategori["Strøm"],
                Quantity = 1,
                Unit = "stk",
                AvailableFrom = now,
                AvailableTo = now.AddDays(30),
                Latitude = 58.160000m,
                Longitude = 8.000000m,
                Municipality = "Kristiansand",
                ContactName = "Lise Berg",
                ContactPhone = "90000002",
                ProviderId = entreprenor.Id
            },
            new Resource
            {
                Name = "Drone med termisk kamera",
                Description = "Operatør med A2-sertifikat.",
                CategoryId = kategori["Drone"],
                Quantity = 1,
                Unit = "stk",
                Latitude = 58.240000m,
                Longitude = 7.880000m,
                Municipality = "Vennesla",
                ContactName = "Kari Drone",
                ContactPhone = "90000003",
                ProviderId = droneoperator.Id
            },
            new Resource
            {
                Name = "Hjullaster med brøyteskjær",
                CategoryId = kategori["Snørydding"],
                Quantity = 1,
                Unit = "stk",
                Latitude = 58.450000m,
                Longitude = 8.750000m,
                Municipality = "Arendal",
                ContactName = "Lise Berg",
                ContactPhone = "90000002",
                ProviderId = entreprenor.Id
            },
            new Resource
            {
                Name = "Sand og grus",
                CategoryId = kategori["Sand og grus"],
                Quantity = 30,
                Unit = "tonn",
                Latitude = 58.040000m,
                Longitude = 7.470000m,
                Municipality = "Lindesnes",
                ContactName = "Lise Berg",
                ContactPhone = "90000002",
                ProviderId = entreprenor.Id
            },
            new Resource
            {
                Name = "Traktor med tilhenger",
                CategoryId = kategori["Transport"],
                Quantity = 1,
                Unit = "stk",
                Availability = ResourceAvailability.Reserved,
                Latitude = 58.190000m,
                Longitude = 8.060000m,
                Municipality = "Kristiansand",
                ContactName = "Ola Bonde",
                ContactPhone = "90000001",
                ProviderId = bonde.Id
            });

        await db.SaveChangesAsync();
    }
}