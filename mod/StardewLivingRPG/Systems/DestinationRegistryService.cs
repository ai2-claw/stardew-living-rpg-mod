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
        public string[] HouseholdNpcIds { get; init; } = Array.Empty<string>();
        public string[] RoleTags { get; init; } = Array.Empty<string>();
        public int OpenTime { get; init; } = 600;
        public int CloseTime { get; init; } = 2600;
        public Point DefaultTile { get; init; } = new(0, 0);
    }

    private IReadOnlyList<AutonomyLocation>? _cachedLocations;
    private int _cachedDay = -1;

    public void InvalidateCache() { _cachedLocations = null; _cachedDay = -1; }

    public IReadOnlyList<AutonomyLocation> BuildLocations(IEnumerable<GameLocation> worldLocations)
    {
        var today = Game1.Date?.TotalDays ?? -1;
        if (_cachedLocations is not null && _cachedDay == today)
            return _cachedLocations;

        var locationList = worldLocations.ToList();
        var npcHomeMap = BuildNpcHomeMap();

        var results = new List<AutonomyLocation>();
        foreach (var location in locationList)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.Name))
                continue;

            var normalized = location.Name.Trim();
            var category = InferCategory(normalized);
            var household = ResolveHousehold(normalized, npcHomeMap);
            var owner = household.Length > 0 ? household[0] : string.Empty;

            results.Add(new AutonomyLocation
            {
                LocationId = normalized,
                DisplayName = normalized,
                Category = category,
                OwnerNpcId = owner,
                HouseholdNpcIds = household,
                RoleTags = InferRoleTags(normalized, category),
                OpenTime = category == "private" ? 900 : 600,
                CloseTime = category == "private" ? 2000 : 2600,
                DefaultTile = InferDefaultTile(location)
            });
        }

        _cachedLocations = results;
        _cachedDay = today;
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
                candidate.HouseholdNpcIds.Contains(targetNpcId, StringComparer.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(candidate.OwnerNpcId)
                    && candidate.OwnerNpcId.Equals(targetNpcId, StringComparison.OrdinalIgnoreCase)))
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

    public bool TryResolveResidenceLocation(
        string npcId,
        IEnumerable<GameLocation> worldLocations,
        out AutonomyLocation location)
    {
        location = BuildLocations(worldLocations)
            .FirstOrDefault(candidate =>
                candidate.HouseholdNpcIds.Contains(npcId, StringComparer.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(candidate.OwnerNpcId)
                    && candidate.OwnerNpcId.Equals(npcId, StringComparison.OrdinalIgnoreCase)))
            ?? new AutonomyLocation();

        return !string.IsNullOrWhiteSpace(location.LocationId);
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

        // Check relationship with ANY household member (G20)
        var householdMembers = location.HouseholdNpcIds.Length > 0
            ? location.HouseholdNpcIds
            : (!string.IsNullOrWhiteSpace(location.OwnerNpcId) ? new[] { location.OwnerNpcId } : Array.Empty<string>());

        if (householdMembers.Length == 0)
            return false;

        // Visitor is a household member themselves
        if (householdMembers.Any(h => string.Equals(h, visitorNpcId, StringComparison.OrdinalIgnoreCase)))
            return true;

        var bestAffinity = int.MinValue;
        var worstAvoidance = 0;
        var worstTension = 0;

        foreach (var resident in householdMembers)
        {
            var pairKey = PairEmotionService.BuildPairKey(visitorNpcId, resident);
            state.Social.PairEmotions.TryGetValue(pairKey, out var pair);
            var affinity = pair?.Affinity ?? 0;
            var tension = pair?.Tension ?? 0;
            var avoidance = pair?.Avoidance ?? 0;

            if (affinity > bestAffinity) bestAffinity = affinity;
            if (avoidance > worstAvoidance) worstAvoidance = avoidance;
            if (tension > worstTension) worstTension = tension;
        }

        if (worstAvoidance >= 50)
        {
            reasonCode = "avoidance";
            return false;
        }

        if (worstTension >= 60)
        {
            reasonCode = "high_tension";
            return false;
        }

        if (bestAffinity >= 40)
            return true;

        if (bestAffinity >= 10 && timeOfDay >= 1000 && timeOfDay <= 1800)
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

    /// <summary>
    /// Build a map of locationId → NPC names using NPC.DefaultMap (G13 fix).
    /// </summary>
    private static Dictionary<string, List<string>> BuildNpcHomeMap()
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var npc in Utility.getAllCharacters())
        {
            if (npc is null || string.IsNullOrWhiteSpace(npc.Name) || string.IsNullOrWhiteSpace(npc.DefaultMap))
                continue;

            if (!map.TryGetValue(npc.DefaultMap, out var list))
            {
                list = new List<string>();
                map[npc.DefaultMap] = list;
            }
            list.Add(npc.Name);
        }
        return map;
    }

    /// <summary>
    /// Resolve all NPCs whose DefaultMap points to the given location (G20 fix).
    /// </summary>
    private static string[] ResolveHousehold(string locationId, Dictionary<string, List<string>> npcHomeMap)
    {
        return npcHomeMap.TryGetValue(locationId, out var list) ? list.ToArray() : Array.Empty<string>();
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
