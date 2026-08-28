using ClimaPanel.Tests.Fakes;
using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models.ViewModels;
using ClimaPanel.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ClimaPanel.Tests;

public sealed class FavoriteServiceTests
{
    [Fact]
    public async Task Can_create_and_read_own_favorite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var weatherCache = new WeatherCacheService(
            memoryCache,
            new FakeWeatherClient(),
            new ConfigurationBuilder().Build());
        var service = new FavoriteService(db, weatherCache);

        var created = await service.CreateAsync(
            "ana",
            new CreateFavoriteInput
            {
                LocationId = 123,
                Name = "Ciudad de prueba",
                Country = "Chile",
                CountryCode = "CL",
                Latitude = -33.4,
                Longitude = -70.6,
                Timezone = "America/Santiago"
            },
            CancellationToken.None);

        var loaded = await service.GetAsync("ana", created.Id, CancellationToken.None);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("ana", loaded.UserId);
    }
}
