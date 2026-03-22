using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcAutonomyGoalEngine
{
    private readonly DestinationRegistryService _destinationRegistryService;
    private readonly NpcResidenceService _npcResidenceService;
    private readonly PairEmotionService _pairEmotionService;
    private readonly ModConfig _config;

    public NpcAutonomyGoalEngine(
        DestinationRegistryService destinationRegistryService,
        NpcResidenceService npcResidenceService,
        PairEmotionService pairEmotionService,
        ModConfig config)
    {
        _destinationRegistryService = destinationRegistryService;
        _npcResidenceService = npcResidenceService;
        _pairEmotionService = pairEmotionService;
        _config = config;
    }

    public NpcContextSnapshot BuildSnapshot(SaveState state, NPC npc, IEnumerable<NPC> nearbyNpcs)
    {
        var nearbyIds = nearbyNpcs
            .Where(other => other is not null && !string.Equals(other.Name, npc.Name, StringComparison.OrdinalIgnoreCase))
            .Select(other => other.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allWorldIds = Utility.getAllCharacters()
            .Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Name)
                        && !string.Equals(c.Name, npc.Name, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new NpcContextSnapshot
        {
            NpcId = npc.Name ?? string.Empty,
            CurrentLocation = npc.currentLocation?.Name ?? Game1.currentLocation?.Name ?? "Town",
            HomeLocation = _npcResidenceService.ResolveHomeLocation(npc.Name ?? string.Empty, Game1.locations, npc.DefaultMap ?? "Town"),
            TimeOfDay = Game1.timeOfDay,
            Season = state.Calendar.Season,
            IsRaining = Game1.isRaining,
            IsFestivalDay = Game1.eventUp,
            RecentTownEventCount = state.TownMemory.Events.Count(ev => state.Calendar.Day - ev.Day <= 2),
            NearbyNpcIds = nearbyIds,
            AllWorldNpcIds = allWorldIds
        };
    }

    public List<ScoredAutonomyGoal> ScoreCandidateGoals(SaveState state, NPC npc, NpcContextSnapshot snapshot, IEnumerable<GameLocation> worldLocations)
    {
        var goals = new List<ScoredAutonomyGoal>();
        var modeMultiplier = ResolveModeMultiplier(state.Config.Mode);
        var locations = worldLocations.ToList();
        var currentDay = state.Calendar.Day;
        var currentNpcId = npc.Name ?? string.Empty;

        goals.Add(new ScoredAutonomyGoal
        {
            GoalType = "wander",
            TargetLocation = _destinationRegistryService.ResolveFallbackLocation(snapshot.CurrentLocation, locations).LocationId,
            Reason = "stretch legs and take in the town",
            Score = 0.20f * modeMultiplier
        });

        goals.Add(new ScoredAutonomyGoal
        {
            GoalType = "rest",
            TargetLocation = string.IsNullOrWhiteSpace(snapshot.HomeLocation) ? snapshot.CurrentLocation : snapshot.HomeLocation,
            Reason = "take a quiet moment",
            Score = (snapshot.IsRaining ? 0.32f : 0.18f) * modeMultiplier
        });

        foreach (var nearbyNpcId in snapshot.NearbyNpcIds)
        {
            if (TryBuildVisitGoal(state, currentNpcId, nearbyNpcId, snapshot, locations, isNearby: true, modeMultiplier, currentDay, out var goal))
                goals.Add(goal);
        }

        // Cross-map visit_npc goals (G15): score ALL world NPCs with travel distance penalty
        var nearbySet = new HashSet<string>(snapshot.NearbyNpcIds, StringComparer.OrdinalIgnoreCase);
        foreach (var worldNpcId in snapshot.AllWorldNpcIds)
        {
            if (nearbySet.Contains(worldNpcId))
                continue;

            if (TryBuildVisitGoal(state, currentNpcId, worldNpcId, snapshot, locations, isNearby: false, modeMultiplier, currentDay, out var goal))
                goals.Add(goal);
        }

        foreach (var location in _destinationRegistryService.BuildLocations(locations))
        {
            if (location.LocationId.Equals(snapshot.CurrentLocation, StringComparison.OrdinalIgnoreCase))
                continue;

            if (location.RoleTags.Contains("square", StringComparer.OrdinalIgnoreCase)
                || location.RoleTags.Contains("saloon", StringComparer.OrdinalIgnoreCase)
                || location.RoleTags.Contains("nature", StringComparer.OrdinalIgnoreCase))
            {
                goals.Add(new ScoredAutonomyGoal
                {
                    GoalType = "socialize",
                    TargetLocation = location.LocationId,
                    Reason = "wander toward a likely meeting place",
                    Score = 0.12f * modeMultiplier
                });
            }
        }

        return goals
            .OrderByDescending(goal => goal.Score)
            .ThenBy(goal => goal.GoalType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(goal => goal.TargetNpcId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(goal => goal.TargetLocation, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private float ResolveModeMultiplier(string? mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "living_chaos" => _config.AutonomyIntensityChaos,
            "story_depth" => _config.AutonomyIntensityStory,
            _ => _config.AutonomyIntensityCozy
        };
    }

    private static int GetAxis(NpcPairEmotionEntry pair, string axis)
    {
        return pair.EmotionAxes.TryGetValue(axis, out var value) ? value : 0;
    }

    private bool TryBuildVisitGoal(
        SaveState state,
        string npcId,
        string targetNpcId,
        NpcContextSnapshot snapshot,
        List<GameLocation> worldLocations,
        bool isNearby,
        float modeMultiplier,
        int currentDay,
        out ScoredAutonomyGoal goal)
    {
        goal = new ScoredAutonomyGoal();
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(targetNpcId))
            return false;

        var pair = _pairEmotionService.GetOrCreate(state, npcId, targetNpcId);
        var friendship = GetAxis(pair, "friendship");
        var trust = GetAxis(pair, "trust");
        var anger = GetAxis(pair, "anger");
        var jealousy = GetAxis(pair, "jealousy");
        var familiarity = pair.Familiarity;
        var avoidance = pair.Avoidance;
        var tension = pair.Tension;
        var hasGrudge = pair.ActiveFlags.Contains("grudge", StringComparer.OrdinalIgnoreCase);
        var frequentVisitors = pair.ActiveFlags.Contains("frequent_visitors", StringComparer.OrdinalIgnoreCase);
        var daysSinceLastInteraction = pair.LastInteractionDay <= 0
            ? 99
            : Math.Max(0, currentDay - pair.LastInteractionDay);
        var locationId = ResolveNpcLocation(targetNpcId, worldLocations, snapshot.CurrentLocation);

        if (string.IsNullOrWhiteSpace(locationId))
            return false;

        var pairKey = PairEmotionService.BuildPairKey(npcId, targetNpcId);
        var pairCooldownPenalty = ResolvePairCooldownPenalty(state, pairKey, currentDay);
        var locationCooldownPenalty = ResolveLocationCooldownPenalty(state, npcId, locationId, currentDay);
        var score = 0.08f;
        score += friendship / 240f;
        score += trust / 320f;
        score += Math.Min(familiarity, 50) / 500f;
        score += frequentVisitors ? 0.05f : 0f;
        score += isNearby ? 0.08f : -0.02f;
        score += daysSinceLastInteraction switch
        {
            <= 1 => 0.06f,
            <= 3 => 0.03f,
            >= 10 when friendship + trust >= 50 => 0.04f,
            _ => 0f
        };
        score -= tension / 360f;
        score -= anger / 320f;
        score -= jealousy / 420f;
        score -= avoidance / 220f;
        score -= hasGrudge ? 0.14f : 0f;
        score -= pairCooldownPenalty;
        score -= locationCooldownPenalty;

        var isStrongNegativePair = hasGrudge || avoidance >= 45 || tension >= 55 || anger >= 45;
        var publicFallback = _destinationRegistryService.ResolveFallbackLocation(locationId, worldLocations);
        if (_destinationRegistryService.TryResolveVisitLocation(
                state,
                npcId,
                targetNpcId,
                snapshot.TimeOfDay,
                worldLocations,
                out var visitLocation,
                out var reasonCode))
        {
            state.Telemetry.Daily.HomeVisitsAllowed += 1;
            locationId = visitLocation.LocationId;
        }
        else
        {
            state.Telemetry.Daily.HomeVisitsDenied += 1;
            IncrementCounter(state.Telemetry.Daily.VisitDenialByReason, reasonCode);
            if (!_config.AutonomyPrivateVisitFallbackToPublic || isStrongNegativePair || string.IsNullOrWhiteSpace(publicFallback.LocationId))
            {
                IncrementCounter(state.Telemetry.Daily.AutonomyRejectByReason, reasonCode);
                return false;
            }

            locationId = publicFallback.LocationId;
            score -= 0.08f;
            goal = new ScoredAutonomyGoal
            {
                GoalType = "visit_npc",
                TargetNpcId = targetNpcId,
                TargetLocation = locationId,
                Reason = hasGrudge ? "circle a neutral place before deciding whether to engage" : "look for them somewhere public first",
                RejectReasonCode = reasonCode,
                RequiresPublicFallback = true,
                Score = Math.Clamp(score * modeMultiplier, 0f, 1f)
            };
            return goal.Score >= _config.AutonomyEncounterScoreThreshold;
        }

        if (score < _config.AutonomyEncounterScoreThreshold)
            return false;

        goal = new ScoredAutonomyGoal
        {
            GoalType = "visit_npc",
            TargetNpcId = targetNpcId,
            TargetLocation = locationId,
            Reason = ResolveVisitReason(friendship, trust, tension, avoidance, hasGrudge, isNearby),
            Score = Math.Clamp(score * modeMultiplier, 0f, 1f)
        };
        return true;
    }

    private static float ResolvePairCooldownPenalty(SaveState state, string pairKey, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(pairKey) || !state.Autonomy.VisitCooldownByPairKey.TryGetValue(pairKey, out var lastDay))
            return 0f;

        var daysElapsed = Math.Max(0, currentDay - lastDay);
        return daysElapsed switch
        {
            0 => 0.30f,
            1 => 0.12f,
            _ => 0f
        };
    }

    private static float ResolveLocationCooldownPenalty(SaveState state, string npcId, string locationId, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(locationId))
            return 0f;

        var key = $"{npcId}|{locationId}".ToLowerInvariant();
        if (!state.Autonomy.LocationCooldownByNpcKey.TryGetValue(key, out var lastDay))
            return 0f;

        var daysElapsed = Math.Max(0, currentDay - lastDay);
        return daysElapsed switch
        {
            0 => 0.18f,
            1 => 0.06f,
            _ => 0f
        };
    }

    private static string ResolveVisitReason(int friendship, int trust, int tension, int avoidance, bool hasGrudge, bool isNearby)
    {
        if (hasGrudge || tension >= 45)
            return isNearby ? "press on a tense thread before it hardens" : "go settle unfinished tension in person";
        if (avoidance >= 35)
            return "test whether a careful check-in is still possible";
        if (friendship + trust >= 85)
            return isNearby ? "make time for someone they genuinely enjoy" : "cross town to spend time with someone important";
        if (friendship + trust >= 45)
            return "check in on someone familiar";
        return "see if a small social visit leads anywhere";
    }

    private static void IncrementCounter(Dictionary<string, int> counters, string key)
    {
        var normalizedKey = string.IsNullOrWhiteSpace(key) ? "(unknown)" : key.Trim().ToLowerInvariant();
        counters.TryGetValue(normalizedKey, out var count);
        counters[normalizedKey] = count + 1;
    }

    private string ResolveNpcLocation(string npcId, IEnumerable<GameLocation> worldLocations, string fallbackLocation)
    {
        var targetNpc = Game1.getCharacterFromName(npcId);
        if (targetNpc?.currentLocation is not null && !string.IsNullOrWhiteSpace(targetNpc.currentLocation.Name))
            return targetNpc.currentLocation.Name;

        return _npcResidenceService.ResolveHomeLocation(npcId, worldLocations, fallbackLocation);
    }
}
