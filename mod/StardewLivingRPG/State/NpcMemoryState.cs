namespace StardewLivingRPG.State;

public sealed class NpcMemoryState
{
    public Dictionary<string, NpcMemoryProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NpcMemoryProfile
{
    public List<NpcMemoryFact> Facts { get; set; } = new();
    public List<ImportantMemoryEntry> ImportantMemories { get; set; } = new();
    public List<NpcMemoryTurn> RecentTurns { get; set; } = new();
    public Dictionary<string, int> TopicCounters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int LastUpdatedDay { get; set; }
}

public sealed class ImportantMemoryEntry
{
    public string MemoryId { get; set; } = string.Empty;
    public string Category { get; set; } = "event";
    public string Summary { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public int Importance { get; set; } = 1;
    public string Visibility { get; set; } = "npc_only";
    public string Status { get; set; } = "active";
    public string SourceRefKind { get; set; } = "chat_rule";
    public string SourceRefId { get; set; } = string.Empty;
    public string SourceExchangeId { get; set; } = string.Empty;
    public string EvidenceSnippet { get; set; } = string.Empty;
    public int CreatedDay { get; set; }
    public int LastUpdatedDay { get; set; }
    public int LastReferencedDay { get; set; }
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
