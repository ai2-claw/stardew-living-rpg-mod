namespace StardewLivingRPG.State;

public sealed class QuestState
{
    public List<QuestEntry> Available { get; set; } = new();
    public List<QuestEntry> Active { get; set; } = new();
    public List<QuestEntry> Completed { get; set; } = new();
    public List<QuestEntry> Failed { get; set; } = new();
}

public sealed class QuestEntry
{
    public string QuestId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string Source { get; set; } = "rumor_mill";
    public string Issuer { get; set; } = string.Empty;
    public int ExpiresDay { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string TargetItem { get; set; } = string.Empty;
    public int TargetCount { get; set; }
    public int RewardGold { get; set; }
}
