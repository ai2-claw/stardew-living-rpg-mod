using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class DestinationRegistryService
{
    public sealed class AutonomyLocation
    {
        public string LocationId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Category { get; init; } = "public";
        public string OwnerNpcId { get; init; } = string.Empty;
        public string[] RoleTags { get; init; } = Array.Empty<string>();
        public int OpenTime { get; init; } = 600;
        public int CloseTime { get; init; } = 2600;
        public Point DefaultTile { get; init; } = new(0, 0);
    }

    public IReadOnlyList<AutonomyLocation> BuildLocations(IEnumerable<GameLocation> worldLocations)
    {
        var results = new List<AutonomyLocation>();
        foreach (var location in worldLocations)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.Name))
                continue;

            var normalized = location.Name.Trim();
            var category = InferCategory(normalized);
            results.Add(new AutonomyLocation
            {
                LocationId = normalized,
                DisplayName = normalized,
                Category = category,
                OwnerNpcId = InferOwner(normalized),
                RoleTags = InferRoleTags(normalized, category),
                OpenTime = category == "private" ? 900 : 600,
                CloseTime = category == "private" ? 2000 : 2600,
                DefaultTile = InferDefaultTile(location)
            });
        }

        return results;
    }

    public bool TryResolveVisitLocation(
        SaveState state,
        string visitorNpcId,
        string targetNpcId,
        int timeOfDay,
        IEnumerable<GameLocation> worldLocations,
        out AutonomyLocation location,
        out string reasonCode)
    {
        reasonCode = "ok";
        location = BuildLocations(worldLocations)
            .FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(candidate.OwnerNpcId)
                && candidate.OwnerNpcId.Equals(targetNpcId, StringComparison.OrdinalIgnoreCase))
            ?? new AutonomyLocation();

        if (string.IsNullOrWhiteSpace(location.LocationId))
        {
            reasonCode = "target_home_missing";
            return false;
        }

        if (!IsVisitAllowed(state, visitorNpcId, location, timeOfDay, out reasonCode))
            return false;

        return true;
    }

    public bool IsVisitAllowed(
        SaveState state,
        string visitorNpcId,
        AutonomyLocation location,
        int timeOfDay,
        out string reasonCode)
    {
        reasonCode = "ok";
        if (timeOfDay < location.OpenTime || timeOfDay > location.CloseTime)
        {
            reasonCode = "closed_hours";
            return false;
        }

        if (!string.Equals(location.Category, "private", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(location.OwnerNpcId))
            return false;

        var pairKey = PairEmotionService.BuildPairKey(visitorNpcId, location.OwnerNpcId);
        state.Social.PairEmotions.TryGetValue(pairKey, out var pair);
        var affinity = pair?.Affinity ?? 0;
        var tension = pair?.Tension ?? 0;
        var avoidance = pair?.Avoidance ?? 0;

        if (avoidance >= 50)
        {
            reasonCode = "avoidance";
            return false;
        }

        if (tension >= 60)
        {
            reasonCode = "high_tension";
            return false;
        }

        if (affinity >= 40)
            return true;

        if (affinity >= 10 && timeOfDay >= 1000 && timeOfDay <= 1800)
            return true;

        reasonCode = "low_affinity";
        return false;
    }

    public AutonomyLocation ResolveFallbackLocation(string currentLocation, IEnumerable<GameLocation> worldLocations)
    {
        var locations = BuildLocations(worldLocations);
        return locations.FirstOrDefault(candidate =>
                   candidate.Category == "public"
                   && (candidate.RoleTags.Contains("square", StringComparer.OrdinalIgnoreCase)
                       || candidate.RoleTags.Contains("saloon", StringComparer.OrdinalIgnoreCase)))
               ?? locations.FirstOrDefault(candidate => candidate.LocationId.Equals(currentLocation, StringComparison.OrdinalIgnoreCase))
               ?? locations.FirstOrDefault()
               ?? new AutonomyLocation { LocationId = currentLocation, DisplayName = currentLocation };
    }

    private static string InferCategory(string locationName)
    {
        if (locationName.Contains("House", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Cabin", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Trailer", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("FarmHouse", StringComparison.OrdinalIgnoreCase))
        {
            return "private";
        }

        if (locationName.Contains("Shop", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Hospital", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Saloon", StringComparison.OrdinalIgnoreCase))
        {
            return "semi_private";
        }

        return "public";
    }

    private static string InferOwner(string locationName)
    {
        if (locationName.Contains("Abigail", StringComparison.OrdinalIgnoreCase))
            return "Abigail";
        if (locationName.Contains("Alex", StringComparison.OrdinalIgnoreCase))
            return "Alex";
        if (locationName.Contains("Caroline", StringComparison.OrdinalIgnoreCase))
            return "Caroline";
        if (locationName.Contains("Emily", StringComparison.OrdinalIgnoreCase))
            return "Emily";
        if (locationName.Contains("Evelyn", StringComparison.OrdinalIgnoreCase))
            return "Evelyn";
        if (locationName.Contains("George", StringComparison.OrdinalIgnoreCase))
            return "George";
        if (locationName.Contains("Haley", StringComparison.OrdinalIgnoreCase))
            return "Haley";
        if (locationName.Contains("Harvey", StringComparison.OrdinalIgnoreCase))
            return "Harvey";
        if (locationName.Contains("Leah", StringComparison.OrdinalIgnoreCase))
            return "Leah";
        if (locationName.Contains("Lewis", StringComparison.OrdinalIgnoreCase))
            return "Lewis";
        if (locationName.Contains("Linus", StringComparison.OrdinalIgnoreCase))
            return "Linus";
        if (locationName.Contains("Marnie", StringComparison.OrdinalIgnoreCase))
            return "Marnie";
        if (locationName.Contains("Pam", StringComparison.OrdinalIgnoreCase))
            return "Pam";
        if (locationName.Contains("Pierre", StringComparison.OrdinalIgnoreCase))
            return "Pierre";
        if (locationName.Contains("Robin", StringComparison.OrdinalIgnoreCase))
            return "Robin";
        if (locationName.Contains("Sam", StringComparison.OrdinalIgnoreCase))
            return "Sam";
        if (locationName.Contains("Sebastian", StringComparison.OrdinalIgnoreCase))
            return "Sebastian";
        if (locationName.Contains("Shane", StringComparison.OrdinalIgnoreCase))
            return "Shane";
        return string.Empty;
    }

    private static string[] InferRoleTags(string locationName, string category)
    {
        var tags = new List<string>();
        if (category == "private")
            tags.Add("home");
        if (locationName.Contains("Saloon", StringComparison.OrdinalIgnoreCase))
            tags.Add("saloon");
        if (locationName.Contains("Town", StringComparison.OrdinalIgnoreCase))
            tags.Add("square");
        if (locationName.Contains("Shop", StringComparison.OrdinalIgnoreCase))
            tags.Add("shop");
        if (locationName.Contains("Forest", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Mountain", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Beach", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("nature");
        }

        if (tags.Count == 0)
            tags.Add(category == "private" ? "home" : "public");

        return tags.ToArray();
    }

    private static Point InferDefaultTile(GameLocation location)
    {
        if (location.characters?.Count > 0)
        {
            var npc = location.characters[0];
            return new Point((int)npc.Tile.X, (int)npc.Tile.Y);
        }

        return Point.Zero;
    }
}
