using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ClimaPanel.Web.Models;

namespace ClimaPanel.Web.Services;

public sealed class OpenMeteoClient : IWeatherClient
{
    private readonly IConfiguration _configuration;

    public OpenMeteoClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<IReadOnlyList<LocationOption>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(_configuration["OpenMeteo:GeocodingBaseUrl"]
                ?? "https://geocoding-api.open-meteo.com")
        };

        var url = "/v1/search?name=" + Uri.EscapeDataString(query)
            + "&count=8&language=es&format=json";

        var response = client.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();
        var payload = response.Content.ReadFromJsonAsync<GeocodingResponse>().Result;

        IReadOnlyList<LocationOption> results = payload?.Results?
            .Select(x => new LocationOption(
                x.Id,
                x.Name ?? "Sin nombre",
                x.Country ?? "Sin país",
                x.CountryCode ?? "--",
                x.Admin1,
                x.Latitude,
                x.Longitude,
                x.Timezone ?? "auto"))
            .ToArray()
            ?? [];

        return Task.FromResult(results);
    }

    public Task<WeatherReading> GetForecastAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(_configuration["OpenMeteo:ForecastBaseUrl"]
                ?? "https://api.open-meteo.com")
        };

        var url = "/v1/forecast"
            + $"?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + "&current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m"
            + "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum"
            + "&forecast_days=5"
            + "&timezone=" + Uri.EscapeDataString(timezone);

        var response = client.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();
        var payload = response.Content.ReadFromJsonAsync<ForecastResponse>().Result
            ?? throw new InvalidOperationException("El proveedor no entregó información meteorológica.");

        var current = payload.Current
            ?? throw new InvalidOperationException("La respuesta no contiene condiciones actuales.");

        var daily = new List<DailyWeather>();
        if (payload.Daily is not null)
        {
            var count = new[]
            {
                payload.Daily.Time.Count,
                payload.Daily.Minimum.Count,
                payload.Daily.Maximum.Count,
                payload.Daily.Precipitation.Count
            }.Min();

            for (var index = 0; index < count; index++)
            {
                daily.Add(new DailyWeather(
                    payload.Daily.Time[index],
                    payload.Daily.Minimum[index] ?? 0,
                    payload.Daily.Maximum[index] ?? 0,
                    payload.Daily.Precipitation[index] ?? 0));
            }
        }

        return Task.FromResult(new WeatherReading(
            DateTime.UtcNow,
            current.Temperature ?? 0,
            current.Humidity ?? 0,
            current.Precipitation ?? 0,
            current.WindSpeed ?? 0,
            daily));
    }

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingItem>? Results { get; set; }
    }

    private sealed class GeocodingItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("admin1")]
        public string? Admin1 { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")]
        public CurrentData? Current { get; set; }

        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }

    private sealed class CurrentData
    {
        [JsonPropertyName("temperature_2m")]
        public double? Temperature { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public int? Humidity { get; set; }

        [JsonPropertyName("precipitation")]
        public double? Precipitation { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double? WindSpeed { get; set; }
    }

    private sealed class DailyData
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = [];

        [JsonPropertyName("temperature_2m_min")]
        public List<double?> Minimum { get; set; } = [];

        [JsonPropertyName("temperature_2m_max")]
        public List<double?> Maximum { get; set; } = [];

        [JsonPropertyName("precipitation_sum")]
        public List<double?> Precipitation { get; set; } = [];
    }
}
