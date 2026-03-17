using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcResidenceService
{
    private readonly DestinationRegistryService _destinationRegistryService;

    public NpcResidenceService(DestinationRegistryService destinationRegistryService)
    {
        _destinationRegistryService = destinationRegistryService;
    }

    public string ResolveHomeLocation(string npcId, IEnumerable<GameLocation> worldLocations, string fallbackLocation = "Town")
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return fallbackLocation;

        var npc = Game1.getCharacterFromName(npcId)
            ?? Utility.getAllCharacters().FirstOrDefault(candidate =>
                candidate is not null
                && string.Equals(candidate.Name, npcId, StringComparison.OrdinalIgnoreCase));
        if (npc is not null && !string.IsNullOrWhiteSpace(npc.DefaultMap))
            return npc.DefaultMap;

        if (_destinationRegistryService.TryResolveResidenceLocation(npcId, worldLocations, out var residence)
            && !string.IsNullOrWhiteSpace(residence.LocationId))
        {
            return residence.LocationId;
        }

        return fallbackLocation;
    }
}
