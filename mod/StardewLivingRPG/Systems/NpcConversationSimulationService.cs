using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcConversationSimulationService
{
    private readonly PairEmotionService _pairEmotionService;
    private readonly NpcSpeechStyleService? _speechStyleService;
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
        ModConfig config,
        NpcSpeechStyleService? speechStyleService = null)
    {
        _pairEmotionService = pairEmotionService;
        _config = config;
        _speechStyleService = speechStyleService;
    }

    public ConversationScenario BuildScenario(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock block)
    {
        var pair = _pairEmotionService.GetOrCreate(state, speaker.Name, listener.Name);
        var friendship = GetAxis(pair, "friendship");
        var trust = GetAxis(pair, "trust");
        var anger = GetAxis(pair, "anger");
        var awkwardness = GetAxis(pair, "awkwardness");
        var jealousy = GetAxis(pair, "jealousy");
        var toneAtStart = ResolveTone(friendship, trust, anger, awkwardness, jealousy);
        var purpose = ResolvePurpose(block, friendship, pair.Tension);
        var softCap = ResolveSoftTurnBudget(friendship, trust, awkwardness, block.Type, speaker.Name, listener.Name);

        return new ConversationScenario
        {
            Purpose = purpose,
            PrimaryTopicTag = purpose,
            OpenerStyle = ResolveOpenerStyle(toneAtStart, block, ResolveAgeClass(speaker.Name), speaker.Name),
            ToneAtStart = toneAtStart,
            ToneTrend = ResolveToneTrend(block, toneAtStart, pair, speaker.Name, listener.Name),
            ExitReason = ResolveExitReason(block, toneAtStart, speaker.Name, listener.Name),
            ArcOutcome = ResolveArcOutcome(toneAtStart, purpose, softCap),
            MinimumTurnExchanges = 2,
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

    public ConversationTurnPlan? BuildNextTurn(SaveState state, ActiveEncounter encounter, NPC npcA, NPC npcB)
    {
        RefreshEncounterDynamics(state, encounter, npcA, npcB);

        if (!encounter.HasMeaningfulOpening)
            return BuildOpeningTurn(encounter, npcA, npcB);

        if (encounter.HasExitStarted)
            return BuildClosingTurn(encounter, npcA, npcB);

        if (ShouldBeginExit(encounter))
        {
            encounter.HasExitStarted = true;
            encounter.ConversationPhase = ConversationPhase.Closing;
            return BuildClosingTurn(encounter, npcA, npcB);
        }

        encounter.ConversationPhase = encounter.TurnsCompleted >= encounter.TurnBudgetSoftCap
            ? ConversationPhase.Shift
            : ConversationPhase.Body;

        var speaker = ResolveNextSpeaker(encounter, npcA, npcB);
        var listener = ReferenceEquals(speaker, npcA) ? npcB : npcA;
        var moveType = ResolveBodyMove(encounter, speaker, listener);
        var text = BuildTurnText(encounter, speaker, listener, moveType);

        encounter.LastSpeakerNpcId = speaker.Name;
        encounter.LastMoveType = moveType;
        encounter.LastLineSummary = text;
        encounter.TurnsCompleted += 1;
        encounter.HasMutualEngagement = encounter.TurnsCompleted >= 3;
        encounter.CurrentMomentum = Math.Clamp(encounter.CurrentMomentum + ResolveMomentumDelta(encounter, moveType), 0.05f, 0.95f);
        encounter.CurrentTone = ResolveCurrentToneAfterMove(encounter, moveType);
        encounter.ArcOutcome = ResolveLiveArcOutcome(encounter);

        return new ConversationTurnPlan
        {
            SequenceIndex = encounter.TurnsCompleted,
            SpeakerNpcId = speaker.Name,
            Phase = encounter.ConversationPhase,
            BeatType = moveType,
            Text = text
        };
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

    private ConversationTurnPlan BuildOpeningTurn(ActiveEncounter encounter, NPC npcA, NPC npcB)
    {
        var speaker = encounter.TurnsCompleted == 0 ? npcA : npcB;
        var listener = ReferenceEquals(speaker, npcA) ? npcB : npcA;
        var moveType = encounter.TurnsCompleted == 0 ? "greet" : "acknowledge";
        var text = BuildTurnText(encounter, speaker, listener, moveType);

        encounter.TurnsCompleted += 1;
        encounter.LastSpeakerNpcId = speaker.Name;
        encounter.LastMoveType = moveType;
        encounter.LastLineSummary = text;
        encounter.ConversationPhase = ConversationPhase.Opening;
        encounter.HasMeaningfulOpening = encounter.TurnsCompleted >= 2;

        return new ConversationTurnPlan
        {
            SequenceIndex = encounter.TurnsCompleted,
            SpeakerNpcId = speaker.Name,
            Phase = ConversationPhase.Opening,
            BeatType = moveType,
            Text = text
        };
    }

    private ConversationTurnPlan? BuildClosingTurn(ActiveEncounter encounter, NPC npcA, NPC npcB)
    {
        var exitNpc = string.Equals(encounter.ExitInitiatorNpcId, npcB.Name, StringComparison.OrdinalIgnoreCase) ? npcB : npcA;
        var otherNpc = ReferenceEquals(exitNpc, npcA) ? npcB : npcA;

        if (!string.Equals(encounter.LastMoveType, "exit_signal", StringComparison.OrdinalIgnoreCase))
        {
            var text = BuildTurnText(encounter, exitNpc, otherNpc, "exit_signal");
            encounter.LastSpeakerNpcId = exitNpc.Name;
            encounter.LastMoveType = "exit_signal";
            encounter.LastLineSummary = text;
            encounter.TurnsCompleted += 1;

            return new ConversationTurnPlan
            {
                SequenceIndex = encounter.TurnsCompleted,
                SpeakerNpcId = exitNpc.Name,
                Phase = ConversationPhase.Closing,
                BeatType = "exit_signal",
                Text = text
            };
        }

        if (!encounter.HasGoodbyeExchange)
        {
            var text = BuildTurnText(encounter, otherNpc, exitNpc, "goodbye");
            encounter.LastSpeakerNpcId = otherNpc.Name;
            encounter.LastMoveType = "goodbye";
            encounter.LastLineSummary = text;
            encounter.TurnsCompleted += 1;
            encounter.HasGoodbyeExchange = true;

            return new ConversationTurnPlan
            {
                SequenceIndex = encounter.TurnsCompleted,
                SpeakerNpcId = otherNpc.Name,
                Phase = ConversationPhase.Closing,
                BeatType = "goodbye",
                Text = text
            };
        }

        encounter.ConversationPhase = ConversationPhase.Released;
        return null;
    }

    private void RefreshEncounterDynamics(SaveState state, ActiveEncounter encounter, NPC npcA, NPC npcB)
    {
        var pair = _pairEmotionService.GetOrCreate(state, npcA.Name, npcB.Name);
        var friendship = GetAxis(pair, "friendship");
        var trust = GetAxis(pair, "trust");
        var anger = GetAxis(pair, "anger");
        var awkwardness = GetAxis(pair, "awkwardness");

        encounter.ContinueDesireA = ComputeContinueDesire(encounter, npcA.Name, friendship, trust, anger, awkwardness);
        encounter.ContinueDesireB = ComputeContinueDesire(encounter, npcB.Name, friendship, trust, anger, awkwardness);
        encounter.LeavePressureA = ComputeLeavePressure(encounter, npcA.Name, anger, awkwardness);
        encounter.LeavePressureB = ComputeLeavePressure(encounter, npcB.Name, anger, awkwardness);

        encounter.CurrentMomentum = Math.Clamp(
            (encounter.ContinueDesireA + encounter.ContinueDesireB - encounter.LeavePressureA - encounter.LeavePressureB + 120) / 240f,
            0.05f,
            0.95f);
    }

    private bool ShouldBeginExit(ActiveEncounter encounter)
    {
        if (encounter.TurnsCompleted < encounter.MinimumTurnExchanges)
            return false;

        var sideAReadyToLeave = encounter.LeavePressureA >= encounter.ContinueDesireA + 8;
        var sideBReadyToLeave = encounter.LeavePressureB >= encounter.ContinueDesireB + 8;
        if (sideAReadyToLeave || sideBReadyToLeave)
        {
            encounter.ExitInitiatorNpcId = sideBReadyToLeave && encounter.LeavePressureB > encounter.LeavePressureA
                ? encounter.NpcB
                : encounter.NpcA;
            return true;
        }

        if (encounter.TurnsCompleted >= encounter.TurnBudgetSoftCap
            && encounter.CurrentMomentum < 0.35f)
        {
            encounter.ExitInitiatorNpcId = encounter.ContinueDesireA >= encounter.ContinueDesireB ? encounter.NpcB : encounter.NpcA;
            return true;
        }

        return false;
    }

    private NPC ResolveNextSpeaker(ActiveEncounter encounter, NPC npcA, NPC npcB)
    {
        if (string.IsNullOrWhiteSpace(encounter.LastSpeakerNpcId))
            return npcA;

        if (string.Equals(encounter.LastSpeakerNpcId, npcA.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (encounter.ContinueDesireB + 4 >= encounter.ContinueDesireA)
                return npcB;
        }
        else if (encounter.ContinueDesireA + 4 >= encounter.ContinueDesireB)
        {
            return npcA;
        }

        return string.Equals(encounter.LastSpeakerNpcId, npcA.Name, StringComparison.OrdinalIgnoreCase) ? npcA : npcB;
    }

    private string ResolveBodyMove(ActiveEncounter encounter, NPC speaker, NPC listener)
    {
        if (encounter.BlockTypeContext is AutonomyPlanBlockType.Work or AutonomyPlanBlockType.Errand)
            return encounter.TurnsCompleted <= 2 ? "practical_update" : "reassure";
        if (encounter.CurrentTone == "tense")
            return encounter.CurrentMomentum >= 0.55f ? "apologize" : "disagree";
        if (encounter.CurrentTone == "awkward")
            return encounter.TurnsCompleted % 2 == 0 ? "pause" : "share";
        if (string.Equals(encounter.CurrentTopicTag, "visit", StringComparison.OrdinalIgnoreCase))
            return encounter.TurnsCompleted <= 3 ? "share" : "ask";
        if (string.Equals(encounter.CurrentTopicTag, "gossip", StringComparison.OrdinalIgnoreCase))
            return encounter.TurnsCompleted % 2 == 0 ? "gossip" : "react";

        return encounter.CurrentMomentum >= 0.60f ? "share" : "ask";
    }

    private string BuildTurnText(ActiveEncounter encounter, NPC speaker, NPC listener, string moveType)
    {
        var listenerName = ResolveDisplayName(listener);
        var age = ResolveAgeClass(speaker.Name);
        var role = ResolveRole(speaker);
        var profile = _speechStyleService?.GetProfile(speaker.Name) ?? NpcVerbalProfile.Traditionalist;

        if (moveType == "greet")
            return encounter.Scenario?.OpenerStyle switch
            {
                "formal" => $"Good day, {listenerName}. Have you a moment?",
                "awkward" => $"Uh... I thought I should say hello.",
                "duty" => $"{listenerName}, I can spare a short minute.",
                "warm" when age == ConversationAgeClass.Child => $"Hi {listenerName}. Are you busy?",
                "warm" => $"Hey {listenerName}. I was glad to see you.",
                _ => $"I figured this was a good time to talk."
            };

        if (moveType == "acknowledge")
            return encounter.CurrentTone switch
            {
                "warm" => "It's good to see you too.",
                "awkward" => "Sure. I'm listening.",
                "tense" => "All right. Say what you need to say.",
                _ when role == "shopkeeper" => "I can stay a moment, then I must get back.",
                _ => "All right. What's on your mind?"
            };

        if (moveType == "exit_signal")
            return encounter.ExitReason switch
            {
                "duty" when age == ConversationAgeClass.Child => "I should get back now.",
                "duty" => "I should get back to my work now.",
                "tension" => "We should leave it here for today.",
                "awkwardness" => "I think that's enough for now.",
                _ when encounter.CurrentTone == "warm" => "I should go, but I'm glad we spoke.",
                _ => "I should be on my way now."
            };

        if (moveType == "goodbye")
            return encounter.CurrentTone switch
            {
                "warm" when age == ConversationAgeClass.Child => "Okay. See you soon.",
                "warm" => $"Take care, {listenerName}.",
                "tense" => "All right. We'll leave it there.",
                _ => "All right. Take care."
            };

        if (age == ConversationAgeClass.Child)
        {
            return moveType switch
            {
                "share" => "I wanted to tell you something small.",
                "ask" => "Do you think things are okay?",
                "react" => "That makes me feel a little better.",
                "apologize" => "I didn't mean for it to feel bad.",
                _ => "It mattered to me."
            };
        }

        return moveType switch
        {
            "practical_update" => role == "shopkeeper"
                ? "Town traffic has been strange all day."
                : "I needed to sort out today's practical work.",
            "reassure" => encounter.CurrentTone == "tense"
                ? "I'd rather settle it than carry it longer."
                : "It helps to say these things plainly.",
            "disagree" => "I could not keep pretending it was nothing.",
            "apologize" => "At least now it is out in the open.",
            "gossip" => $"Word keeps circling back to {listenerName}.",
            "react" => "I was hoping to hear your side of it.",
            "pause" => profile == NpcVerbalProfile.Recluse
                ? "I don't say that often."
                : "It seemed worth saying face to face.",
            "ask" => encounter.CurrentTone == "warm"
                ? "I wanted a real moment, not just a passing nod."
                : "I did not want to let the moment pass quietly.",
            _ => ResolveShareLine(encounter, profile, listenerName)
        };
    }

    private static string ResolveShareLine(ActiveEncounter encounter, NpcVerbalProfile profile, string listenerName)
    {
        return encounter.CurrentTone switch
        {
            "warm" when encounter.CurrentMomentum >= 0.65f => "I kept thinking about checking in on you.",
            "tense" when encounter.CurrentMomentum >= 0.45f => "There has been some strain between us.",
            "awkward" => "The day kept pulling at my thoughts.",
            _ when profile == NpcVerbalProfile.Professional => $"Still, it was good to stop and speak plainly, {listenerName}.",
            _ => "It helped to talk this through properly."
        };
    }

    private int ComputeContinueDesire(ActiveEncounter encounter, string npcId, int friendship, int trust, int anger, int awkwardness)
    {
        var score = 40;
        score += friendship / 4;
        score += trust / 5;
        score -= anger / 5;
        score -= awkwardness / 6;
        score += encounter.HasMutualEngagement ? 8 : 0;
        score += encounter.TurnsCompleted < encounter.MinimumTurnExchanges ? 10 : 0;
        score += IsDutyFirstNpc(npcId) ? -8 : 0;
        return Math.Clamp(score, 0, 100);
    }

    private int ComputeLeavePressure(ActiveEncounter encounter, string npcId, int anger, int awkwardness)
    {
        var score = 12;
        if (IsDutyFirstNpc(npcId))
            score += 12;
        if (encounter.BlockTypeContext is AutonomyPlanBlockType.Work or AutonomyPlanBlockType.Errand)
            score += 18;
        if (encounter.BlockEndTime > 0)
        {
            var remainingMinutes = ConvertTimeToMinutes(encounter.BlockEndTime) - ConvertTimeToMinutes(Game1.timeOfDay);
            if (remainingMinutes <= 20)
                score += 20;
            else if (remainingMinutes <= 45)
                score += 10;
        }

        score += anger / 7;
        score += awkwardness / 5;
        score += encounter.TurnsCompleted > encounter.TurnBudgetSoftCap ? 10 : 0;
        return Math.Clamp(score, 0, 100);
    }

    private static float ResolveMomentumDelta(ActiveEncounter encounter, string moveType)
    {
        return moveType switch
        {
            "share" => 0.08f,
            "reassure" => 0.06f,
            "gossip" => 0.05f,
            "ask" => 0.03f,
            "pause" => -0.07f,
            "disagree" => -0.10f,
            "exit_signal" => -0.20f,
            _ => 0f
        };
    }

    private static string ResolveCurrentToneAfterMove(ActiveEncounter encounter, string moveType)
    {
        if (moveType == "disagree")
            return "tense";
        if (moveType == "pause" && encounter.CurrentTone != "tense")
            return "awkward";
        if (moveType is "share" or "reassure" && encounter.CurrentMomentum >= 0.60f)
            return "warm";
        return encounter.CurrentTone;
    }

    private static string ResolveLiveArcOutcome(ActiveEncounter encounter)
    {
        if (encounter.CurrentTone == "warm" && encounter.TurnsCompleted >= encounter.TurnBudgetSoftCap)
            return "warm_long_talk";
        if (encounter.CurrentTone == "tense" && encounter.CurrentMomentum >= 0.50f)
            return "apology_softened";
        if (encounter.CurrentTone == "tense")
            return "unresolved_tension";
        if (encounter.CurrentTone == "awkward")
            return "awkward_but_polite";
        if (encounter.BlockTypeContext is AutonomyPlanBlockType.Work or AutonomyPlanBlockType.Errand)
            return "practical_exchange";
        return "friendly";
    }

    private int ResolveSoftTurnBudget(int friendship, int trust, int awkwardness, AutonomyPlanBlockType blockType, string speakerNpcId, string listenerNpcId)
    {
        var turns = _config.AutonomyMinimumConversationTurns;

        if (friendship + trust >= 80)
            turns += 2;
        if (awkwardness >= 30)
            turns -= 1;
        if (blockType == AutonomyPlanBlockType.VisitNpc)
            turns += 1;
        if (IsDutyFirstNpc(speakerNpcId) || IsDutyFirstNpc(listenerNpcId))
            turns -= 1;

        return Math.Clamp(turns, _config.AutonomyMinimumConversationTurns, _config.AutonomyMaximumConversationTurns);
    }

    private static string ResolvePurpose(AutonomyPlanBlock block, int friendship, int tension)
    {
        return block.Type switch
        {
            AutonomyPlanBlockType.VisitNpc when tension >= 35 => "conflict",
            AutonomyPlanBlockType.VisitNpc when friendship >= 25 => "visit",
            AutonomyPlanBlockType.VisitNpc => "check_in",
            AutonomyPlanBlockType.Errand => "work_coordination",
            AutonomyPlanBlockType.Work => "work_coordination",
            AutonomyPlanBlockType.Socialize => "check_in",
            _ => tension >= 30 ? "awkward_encounter" : "check_in"
        };
    }

    private static string ResolveTone(int friendship, int trust, int anger, int awkwardness, int jealousy)
    {
        if (anger >= 35 || jealousy >= 35)
            return "tense";
        if (awkwardness >= 30)
            return "awkward";
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
        if (toneAtStart == "tense")
            return "unresolved_tension";
        if (toneAtStart == "awkward")
            return "awkward_but_polite";
        return "friendly";
    }

    private static string ResolveRole(NPC npc)
    {
        var name = npc.Name ?? string.Empty;
        var location = npc.currentLocation?.Name ?? npc.DefaultMap ?? string.Empty;
        if (name is "Pierre" or "Morris" || location.Contains("Shop", StringComparison.OrdinalIgnoreCase) || location.Contains("Joja", StringComparison.OrdinalIgnoreCase))
            return "shopkeeper";
        if (name is "Harvey")
            return "doctor";
        if (name is "Robin" or "Clint")
            return "craftsman";
        if (name is "Lewis")
            return "official";
        return "resident";
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

    private static string ResolveDisplayName(NPC npc)
    {
        return string.IsNullOrWhiteSpace(npc.displayName) ? npc.Name : npc.displayName;
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
