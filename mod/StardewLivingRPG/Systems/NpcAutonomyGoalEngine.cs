using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcAutonomyGoalEngine
{
    private readonly DestinationRegistryService _destinationRegistryService;
    private readonly PairEmotionService _pairEmotionService;
    private readonly ModConfig _config;

    public NpcAutonomyGoalEngine(
        DestinationRegistryService destinationRegistryService,
        PairEmotionService pairEmotionService,
        ModConfig config)
    {
        _destinationRegistryService = destinationRegistryService;
        _pairEmotionService = pairEmotionService;
        _config = config;
    }

    public NpcContextSnapshot BuildSnapshot(SaveState state, NPC npc, IEnumerable<NPC> nearbyNpcs)
    {
        return new NpcContextSnapshot
        {
            NpcId = npc.Name ?? string.Empty,
            CurrentLocation = npc.currentLocation?.Name ?? Game1.currentLocation?.Name ?? "Town",
            TimeOfDay = Game1.timeOfDay,
            Season = state.Calendar.Season,
            IsRaining = Game1.isRaining,
            IsFestivalDay = Game1.eventUp,
            RecentTownEventCount = state.TownMemory.Events.Count(ev => state.Calendar.Day - ev.Day <= 2),
            NearbyNpcIds = nearbyNpcs
                .Where(other => other is not null && !string.Equals(other.Name, npc.Name, StringComparison.OrdinalIgnoreCase))
                .Select(other => other.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    public List<ScoredAutonomyGoal> ScoreCandidateGoals(SaveState state, NPC npc, NpcContextSnapshot snapshot, IEnumerable<GameLocation> worldLocations)
    {
        var goals = new List<ScoredAutonomyGoal>();
        var modeMultiplier = ResolveModeMultiplier(state.Config.Mode);

        goals.Add(new ScoredAutonomyGoal
        {
            GoalType = "wander",
            TargetLocation = snapshot.CurrentLocation,
            Reason = "stretch legs and take in the town",
            Score = 0.20f * modeMultiplier
        });

        goals.Add(new ScoredAutonomyGoal
        {
            GoalType = "rest",
            TargetLocation = snapshot.CurrentLocation,
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
                TargetLocation = snapshot.CurrentLocation,
                Reason = friendship >= tension ? "check in on someone familiar" : "resolve lingering tension nearby",
                Score = score * modeMultiplier
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
}
