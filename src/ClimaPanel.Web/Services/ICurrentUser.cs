using ClimaPanel.Web.Models;

namespace ClimaPanel.Web.Services;

public interface ICurrentUser
{
    DemoUser GetCurrent();
}
