using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.Systems;

public sealed class EconomyService
{
    public void EnsureInitialized(EconomyState economy)
    {
        foreach (var (crop, cropData) in VanillaCropCatalog.GetEntries())
        {
            if (economy.Crops.ContainsKey(crop))
                continue;

            var basePrice = Math.Max(1, cropData.BasePrice);
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

        // Keep only active/future market events so stale modifiers do not accumulate forever.
        state.Economy.MarketEvents.RemoveAll(ev => ev.EndDay < state.Calendar.Day);

        var activeMarketEventModifierByCrop = state.Economy.MarketEvents
            .Where(ev => ev.StartDay <= state.Calendar.Day && state.Calendar.Day <= ev.EndDay)
            .GroupBy(ev => (ev.Crop ?? string.Empty).Trim().ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => Clamp(g.Sum(ev => ev.DeltaPct), -0.25f, 0.25f),
                StringComparer.OrdinalIgnoreCase);

        // Identify oversupplied crop(s) to generate scarcity bonus opportunities for alternatives.
        var maxVolume = state.Economy.Crops.Values.Max(c => c.RollingSellVolume7D);
        var oversupplied = state.Economy.Crops
            .Where(kv => kv.Value.RollingSellVolume7D == maxVolume && maxVolume > 0)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (crop, entry) in state.Economy.Crops)
        {
            // Track price history before updating
            entry.PriceHistory7D.Add(entry.PriceToday);
            if (entry.PriceHistory7D.Count > 7)
                entry.PriceHistory7D.RemoveAt(0);

            entry.PriceYesterday = entry.PriceToday;

            // Gentle pressure in cozy mode: saturating penalty, never a hard crash.
            var pressure = entry.RollingSellVolume7D / 250f; // heuristic scale
            var pressurePenalty = MathF.Min(0.18f, pressure * 0.02f);
            entry.SupplyPressureFactor = 1f - pressurePenalty;

            entry.DemandFactor = ComputeSeasonalDemand(state.Calendar.Season, crop);
            entry.SentimentFactor = 1f + Clamp(state.Social.TownSentiment.Economy / 1000f, -0.08f, 0.08f);

            entry.ScarcityBonus = (!oversupplied.Contains(crop) && oversupplied.Count > 0) ? 0.04f : 0f;

            var raw = entry.BasePrice * entry.DemandFactor * entry.SupplyPressureFactor * entry.SentimentFactor * (1f + entry.ScarcityBonus);
            if (activeMarketEventModifierByCrop.TryGetValue(crop, out var marketEventDeltaPct))
                raw *= 1f + marketEventDeltaPct;
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

    public bool TryNormalizeCropKey(string rawName, out string cropKey)
    {
        cropKey = string.Empty;
        if (string.IsNullOrWhiteSpace(rawName))
            return false;

        var known = VanillaCropCatalog.GetEntries();
        var key = VanillaCropCatalog.NormalizeCropKey(rawName);
        if (known.ContainsKey(key))
        {
            cropKey = key;
            return true;
        }

        if (key.EndsWith("ies", StringComparison.Ordinal) && key.Length > 3)
        {
            var singularY = key[..^3] + "y";
            if (known.ContainsKey(singularY))
            {
                cropKey = singularY;
                return true;
            }
        }

        if (key.EndsWith("s", StringComparison.Ordinal) && key.Length > 1)
        {
            var singular = key[..^1];
            if (known.ContainsKey(singular))
            {
                cropKey = singular;
                return true;
            }
        }

        return false;
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
