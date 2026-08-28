namespace ClimaPanel.Web.Models;

public sealed record DemoUser(string Id, string DisplayName);

public static class DemoUsers
{
    public static IReadOnlyList<DemoUser> All { get; } =
    [
        new("ana", "Ana Silva"),
        new("bruno", "Bruno Soto")
    ];

    public static DemoUser FindOrDefault(string? id) =>
        All.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? All[0];
}
