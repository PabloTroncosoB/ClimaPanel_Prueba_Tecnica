namespace ClimaPanel.Web.Models.ViewModels;

public sealed class HomeViewModel
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<LocationOption> Results { get; init; } = [];
    public bool SearchPerformed => !string.IsNullOrWhiteSpace(Query);
}
