using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class EncounterBadTileMaskService
{
    private const string SveUniqueId = "FlashShifter.StardewValleyExpandedCP";

    private readonly bool _sveLoaded;
    private readonly Dictionary<string, MapMaskEntry> _entries;

    public EncounterBadTileMaskService(IModHelper helper)
    {
        _sveLoaded = helper.ModRegistry.IsLoaded(SveUniqueId);
        var loaded = helper.Data.ReadJsonFile<Dictionary<string, MapMaskEntry>>("assets/map_paths.json");
        _entries = loaded is null
            ? new Dictionary<string, MapMaskEntry>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, MapMaskEntry>(loaded, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasActiveMask(GameLocation? location)
    {
        return TryGetEntry(location, out _);
    }

    public bool IsMaskedBadTile(GameLocation? location, Point tile)
    {
        if (!TryGetEntry(location, out var entry))
            return false;
        if (tile.X < 0 || tile.Y < 0 || tile.X >= entry.Width || tile.Y >= entry.Height)
            return false;

        var index = (tile.Y * entry.Width) + tile.X;
        return index >= 0
            && index < entry.Data.Count
            && entry.Data[index] == 3;
    }

    private bool TryGetEntry(GameLocation? location, out MapMaskEntry entry)
    {
        entry = null!;
        if (!_sveLoaded || location?.Map is null)
            return false;
        if (!_entries.TryGetValue(location.Name ?? string.Empty, out var candidate))
            return false;

        var backLayer = location.Map.GetLayer("Back");
        if (backLayer is null)
            return false;
        if (candidate is null)
            return false;
        if (candidate.Data.Count != candidate.Width * candidate.Height)
            return false;

        if (candidate.Width != backLayer.LayerWidth || candidate.Height != backLayer.LayerHeight)
            return false;

        entry = candidate;
        return true;
    }

    public sealed class MapMaskEntry
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<int> Data { get; set; } = new();
    }
}
