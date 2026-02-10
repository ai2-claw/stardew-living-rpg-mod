using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class MarketBoardService
{
    public IEnumerable<string> BuildTopRows(SaveState state, int count = 6)
    {
        return state.Economy.Crops
            .OrderByDescending(kv => kv.Value.TrendEma)
            .Take(count)
            .Select(kv =>
            {
                var e = kv.Value;
                var arrow = e.PriceToday > e.PriceYesterday ? "↑" : e.PriceToday < e.PriceYesterday ? "↓" : "→";
                return $"{kv.Key,-12} {e.PriceToday,4}g {arrow} (demand {e.DemandFactor:F2}, supply {e.SupplyPressureFactor:F2}, scarcity+ {e.ScarcityBonus:P0})";
            });
    }
}
