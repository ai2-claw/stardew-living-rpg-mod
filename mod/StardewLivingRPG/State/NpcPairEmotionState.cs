namespace StardewLivingRPG.State;

public sealed class NpcPairEmotionEntry
{
    public int Affinity { get; set; }
    public int Familiarity { get; set; }
    public int Tension { get; set; }
    public int Avoidance { get; set; }
    public int LastInteractionDay { get; set; }
    public Dictionary<string, int> EmotionAxes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> ActiveFlags { get; set; } = new();
}

