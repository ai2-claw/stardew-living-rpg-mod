using Microsoft.Xna.Framework;

namespace StardewLivingRPG.State;

public enum AutonomyPlanBlockType
{
    BaseAnchor,
    Travel,
    RequiredDuty,
    Work,
    Rest,
    Wander,
    VisitNpc,
    Errand,
    Socialize,
    ObserveEvent,
    ReturnHome,
    Arrive,
    WaitForTarget
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
    public string DutyRoleId { get; set; } = string.Empty;
    public string TargetNpcId { get; set; } = string.Empty;
    public string TargetLocation { get; set; } = string.Empty;
    public string TargetZoneId { get; set; } = string.Empty;
    public string TargetSpotRole { get; set; } = string.Empty;
    public string TargetSpotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public AutonomyPlanBlockStatus Status { get; set; } = AutonomyPlanBlockStatus.Pending;
    public CompiledRoute? Route { get; set; }
    public Point TargetTile { get; set; }
    public bool RequiresExactTile { get; set; }
    public int EstimatedArrivalTime { get; set; }
    public int MaxWaitMinutes { get; set; } = 30;
    public string? FailureReason { get; set; }
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
    public int ActiveRouteSegmentIndex { get; set; } = -1;
    public List<Point> CurrentSegmentPath { get; set; } = new();
    public Point CurrentSegmentTargetTile { get; set; }
    public Point LastKnownTile { get; set; }
    public Point PreviousKnownTile { get; set; }
    public int StationaryTicks { get; set; }
    public int OscillationTicks { get; set; }
    public bool NeedsLocalRepath { get; set; }
    public string StuckReason { get; set; } = string.Empty;
    public string ExpectedLocationId { get; set; } = string.Empty;
    public Point ExpectedTile { get; set; }
    public int ExpectedArrivalTime { get; set; }
    public int OffscreenProgressMinutes { get; set; }
    public string MovementPhase { get; set; } = "idle";
    public string? ActiveEncounterId { get; set; }
    public string? EncounterLockedPartnerNpcId { get; set; }
    public string? StagingTargetNpcId { get; set; }
    public int EncountersToday { get; set; }
    public string? LastEncounterPartnerNpcId { get; set; }
    public string? LastEncounterSummary { get; set; }
    public string? LastEncounterTopicTag { get; set; }
    public string? LastEncounterClosingLine { get; set; }
    public bool LastEncounterClosedWithGoodbye { get; set; }
    public DateTime LastEncounterEndedUtc { get; set; }
    public DateTime SamePairEncounterBlockedUntilUtc { get; set; }
}

public sealed class NpcContextSnapshot
{
    public string NpcId { get; init; } = string.Empty;
    public string CurrentLocation { get; init; } = string.Empty;
    public string HomeLocation { get; init; } = string.Empty;
    public int TimeOfDay { get; init; }
    public string Season { get; init; } = "spring";
    public bool IsRaining { get; init; }
    public bool IsFestivalDay { get; init; }
    public int RecentTownEventCount { get; init; }
    public IReadOnlyList<string> NearbyNpcIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllWorldNpcIds { get; init; } = Array.Empty<string>();
}

