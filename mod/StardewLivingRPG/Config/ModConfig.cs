namespace StardewLivingRPG.Config;

using StardewModdingAPI;

public sealed class ModConfig
{
    public string Mode { get; set; } = "cozy_canon"; // cozy_canon | story_depth | living_chaos
    public float PriceFloorPct { get; set; } = 0.80f;
    public float PriceCeilingPct { get; set; } = 1.40f;
    public float DailyPriceDeltaCapPct { get; set; } = 0.10f;

    // Open text market board menu.
    public SButton OpenBoardKey { get; set; } = SButton.K;
}
