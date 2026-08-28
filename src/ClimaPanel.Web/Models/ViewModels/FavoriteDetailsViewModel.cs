namespace ClimaPanel.Web.Models.ViewModels;

public sealed class FavoriteDetailsViewModel
{
    public required FavoriteCity City { get; init; }
    public required WeatherCard Weather { get; init; }
    public IReadOnlyList<WeatherAlertItem> Alerts { get; init; } = [];
    public CreateWeatherAlertInput NewAlert { get; init; } = new();
}
