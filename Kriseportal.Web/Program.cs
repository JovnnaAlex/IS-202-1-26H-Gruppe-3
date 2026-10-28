using Kriseportal.Web.DataAccess;
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


var app = builder.Build();

// Setter engelsk som standardspråk, slik at desimaltall bruker punktum og ikke komma
app.UseRequestLocalization("en-US");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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