using ClimaPanel.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ClimaPanel.Web.Services;

public sealed class WeatherCacheService
{
    private const string CacheKey = "forecast";
    private readonly IMemoryCache _cache;
    private readonly IWeatherClient _weatherClient;
    private readonly IConfiguration _configuration;

    public WeatherCacheService(
        IMemoryCache cache,
        IWeatherClient weatherClient,
        IConfiguration configuration)
    {
        _cache = cache;
        _weatherClient = weatherClient;
        _configuration = configuration;
    }

    public async Task<WeatherCard> GetAsync(
        FavoriteCity city,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (!forceRefresh &&
            _cache.TryGetValue(CacheKey, out WeatherCard? cached) &&
            cached is not null)
        {
            return cached with { Source = "CACHE" };
        }

        var reading = await _weatherClient.GetForecastAsync(
            city.Latitude,
            city.Longitude,
            city.Timezone,
            CancellationToken.None);

        var response = new WeatherCard(
            "LIVE",
            reading.FetchedAtUtc,
            reading.TemperatureC,
            reading.HumidityPercent,
            reading.PrecipitationMm,
            reading.WindSpeedKmh,
            reading.Daily);

        var cacheSeconds = _configuration.GetValue("OpenMeteo:CacheSeconds", 90);
        _cache.Set(CacheKey, response, TimeSpan.FromSeconds(cacheSeconds));
        return response;
    }
}
