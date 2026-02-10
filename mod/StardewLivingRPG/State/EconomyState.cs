namespace StardewLivingRPG.State;

public sealed class EconomyState
{
    public Dictionary<string, CropEconomyEntry> Crops { get; set; } = new();
    public List<MarketEventEntry> MarketEvents { get; set; } = new();
}

public sealed class CropEconomyEntry
{
    public int BasePrice { get; set; }
    public int PriceToday { get; set; }
    public int PriceYesterday { get; set; }
    public int RollingSellVolume7D { get; set; }
    public float DemandFactor { get; set; } = 1.0f;
    public float SupplyPressureFactor { get; set; } = 1.0f;
    public float SentimentFactor { get; set; } = 1.0f;
    public float ScarcityBonus { get; set; } = 0.0f;
    public float TrendEma { get; set; } = 0.0f;
    public List<string> Flags { get; set; } = new();
}

public sealed class MarketEventEntry
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Crop { get; set; } = string.Empty;
    public float DeltaPct { get; set; }
    public int StartDay { get; set; }
    public int EndDay { get; set; }
}
