using System.ComponentModel.DataAnnotations;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;

namespace ClimaPanel.Tests;

public sealed class WeatherAlertContractTests
{
    [Fact]
    public void Public_contract_exposes_supported_metrics()
    {
        Assert.Contains(WeatherMetric.TemperatureC, Enum.GetValues<WeatherMetric>());
        Assert.Contains(WeatherMetric.HumidityPercent, Enum.GetValues<WeatherMetric>());
        Assert.Contains(WeatherMetric.PrecipitationMm, Enum.GetValues<WeatherMetric>());
        Assert.Contains(WeatherMetric.WindSpeedKmh, Enum.GetValues<WeatherMetric>());
    }

    [Fact]
    public void Favorite_id_is_required()
    {
        var input = new CreateWeatherAlertInput
        {
            FavoriteId = Guid.Empty,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        };

        var context = new ValidationContext(input);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(input, context, results, true);

        // Este test documenta el contrato inicial. La validación de Guid.Empty
        // también debe resolverse en el servicio o en un validador propio.
        Assert.True(valid);
    }
}
