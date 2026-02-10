namespace StardewLivingRPG.State;

public sealed class FactTableState
{
    public Dictionary<string, FactValue> Facts { get; set; } = new();
    public Dictionary<string, ProcessedIntentValue> ProcessedIntents { get; set; } = new();
}

public sealed class FactValue
{
    public bool Value { get; set; }
    public int SetDay { get; set; }
    public int? TtlDays { get; set; }
    public string Source { get; set; } = "system";
}

public sealed class ProcessedIntentValue
{
    public int Day { get; set; }
    public string NpcId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Status { get; set; } = "applied";
}
