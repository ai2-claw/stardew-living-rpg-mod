namespace StardewLivingRPG.State;

public sealed class TownMemoryState
{
    public List<TownMemoryEvent> Events { get; set; } = new();
    public Dictionary<string, NpcTownKnowledge> KnowledgeByNpc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int LastPruneDay { get; set; }
}

public sealed class TownMemoryEvent
{
    public string EventId { get; set; } = string.Empty;
    public string Kind { get; set; } = "incident";
    public string Summary { get; set; } = string.Empty;
    public int Day { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Severity { get; set; } = 1;
    public string Visibility { get; set; } = "local";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int MentionBudget { get; set; } = 3;
}

public sealed class NpcTownKnowledge
{
    public Dictionary<string, TownKnowledgeEntry> ByEventId { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TownKnowledgeEntry
{
    public bool Knows { get; set; }
    public int LearnedDay { get; set; }
    public int MentionCount { get; set; }
    public int LastMentionDay { get; set; }
    public string Angle { get; set; } = "neutral";
}
