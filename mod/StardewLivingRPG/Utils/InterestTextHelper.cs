using StardewLivingRPG.State;

namespace StardewLivingRPG.Utils;

public static class InterestTextHelper
{
    private static readonly HashSet<string> FarmTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "parsnip", "potato", "cauliflower", "green_bean", "kale", "garlic", "blueberry", "melon", "corn", "pepper",
        "tomato", "wheat", "radish", "pumpkin", "cranberry", "yam", "bok_choy", "amaranth", "eggplant", "artichoke",
        "beet", "fairy_rose", "sunflower", "coffee_bean", "tea_leaves", "rice_shoot", "hops", "grape",
        "egg", "large_egg", "brown_egg", "large_brown_egg", "milk", "large_milk", "goat_milk", "large_goat_milk",
        "wool", "hay"
    };

    private static readonly HashSet<string> NatureTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "wild_horseradish", "daffodil", "leek", "dandelion", "salmonberry", "blackberry", "spice_berry", "sweet_pea",
        "hazelnut", "common_mushroom", "chanterelle", "morel", "red_mushroom", "purple_mushroom", "winter_root",
        "crystal_fruit", "snow_yam", "crocus", "holly", "anchovy", "sardine", "herring", "tuna", "salmon",
        "sunfish", "catfish", "shad", "smallmouth_bass", "largemouth_bass", "carp", "bream", "pike", "red_mullet",
        "tilapia", "squid", "halibut", "walleye", "eel", "flounder", "chub", "sturgeon", "ghostfish",
        "wood", "hardwood", "fiber", "sap", "clay"
    };

    private static readonly HashSet<string> MineTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "stone", "copper_ore", "iron_ore", "gold_ore", "iridium_ore", "coal", "quartz", "refined_quartz",
        "earth_crystal", "frozen_tear", "fire_quartz", "amethyst", "topaz", "jade", "aquamarine", "ruby",
        "emerald", "diamond"
    };

    public static string GetDisplayName(string? interestId)
    {
        return NormalizeInterest(interestId) switch
        {
            "farmers_circle" => I18n.Get("interest.group.farmers", "Farmers' Circle"),
            "shopkeepers_guild" => I18n.Get("interest.group.shopkeepers", "Shopkeepers' Guild"),
            "adventurers_club" => I18n.Get("interest.group.adventurers", "Adventurers' Club"),
            "nature_keepers" => I18n.Get("interest.group.nature", "Nature Keepers"),
            _ => QuestTextHelper.PrettyName(interestId)
        };
    }

    public static string BuildShiftToast(string? interestId)
    {
        return NormalizeInterest(interestId) switch
        {
            "farmers_circle" => I18n.Get("hud.simulation.interest_shift.farmers", "Town current: the growers picked up momentum."),
            "shopkeepers_guild" => I18n.Get("hud.simulation.interest_shift.shopkeepers", "Town current: the merchants picked up momentum."),
            "adventurers_club" => I18n.Get("hud.simulation.interest_shift.adventurers", "Town current: safety-minded voices picked up momentum."),
            "nature_keepers" => I18n.Get("hud.simulation.interest_shift.nature", "Town current: the valley's caretakers picked up momentum."),
            _ => I18n.Get("hud.simulation.interest_shift", "Town groups shifted their focus.")
        };
    }

    public static string BuildTownCurrentsSection(SaveState state)
    {
        if (!TryGetReadableInterest(state, out var interestId, out var interestState))
            return string.Empty;

        var priorities = BuildPrioritySummary(interestId, interestState.Priorities);
        return interestId switch
        {
            "farmers_circle" => I18n.Get(
                "interest.newspaper.farmers",
                "Town current: local growers have been setting the pace. Folks keep talking about {{priorities}}.",
                new { priorities }),
            "shopkeepers_guild" => I18n.Get(
                "interest.newspaper.shopkeepers",
                "Town current: merchants have been carrying more weight lately. Shop talk keeps returning to {{priorities}}.",
                new { priorities }),
            "adventurers_club" => I18n.Get(
                "interest.newspaper.adventurers",
                "Town current: safety-minded voices are carrying farther than usual. Folks are pushing for {{priorities}}.",
                new { priorities }),
            "nature_keepers" => I18n.Get(
                "interest.newspaper.nature",
                "Town current: the valley's caretakers are being heard a little more. People keep circling back to {{priorities}}.",
                new { priorities }),
            _ => string.Empty
        };
    }

    public static string BuildQuestDetailLine(QuestEntry quest)
    {
        if (!TryResolveQuestInterest(quest, out var interestId))
            return string.Empty;

        return interestId switch
        {
            "farmers_circle" => I18n.Get("interest.quest.detail.farmers", "This kind of work helps steady the growers and keep pantry staples moving."),
            "shopkeepers_guild" => I18n.Get("interest.quest.detail.shopkeepers", "This posting would ease the pressure on shop counters and keep trade moving."),
            "adventurers_club" => I18n.Get("interest.quest.detail.adventurers", "This sort of haul helps the dangerous work stay supplied."),
            "nature_keepers" => I18n.Get("interest.quest.detail.nature", "This request lines up with the folk trying to keep the valley's wild places healthy."),
            _ => string.Empty
        };
    }

    public static string BuildQuestAcceptLine(QuestEntry quest)
    {
        if (!TryResolveQuestInterest(quest, out var interestId))
            return string.Empty;

        return interestId switch
        {
            "farmers_circle" => I18n.Get("interest.quest.accept.farmers", "Townsfolk will read this as backing the growers."),
            "shopkeepers_guild" => I18n.Get("interest.quest.accept.shopkeepers", "Townsfolk will read this as easing the merchants' load."),
            "adventurers_club" => I18n.Get("interest.quest.accept.adventurers", "Townsfolk will read this as supporting the valley's rougher work."),
            "nature_keepers" => I18n.Get("interest.quest.accept.nature", "Townsfolk will read this as backing the valley's quieter corners."),
            _ => string.Empty
        };
    }

    public static string BuildQuestCompleteLine(QuestEntry quest)
    {
        if (!TryResolveQuestInterest(quest, out var interestId))
            return string.Empty;

        return interestId switch
        {
            "farmers_circle" => I18n.Get("interest.quest.complete.farmers", "Word like this travels fast among the growers."),
            "shopkeepers_guild" => I18n.Get("interest.quest.complete.shopkeepers", "Word like this settles shop nerves in a hurry."),
            "adventurers_club" => I18n.Get("interest.quest.complete.adventurers", "Word like this helps the town feel better supplied against trouble."),
            "nature_keepers" => I18n.Get("interest.quest.complete.nature", "Word like this makes the valley's caretakers feel heard."),
            _ => string.Empty
        };
    }

    public static bool TryResolveQuestInterest(QuestEntry quest, out string interestId)
    {
        interestId = string.Empty;
        if (quest is null)
            return false;

        var templateId = (quest.TemplateId ?? string.Empty).Trim().ToLowerInvariant();
        var target = NormalizeTarget(quest.TargetItem);
        switch (templateId)
        {
            case "gather_crop":
                interestId = "farmers_circle";
                return true;
            case "mine_resource":
                interestId = "adventurers_club";
                return true;
            case "social_visit":
                return false;
            case "deliver_item":
                if (MineTargets.Contains(target))
                {
                    interestId = "adventurers_club";
                    return true;
                }

                if (NatureTargets.Contains(target))
                {
                    interestId = "nature_keepers";
                    return true;
                }

                if (FarmTargets.Contains(target))
                {
                    interestId = "farmers_circle";
                    return true;
                }

                interestId = "shopkeepers_guild";
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetReadableInterest(SaveState state, out string interestId, out InterestState interestState)
    {
        interestId = string.Empty;
        interestState = new InterestState();

        if (state is null)
            return false;

        var recentCutoffDay = Math.Max(1, state.Calendar.Day - 2);
        var recentInterest = state.Facts.Facts
            .Where(entry =>
                entry.Value.Value
                && entry.Value.SetDay >= recentCutoffDay
                && entry.Key.StartsWith("interest:", StringComparison.OrdinalIgnoreCase)
                && entry.Key.Contains(":shifted:", StringComparison.OrdinalIgnoreCase))
            .Select(entry => new
            {
                entry.Value.SetDay,
                Interest = ParseInterestFactKey(entry.Key)
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Interest))
            .OrderByDescending(entry => entry.SetDay)
            .ThenBy(entry => entry.Interest, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (recentInterest is not null
            && state.Social.Interests.TryGetValue(recentInterest.Interest, out var recentState))
        {
            interestId = recentInterest.Interest;
            interestState = recentState;
            return true;
        }

        var leadingInterest = state.Social.Interests
            .Where(entry => entry.Value is not null && entry.Value.Influence > 0)
            .OrderByDescending(entry => entry.Value.Influence)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(leadingInterest.Key) || leadingInterest.Value is null)
            return false;

        interestId = leadingInterest.Key;
        interestState = leadingInterest.Value;
        return true;
    }

    private static string ParseInterestFactKey(string key)
    {
        var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 4 && parts[0].Equals("interest", StringComparison.OrdinalIgnoreCase)
            ? parts[1].Trim().ToLowerInvariant()
            : string.Empty;
    }

    private static string BuildPrioritySummary(string interestId, List<string>? priorities)
    {
        var readable = (priorities ?? new List<string>())
            .Where(priority => !string.IsNullOrWhiteSpace(priority))
            .Select(BuildPriorityLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        if (readable.Count == 0)
        {
            return interestId switch
            {
                "farmers_circle" => I18n.Get("interest.priority.fallback.farmers", "steady crops and stocked seed shelves"),
                "shopkeepers_guild" => I18n.Get("interest.priority.fallback.shopkeepers", "steady trade and reliable shop traffic"),
                "adventurers_club" => I18n.Get("interest.priority.fallback.adventurers", "safe mine runs and prepared crews"),
                "nature_keepers" => I18n.Get("interest.priority.fallback.nature", "healthy woods and a gentler footprint"),
                _ => I18n.Get("interest.priority.fallback.generic", "the town's day-to-day needs")
            };
        }

        return readable.Count switch
        {
            1 => readable[0],
            _ => $"{readable[0]} {I18n.Get("interest.priority.and", "and")} {readable[1]}"
        };
    }

    private static string BuildPriorityLabel(string priority)
    {
        var normalized = NormalizeTarget(priority);
        return normalized switch
        {
            "stable_prices" => I18n.Get("interest.priority.stable_prices", "steadier crop prices"),
            "seed_access" => I18n.Get("interest.priority.seed_access", "reliable seed shelves"),
            "foot_traffic" => I18n.Get("interest.priority.foot_traffic", "stronger foot traffic"),
            "margin_stability" => I18n.Get("interest.priority.margin_stability", "steady shop margins"),
            "biodiversity" => I18n.Get("interest.priority.biodiversity", "healthier wildlife"),
            "forest_health" => I18n.Get("interest.priority.forest_health", "the forest staying healthy"),
            _ => QuestTextHelper.PrettyName(priority).ToLowerInvariant()
        };
    }

    private static string NormalizeInterest(string? interestId)
    {
        return (interestId ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeTarget(string? target)
    {
        return (target ?? string.Empty).Trim().ToLowerInvariant();
    }
}
