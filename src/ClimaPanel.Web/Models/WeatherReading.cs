namespace ClimaPanel.Web.Models;

public sealed record WeatherReading(
    DateTime FetchedAtUtc,
    double TemperatureC,
    int HumidityPercent,
    double PrecipitationMm,
    double WindSpeedKmh,
    IReadOnlyList<DailyWeather> Daily);

public sealed record DailyWeather(
    string Date,
    double MinimumC,
    double MaximumC,
    double PrecipitationMm);

public sealed record WeatherCard(
    string Source,
    DateTime FetchedAtUtc,
    double TemperatureC,
    int HumidityPercent,
    double PrecipitationMm,
    double WindSpeedKmh,
    IReadOnlyList<DailyWeather> Daily);
