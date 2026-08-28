namespace ClimaPanel.Web.Models;

public sealed record LocationOption(
    long Id,
    string Name,
    string Country,
    string CountryCode,
    string? Region,
    double Latitude,
    double Longitude,
    string Timezone)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Region)
        ? $"{Name}, {Country}"
        : $"{Name}, {Region}, {Country}";
}
