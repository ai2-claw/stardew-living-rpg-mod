using StardewLivingRPG.Config;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcAmbientIndoorChatterService
{
    private readonly ModConfig _config;
    private readonly NpcResidenceService _residenceService;
    private readonly Dictionary<string, DateTime> _nextAllowedByLocation = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _nextAllowedByPair = new(StringComparer.OrdinalIgnoreCase);

    public NpcAmbientIndoorChatterService(ModConfig config, NpcResidenceService residenceService)
    {
        _config = config;
        _residenceService = residenceService;
    }

    public bool SupportsLocation(GameLocation? location)
    {
        return location is not null
            && !location.IsOutdoors
            && location.characters is not null
            && location.characters.Count >= 2;
    }

    public bool TrySelectPair(GameLocation location, IEnumerable<NPC> occupants, out NPC? speaker, out NPC? listener)
    {
        speaker = null;
        listener = null;
        if (!SupportsLocation(location))
            return false;

        if (_nextAllowedByLocation.TryGetValue(location.Name, out var nextAllowedUtc) && DateTime.UtcNow < nextAllowedUtc)
            return false;

        var bestScore = float.MinValue;
        foreach (var npcA in occupants)
        {
            foreach (var npcB in occupants)
            {
                if (npcA is null || npcB is null || ReferenceEquals(npcA, npcB))
                    continue;

                var pairKey = BuildPairKey(npcA.Name, npcB.Name);
                if (_nextAllowedByPair.TryGetValue(pairKey, out var pairNextAllowed) && DateTime.UtcNow < pairNextAllowed)
                    continue;

                var score = ScorePair(location, npcA, npcB);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                speaker = npcA;
                listener = npcB;
            }
        }

        if (speaker is null || listener is null)
            return false;

        return bestScore >= 0.35f;
    }

    public void MarkStarted(GameLocation location, string npcA, string npcB)
    {
        var intervalSeconds = IsHomeLike(location, npcA, npcB) ? 8 : 14;
        _nextAllowedByLocation[location.Name] = DateTime.UtcNow.AddSeconds(intervalSeconds);
        _nextAllowedByPair[BuildPairKey(npcA, npcB)] = DateTime.UtcNow.AddSeconds(intervalSeconds + 8);
    }

    private float ScorePair(GameLocation location, NPC npcA, NPC npcB)
    {
        var distance = Math.Abs((int)npcA.Tile.X - (int)npcB.Tile.X) + Math.Abs((int)npcA.Tile.Y - (int)npcB.Tile.Y);
        if (distance > _config.AutonomyFaceToFaceDistanceTiles)
            return 0f;

        var score = 0.15f;
        if (distance <= 2)
            score += 0.24f;
        else if (distance <= 4)
            score += 0.16f;

        var homeA = _residenceService.ResolveHomeLocation(npcA.Name, Game1.locations, npcA.DefaultMap ?? string.Empty);
        var homeB = _residenceService.ResolveHomeLocation(npcB.Name, Game1.locations, npcB.DefaultMap ?? string.Empty);
        if (string.Equals(homeA, location.Name, StringComparison.OrdinalIgnoreCase))
            score += 0.18f;
        if (string.Equals(homeB, location.Name, StringComparison.OrdinalIgnoreCase))
            score += 0.18f;
        if (!string.IsNullOrWhiteSpace(homeA)
            && string.Equals(homeA, homeB, StringComparison.OrdinalIgnoreCase)
            && string.Equals(homeA, location.Name, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.22f;
        }

        if (location.Name.Contains("Saloon", StringComparison.OrdinalIgnoreCase))
            score += 0.08f;
        else if (!location.IsOutdoors)
            score += 0.06f;

        return score;
    }

    private bool IsHomeLike(GameLocation location, string npcA, string npcB)
    {
        var homeA = _residenceService.ResolveHomeLocation(npcA, Game1.locations, string.Empty);
        var homeB = _residenceService.ResolveHomeLocation(npcB, Game1.locations, string.Empty);
        return string.Equals(homeA, location.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(homeB, location.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPairKey(string npcA, string npcB)
    {
        return string.Compare(npcA, npcB, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{npcA}|{npcB}"
            : $"{npcB}|{npcA}";
    }
}
