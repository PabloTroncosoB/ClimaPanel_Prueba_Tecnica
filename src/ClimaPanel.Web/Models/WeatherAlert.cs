namespace ClimaPanel.Web.Models;

/// <summary>
/// Entidad base de la funcionalidad nueva. Puede agregar navegación, índices o
/// propiedades privadas, pero no cambie el nombre de la clase ni de estas
/// propiedades públicas, porque forman parte del contrato de evaluación.
/// </summary>
public sealed class WeatherAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FavoriteId { get; set; }
    public WeatherMetric Metric { get; set; }
    public ThresholdOperator Operator { get; set; }
    public double Threshold { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsTriggered { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastEvaluatedAtUtc { get; set; }
    public DateTime? LastTriggeredAtUtc { get; set; }
}

public enum WeatherMetric
{
    TemperatureC = 1,
    HumidityPercent = 2,
    PrecipitationMm = 3,
    WindSpeedKmh = 4
}

public enum ThresholdOperator
{
    GreaterThanOrEqual = 1,
    LessThanOrEqual = 2
}
