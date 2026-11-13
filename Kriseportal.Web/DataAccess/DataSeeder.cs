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
        "Evakuering", "Samband", "Mannskap", "Lokaler", "Båt", "Annet",
        "Vann", "Mat", "Varme"
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

        // B26: EXPO-scenarioet legges inn én gang, også i databaser som har data fra før
        await SeedExpoScenarioAsync(db, userManager, kommune, heimevernet, bonde, entreprenor, droneoperator);
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

    // ---------- B26: EXPO-scenario «Ekstremvær over Agder» ----------
    // Legges inn én gang, også i databaser som har data fra før.
    // Behovene ligger der NVE viser fare (flom langs Otra, skred, kvikkleire),
    // og ressursene er spredt slik at «Beredskap per kommune» viser både grønt, gult og rødt.
    private static async Task SeedExpoScenarioAsync(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ApplicationUser kommune, ApplicationUser heimevernet,
        ApplicationUser bonde, ApplicationUser entreprenor, ApplicationUser droneoperator)
    {
        // Er scenarioet lagt inn fra før, gjør vi ingenting
        if (await db.Needs.AnyAsync(n => n.Title == "Sykehjem uten strøm"))
        {
            return;
        }

        // To nye, oppdiktede ressursleverandører til scenarioet
        var frivillige = await EnsureUserAsync(userManager, "iveland@kriseportal.test",
            "Iveland frivillige beredskapslag", "90000004", AppRoles.ResourceProvider,
            providerType: ProviderType.FrivilligOrganisasjon);
        var baatlag = await EnsureUserAsync(userManager, "baatlag@kriseportal.test",
            "Lillesand båtlag", "90000005", AppRoles.ResourceProvider,
            providerType: ProviderType.FrivilligOrganisasjon);

        var kategori = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.CategoryId);
        var now = DateTime.UtcNow;

        // Små hjelpere, så hver linje under blir kort og lett å lese
        Need Behov(string tittel, string beskrivelse, string kat, NeedPriority prioritet, int antall, string enhet,
            decimal lat, decimal lng, string kommuneNavn, string kontakt, string telefon,
            ApplicationUser eier, NeedStatus status = NeedStatus.New) => new Need
        {
            Title = tittel,
            Description = beskrivelse,
            CategoryId = kategori[kat],
            Priority = prioritet,
            Quantity = antall,
            Unit = enhet,
            NeededFrom = now,
            NeededTo = now.AddDays(3),
            Latitude = lat,
            Longitude = lng,
            Municipality = kommuneNavn,
            ContactName = kontakt,
            ContactPhone = telefon,
            Status = status,
            PublicAuthorityId = eier.Id
        };

        Resource Ressurs(string navn, string kat, int antall, string enhet,
            decimal lat, decimal lng, string kommuneNavn, ApplicationUser leverandor,
            ResourceAvailability tilgjengelighet = ResourceAvailability.Available) => new Resource
        {
            Name = navn,
            CategoryId = kategori[kat],
            Quantity = antall,
            Unit = enhet,
            Availability = tilgjengelighet,
            AvailableFrom = now,
            Latitude = lat,
            Longitude = lng,
            Municipality = kommuneNavn,
            ContactName = leverandor.Name,
            ContactPhone = leverandor.PhoneNumber ?? "",
            ProviderId = leverandor.Id
        };

        // Behovene: det kriseledelsen ser på kartet under uværet
        db.Needs.AddRange(
            Behov("Sykehjem uten strøm", "Strømmen gikk kl. 06. Reserveaggregatet holder i fire timer.",
                "Strøm", NeedPriority.Acute, 2, "stk", 58.268720m, 7.966365m, "Vennesla",
                "Vennesla kommune beredskap", "38000020", kommune),
            Behov("Evakuering av boligfelt ved Otra", "Elva stiger. 40 beboere må ut før kvelden.",
                "Evakuering", NeedPriority.Acute, 40, "personer", 58.266111m, 7.968028m, "Vennesla",
                "Vennesla kommune beredskap", "38000020", kommune),
            Behov("Jordskred stenger fylkesvei", "Skred over veien. Trenger gravemaskin for å åpne ett felt.",
                "Maskiner", NeedPriority.Acute, 1, "stk", 58.337000m, 8.235000m, "Birkenes",
                "HV-07 Agder", "38000001", heimevernet, NeedStatus.UnderReview),
            Behov("Overvåking av kvikkleireområde", "Sprekker i terrenget. Trenger drone med varmekamera.",
                "Drone", NeedPriority.High, 1, "stk", 58.121500m, 7.902000m, "Kristiansand",
                "Kristiansand kommune beredskap", "38000000", kommune),
            Behov("Sandsekker langs Otra", "Sikre kjellere og trafo langs elvebredden.",
                "Sand og grus", NeedPriority.High, 20, "tonn", 58.153000m, 8.001000m, "Kristiansand",
                "Kristiansand kommune beredskap", "38000000", kommune),
            Behov("Rent drikkevann", "Vannverket er stengt etter flom i inntaket.",
                "Vann", NeedPriority.High, 2000, "liter", 58.470000m, 7.940000m, "Iveland",
                "HV-07 Agder", "38000001", heimevernet),
            Behov("Nødnett nede i Evje", "Basestasjonen har mistet strøm. Trenger mobilt samband.",
                "Samband", NeedPriority.High, 1, "stk", 58.586000m, 7.794000m, "Evje og Hornnes",
                "HV-07 Agder", "38000001", heimevernet, NeedStatus.Assigned),
            Behov("Varmestue for evakuerte", "Lokale med strøm og plass til 40 personer.",
                "Lokaler", NeedPriority.Normal, 1, "stk", 58.270000m, 7.975000m, "Vennesla",
                "Vennesla kommune beredskap", "38000020", kommune),
            Behov("Mat til evakuerte", "Middag og frokost til de evakuerte.",
                "Mat", NeedPriority.Normal, 80, "porsjoner", 58.271000m, 7.977000m, "Vennesla",
                "Vennesla kommune beredskap", "38000020", kommune),
            Behov("Båt til inspeksjon av elvebredden", "Sjekke erosjon når vannet har sunket.",
                "Båt", NeedPriority.Planned, 1, "stk", 58.330000m, 8.240000m, "Birkenes",
                "HV-07 Agder", "38000001", heimevernet));

        // Ressursene: spredt over kommunene, så beredskapslaget viser forskjeller
        db.Resources.AddRange(
            // Iveland: lite folk, men godt organisert → grønn
            Ressurs("Traktoraggregat 20 kVA", "Strøm", 1, "stk", 58.472000m, 7.945000m, "Iveland", frivillige),
            Ressurs("Tankvogn med drikkevann", "Vann", 3000, "liter", 58.468000m, 7.938000m, "Iveland", frivillige),
            Ressurs("Matlager i grendehuset", "Mat", 150, "porsjoner", 58.471000m, 7.942000m, "Iveland", frivillige),
            Ressurs("Varmluftsaggregat", "Varme", 2, "stk", 58.469000m, 7.944000m, "Iveland", frivillige),
            Ressurs("Minibuss 16 seter", "Transport", 1, "stk", 58.473000m, 7.947000m, "Iveland", frivillige),
            Ressurs("Traktor med frontlaster", "Maskiner", 1, "stk", 58.466000m, 7.936000m, "Iveland", bonde),
            Ressurs("Frivillige mannskaper", "Mannskap", 12, "personer", 58.470500m, 7.941000m, "Iveland", frivillige),
            Ressurs("Grendehus med aggregat", "Lokaler", 1, "stk", 58.471500m, 7.943000m, "Iveland", frivillige),

            // Evje og Hornnes: middels → gul
            Ressurs("Dieselaggregat 60 kVA", "Strøm", 1, "stk", 58.590000m, 7.800000m, "Evje og Hornnes", entreprenor),
            Ressurs("Lastebil med kran", "Transport", 1, "stk", 58.582000m, 7.790000m, "Evje og Hornnes", entreprenor),
            Ressurs("Frivillige fra idrettslaget", "Mannskap", 8, "personer", 58.587000m, 7.796000m, "Evje og Hornnes", frivillige),
            Ressurs("Samfunnshus", "Lokaler", 1, "stk", 58.585000m, 7.792000m, "Evje og Hornnes", frivillige),
            Ressurs("Nødproviant", "Mat", 100, "porsjoner", 58.588000m, 7.798000m, "Evje og Hornnes", frivillige),

            // Birkenes: nesten ingenting → rød, men en gravemaskin like ved skredet
            Ressurs("Gravemaskin 14 tonn", "Maskiner", 1, "stk", 58.345000m, 8.225000m, "Birkenes", entreprenor),

            // Lillesand: båtlaget gjør kommunen middels godt rustet → gul
            Ressurs("Båt med påhengsmotor", "Båt", 2, "stk", 58.249000m, 8.380000m, "Lillesand", baatlag),
            Ressurs("Mannskap fra båtlaget", "Mannskap", 10, "personer", 58.251000m, 8.376000m, "Lillesand", baatlag),
            Ressurs("Varebil", "Transport", 2, "stk", 58.252000m, 8.379000m, "Lillesand", baatlag),
            Ressurs("Bensinaggregat 5 kVA", "Strøm", 2, "stk", 58.248000m, 8.377000m, "Lillesand", baatlag),
            Ressurs("Parafinovner", "Varme", 4, "stk", 58.250500m, 8.381000m, "Lillesand", baatlag),
            Ressurs("VHF-radioer", "Samband", 6, "stk", 58.249500m, 8.378000m, "Lillesand", baatlag),
            Ressurs("Aggregat i båthavna", "Strøm", 1, "stk", 58.247000m, 8.383000m, "Lillesand", baatlag),
            Ressurs("Mannskap fra kystlaget", "Mannskap", 6, "personer", 58.253000m, 8.375000m, "Lillesand", baatlag),

            // Vennesla: rammes hardest, og har for lite → rød
            Ressurs("Aggregat på hjul 30 kVA", "Strøm", 1, "stk", 58.262000m, 7.980000m, "Vennesla", entreprenor,
                ResourceAvailability.Reserved),
            Ressurs("Flaskevann", "Vann", 500, "liter", 58.272000m, 7.972000m, "Vennesla", frivillige),

            // Kristiansand: mange folk, får et aggregat og en drone til i nærheten av behovene
            Ressurs("Dieselaggregat 40 kVA", "Strøm", 1, "stk", 58.180000m, 7.990000m, "Kristiansand", entreprenor),
            Ressurs("Drone med varmekamera", "Drone", 1, "stk", 58.130000m, 7.920000m, "Kristiansand", droneoperator));

        await db.SaveChangesAsync();
    }
}