using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcConversationSimulationService
{
    private readonly PairEmotionService _pairEmotionService;
    private readonly ModConfig _config;

    private static readonly HashSet<string> ChildNpcNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Jas", "Vincent", "Leo"
    };

    private static readonly HashSet<string> YoungNpcNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abigail", "Alex", "Haley", "Sam", "Sebastian", "Maru", "Penny", "Emily"
    };

    private static readonly HashSet<string> ElderNpcNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Evelyn", "George", "Lewis", "Marnie", "Gunther", "Gus"
    };

    public NpcConversationSimulationService(
        PairEmotionService pairEmotionService,
        ModConfig config)
    {
        _pairEmotionService = pairEmotionService;
        _config = config;
    }

    public ConversationScenario BuildScenario(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock block)
    {
        var pair = _pairEmotionService.GetOrCreate(state, speaker.Name, listener.Name);
        var friendship = GetAxis(pair, "friendship");
        var trust = GetAxis(pair, "trust");
        var anger = GetAxis(pair, "anger");
        var awkwardness = GetAxis(pair, "awkwardness");
        var jealousy = GetAxis(pair, "jealousy");
        var avoidance = pair.Avoidance;
        var hasGrudge = pair.ActiveFlags.Contains("grudge", StringComparer.OrdinalIgnoreCase);
        var toneAtStart = ResolveTone(friendship, trust, anger, awkwardness, jealousy, avoidance, hasGrudge);
        var purpose = ResolvePurpose(block, friendship, pair.Tension, avoidance, hasGrudge);
        var softCap = ResolveSoftTurnBudget(friendship, trust, awkwardness, avoidance, block.Type, speaker.Name, listener.Name);

        return new ConversationScenario
        {
            Purpose = purpose,
            PrimaryTopicTag = purpose,
            OpenerStyle = ResolveOpenerStyle(toneAtStart, block, ResolveAgeClass(speaker.Name), speaker.Name),
            ToneAtStart = toneAtStart,
            ToneTrend = ResolveToneTrend(block, toneAtStart, pair, speaker.Name, listener.Name),
            ExitReason = ResolveExitReason(block, toneAtStart, speaker.Name, listener.Name),
            ArcOutcome = ResolveArcOutcome(toneAtStart, purpose, softCap),
            MinimumTurnExchanges = 4,
            PlannedTurnCount = softCap,
            RequiresClosing = _config.AutonomyRequireConversationClosing,
            SpeakerAgeClass = ResolveAgeClass(speaker.Name),
            ListenerAgeClass = ResolveAgeClass(listener.Name)
        };
    }

    public void InitializeEncounter(ActiveEncounter encounter, ConversationScenario scenario, AutonomyPlanBlock block, string initiatorNpcId)
    {
        encounter.Scenario = scenario;
        encounter.ConversationPhase = ConversationPhase.Approach;
        encounter.CurrentTopicTag = scenario.PrimaryTopicTag;
        encounter.CurrentTone = scenario.ToneAtStart;
        encounter.CurrentMomentum = scenario.ToneAtStart == "warm" ? 0.65f : scenario.ToneAtStart == "tense" ? 0.40f : 0.50f;
        encounter.MinimumTurnExchanges = scenario.MinimumTurnExchanges;
        encounter.BlockTypeContext = block.Type;
        encounter.BlockEndTime = block.EndTime;
        encounter.TurnBudgetSoftCap = scenario.PlannedTurnCount;
        encounter.ExitReason = scenario.ExitReason;
        encounter.ExitInitiatorNpcId = initiatorNpcId;
        encounter.ArcOutcome = scenario.ArcOutcome;
        encounter.HasMeaningfulOpening = false;
        encounter.HasGoodbyeExchange = false;
        encounter.HasMutualEngagement = false;
        encounter.HasExitStarted = false;
        encounter.LastSpeakerNpcId = string.Empty;
        encounter.LastMoveType = string.Empty;
        encounter.LastLineSummary = string.Empty;
        encounter.TurnsCompleted = 0;
    }

    public string ResolveOutcome(ActiveEncounter encounter)
    {
        if (encounter.HasGoodbyeExchange && encounter.HasMutualEngagement && encounter.CurrentTone == "warm")
            return encounter.TurnsCompleted >= Math.Max(4, encounter.TurnBudgetSoftCap) ? "warm_long_talk" : "friendly";
        if (encounter.CurrentTone == "tense")
            return encounter.HasMutualEngagement ? "unresolved_tension" : "hostile";
        if (encounter.CurrentTone == "awkward")
            return "awkward_but_polite";
        if (encounter.BlockTypeContext is AutonomyPlanBlockType.Work or AutonomyPlanBlockType.Errand)
            return "practical_exchange";

        return encounter.ArcOutcome;
    }

    private int ResolveSoftTurnBudget(int friendship, int trust, int awkwardness, int avoidance, AutonomyPlanBlockType blockType, string speakerNpcId, string listenerNpcId)
    {
        var turns = _config.AutonomyMinimumConversationTurns;

        if (friendship + trust >= 80)
            turns += 2;
        if (awkwardness >= 30)
            turns -= 1;
        if (avoidance >= 35)
            turns -= 1;
        if (blockType == AutonomyPlanBlockType.VisitNpc)
            turns += 1;
        if (IsDutyFirstNpc(speakerNpcId) || IsDutyFirstNpc(listenerNpcId))
            turns -= 1;

        return Math.Clamp(turns, Math.Max(4, _config.AutonomyMinimumConversationTurns), _config.AutonomyMaximumConversationTurns);
    }

    private static string ResolvePurpose(AutonomyPlanBlock block, int friendship, int tension, int avoidance, bool hasGrudge)
    {
        return block.Type switch
        {
            AutonomyPlanBlockType.VisitNpc when hasGrudge => "grievance",
            AutonomyPlanBlockType.VisitNpc when avoidance >= 45 => "guarded_check_in",
            AutonomyPlanBlockType.VisitNpc when tension >= 45 => "grievance",
            AutonomyPlanBlockType.VisitNpc when tension >= 35 => "conflict",
            AutonomyPlanBlockType.VisitNpc when friendship >= 45 => "gossip_visit",
            AutonomyPlanBlockType.VisitNpc when friendship >= 25 => "visit",
            AutonomyPlanBlockType.VisitNpc => "check_in",
            AutonomyPlanBlockType.Errand when tension >= 25 => "work_strain",
            AutonomyPlanBlockType.Errand => "work_coordination",
            AutonomyPlanBlockType.Work when tension >= 25 => "work_strain",
            AutonomyPlanBlockType.Work => "work_coordination",
            AutonomyPlanBlockType.Socialize when tension >= 30 => "rumor_check",
            AutonomyPlanBlockType.Socialize when friendship >= 30 => "gossip",
            AutonomyPlanBlockType.Socialize => "check_in",
            _ => tension >= 35 ? "awkward_encounter" : "check_in"
        };
    }

    private static string ResolveTone(int friendship, int trust, int anger, int awkwardness, int jealousy, int avoidance, bool hasGrudge)
    {
        if (hasGrudge || avoidance >= 55)
            return "frustrated";
        if (anger >= 45)
            return "frustrated";
        if (jealousy >= 40)
            return "suspicious";
        if (anger >= 35 || jealousy >= 35)
            return "tense";
        if (awkwardness >= 30)
            return "awkward";
        if (friendship + trust >= 90)
            return "excited";
        if (friendship + trust >= 60)
            return "warm";
        return "neutral";
    }

    private static string ResolveOpenerStyle(string toneAtStart, AutonomyPlanBlock block, ConversationAgeClass age, string speakerNpcId)
    {
        if (IsDutyFirstNpc(speakerNpcId))
            return "duty";
        if (age == ConversationAgeClass.Elder)
            return "formal";
        if (toneAtStart == "frustrated" || toneAtStart == "suspicious")
            return "blunt";
        if (toneAtStart == "excited")
            return "eager";
        if (toneAtStart == "warm")
            return "warm";
        if (toneAtStart == "awkward" || block.Type == AutonomyPlanBlockType.Wander)
            return "awkward";
        return "practical";
    }

    private static string ResolveToneTrend(AutonomyPlanBlock block, string toneAtStart, NpcPairEmotionEntry pair, string speakerNpcId, string listenerNpcId)
    {
        if (block.Type == AutonomyPlanBlockType.Work || IsDutyFirstNpc(speakerNpcId) || IsDutyFirstNpc(listenerNpcId))
            return toneAtStart == "warm" ? "practical_to_friendly" : "friendly_to_urgent";
        if (pair.ActiveFlags.Contains("grudge", StringComparer.OrdinalIgnoreCase))
            return pair.FriendshipLikeValue() >= 35 ? "repair_or_escalate" : "escalating";
        if (pair.Avoidance >= 40)
            return "guarded";
        if (toneAtStart == "frustrated")
            return pair.FriendshipLikeValue() >= 25 ? "repair_or_escalate" : "escalating";
        if (toneAtStart == "suspicious")
            return "digging_for_answers";
        if (toneAtStart == "excited")
            return "story_hook";
        if (toneAtStart == "tense" && pair.FriendshipLikeValue() >= 20)
            return "warming_up";
        if (toneAtStart == "warm" && pair.Tension >= 25)
            return "cooling_off";
        return "steady";
    }

    private static string ResolveExitReason(AutonomyPlanBlock block, string toneAtStart, string speakerNpcId, string listenerNpcId)
    {
        if (block.Type is AutonomyPlanBlockType.Work or AutonomyPlanBlockType.Errand || IsDutyFirstNpc(speakerNpcId) || IsDutyFirstNpc(listenerNpcId))
            return "duty";
        if (toneAtStart == "frustrated")
            return "distance";
        if (toneAtStart == "awkward")
            return "awkwardness";
        if (toneAtStart == "tense")
            return "tension";
        return "natural";
    }

    private static string ResolveArcOutcome(string toneAtStart, string purpose, int softCap)
    {
        if (toneAtStart == "warm" && purpose == "visit" && softCap >= 5)
            return "warm_long_talk";
        if (toneAtStart == "excited")
            return "story_hook";
        if (toneAtStart == "suspicious" || purpose == "rumor_check")
            return "mystery_hook";
        if (toneAtStart == "frustrated" || purpose is "grievance" or "guarded_check_in" or "work_strain")
            return "friction_left_hanging";
        if (purpose == "gossip" || purpose == "gossip_visit")
            return "rumor_shared";
        if (toneAtStart == "tense")
            return "unresolved_tension";
        if (toneAtStart == "awkward")
            return "awkward_but_polite";
        return "friendly";
    }

    private static ConversationAgeClass ResolveAgeClass(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return ConversationAgeClass.Adult;
        if (ChildNpcNames.Contains(npcName))
            return ConversationAgeClass.Child;
        if (YoungNpcNames.Contains(npcName))
            return ConversationAgeClass.Young;
        if (ElderNpcNames.Contains(npcName))
            return ConversationAgeClass.Elder;
        return ConversationAgeClass.Adult;
    }

    private static bool IsDutyFirstNpc(string? npcName)
    {
        return npcName is "Pierre" or "Morris" or "Gus" or "Robin" or "Clint" or "Harvey" or "Lewis";
    }

    private static int GetAxis(NpcPairEmotionEntry pair, string axis)
    {
        return pair.EmotionAxes.TryGetValue(axis, out var value) ? value : 0;
    }

    private static int ConvertTimeToMinutes(int timeOfDay)
    {
        var hours = timeOfDay / 100;
        var minutes = timeOfDay % 100;
        return (hours * 60) + minutes;
    }
}

internal static class NpcPairEmotionEntryExtensions
{
    public static int FriendshipLikeValue(this NpcPairEmotionEntry entry)
    {
        var friendship = entry.EmotionAxes.TryGetValue("friendship", out var friendshipValue) ? friendshipValue : 0;
        var trust = entry.EmotionAxes.TryGetValue("trust", out var trustValue) ? trustValue : 0;
        return friendship + trust;
    }
}
