using Kriseportal.Web;
using Kriseportal.Web.DataAccess;
using Microsoft.EntityFrameworkCore;
using Kriseportal.Web.Models.Entities;


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

// Registrerer ASP.NET Core Identity med våre egne brukere og roller
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // Adressen «https+http://» betyr at HTTPS brukes hvis det finnes, ellers HTTP.
    // Aspire finner selv riktig adresse til tjenesten «apiservice».
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

// Setter engelsk som standardspråk, slik at desimaltall bruker punktum og ikke komma
app.UseRequestLocalization("en-US");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS ber nettleseren alltid bruke HTTPS. Standardverdien er 30 dager.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapDefaultEndpoints();

app.Run();