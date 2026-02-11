namespace StardewLivingRPG.State;

public sealed class NpcMemoryState
{
    public Dictionary<string, NpcMemoryProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NpcMemoryProfile
{
    public List<NpcMemoryFact> Facts { get; set; } = new();
    public List<NpcMemoryTurn> RecentTurns { get; set; } = new();
    public Dictionary<string, int> TopicCounters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int LastUpdatedDay { get; set; }
}

public sealed class NpcMemoryFact
{
    public string FactId { get; set; } = string.Empty;
    public string Category { get; set; } = "event";
    public string Text { get; set; } = string.Empty;
    public int Day { get; set; }
    public int Weight { get; set; } = 1;
    public int LastReferencedDay { get; set; }
}

public sealed class NpcMemoryTurn
{
    public int Day { get; set; }
    public string PlayerText { get; set; } = string.Empty;
    public string NpcText { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
}
