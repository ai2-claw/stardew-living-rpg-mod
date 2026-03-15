namespace StardewLivingRPG.State;

public enum AutonomyPlanBlockType
{
    BaseAnchor,
    Travel,
    Work,
    Rest,
    Wander,
    VisitNpc,
    Errand,
    Socialize,
    ObserveEvent,
    ReturnHome
}

public enum AutonomyPlanBlockStatus
{
    Pending,
    Active,
    Completed,
    Failed,
    Cancelled
}

public enum AutonomyOverrideStatus
{
    Idle,
    Planned,
    Active,
    CoolingDown
}

public sealed class NpcDailyPlan
{
    public string NpcId { get; set; } = string.Empty;
    public int Day { get; set; }
    public List<AutonomyPlanBlock> Blocks { get; set; } = new();
}

public sealed class AutonomyPlanBlock
{
    public string BlockId { get; set; } = string.Empty;
    public AutonomyPlanBlockType Type { get; set; } = AutonomyPlanBlockType.Rest;
    public string TargetNpcId { get; set; } = string.Empty;
    public string TargetLocation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public AutonomyPlanBlockStatus Status { get; set; } = AutonomyPlanBlockStatus.Pending;
}

public sealed class AutonomyRuntimeState
{
    public string NpcId { get; set; } = string.Empty;
    public NpcDailyPlan? ActivePlan { get; set; }
    public int ActiveBlockIndex { get; set; } = -1;
    public string CurrentTargetNpcId { get; set; } = string.Empty;
    public string CurrentTargetLocation { get; set; } = string.Empty;
    public AutonomyOverrideStatus OverrideStatus { get; set; } = AutonomyOverrideStatus.Idle;
    public int RetryCount { get; set; }
    public int ReplanCount { get; set; }
    public int CompletedBlocksToday { get; set; }
    public int FailedBlocksToday { get; set; }
    public DateTime LastProgressUtc { get; set; }
    public DateTime NextEncounterAllowedUtc { get; set; }
}

public sealed class NpcContextSnapshot
{
    public string NpcId { get; init; } = string.Empty;
    public string CurrentLocation { get; init; } = string.Empty;
    public int TimeOfDay { get; init; }
    public string Season { get; init; } = "spring";
    public bool IsRaining { get; init; }
    public bool IsFestivalDay { get; init; }
    public int RecentTownEventCount { get; init; }
    public IReadOnlyList<string> NearbyNpcIds { get; init; } = Array.Empty<string>();
}

public sealed class ScoredAutonomyGoal
{
    public string GoalType { get; init; } = string.Empty;
    public string TargetNpcId { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public float Score { get; init; }
}

public sealed class AutonomyGoalSuggestion
{
    public string GoalType { get; init; } = string.Empty;
    public string TargetNpcId { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public float Urgency { get; init; }
}
