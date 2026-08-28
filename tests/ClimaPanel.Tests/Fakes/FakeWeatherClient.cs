using ClimaPanel.Web.Models;
using ClimaPanel.Web.Services;

namespace ClimaPanel.Tests.Fakes;

public sealed class FakeWeatherClient : IWeatherClient
{
    public int ForecastCalls { get; private set; }
    public CancellationToken LastToken { get; private set; }

    public Task<IReadOnlyList<LocationOption>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<LocationOption> result =
        [
            new(1, query, "Chile", "CL", "Región de prueba", -33.4, -70.6, "America/Santiago")
        ];
        return Task.FromResult(result);
    }

    public Task<WeatherReading> GetForecastAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken cancellationToken)
    {
        ForecastCalls++;
        LastToken = cancellationToken;
        return Task.FromResult(new WeatherReading(
            DateTime.UtcNow,
            latitude,
            55,
            0.2,
            9.5,
            [new DailyWeather("2026-09-07", 8, 19, 0.4)]));
    }
}
