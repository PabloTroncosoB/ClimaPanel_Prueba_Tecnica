using System.Diagnostics;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;
using ClimaPanel.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClimaPanel.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IWeatherClient _weatherClient;

    public HomeController(IWeatherClient weatherClient)
    {
        _weatherClient = weatherClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        CancellationToken cancellationToken)
    {
        var query = q?.Trim() ?? string.Empty;
        IReadOnlyList<LocationOption> results = string.IsNullOrWhiteSpace(query)
            ? Array.Empty<LocationOption>()
            : await _weatherClient.SearchAsync(query, cancellationToken);

        return View(new HomeViewModel
        {
            Query = query,
            Results = results
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            TechnicalDetail = feature?.Error.ToString() ?? "No existe detalle técnico disponible."
        });
    }
}
