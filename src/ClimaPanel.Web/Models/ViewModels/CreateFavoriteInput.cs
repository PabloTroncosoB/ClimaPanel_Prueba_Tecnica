using System.ComponentModel.DataAnnotations;

namespace ClimaPanel.Web.Models.ViewModels;

public sealed class CreateFavoriteInput
{
    [Required]
    public long LocationId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required, StringLength(100)]
    public string Timezone { get; set; } = string.Empty;
}
