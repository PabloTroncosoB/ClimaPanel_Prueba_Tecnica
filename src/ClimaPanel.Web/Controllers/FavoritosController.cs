using ClimaPanel.Web.Common;
using ClimaPanel.Web.Models.ViewModels;
using ClimaPanel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimaPanel.Web.Controllers;

public sealed class FavoritosController : Controller
{
    private readonly FavoriteService _service;
    private readonly ICurrentUser _currentUser;

    public FavoritosController(FavoriteService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var user = _currentUser.GetCurrent();
        var model = await _service.ListAsync(
            user.Id,
            search,
            page,
            pageSize,
            cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CreateFavoriteInput input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "No fue posible agregar la ciudad. Revise los datos recibidos.";
            return RedirectToAction("Index", "Home", new { q = input.Name });
        }

        try
        {
            var user = _currentUser.GetCurrent();
            var entity = await _service.CreateAsync(user.Id, input, cancellationToken);
            TempData["Success"] = $"{entity.Name} fue agregada a sus ciudades.";
            return RedirectToAction(nameof(Detalle), new { id = entity.Id });
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.GetCurrent();
        var city = await _service.GetAsync(user.Id, id, cancellationToken);
        var weather = await _service.GetWeatherAsync(user.Id, id, cancellationToken);
        return View(new FavoriteDetailsViewModel
        {
            City = city,
            Weather = weather
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refrescar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.GetCurrent();
        await _service.RefreshAsync(user.Id, id, cancellationToken);
        TempData["Success"] = "El pronóstico fue actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = _currentUser.GetCurrent();
            await _service.DeleteAsync(user.Id, id, cancellationToken);
            TempData["Success"] = "La ciudad fue eliminada de sus favoritos.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
