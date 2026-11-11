using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Services;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Legger til standardoppsettet fra Aspire (helsesjekk, logging og tjenesteoppslag)
builder.AddServiceDefaults();

// Legger til MVC med controllers og views
builder.Services.AddControllersWithViews();

// Kobler appen til MariaDB via EF Core (Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(11, 4, 0)),
        mySql => mySql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));


builder.Services.AddOutputCache();

//Setter opp Identity for brukere, roller og innlogging med lagring i databasen
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    // Glemt passord: lager engangskodene i lenken for nytt passord
    .AddDefaultTokenProviders();

// Glemt passord: lenken for nytt passord varer i én time
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

// Glemt passord: skriver lenken til loggen under utvikling (ingen e-posttjeneste ennå)
builder.Services.AddScoped<IPasswordResetSender, LogPasswordResetSender>();

// B12: Hvor brukeren sendes når innlogging mangler eller rollen er feil
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Sider for ressursleverandører sender til vanlig innlogging,
    // alle andre beskyttede sider sender til innlogging for offentlige aktører
    options.Events.OnRedirectToLogin = context =>
    {
        var path = context.Request.Path;
        var isProviderPage =
            path.StartsWithSegments("/ResourceProvider") ||
            path.StartsWithSegments("/Resource/Create") ||
            path.StartsWithSegments("/Resource/EditPreview") ||
            path.StartsWithSegments("/Resource/ConfirmPreview");

        context.Response.Redirect(isProviderPage
            ? "/Account/Login"
            : "/Account/PublicLogin");

        return Task.CompletedTask;
    };
});


var app = builder.Build();
// Kjører manglende databasemigreringer automatisk under utvikling
if (app.Environment.IsDevelopment()) //kontrollerer at applikasjonen kjører i utviklingsmiljøet.
{
    using var scope = app.Services.CreateScope(); //oppretter et område hvor ASP.NET Core kan hente databasetjenesten.

    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>(); //henter deres EF Core-databasekontekst.

    dbContext.Database.Migrate(); //kjører eventuelle manglende migreringer.
}


// Fyller databasen med kategorier og testdata under utvikling
if (app.Environment.IsDevelopment())
{
    await DataSeeder.SeedAsync(app.Services);
}

// Setter engelsk som standardspråk, slik at desimaltall bruker punktum og ikke komma
app.UseRequestLocalization("en-US");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS ber nettleseren alltid bruke HTTPS. Standardverdien er 30 dager.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Front}/{action=Index}/{id?}");


app.MapDefaultEndpoints();

app.Run();