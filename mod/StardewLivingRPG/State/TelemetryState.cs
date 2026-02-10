namespace StardewLivingRPG.State;

public sealed class TelemetryState
{
    public DailyTelemetry Daily { get; set; } = new();
}

public sealed class DailyTelemetry
{
    public int MarketBoardOpens { get; set; }
    public int RumorBoardAccepts { get; set; }
    public int RumorBoardCompletions { get; set; }
    public int AnchorEventsTriggered { get; set; }
    public int WorldMutations { get; set; }
}
