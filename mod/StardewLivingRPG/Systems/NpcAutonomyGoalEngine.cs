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

        goals.Add(new ScoredAutonomyGoal
        {
            GoalType = "wander",
            TargetLocation = _destinationRegistryService.ResolveFallbackLocation(snapshot.CurrentLocation, worldLocations).LocationId,
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
            var pair = _pairEmotionService.GetOrCreate(state, npc.Name, nearbyNpcId);
            var friendship = GetAxis(pair, "friendship");
            var trust = GetAxis(pair, "trust");
            var tension = pair.Tension;
            var score = 0.18f + ((friendship + trust) / 300f) - (tension / 400f);
            if (score <= 0f)
                continue;

            goals.Add(new ScoredAutonomyGoal
            {
                GoalType = "visit_npc",
                TargetNpcId = nearbyNpcId,
                TargetLocation = ResolveNpcLocation(nearbyNpcId, worldLocations, snapshot.CurrentLocation),
                Reason = friendship >= tension ? "check in on someone familiar" : "resolve lingering tension nearby",
                Score = score * modeMultiplier
            });
        }

        // Cross-map visit_npc goals (G15): score ALL world NPCs with travel distance penalty
        var nearbySet = new HashSet<string>(snapshot.NearbyNpcIds, StringComparer.OrdinalIgnoreCase);
        foreach (var worldNpcId in snapshot.AllWorldNpcIds)
        {
            if (nearbySet.Contains(worldNpcId))
                continue; // Already scored above with higher proximity bonus

            var pair = _pairEmotionService.GetOrCreate(state, npc.Name, worldNpcId);
            var friendship = GetAxis(pair, "friendship");
            var trust = GetAxis(pair, "trust");
            var tension = pair.Tension;

            var motivation = 0.15f + ((friendship + trust) / 250f) - (tension / 400f);

            // Travel distance penalty: estimate using DefaultMap resolution
            var targetLocation = ResolveNpcLocation(worldNpcId, worldLocations, snapshot.CurrentLocation);
            if (string.IsNullOrWhiteSpace(targetLocation))
                continue;

            // Cross-map penalty: -0.30 base for distant NPCs (refined by route planner later)
            motivation -= 0.10f;

            // Same-map adjacency bonus already handled in nearby loop
            if (motivation <= 0.05f)
                continue;

            goals.Add(new ScoredAutonomyGoal
            {
                GoalType = "visit_npc",
                TargetNpcId = worldNpcId,
                TargetLocation = targetLocation,
                Reason = friendship >= tension ? "feel drawn to visit a friend across town" : "need to address something with a distant acquaintance",
                Score = Math.Clamp(motivation * modeMultiplier, 0f, 1f)
            });
        }

        foreach (var location in _destinationRegistryService.BuildLocations(worldLocations))
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
            .ToList();
    }

    public bool TryValidateSuggestion(
        SaveState state,
        string npcId,
        AutonomyGoalSuggestion suggestion,
        IEnumerable<GameLocation> worldLocations,
        out ScoredAutonomyGoal goal,
        out string reasonCode)
    {
        goal = new ScoredAutonomyGoal();
        reasonCode = "ok";

        if (string.IsNullOrWhiteSpace(suggestion.GoalType))
        {
            reasonCode = "goal_missing";
            return false;
        }

        if (suggestion.Urgency < 0f || suggestion.Urgency > 1f)
        {
            reasonCode = "urgency_out_of_range";
            return false;
        }

        var cappedUrgency = Math.Min(suggestion.Urgency, _config.Player2GoalMaxUrgencyInfluence);
        var targetLocation = suggestion.TargetLocation;

        if (suggestion.GoalType.Equals("visit_npc", StringComparison.OrdinalIgnoreCase))
        {
            if (!_destinationRegistryService.TryResolveVisitLocation(
                    state,
                    npcId,
                    suggestion.TargetNpcId,
                    Game1.timeOfDay,
                    worldLocations,
                    out var visitLocation,
                    out reasonCode))
            {
                return false;
            }

            targetLocation = visitLocation.LocationId;
        }

        goal = new ScoredAutonomyGoal
        {
            GoalType = suggestion.GoalType.Trim().ToLowerInvariant(),
            TargetNpcId = suggestion.TargetNpcId,
            TargetLocation = targetLocation,
            Reason = string.IsNullOrWhiteSpace(suggestion.Reason) ? "follow a sudden impulse" : suggestion.Reason.Trim(),
            Score = Math.Clamp(0.10f + cappedUrgency, 0.10f, 0.90f)
        };
        return true;
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

    private string ResolveNpcLocation(string npcId, IEnumerable<GameLocation> worldLocations, string fallbackLocation)
    {
        var targetNpc = Game1.getCharacterFromName(npcId);
        if (targetNpc?.currentLocation is not null && !string.IsNullOrWhiteSpace(targetNpc.currentLocation.Name))
            return targetNpc.currentLocation.Name;

        return _npcResidenceService.ResolveHomeLocation(npcId, worldLocations, fallbackLocation);
    }
}
