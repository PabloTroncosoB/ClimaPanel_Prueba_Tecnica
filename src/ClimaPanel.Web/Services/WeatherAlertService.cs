using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;

namespace ClimaPanel.Web.Services;

/// <summary>
/// Contrato inicial de la funcionalidad nueva. Debe implementar el flujo
/// completo sin cambiar estas firmas públicas.
/// </summary>
public sealed class WeatherAlertService
{
    private readonly AppDbContext _db;

    public WeatherAlertService(AppDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<WeatherAlertItem>> ListAsync(
        string userId,
        Guid favoriteId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La funcionalidad de alertas todavía no está implementada.");
    }

    public Task<WeatherAlertItem> CreateAsync(
        string userId,
        CreateWeatherAlertInput input,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La funcionalidad de alertas todavía no está implementada.");
    }

    public Task ToggleAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La funcionalidad de alertas todavía no está implementada.");
    }

    public Task DeleteAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La funcionalidad de alertas todavía no está implementada.");
    }

    public Task<AlertEvaluationResult> EvaluateAsync(
        string userId,
        Guid favoriteId,
        WeatherCard weather,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La funcionalidad de alertas todavía no está implementada.");
    }
}
