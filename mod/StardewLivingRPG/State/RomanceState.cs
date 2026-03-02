namespace StardewLivingRPG.State;

public sealed class RomanceState
{
    public Dictionary<string, LoveLanguageProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MicroDateState> ActiveMicroDates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LoveLanguageProfile
{
    public Dictionary<string, int> Axes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Trust { get; set; }
    public int Safety { get; set; }
    public string NextBeat { get; set; } = "warmth";
    public int LastUpdatedDay { get; set; }
    public List<RomanceSignalEntry> RecentSignals { get; set; } = new();
}

public sealed class RomanceSignalEntry
{
    public int Day { get; set; }
    public string Axis { get; set; } = string.Empty;
    public int Delta { get; set; }
    public float Confidence { get; set; }
    public string Evidence { get; set; } = string.Empty;
}

public sealed class MicroDateState
{
    public string ObjectiveType { get; set; } = string.Empty;
    public string ObjectivePayload { get; set; } = string.Empty;
    public string RewardBundle { get; set; } = string.Empty;
    public int IssuedDay { get; set; }
    public int ExpiresDay { get; set; }
    public string Status { get; set; } = "active";
}
