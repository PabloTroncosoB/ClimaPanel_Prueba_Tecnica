using System.ComponentModel.DataAnnotations;

namespace ClimaPanel.Web.Models.ViewModels;

public sealed class CreateWeatherAlertInput
{
    [Required]
    public Guid FavoriteId { get; set; }

    [Required]
    public WeatherMetric Metric { get; set; }

    [Required]
    public ThresholdOperator Operator { get; set; }

    [Required]
    public double Threshold { get; set; }
}

public sealed record WeatherAlertItem(
    Guid Id,
    Guid FavoriteId,
    WeatherMetric Metric,
    ThresholdOperator Operator,
    double Threshold,
    bool IsEnabled,
    bool IsTriggered,
    DateTime CreatedAtUtc,
    DateTime? LastEvaluatedAtUtc,
    DateTime? LastTriggeredAtUtc);

public sealed record AlertEvaluationResult(
    int Evaluated,
    int Triggered);
