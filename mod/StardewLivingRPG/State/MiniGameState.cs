namespace StardewLivingRPG.State;

public sealed class MiniGameState
{
    public TownSquareMagicianState TownSquareMagician { get; set; } = new();
}

public sealed class TownSquareMagicianState
{
    public int Day { get; set; }
    public string Season { get; set; } = string.Empty;
    public string FeaturedRoundId { get; set; } = string.Empty;
    public string RoundId { get; set; } = string.Empty;
    public string RoundMode { get; set; } = string.Empty;
    public int AttemptsUsed { get; set; }
    public int HintsUsed { get; set; }
    public bool SolvedToday { get; set; }
    public bool RewardClaimedToday { get; set; }
    public string LastOutcome { get; set; } = "fresh";
    public string LastFeedback { get; set; } = string.Empty;
    public int SessionsStartedToday { get; set; }
    public int LifetimeWins { get; set; }
    public int LifetimeRewardClaims { get; set; }
    public List<string> PlayedRoundIdsToday { get; set; } = new();
}
