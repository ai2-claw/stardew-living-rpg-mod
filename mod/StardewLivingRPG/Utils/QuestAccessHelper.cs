using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Utils;

public static class QuestAccessHelper
{
    private static readonly HashSet<string> AlwaysAccessibleItems = new(StringComparer.OrdinalIgnoreCase)
    {
        "wild_horseradish", "daffodil", "leek", "dandelion",
        "spice_berry", "sweet_pea", "grape", "blackberry", "hazelnut",
        "winter_root", "crystal_fruit", "snow_yam", "crocus", "holly",
        "anchovy", "sardine", "herring", "sunfish", "smallmouth_bass", "carp", "bream", "chub",
        "tuna", "red_mullet", "tilapia", "salmon", "walleye", "squid",
        "egg", "large_egg", "brown_egg", "large_brown_egg", "duck_egg", "void_egg",
        "milk", "large_milk", "goat_milk", "large_goat_milk",
        "stone", "copper_ore", "iron_ore", "gold_ore", "iridium_ore",
        "coal", "quartz", "refined_quartz", "earth_crystal", "frozen_tear", "fire_quartz",
        "amethyst", "topaz", "jade", "aquamarine", "ruby", "emerald", "diamond",
        "wood", "hardwood", "fiber", "sap", "clay"
    };

    private static readonly Dictionary<string, HashSet<string>> SeasonalItems = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spring"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "parsnip", "potato", "cauliflower", "green_bean", "kale", "garlic", "strawberry", "rhubarb"
        },
        ["summer"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "blueberry", "melon", "tomato", "corn", "hot_pepper", "radish", "wheat", "hops"
        },
        ["fall"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pumpkin", "cranberry", "corn", "wheat", "eggplant", "yam", "bok_choy", "beet", "amaranth", "artichoke"
        },
        ["winter"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
        }
    };

    private static readonly HashSet<string> DesertItems = new(StringComparer.OrdinalIgnoreCase)
    {
        "coconut", "cactus_fruit"
    };

    public static HashSet<string> GetAccessibleSupplyItems(SaveState state)
    {
        var accessible = new HashSet<string>(AlwaysAccessibleItems, StringComparer.OrdinalIgnoreCase);
        var season = NormalizeToken(state.Calendar.Season);
        if (SeasonalItems.TryGetValue(season, out var seasonal))
            accessible.UnionWith(seasonal);

        if (IsDesertUnlocked())
            accessible.UnionWith(DesertItems);

        foreach (var key in VanillaCropCatalog.GetEntries().Keys)
        {
            var normalized = NormalizeToken(key);
            if (IsLikelyAccessibleNow(normalized, state))
                accessible.Add(normalized);
        }

        foreach (var key in state.Economy.Crops.Keys)
        {
            var normalized = NormalizeToken(key);
            if (IsLikelyAccessibleNow(normalized, state))
                accessible.Add(normalized);
        }

        return accessible;
    }

    public static bool IsLikelyAccessibleNow(string? rawItemKey, SaveState state)
    {
        var itemKey = NormalizeToken(rawItemKey);
        if (string.IsNullOrWhiteSpace(itemKey))
            return false;

        if (AlwaysAccessibleItems.Contains(itemKey))
            return true;

        if (DesertItems.Contains(itemKey))
            return IsDesertUnlocked();

        var season = NormalizeToken(state.Calendar.Season);
        return SeasonalItems.TryGetValue(season, out var seasonal)
            && seasonal.Contains(itemKey);
    }

    private static bool IsDesertUnlocked()
    {
        return HasMailFlag("ccVault")
            || HasMailFlag("jojaVault")
            || HasMailFlag("bus_repaired");
    }

    private static bool HasMailFlag(string flag)
    {
        return Game1.player?.mailReceived.Contains(flag, StringComparer.OrdinalIgnoreCase) == true
            || Game1.MasterPlayer?.mailReceived.Contains(flag, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static string NormalizeToken(string? raw)
    {
        return VanillaCropCatalog.NormalizeCropKey(raw);
    }
}
