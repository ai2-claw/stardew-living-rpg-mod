namespace StardewLivingRPG.State;

public sealed class SocialState
{
    public Dictionary<string, InterestState> Interests { get; set; } = new();
    public Dictionary<string, int> NpcReputation { get; set; } = new();
    public Dictionary<string, RelationshipState> NpcRelationships { get; set; } = new();
    public TownSentimentState TownSentiment { get; set; } = new();
}

public sealed class InterestState
{
    public int Influence { get; set; }
    public int Trust { get; set; }
    public List<string> Priorities { get; set; } = new();
}

public sealed class RelationshipState
{
    public string Stance { get; set; } = "neutral";
    public int Trust { get; set; }
}

public sealed class TownSentimentState
{
    public int Economy { get; set; }
    public int Community { get; set; }
    public int Environment { get; set; }
}
