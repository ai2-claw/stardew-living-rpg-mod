namespace StardewLivingRPG.Systems;

public sealed class QuestProposalResult
{
    public string? CreatedQuestId { get; set; }

    public string RequestedTemplate { get; set; } = string.Empty;
    public string RequestedTarget { get; set; } = string.Empty;
    public string RequestedUrgency { get; set; } = string.Empty;

    public string AppliedTemplate { get; set; } = string.Empty;
    public string AppliedTarget { get; set; } = string.Empty;
    public string AppliedUrgency { get; set; } = string.Empty;

    public int Count { get; set; }
    public int RewardGold { get; set; }
    public int ExpiresDelta { get; set; }

    public bool IsDuplicate { get; set; }

    public static QuestProposalResult Duplicate => new() { IsDuplicate = true };
}
