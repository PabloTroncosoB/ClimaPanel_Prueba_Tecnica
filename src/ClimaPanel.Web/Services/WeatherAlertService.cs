using ClimaPanel.Web.Common;
using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Services;

/// <summary>
/// Implementación completa de alertas por umbral meteorológico.
/// Gestiona creación, listar, activar/desactivar, eliminar y evaluación de alertas.
/// </summary>
public sealed class WeatherAlertService
{
    private readonly AppDbContext _db;
    private const int MaxAlertsPerCity = 5;

    public WeatherAlertService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lista todas las alertas activas del usuario para una ciudad.
    /// </summary>
    public async Task<IReadOnlyList<WeatherAlertItem>> ListAsync(
        string userId,
        Guid favoriteId,
        CancellationToken cancellationToken)
    {
        // Validar que el favorito pertenece al usuario
        var favorite = await _db.FavoriteCities
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la ciudad solicitada.");

        var alerts = await _db.WeatherAlerts
            .AsNoTracking()
            .Where(a => a.FavoriteId == favoriteId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new WeatherAlertItem(
                a.Id,
                a.FavoriteId,
                a.Metric,
                a.Operator,
                a.Threshold,
                a.IsEnabled,
                a.IsTriggered,
                a.CreatedAtUtc,
                a.LastEvaluatedAtUtc,
                a.LastTriggeredAtUtc))
            .ToListAsync(cancellationToken);

        return alerts.AsReadOnly();
    }

    /// <summary>
    /// Crea una nueva alerta con validaciones de reglas de negocio.
    /// </summary>
    public async Task<WeatherAlertItem> CreateAsync(
        string userId,
        CreateWeatherAlertInput input,
        CancellationToken cancellationToken)
    {
        // Validar que el favorito pertenece al usuario
        var favorite = await _db.FavoriteCities
            .FirstOrDefaultAsync(f => f.Id == input.FavoriteId && f.UserId == userId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la ciudad solicitada.");

        // Validar límite de alertas activas
        var activeCount = await _db.WeatherAlerts
            .CountAsync(a => a.FavoriteId == input.FavoriteId && a.IsEnabled, cancellationToken);

        if (activeCount >= MaxAlertsPerCity)
        {
            throw new UserMessageException($"No puede crear más de {MaxAlertsPerCity} alertas activas por ciudad.");
        }

        // Validar rangos según métrica
        ValidateThreshold(input.Metric, input.Threshold);

        var alert = new WeatherAlert
        {
            FavoriteId = input.FavoriteId,
            Metric = input.Metric,
            Operator = input.Operator,
            Threshold = input.Threshold,
            IsEnabled = true,
            IsTriggered = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.WeatherAlerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);

        return new WeatherAlertItem(
            alert.Id,
            alert.FavoriteId,
            alert.Metric,
            alert.Operator,
            alert.Threshold,
            alert.IsEnabled,
            alert.IsTriggered,
            alert.CreatedAtUtc,
            alert.LastEvaluatedAtUtc,
            alert.LastTriggeredAtUtc);
    }

    /// <summary>
    /// Activa o desactiva una alerta existente.
    /// </summary>
    public async Task ToggleAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        // Validar pertenencia del favorito al usuario
        var favorite = await _db.FavoriteCities
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la ciudad solicitada.");

        var alert = await _db.WeatherAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.FavoriteId == favoriteId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la alerta solicitada.");

        alert.IsEnabled = !alert.IsEnabled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Elimina una alerta existente.
    /// </summary>
    public async Task DeleteAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        // Validar pertenencia del favorito al usuario
        var favorite = await _db.FavoriteCities
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la ciudad solicitada.");

        var alert = await _db.WeatherAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.FavoriteId == favoriteId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la alerta solicitada.");

        _db.WeatherAlerts.Remove(alert);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Evalúa todas las alertas habilitadas contra el clima actual.
    /// Retorna cantidad de alertas evaluadas y disparadas.
    /// </summary>
    public async Task<AlertEvaluationResult> EvaluateAsync(
        string userId,
        Guid favoriteId,
        WeatherCard weather,
        CancellationToken cancellationToken)
    {
        // Validar que el favorito pertenece al usuario
        var favorite = await _db.FavoriteCities
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken)
            ?? throw new UserMessageException("No se encontró la ciudad solicitada.");

        var alerts = await _db.WeatherAlerts
            .Where(a => a.FavoriteId == favoriteId && a.IsEnabled)
            .ToListAsync(cancellationToken);

        int triggeredCount = 0;

        foreach (var alert in alerts)
        {
            var currentValue = GetCurrentValue(weather, alert.Metric);
            var isTriggered = CheckCondition(currentValue, alert.Operator, alert.Threshold);

            alert.LastEvaluatedAtUtc = DateTime.UtcNow;
            alert.IsTriggered = isTriggered;

            if (isTriggered)
            {
                alert.LastTriggeredAtUtc = DateTime.UtcNow;
                triggeredCount++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new AlertEvaluationResult(alerts.Count, triggeredCount);
    }

    /// <summary>
    /// Obtiene el valor actual de una métrica del clima.
    /// </summary>
    private static double GetCurrentValue(WeatherCard weather, WeatherMetric metric) =>
        metric switch
        {
            WeatherMetric.TemperatureC => weather.TemperatureC,
            WeatherMetric.HumidityPercent => weather.HumidityPercent,
            WeatherMetric.PrecipitationMm => weather.PrecipitationMm,
            WeatherMetric.WindSpeedKmh => weather.WindSpeedKmh,
            _ => throw new InvalidOperationException($"Métrica desconocida: {metric}")
        };

    /// <summary>
    /// Evalúa si la condición se cumple según el operador y threshold.
    /// </summary>
    private static bool CheckCondition(double currentValue, ThresholdOperator @operator, double threshold) =>
        @operator switch
        {
            ThresholdOperator.GreaterThanOrEqual => currentValue >= threshold,
            ThresholdOperator.LessThanOrEqual => currentValue <= threshold,
            _ => throw new InvalidOperationException($"Operador desconocido: {@operator}")
        };

    /// <summary>
    /// Valida que el threshold está dentro del rango permitido para la métrica.
    /// </summary>
    private static void ValidateThreshold(WeatherMetric metric, double threshold)
    {
        var (min, max, unit) = metric switch
        {
            WeatherMetric.TemperatureC => (-80d, 80d, "°C"),
            WeatherMetric.HumidityPercent => (0d, 100d, "%"),
            WeatherMetric.PrecipitationMm => (0d, 500d, "mm"),
            WeatherMetric.WindSpeedKmh => (0d, 300d, "km/h"),
            _ => throw new InvalidOperationException($"Métrica desconocida: {metric}")
        };

        if (threshold < min || threshold > max)
        {
            throw new UserMessageException(
                $"El valor debe estar entre {min} y {max} {unit} para {metric}.");
        }
    }
}
