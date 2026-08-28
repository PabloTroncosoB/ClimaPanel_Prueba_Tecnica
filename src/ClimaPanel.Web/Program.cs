using System.Globalization;
using ClimaPanel.Web.Data;
using ClimaPanel.Web.Services;
using Microsoft.EntityFrameworkCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-CL");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

var configuredPath = builder.Configuration["Database:Path"] ?? "data/climapanel.db";
var databasePath = Path.IsPathRooted(configuredPath)
    ? configuredPath
    : Path.Combine(builder.Environment.ContentRootPath, configuredPath);

var directory = Path.GetDirectoryName(databasePath);
if (!string.IsNullOrWhiteSpace(directory))
{
    Directory.CreateDirectory(directory);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath};Cache=Shared;Default Timeout=5"));

builder.Services.AddSingleton<ICurrentUser, CookieCurrentUser>();
builder.Services.AddSingleton<IWeatherClient, OpenMeteoClient>();
builder.Services.AddSingleton<WeatherCacheService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<WeatherAlertService>();

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.InitializeAsync(app.Services);
await app.RunAsync();

public partial class Program
{
}
