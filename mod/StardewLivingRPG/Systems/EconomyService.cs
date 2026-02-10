using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class EconomyService
{
    private static readonly Dictionary<string, int> BasePrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["parsnip"] = 35,
        ["potato"] = 80,
        ["cauliflower"] = 175,
        ["blueberry"] = 50,
        ["melon"] = 250,
        ["pumpkin"] = 320,
        ["cranberry"] = 75,
        ["corn"] = 50,
        ["wheat"] = 25,
        ["tomato"] = 60
    };

    public void EnsureInitialized(EconomyState economy)
    {
        foreach (var (crop, basePrice) in BasePrices)
        {
            if (economy.Crops.ContainsKey(crop))
                continue;

            economy.Crops[crop] = new CropEconomyEntry
            {
                BasePrice = basePrice,
                PriceToday = basePrice,
                PriceYesterday = basePrice,
                DemandFactor = 1f,
                SupplyPressureFactor = 1f,
                SentimentFactor = 1f,
                TrendEma = 0f
            };
        }
    }

    public void IngestSales(EconomyState economy, Dictionary<string, int> soldToday)
    {
        foreach (var (crop, count) in soldToday)
        {
            if (!economy.Crops.TryGetValue(crop, out var entry))
                continue;

            entry.RollingSellVolume7D += Math.Max(0, count);
        }
    }

    public void RunDailyPricing(SaveState state)
    {
        EnsureInitialized(state.Economy);

        // Identify oversupplied crop(s) to generate scarcity bonus opportunities for alternatives.
        var maxVolume = state.Economy.Crops.Values.Max(c => c.RollingSellVolume7D);
        var oversupplied = state.Economy.Crops
            .Where(kv => kv.Value.RollingSellVolume7D == maxVolume && maxVolume > 0)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (crop, entry) in state.Economy.Crops)
        {
            entry.PriceYesterday = entry.PriceToday;

            // Gentle pressure in cozy mode: saturating penalty, never a hard crash.
            var pressure = entry.RollingSellVolume7D / 250f; // heuristic scale
            var pressurePenalty = MathF.Min(0.18f, pressure * 0.02f);
            entry.SupplyPressureFactor = 1f - pressurePenalty;

            entry.DemandFactor = ComputeSeasonalDemand(state.Calendar.Season, crop);
            entry.SentimentFactor = 1f + Clamp(state.Social.TownSentiment.Economy / 1000f, -0.08f, 0.08f);

            entry.ScarcityBonus = (!oversupplied.Contains(crop) && oversupplied.Count > 0) ? 0.04f : 0f;

            var raw = entry.BasePrice * entry.DemandFactor * entry.SupplyPressureFactor * entry.SentimentFactor * (1f + entry.ScarcityBonus);
            var floor = entry.BasePrice * state.Config.PriceFloorPct;
            var ceiling = entry.BasePrice * state.Config.PriceCeilingPct;

            // Apply daily delta cap first, then absolute clamp.
            var maxUp = entry.PriceYesterday * (1f + state.Config.DailyPriceDeltaCapPct);
            var maxDown = entry.PriceYesterday * (1f - state.Config.DailyPriceDeltaCapPct);
            var boundedDaily = Clamp(raw, maxDown, maxUp);

            entry.PriceToday = (int)MathF.Round(Clamp(boundedDaily, floor, ceiling));

            var deltaPct = entry.PriceYesterday == 0 ? 0f : (entry.PriceToday - entry.PriceYesterday) / (float)entry.PriceYesterday;
            entry.TrendEma = (entry.TrendEma * 0.7f) + (deltaPct * 0.3f);

            // Rolling decay to approximate 7-day window without storing full queue in M1.
            entry.RollingSellVolume7D = (int)MathF.Round(entry.RollingSellVolume7D * 0.86f);
        }
    }

    private static float ComputeSeasonalDemand(string season, string crop)
    {
        var s = season.ToLowerInvariant();
        var c = crop.ToLowerInvariant();

        return (s, c) switch
        {
            ("spring", "parsnip") => 1.06f,
            ("spring", "cauliflower") => 1.05f,
            ("summer", "blueberry") => 1.04f,
            ("summer", "melon") => 1.06f,
            ("fall", "pumpkin") => 1.08f,
            ("fall", "cranberry") => 1.05f,
            _ => 1.00f
        };
    }

    private static float Clamp(float value, float min, float max)
        => MathF.Max(min, MathF.Min(max, value));
}
