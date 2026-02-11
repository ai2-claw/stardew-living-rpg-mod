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

    public int NpcIntentsApplied { get; set; }
    public int NpcIntentsRejected { get; set; }
    public int NpcIntentsDuplicate { get; set; }
    public Dictionary<string, int> NpcCommandAppliedByType { get; set; } = new();
}
