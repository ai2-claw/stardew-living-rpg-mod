using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcSocialEncounterService
{
    private readonly PairEmotionService _pairEmotionService;
    private readonly ModConfig _config;
    private readonly Dictionary<string, ActiveEncounter> _activeEncounters = new(StringComparer.OrdinalIgnoreCase);
    private int _nextEncounterId;

    public NpcSocialEncounterService(PairEmotionService pairEmotionService, ModConfig config)
    {
        _pairEmotionService = pairEmotionService;
        _config = config;
    }

    public float ScoreEncounter(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock? block)
    {
        if (speaker is null || listener is null)
            return 0f;
        if (speaker.currentLocation is null || listener.currentLocation is null)
            return 0f;
        if (!string.Equals(speaker.currentLocation.Name, listener.currentLocation.Name, StringComparison.OrdinalIgnoreCase))
            return 0f;

        var pair = _pairEmotionService.GetOrCreate(state, speaker.Name, listener.Name);
        var friendship = TryGetAxis(pair, "friendship");
        var trust = TryGetAxis(pair, "trust");
        var jealousy = TryGetAxis(pair, "jealousy");
        var anger = TryGetAxis(pair, "anger");

        var score = 0.15f;
        score += friendship / 250f;
        score += trust / 300f;
        score -= jealousy / 350f;
        score -= anger / 300f;

        if (block?.Type == AutonomyPlanBlockType.VisitNpc)
            score += 0.25f;
        else if (block?.Type == AutonomyPlanBlockType.Socialize)
            score += 0.12f;
        else if (block?.Type is AutonomyPlanBlockType.ReturnHome or AutonomyPlanBlockType.Rest or AutonomyPlanBlockType.Wander)
            score += 0.10f;

        if (speaker.currentLocation.IsOutdoors)
            score += 0.05f;
        else
            score += 0.08f;

        var proximity = Math.Abs(speaker.Tile.X - listener.Tile.X) + Math.Abs(speaker.Tile.Y - listener.Tile.Y);
        score += Math.Max(0f, (5f - proximity) / 25f);

        // Event awareness bonus
        if (pair.Tension >= 30)
            score += 0.08f;

        // Location fit
        var locationName = speaker.currentLocation.Name ?? string.Empty;
        if (locationName.Contains("Saloon", StringComparison.OrdinalIgnoreCase))
            score += 0.06f;
        else if (locationName.Contains("House", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Shop", StringComparison.OrdinalIgnoreCase))
        {
            score += 0.08f;
        }

        return Math.Clamp(score, 0f, 1f);
    }

    public float ScoreEncounterByNames(SaveState state, string npcA, string npcB, string locationId, EncounterSource source)
    {
        var pair = _pairEmotionService.GetOrCreate(state, npcA, npcB);
        var friendship = TryGetAxis(pair, "friendship");
        var trust = TryGetAxis(pair, "trust");
        var tension = pair.Tension;

        var salience = (Math.Abs(pair.Affinity) / 400f) + (tension / 500f) + (pair.Familiarity / 500f);
        var sourceBonus = source switch
        {
            EncounterSource.PlannedVisit => 0.25f,
            EncounterSource.EventConvergence => 0.15f,
            _ => 0.05f
        };

        return Math.Clamp(salience + sourceBonus + 0.10f, 0f, 1f);
    }

    public bool ShouldStartEncounter(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock? block)
    {
        if (speaker.currentLocation is null
            || listener.currentLocation is null
            || !string.Equals(speaker.currentLocation.Name, listener.currentLocation.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var dx = Math.Abs(speaker.Tile.X - listener.Tile.X);
        var dy = Math.Abs(speaker.Tile.Y - listener.Tile.Y);
        if (dx + dy > 5f)
            return false;

        var score = ScoreEncounter(state, speaker, listener, block);
        return score >= _config.AutonomyEncounterScoreThreshold;
    }

    public bool IsNpcInActiveEncounter(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return false;

        return _activeEncounters.Values.Any(encounter =>
            encounter.Phase is not (EncounterPhase.Complete or EncounterPhase.Cancelled)
            && (string.Equals(encounter.NpcA, npcId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(encounter.NpcB, npcId, StringComparison.OrdinalIgnoreCase)));
    }

    public void MarkStaging(string encounterId)
    {
        if (_activeEncounters.TryGetValue(encounterId, out var encounter))
            encounter.Phase = EncounterPhase.Staging;
    }

    public void MarkTalking(string encounterId)
    {
        if (_activeEncounters.TryGetValue(encounterId, out var encounter))
            encounter.Phase = EncounterPhase.Talking;
    }

    public ActiveEncounter? EvaluatePlannedVisit(
        SaveState state,
        AutonomyRuntimeState visitorRuntime,
        string hostNpcId,
        string locationId)
    {
        var score = ScoreEncounterByNames(state, visitorRuntime.NpcId, hostNpcId, locationId, EncounterSource.PlannedVisit);
        if (score < _config.AutonomyEncounterScoreThreshold)
            return null;

        return CreateEncounter(visitorRuntime.NpcId, hostNpcId, locationId, EncounterSource.PlannedVisit, score);
    }

    public ActiveEncounter? EvaluateOpportunistic(
        SaveState state,
        string npcA,
        string npcB,
        string locationId)
    {
        var score = ScoreEncounterByNames(state, npcA, npcB, locationId, EncounterSource.Opportunistic);
        if (score < _config.AutonomyEncounterScoreThreshold)
            return null;

        return CreateEncounter(npcA, npcB, locationId, EncounterSource.Opportunistic, score);
    }

    public void ProcessConsequences(
        SaveState state,
        ActiveEncounter encounter,
        string conversationTone)
    {
        if (encounter.Phase == EncounterPhase.Cancelled)
            return;

        encounter.Phase = EncounterPhase.Consequences;

        var outcome = string.IsNullOrWhiteSpace(encounter.ArcOutcome) ? conversationTone : encounter.ArcOutcome;
        var adjustments = outcome switch
        {
            "warm_long_talk" => new[] { ("friendship", 3), ("trust", 2) },
            "apology_softened" => new[] { ("trust", 2), ("awkwardness", -1) },
            "awkward_but_polite" => new[] { ("awkwardness", 1), ("trust", 1) },
            "practical_exchange" => new[] { ("trust", 2) },
            "unresolved_tension" => new[] { ("anger", 2), ("tension_adjust", 2), ("awkwardness", 1) },
            "friendly" => new[] { ("friendship", 2), ("trust", 1) },
            "tense" => new[] { ("anger", 2), ("tension_adjust", 2) },
            "hostile" => new[] { ("anger", 4), ("tension_adjust", 3) },
            _ => new[] { ("trust", 1) }
        };

        foreach (var (axis, delta) in adjustments)
        {
            if (axis == "tension_adjust")
                continue;
            _pairEmotionService.TryAdjustAxis(state, encounter.NpcA, encounter.NpcB, axis, delta, out _);
        }

        encounter.Phase = EncounterPhase.Complete;
        encounter.CompletedUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<ActiveEncounter> GetActiveEncounters()
    {
        return _activeEncounters.Values.Where(e => e.Phase is not (EncounterPhase.Complete or EncounterPhase.Cancelled)).ToList();
    }

    public ActiveEncounter? GetEncounter(string encounterId)
    {
        return _activeEncounters.TryGetValue(encounterId, out var enc) ? enc : null;
    }

    public void CancelEncounter(string encounterId, string reason)
    {
        if (_activeEncounters.TryGetValue(encounterId, out var encounter))
        {
            encounter.Phase = EncounterPhase.Cancelled;
            encounter.CompletedUtc = DateTime.UtcNow;
        }
    }

    public void ClearCompleted()
    {
        foreach (var key in _activeEncounters.Keys.ToArray())
        {
            if (_activeEncounters[key].Phase is EncounterPhase.Complete or EncounterPhase.Cancelled)
                _activeEncounters.Remove(key);
        }
    }

    private ActiveEncounter CreateEncounter(string npcA, string npcB, string locationId, EncounterSource source, float score)
    {
        var id = $"enc_{++_nextEncounterId}";
        var encounter = new ActiveEncounter
        {
            EncounterId = id,
            NpcA = npcA,
            NpcB = npcB,
            LocationId = locationId,
            Source = source,
            Score = score,
            TurnDepth = source == EncounterSource.PlannedVisit ? 3 : 2,
            StartedUtc = DateTime.UtcNow
        };
        _activeEncounters[id] = encounter;
        return encounter;
    }

    private static int TryGetAxis(NpcPairEmotionEntry pair, string axis)
    {
        return pair.EmotionAxes.TryGetValue(axis, out var value) ? value : 0;
    }
}
