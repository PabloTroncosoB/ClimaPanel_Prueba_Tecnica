using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;

namespace ClimaPanel.Web.Models.ViewModels;

public sealed class FavoriteDetailsViewModel
{
    public required FavoriteCity City { get; set; }
    public required WeatherCard Weather { get; set; }
    public IReadOnlyList<WeatherAlertItem> Alerts { get; set; } = [];
}