public sealed class ScoredAutonomyGoal
{
    public string GoalType { get; init; } = string.Empty;
    public string TargetNpcId { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string RejectReasonCode { get; init; } = string.Empty;
    public bool RequiresPublicFallback { get; init; }
    public float Score { get; init; }
}

public enum RouteSegmentStatus { Pending, InProgress, Completed, Failed }

public sealed class RouteSegment
{
    public string FromLocationId { get; init; } = string.Empty;
    public string ToLocationId { get; init; } = string.Empty;
    public Point DepartureTile { get; init; }
    public Point ArrivalTile { get; init; }
    public int EstimatedMinutes { get; init; }
    public bool IsWarp { get; init; }
    public bool IsDoor { get; init; }
    public RouteSegmentStatus Status { get; set; } = RouteSegmentStatus.Pending;
}

public sealed class CompiledRoute
{
    public string SourceLocationId { get; init; } = string.Empty;
    public string DestinationLocationId { get; init; } = string.Empty;
    public List<RouteSegment> Segments { get; init; } = new();
    public int TotalEstimatedMinutes { get; init; }
    public Point FinalArrivalTile { get; init; }
}

public enum EncounterSource { PlannedVisit, Opportunistic, EventConvergence }
public enum EncounterPhase { Pending, Staging, Talking, Consequences, Complete, Cancelled }
public enum ConversationPhase { Approach, Opening, Body, Shift, Closing, Released, Interrupted }
public enum ConversationAgeClass { Child, Young, Adult, Elder }

public sealed class ConversationTurnPlan
{
    public int SequenceIndex { get; set; }
    public string SpeakerNpcId { get; set; } = string.Empty;
    public ConversationPhase Phase { get; set; } = ConversationPhase.Body;
    public string BeatType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class ConversationScenario
{
    public string Purpose { get; set; } = "check_in";
    public string OpenerStyle { get; set; } = "practical";
    public string PrimaryTopicTag { get; set; } = "check_in";
    public string ToneAtStart { get; set; } = "neutral";
    public string ToneTrend { get; set; } = "steady";
    public string ExitReason { get; set; } = "natural";
    public string ArcOutcome { get; set; } = "neutral_short_talk";
    public int MinimumTurnExchanges { get; set; } = 2;
    public int PlannedTurnCount { get; set; } = 4;
    public bool RequiresClosing { get; set; } = true;
    public ConversationAgeClass SpeakerAgeClass { get; set; } = ConversationAgeClass.Adult;
    public ConversationAgeClass ListenerAgeClass { get; set; } = ConversationAgeClass.Adult;
    public List<ConversationTurnPlan> Turns { get; set; } = new();
}

public sealed class ActiveEncounter
{
    public string EncounterId { get; init; } = string.Empty;
    public string NpcA { get; init; } = string.Empty;
    public string NpcB { get; init; } = string.Empty;
    public string LocationId { get; init; } = string.Empty;
    public EncounterSource Source { get; init; }
    public float Score { get; init; }
    public string SelectedBeat { get; init; } = string.Empty;
    public int TurnDepth { get; init; }
    public EncounterPhase Phase { get; set; } = EncounterPhase.Pending;
    public int TurnsCompleted { get; set; }
    public ConversationPhase ConversationPhase { get; set; } = ConversationPhase.Approach;
    public string ExitInitiatorNpcId { get; set; } = string.Empty;
    public string ExitReason { get; set; } = string.Empty;
    public string ArcOutcome { get; set; } = string.Empty;
    public int MinimumTurnExchanges { get; set; } = 2;
    public bool HasMeaningfulOpening { get; set; }
    public bool HasGoodbyeExchange { get; set; }
    public string CurrentTopicTag { get; set; } = string.Empty;
    public string CurrentTone { get; set; } = string.Empty;
    public float CurrentMomentum { get; set; }
    public int ContinueDesireA { get; set; }
    public int ContinueDesireB { get; set; }
    public int LeavePressureA { get; set; }
    public int LeavePressureB { get; set; }
    public string LastSpeakerNpcId { get; set; } = string.Empty;
    public string LastMoveType { get; set; } = string.Empty;
    public string LastLineSummary { get; set; } = string.Empty;
    public int TurnBudgetSoftCap { get; set; } = 4;
    public bool HasMutualEngagement { get; set; }
    public bool HasExitStarted { get; set; }
    public bool ClosingComplete { get; set; }
    public AutonomyPlanBlockType BlockTypeContext { get; set; } = AutonomyPlanBlockType.Socialize;
    public int BlockEndTime { get; set; }
    public ConversationScenario? Scenario { get; set; }
    public DateTime StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; set; }
}

public sealed class TopologyNode
{
    public string LocationId { get; init; } = string.Empty;
    public string Category { get; init; } = "public";
    public string? OwnerNpcId { get; init; }
    public string[] HouseholdNpcIds { get; init; } = Array.Empty<string>();
    public Point DefaultTile { get; init; }
    public bool IsInterior { get; init; }
}

public sealed class TopologyEdge
{
    public string FromLocationId { get; init; } = string.Empty;
    public string ToLocationId { get; init; } = string.Empty;
    public Point WarpFromTile { get; init; }
    public Point WarpToTile { get; init; }
    public int EstimatedTravelMinutes { get; init; } = 5;
    public bool IsWarp { get; init; }
    public bool IsDoor { get; init; }
}

public sealed class WorldGraph
{
    public Dictionary<string, TopologyNode> Nodes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<TopologyEdge>> AdjacencyBySource { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AutonomySaveState
{
    public Dictionary<string, AutonomyRuntimeState> RuntimeByNpc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> VisitCooldownByPairKey { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LocationCooldownByNpcKey { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AutonomyNpcSummary> RuntimeSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AutonomyNpcSummary
{
    public string NpcId { get; set; } = string.Empty;
    public int CompletedBlocks { get; set; }
    public int FailedBlocks { get; set; }
    public int Encounters { get; set; }
    public string LastStatus { get; set; } = "Idle";
}
