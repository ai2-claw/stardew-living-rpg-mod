namespace StardewLivingRPG.State;

public sealed class TranscriptArchiveState
{
    public Dictionary<string, NpcTranscriptArchive> Archives { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NpcTranscriptArchive
{
    public List<TranscriptExchange> RawExchanges { get; set; } = new();
    public List<TranscriptChunkHeader> Chunks { get; set; } = new();
    public List<PendingTranscriptExchange> PendingExchanges { get; set; } = new();
    public int LastUpdatedDay { get; set; }
}

public sealed class PendingTranscriptExchange
{
    public string ExchangeId { get; set; } = string.Empty;
    public string RequestToken { get; set; } = string.Empty;
    public string NpcId { get; set; } = string.Empty;
    public string NpcDisplayName { get; set; } = string.Empty;
    public int Day { get; set; }
    public int TimeOfDay { get; set; }
    public string Season { get; set; } = "spring";
    public int Year { get; set; } = 1;
    public string LocationName { get; set; } = string.Empty;
    public string ContextTag { get; set; } = "player_chat";
    public string PlayerText { get; set; } = string.Empty;
    public string Visibility { get; set; } = "npc_only";
    public string SourceRefKind { get; set; } = "chat";
    public string SourceRefId { get; set; } = string.Empty;
}

public sealed class TranscriptExchange
{
    public string ExchangeId { get; set; } = string.Empty;
    public string RequestToken { get; set; } = string.Empty;
    public string NpcId { get; set; } = string.Empty;
    public string NpcDisplayName { get; set; } = string.Empty;
    public int Day { get; set; }
    public int TimeOfDay { get; set; }
    public string Season { get; set; } = "spring";
    public int Year { get; set; } = 1;
    public string LocationName { get; set; } = string.Empty;
    public string ContextTag { get; set; } = "player_chat";
    public string PlayerText { get; set; } = string.Empty;
    public string NpcText { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public int Importance { get; set; } = 1;
    public string Visibility { get; set; } = "npc_only";
    public string CompletionState { get; set; } = "complete";
    public string SourceRefKind { get; set; } = "chat";
    public string SourceRefId { get; set; } = string.Empty;
    public List<string> LinkedImportantMemoryIds { get; set; } = new();
}

public sealed class TranscriptChunkHeader
{
    public string ChunkId { get; set; } = string.Empty;
    public string NpcId { get; set; } = string.Empty;
    public int DayRangeStart { get; set; }
    public int DayRangeEnd { get; set; }
    public int ExchangeCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string[] TopKeywords { get; set; } = Array.Empty<string>();
    public int ImportanceMax { get; set; } = 1;
    public string CompressionCodec { get; set; } = "gzip";
    public string CompressedPayloadBase64 { get; set; } = string.Empty;
}
