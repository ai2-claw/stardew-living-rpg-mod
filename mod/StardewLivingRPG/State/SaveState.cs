using StardewLivingRPG.Config;

namespace StardewLivingRPG.State;

public sealed class SaveState
{
    public string Version { get; set; } = "0.1.0";
    public SaveConfig Config { get; set; } = new();
    public CalendarState Calendar { get; set; } = new();
    public EconomyState Economy { get; set; } = new();
    public SocialState Social { get; set; } = new();
    public QuestState Quests { get; set; } = new();
    public FactTableState Facts { get; set; } = new();
    public NewspaperState Newspaper { get; set; } = new();
    public TelemetryState Telemetry { get; set; } = new();

    public static SaveState CreateDefault() => new();

    public void ApplyConfig(ModConfig config)
    {
        Config.Mode = config.Mode;
        Config.PriceFloorPct = config.PriceFloorPct;
        Config.PriceCeilingPct = config.PriceCeilingPct;
        Config.DailyPriceDeltaCapPct = config.DailyPriceDeltaCapPct;
    }
}

public sealed class SaveConfig
{
    public string Mode { get; set; } = "cozy_canon";
    public float PriceFloorPct { get; set; } = 0.80f;
    public float PriceCeilingPct { get; set; } = 1.40f;
    public float DailyPriceDeltaCapPct { get; set; } = 0.10f;
}

public sealed class CalendarState
{
    public int Day { get; set; } = 1;
    public string Season { get; set; } = "spring";
    public int Year { get; set; } = 1;
}
