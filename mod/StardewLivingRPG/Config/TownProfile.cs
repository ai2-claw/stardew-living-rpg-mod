namespace StardewLivingRPG.Config;

public sealed class TownProfile
{
    public string Id { get; init; } = "pelican";
    public string CanonTown { get; init; } = "Pelican Town";
    public string NewspaperTitle { get; init; } = "The Pelican Times";
    public string MarketBoardTitle { get; init; } = "Pelican Market Board";
    public string NewspaperEditorName { get; init; } = "Pelican Times Editor";
    public string NewspaperDeskName { get; init; } = "Pelican Times Desk";
    public IReadOnlyList<string> EditorSourceAliases { get; init; } = Array.Empty<string>();

    public bool IsEditorSource(string? sourceNpc)
    {
        var source = (sourceNpc ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(source))
            return false;

        if (source.Equals(NewspaperEditorName, StringComparison.OrdinalIgnoreCase)
            || source.Equals(NewspaperTitle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return EditorSourceAliases.Any(alias => source.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }
}

public static class TownProfileResolver
{
    private static readonly TownProfile PelicanProfile = new()
    {
        Id = "pelican",
        CanonTown = "Pelican Town",
        NewspaperTitle = "The Pelican Times",
        MarketBoardTitle = "Pelican Market Board",
        NewspaperEditorName = "Pelican Times Editor",
        NewspaperDeskName = "Pelican Times Desk",
        EditorSourceAliases = new[]
        {
            "Pelican Times Editor",
            "Editor",
            "Town Reporter",
            "Town Report",
            "The Pelican Times"
        }
    };

    private static readonly TownProfile RidgesideProfile = new()
    {
        Id = "ridgeside",
        CanonTown = "Ridgeside Village",
        NewspaperTitle = "The Ridgeside Register",
        MarketBoardTitle = "Ridgeside Market Board",
        NewspaperEditorName = "Ridgeside Register Editor",
        NewspaperDeskName = "Ridgeside News Desk",
        EditorSourceAliases = new[]
        {
            "Ridgeside Register Editor",
            "Editor",
            "Town Reporter",
            "Town Report",
            "The Ridgeside Register"
        }
    };

    public static TownProfile ResolveForLocation(string? locationName)
    {
        return IsRidgesideLocation(locationName) ? RidgesideProfile : PelicanProfile;
    }

    public static bool IsRidgesideLocation(string? locationName)
    {
        var name = (locationName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.StartsWith("Custom_Ridgeside_", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Ridgeside", StringComparison.OrdinalIgnoreCase);
    }
}
