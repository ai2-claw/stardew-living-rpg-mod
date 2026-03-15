namespace StardewLivingRPG.State;

public sealed class TelemetryState
{
    public DailyTelemetry Daily { get; set; } = new();
}

public sealed class DailyTelemetry
{
    public int MarketBoardOpens { get; set; }
    public int RumorBoardAccepts { get; set; }
    public int RumorBoardCompletions { get; set; }
    public int TownSquareMagicianSessions { get; set; }
    public int TownSquareMagicianWins { get; set; }
    public int TownSquareMagicianRewardClaims { get; set; }
    public int AnchorEventsTriggered { get; set; }
    public int WorldMutations { get; set; }

    public int NpcIntentsApplied { get; set; }
    public int NpcIntentsRejected { get; set; }
    public int NpcIntentsDuplicate { get; set; }
    public int NpcIntentsAutoApplied { get; set; }
    public int NpcIntentsManualApplied { get; set; }
    public int NpcIntentsAutoRejected { get; set; }
    public int NpcIntentsManualRejected { get; set; }
    public int NpcAskGateAccepted { get; set; }
    public int NpcAskGateDeferred { get; set; }
    public int NpcAskGateRejected { get; set; }
    public Dictionary<string, int> NpcCommandAppliedByType { get; set; } = new();
    public Dictionary<string, int> NpcPolicyRejectByReason { get; set; } = new();
    public Dictionary<string, int> AmbientCommandAppliedByType { get; set; } = new();
    public Dictionary<string, int> AmbientCommandRejectedByType { get; set; } = new();
    public Dictionary<string, int> AmbientCommandDuplicateByType { get; set; } = new();
    public int AmbientLowInfoSuppressed { get; set; }
    public int AmbientCadenceSkips { get; set; }
    public int AutonomyGoalsProposed { get; set; }
    public int AutonomyGoalsAccepted { get; set; }
    public int AutonomyGoalsRejected { get; set; }
    public int AutonomyPlansCreated { get; set; }
    public int AutonomyBlocksCompleted { get; set; }
    public int AutonomyBlocksFailed { get; set; }
    public int AutonomyReplans { get; set; }
    public int ScheduleOverridesApplied { get; set; }
    public int ArrivalsSucceeded { get; set; }
    public int ArrivalsFailed { get; set; }
    public int HomeVisitsAllowed { get; set; }
    public int HomeVisitsDenied { get; set; }
    public int EncountersStarted { get; set; }
    public int EncountersCompleted { get; set; }
    public int EncountersCancelled { get; set; }
    public int EncountersPlanned { get; set; }
    public int EncountersOpportunistic { get; set; }
    public int PairEmotionUpdates { get; set; }
    public int BubblesDisplayed { get; set; }
    public int BubblesFallbackUsed { get; set; }
    public int BubblesCancelled { get; set; }
    public Dictionary<string, int> VisitDenialByReason { get; set; } = new();
    public Dictionary<string, int> PairEmotionUpdatesByAxis { get; set; } = new();
    public Dictionary<string, int> AutonomyRejectByReason { get; set; } = new();

    public int RomanceCommandsApplied { get; set; }
    public int RomanceCommandsRejected { get; set; }
    public int RomanceMicroDatesIssued { get; set; }
    public int RomanceMicroDatesCompleted { get; set; }
    public int RomanceMicroDatesExpired { get; set; }
    public Dictionary<string, int> RomanceAxisUpdatesByType { get; set; } = new();
    public Dictionary<string, int> RomanceRejectByReason { get; set; } = new();
}

