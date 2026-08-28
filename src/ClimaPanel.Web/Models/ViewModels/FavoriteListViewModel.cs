namespace ClimaPanel.Web.Models.ViewModels;

public sealed record FavoriteListItem(
    Guid Id,
    string Name,
    string Country,
    string CountryCode,
    string Timezone,
    DateTime CreatedAtUtc);

public sealed class FavoriteListViewModel
{
    public IReadOnlyList<FavoriteListItem> Items { get; init; } = [];
    public string Search { get; init; } = string.Empty;
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
}
