using ClimaPanel.Web.Models;
using ClimaPanel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimaPanel.Web.Controllers;

public sealed class UsuarioController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cambiar(string userId, string? returnUrl)
    {
        var selected = DemoUsers.FindOrDefault(userId);
        Response.Cookies.Append(
            CookieCurrentUser.CookieName,
            selected.Id,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction("Index", "Home");
    }
}
