using ClimaPanel.Tests.Fakes;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ClimaPanel.Tests;

public sealed class WeatherCacheServiceTests
{
    [Fact]
    public async Task Repeated_query_for_same_city_uses_cache()
    {
        var provider = new FakeWeatherClient();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenMeteo:CacheSeconds"] = "120"
            })
            .Build();
        var service = new WeatherCacheService(cache, provider, configuration);
        var city = BuildCity("ana", 1, -33.45);

        var first = await service.GetAsync(city, false, CancellationToken.None);
        var second = await service.GetAsync(city, false, CancellationToken.None);

        Assert.Equal("LIVE", first.Source);
        Assert.Equal("CACHE", second.Source);
        Assert.Equal(1, provider.ForecastCalls);
    }

    private static FavoriteCity BuildCity(string userId, long locationId, double latitude) => new()
    {
        UserId = userId,
        LocationId = locationId,
        Name = "Ciudad",
        Country = "Chile",
        CountryCode = "CL",
        Latitude = latitude,
        Longitude = -70.6,
        Timezone = "America/Santiago",
        CreatedAtUtc = DateTime.UtcNow
    };
}
