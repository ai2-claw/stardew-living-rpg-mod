using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcAutonomyPlannerService
{
    private readonly DestinationRegistryService _destinationRegistryService;

    public NpcAutonomyPlannerService(DestinationRegistryService destinationRegistryService)
    {
        _destinationRegistryService = destinationRegistryService;
    }

    public NpcDailyPlan? SynthesizeDailyPlan(
        SaveState state,
        string npcId,
        NpcContextSnapshot snapshot,
        IReadOnlyList<ScoredAutonomyGoal> goals,
        ModConfig config)
    {
        if (snapshot.IsFestivalDay || string.IsNullOrWhiteSpace(npcId))
            return null;

        var blocks = new List<AutonomyPlanBlock>();
        var nextStart = Math.Max(900, snapshot.TimeOfDay);
        var blockCount = 0;

        foreach (var goal in goals)
        {
            if (blockCount >= config.AutonomyMaxBlocksPerDay)
                break;

            var duration = goal.GoalType.Equals("visit_npc", StringComparison.OrdinalIgnoreCase) ? 90 : 60;
            var endTime = AddMinutes(nextStart, duration);
            if (endTime >= 2400)
                break;

            blocks.Add(new AutonomyPlanBlock
            {
                BlockId = $"{npcId}:{goal.GoalType}:{blockCount + 1}",
                Type = ResolveBlockType(goal.GoalType),
                TargetNpcId = goal.TargetNpcId,
                TargetLocation = string.IsNullOrWhiteSpace(goal.TargetLocation) ? snapshot.CurrentLocation : goal.TargetLocation,
                Reason = goal.Reason,
                StartTime = nextStart,
                EndTime = endTime
            });

            nextStart = AddMinutes(endTime, 20);
            blockCount += 1;
        }

        if (blocks.Count == 0)
        {
            var fallback = _destinationRegistryService.ResolveFallbackLocation(snapshot.CurrentLocation, Game1.locations);
            blocks.Add(new AutonomyPlanBlock
            {
                BlockId = $"{npcId}:wander:1",
                Type = AutonomyPlanBlockType.Wander,
                TargetLocation = fallback.LocationId,
                Reason = "keep moving through a familiar place",
                StartTime = Math.Max(900, snapshot.TimeOfDay),
                EndTime = AddMinutes(Math.Max(900, snapshot.TimeOfDay), 60)
            });
        }

        blocks.Add(new AutonomyPlanBlock
        {
            BlockId = $"{npcId}:return_home",
            Type = AutonomyPlanBlockType.ReturnHome,
            TargetLocation = snapshot.CurrentLocation,
            Reason = "head home before the day fully winds down",
            StartTime = Math.Max(nextStart, 2100),
            EndTime = 2400
        });

        return new NpcDailyPlan
        {
            NpcId = npcId,
            Day = state.Calendar.Day,
            Blocks = blocks
        };
    }

    public AutonomyPlanBlock? AdvancePlan(AutonomyRuntimeState runtime, int timeOfDay)
    {
        if (runtime.ActivePlan is null || runtime.ActivePlan.Blocks.Count == 0)
        {
            runtime.OverrideStatus = AutonomyOverrideStatus.Idle;
            return null;
        }

        if (runtime.ActiveBlockIndex < 0)
            runtime.ActiveBlockIndex = 0;

        while (runtime.ActiveBlockIndex < runtime.ActivePlan.Blocks.Count)
        {
            var block = runtime.ActivePlan.Blocks[runtime.ActiveBlockIndex];
            if (timeOfDay > block.EndTime && block.Status is not (AutonomyPlanBlockStatus.Completed or AutonomyPlanBlockStatus.Cancelled))
            {
                block.Status = AutonomyPlanBlockStatus.Completed;
                runtime.CompletedBlocksToday += 1;
                runtime.ActiveBlockIndex += 1;
                continue;
            }

            if (timeOfDay < block.StartTime)
            {
                runtime.OverrideStatus = AutonomyOverrideStatus.Planned;
                return block;
            }

            if (block.Status == AutonomyPlanBlockStatus.Pending)
                block.Status = AutonomyPlanBlockStatus.Active;

            runtime.OverrideStatus = AutonomyOverrideStatus.Active;
            runtime.CurrentTargetNpcId = block.TargetNpcId;
            runtime.CurrentTargetLocation = block.TargetLocation;
            runtime.LastProgressUtc = DateTime.UtcNow;
            return block;
        }

        runtime.OverrideStatus = AutonomyOverrideStatus.CoolingDown;
        return null;
    }

    public void ReplanWithFallback(
        SaveState state,
        AutonomyRuntimeState runtime,
        string currentLocation,
        string reason)
    {
        if (runtime.ActivePlan is null)
            return;

        runtime.ReplanCount += 1;
        runtime.FailedBlocksToday += 1;

        if (runtime.ActiveBlockIndex >= 0 && runtime.ActiveBlockIndex < runtime.ActivePlan.Blocks.Count)
            runtime.ActivePlan.Blocks[runtime.ActiveBlockIndex].Status = AutonomyPlanBlockStatus.Failed;

        var fallback = _destinationRegistryService.ResolveFallbackLocation(currentLocation, Game1.locations);
        runtime.ActivePlan.Blocks.Add(new AutonomyPlanBlock
        {
            BlockId = $"{runtime.NpcId}:fallback:{runtime.ReplanCount}",
            Type = AutonomyPlanBlockType.Wander,
            TargetLocation = fallback.LocationId,
            Reason = string.IsNullOrWhiteSpace(reason) ? "recover after a failed plan" : reason,
            StartTime = Game1.timeOfDay,
            EndTime = AddMinutes(Game1.timeOfDay, 60)
        });
    }

    private static AutonomyPlanBlockType ResolveBlockType(string goalType)
    {
        return goalType switch
        {
            "visit_npc" => AutonomyPlanBlockType.VisitNpc,
            "socialize" => AutonomyPlanBlockType.Socialize,
            "wander" => AutonomyPlanBlockType.Wander,
            "errand" => AutonomyPlanBlockType.Errand,
            "observe_event" => AutonomyPlanBlockType.ObserveEvent,
            _ => AutonomyPlanBlockType.Rest
        };
    }

    private static int AddMinutes(int timeOfDay, int minutes)
    {
        var hours = timeOfDay / 100;
        var mins = timeOfDay % 100;
        mins += minutes;
        hours += mins / 60;
        mins %= 60;
        return (hours * 100) + mins;
    }
}
