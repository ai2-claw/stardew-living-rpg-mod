namespace StardewLivingRPG.State;

public sealed class NewspaperState
{
    public List<NewspaperIssue> Issues { get; set; } = new();
}

public sealed class NewspaperIssue
{
    public int Day { get; set; }
    public string Headline { get; set; } = string.Empty;
    public List<string> Sections { get; set; } = new();
    public List<string> PredictiveHints { get; set; } = new();
}
