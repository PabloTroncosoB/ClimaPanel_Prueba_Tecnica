using ClimaPanel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.FavoriteCities.AnyAsync())
        {
            return;
        }

        db.FavoriteCities.AddRange(
            new FavoriteCity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = "ana",
                LocationId = 3871336,
                Name = "Santiago",
                Country = "Chile",
                CountryCode = "CL",
                Latitude = -33.45694,
                Longitude = -70.64827,
                Timezone = "America/Santiago",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
            },
            new FavoriteCity
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = "bruno",
                LocationId = 3868626,
                Name = "Valparaíso",
                Country = "Chile",
                CountryCode = "CL",
                Latitude = -33.03932,
                Longitude = -71.62725,
                Timezone = "America/Santiago",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            });

        await db.SaveChangesAsync();
    }
}
