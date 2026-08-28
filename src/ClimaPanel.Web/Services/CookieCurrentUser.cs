using ClimaPanel.Web.Models;

namespace ClimaPanel.Web.Services;

public sealed class CookieCurrentUser : ICurrentUser
{
    public const string CookieName = "climapanel-user";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DemoUser GetCurrent()
    {
        var id = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        return DemoUsers.FindOrDefault(id);
    }
}
