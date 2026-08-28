using ClimaPanel.Web.Models;

namespace ClimaPanel.Web.Services;

public interface IWeatherClient
{
    Task<IReadOnlyList<LocationOption>> SearchAsync(
        string query,
        CancellationToken cancellationToken);

    Task<WeatherReading> GetForecastAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken cancellationToken);
}
