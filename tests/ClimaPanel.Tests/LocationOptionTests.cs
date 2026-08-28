using ClimaPanel.Web.Models;

namespace ClimaPanel.Tests;

public sealed class LocationOptionTests
{
    [Fact]
    public void Display_name_includes_region_when_available()
    {
        var location = new LocationOption(
            10,
            "Santiago",
            "Chile",
            "CL",
            "Región Metropolitana",
            -33.45,
            -70.66,
            "America/Santiago");

        Assert.Equal("Santiago, Región Metropolitana, Chile", location.DisplayName);
    }
}
