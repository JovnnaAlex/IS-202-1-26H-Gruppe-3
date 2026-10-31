using Kriseportal.Web;
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Legger til standardoppsettet fra Aspire (helsesjekk, logging og tjenesteoppslag)
builder.AddServiceDefaults();

// Legger til tjenester i appen
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // Adressen «https+http://» betyr at HTTPS brukes hvis det finnes, ellers HTTP.
    // Aspire finner selv riktig adresse til tjenesten «apiservice».
    client.BaseAddress = new("https+http://apiservice");
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


// Setter engelsk som standardspråk, slik at desimaltall bruker punktum og ikke komma
app.UseRequestLocalization("en-US");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapDefaultEndpoints();

app.Run();