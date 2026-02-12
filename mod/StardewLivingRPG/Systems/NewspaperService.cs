using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NewspaperService
{
    public NewspaperIssue BuildIssue(SaveState state, string? anchorNote = null)
    {
        var topDown = GetTopDown(state);
        var topUp = GetTopUp(state);

        var issue = new NewspaperIssue
        {
            Day = state.Calendar.Day,
            Headline = BuildHeadline(topDown, topUp),
            Sections = new List<string>(),
            PredictiveHints = new List<string>(),
            Articles = state.Newspaper.Articles.Where(a => a.Day == state.Calendar.Day && a.ExpirationDay >= state.Calendar.Day).ToList()
        };

        if (topDown is not null)
            issue.Sections.Add($"Market: {Cap(topDown.Crop)} softened {(Math.Abs(topDown.DeltaPct) * 100):0.#}% to {topDown.Today}g.");

        if (topUp is not null)
            issue.Sections.Add($"Opportunity: {Cap(topUp.Crop)} rose {(topUp.DeltaPct * 100):0.#}% to {topUp.Today}g.");

        if (!string.IsNullOrWhiteSpace(anchorNote))
            issue.Sections.Add(anchorNote);

        // lightweight predictive hints for planner players
        var scarcityCandidate = state.Economy.Crops
            .Where(kv => kv.Value.ScarcityBonus > 0.0f)
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(scarcityCandidate.Key))
        {
            issue.PredictiveHints.Add($"Supply outlook: {Cap(scarcityCandidate.Key)} may stay strong if oversupply persists elsewhere.");
        }

        var seasonHint = state.Calendar.Season.ToLowerInvariant() switch
        {
            "spring" => "Season watch: Cauliflower and Parsnip demand tends to hold in spring.",
            "summer" => "Season watch: Melon and Blueberry demand remains healthy in summer.",
            "fall" => "Season watch: Pumpkin and Cranberry demand tends to strengthen in fall.",
            _ => "Season watch: mixed demand expected tomorrow."
        };
        issue.PredictiveHints.Add(seasonHint);

        return issue;
    }

    private static string BuildHeadline(dynamic? topDown, dynamic? topUp)
    {
        if (topDown is null && topUp is null)
            return "Quiet Day at Pelican Market";

        if (topDown is not null && topUp is not null)
            return $"{Cap(topDown.Crop)} Slips While {Cap(topUp.Crop)} Climbs";

        if (topDown is not null)
            return $"{Cap(topDown.Crop)} Pulls Back at Market";

        return $"{Cap(topUp!.Crop)} Leads Today's Gains";
    }

    private static string Cap(string s)
        => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static CropPriceChange? GetTopDown(SaveState state)
    {
        if (state.Economy.Crops is null) return null;
        return state.Economy.Crops
            .Where(kv => kv.Value.PriceYesterday > 0)
            .Select(kv => new CropPriceChange
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                Yesterday = kv.Value.PriceYesterday,
                DeltaPct = (double)(kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .Where(x => x.DeltaPct < 0)
            .OrderByDescending(x => Math.Abs(x.DeltaPct))
            .FirstOrDefault();
    }

    private static CropPriceChange? GetTopUp(SaveState state)
    {
        if (state.Economy.Crops is null) return null;
        return state.Economy.Crops
            .Where(kv => kv.Value.PriceYesterday > 0)
            .Select(kv => new CropPriceChange
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                Yesterday = kv.Value.PriceYesterday,
                DeltaPct = (double)(kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .Where(x => x.DeltaPct > 0)
            .OrderByDescending(x => x.DeltaPct)
            .FirstOrDefault();
    }

    private class CropPriceChange
    {
        public string Crop { get; set; } = string.Empty;
        public int Today { get; set; }
        public int Yesterday { get; set; }
        public double DeltaPct { get; set; }
    }
}
