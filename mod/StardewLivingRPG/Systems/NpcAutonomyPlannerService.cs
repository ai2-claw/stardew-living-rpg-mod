using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;
using System.Reflection;

namespace StardewLivingRPG.Systems;

public sealed class NpcAutonomyPlannerService
{
    private readonly DestinationRegistryService _destinationRegistryService;
    private readonly NpcDutyRosterService _dutyRosterService;
    private readonly Random _random = new();

    private static readonly string[] ScheduleLocationMemberCandidates = { "targetLocationName", "TargetLocationName", "locationName", "LocationName", "location", "Location", "locationId", "LocationId" };
    private static readonly string[] ScheduleTileMemberCandidates = { "route", "Route", "endPoint", "EndPoint", "targetTile", "TargetTile", "tile", "Tile", "point", "Point" };

    public NpcAutonomyPlannerService(
        DestinationRegistryService destinationRegistryService,
        NpcDutyRosterService dutyRosterService)
    {
        _destinationRegistryService = destinationRegistryService;
        _dutyRosterService = dutyRosterService;
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

        var npc = Game1.getCharacterFromName(npcId);
        var anchors = BuildVanillaAnchorBlocks(npcId, npc, snapshot);
        if (anchors.Count == 0)
            anchors = _dutyRosterService.BuildRequiredDutyBlocks(npcId, Game1.locations).OrderBy(block => block.StartTime).ToList();

        var blocks = new List<AutonomyPlanBlock>();
        var remainingGoals = goals.ToList();
        var blockCount = 0;
        var previousEnd = Math.Max(900, snapshot.TimeOfDay);

        foreach (var anchor in anchors.OrderBy(block => block.StartTime))
        {
            FillDetourWindow(npcId, snapshot, remainingGoals, config, blocks, ref blockCount, previousEnd, anchor.StartTime);
            blocks.Add(anchor);
            previousEnd = Math.Max(previousEnd, anchor.EndTime);
        }

        FillDetourWindow(npcId, snapshot, remainingGoals, config, blocks, ref blockCount, previousEnd, 2100);

        if (ShouldAddReturnHome(snapshot, anchors))
        {
            blocks.Add(new AutonomyPlanBlock
            {
                BlockId = $"{npcId}:return_home",
                Type = AutonomyPlanBlockType.ReturnHome,
                TargetLocation = string.IsNullOrWhiteSpace(snapshot.HomeLocation) ? snapshot.CurrentLocation : snapshot.HomeLocation,
                TargetTile = Point.Zero,
                Reason = "head home before the day fully winds down",
                StartTime = Math.Max(previousEnd, 2100),
                EndTime = 2400
            });
        }

        if (blocks.Count == 0)
            return null;

        return new NpcDailyPlan
        {
            NpcId = npcId,
            Day = state.Calendar.Day,
            Blocks = blocks.OrderBy(block => block.StartTime).ToList()
        };
    }

    public AutonomyPlanBlock? AdvancePlan(AutonomyRuntimeState runtime, int timeOfDay, NPC? npc = null)
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

            if (block.Status == AutonomyPlanBlockStatus.Active)
            {
                var hasArrived = npc is not null
                    && npc.currentLocation is not null
                    && string.Equals(npc.currentLocation.Name, block.TargetLocation, StringComparison.OrdinalIgnoreCase)
                    && (!block.RequiresExactTile
                        || (block.TargetTile != Point.Zero
                            && Vector2.Distance(npc.Tile, new Vector2(block.TargetTile.X, block.TargetTile.Y)) <= 1.25f));

                var hardTimeout = AddMinutes(block.EndTime, 30);
                if ((hasArrived && timeOfDay >= block.EndTime) || timeOfDay > hardTimeout)
                {
                    block.Status = AutonomyPlanBlockStatus.Completed;
                    runtime.CompletedBlocksToday += 1;
                    runtime.ActiveBlockIndex += 1;
                    continue;
                }
            }
            else if (timeOfDay > block.EndTime && block.Status is not (AutonomyPlanBlockStatus.Completed or AutonomyPlanBlockStatus.Cancelled))
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

        var anchorRecovery = FindNextAnchor(runtime.ActivePlan, runtime.ActiveBlockIndex + 1);
        if (anchorRecovery is not null)
        {
            runtime.ActivePlan.Blocks.Insert(runtime.ActiveBlockIndex + 1, new AutonomyPlanBlock
            {
                BlockId = $"{runtime.NpcId}:return_to_anchor:{runtime.ReplanCount}",
                Type = anchorRecovery.Type == AutonomyPlanBlockType.RequiredDuty ? AutonomyPlanBlockType.RequiredDuty : AutonomyPlanBlockType.BaseAnchor,
                DutyRoleId = anchorRecovery.DutyRoleId,
                TargetLocation = anchorRecovery.TargetLocation,
                TargetTile = anchorRecovery.TargetTile,
                Reason = string.IsNullOrWhiteSpace(reason) ? "return to the scripted route" : $"return to scripted route after {reason}",
                StartTime = Game1.timeOfDay,
                EndTime = anchorRecovery.StartTime > Game1.timeOfDay ? anchorRecovery.StartTime : anchorRecovery.EndTime,
                RequiresExactTile = anchorRecovery.RequiresExactTile,
                TargetZoneId = anchorRecovery.TargetZoneId,
                TargetSpotRole = anchorRecovery.TargetSpotRole,
                TargetSpotId = anchorRecovery.TargetSpotId
            });
            return;
        }

        var fallback = _destinationRegistryService.ResolveFallbackLocation(currentLocation, Game1.locations);
        runtime.ActivePlan.Blocks.Add(new AutonomyPlanBlock
        {
            BlockId = $"{runtime.NpcId}:fallback:{runtime.ReplanCount}",
            Type = AutonomyPlanBlockType.Wander,
            TargetLocation = fallback.LocationId,
            TargetTile = Point.Zero,
            Reason = string.IsNullOrWhiteSpace(reason) ? "recover after a failed detour" : reason,
            StartTime = Game1.timeOfDay,
            EndTime = AddMinutes(Game1.timeOfDay, 20)
        });
    }

    private List<AutonomyPlanBlock> BuildVanillaAnchorBlocks(string npcId, NPC? npc, NpcContextSnapshot snapshot)
    {
        var blocks = new List<AutonomyPlanBlock>();
        if (npc?.Schedule is null || npc.Schedule.Count == 0)
            return blocks;

        var orderedEntries = npc.Schedule
            .OrderBy(entry => entry.Key)
            .Where(entry => entry.Key >= Math.Max(600, snapshot.TimeOfDay - 100))
            .ToList();
        for (var i = 0; i < orderedEntries.Count; i++)
        {
            var current = orderedEntries[i];
            if (!TryReadScheduleLocation(current.Value, out var locationId))
                continue;

            var startTime = Math.Max(current.Key, snapshot.TimeOfDay);
            var endTime = i + 1 < orderedEntries.Count ? orderedEntries[i + 1].Key : 2400;
            if (endTime <= startTime)
                continue;

            var tile = TryReadScheduleTile(current.Value, out var scheduledTile) ? scheduledTile : Point.Zero;
            var blockType = _dutyRosterService.TryResolveDutyRoleForWindow(npcId, locationId, startTime, endTime, out var dutyRoleId)
                ? AutonomyPlanBlockType.RequiredDuty
                : AutonomyPlanBlockType.BaseAnchor;

            blocks.Add(new AutonomyPlanBlock
            {
                BlockId = $"{npcId}:anchor:{startTime}",
                Type = blockType,
                DutyRoleId = blockType == AutonomyPlanBlockType.RequiredDuty ? dutyRoleId : string.Empty,
                TargetLocation = locationId,
                TargetTile = tile,
                Reason = blockType == AutonomyPlanBlockType.RequiredDuty ? "keep the scripted duty window staffed" : "follow the scripted schedule",
                StartTime = startTime,
                EndTime = endTime
            });
        }

        return blocks;
    }

    private void FillDetourWindow(
        string npcId,
        NpcContextSnapshot snapshot,
        List<ScoredAutonomyGoal> remainingGoals,
        ModConfig config,
        List<AutonomyPlanBlock> blocks,
        ref int blockCount,
        int windowStart,
        int windowEnd)
    {
        var availableMinutes = DiffMinutes(windowStart, windowEnd);
        if (availableMinutes < 90 || remainingGoals.Count == 0 || blockCount >= config.AutonomyMaxBlocksPerDay)
            return;

        // Randomly skip ~50% of eligible windows to keep detours sparse
        if (_random.NextDouble() < 0.5)
            return;

        var goal = remainingGoals[0];
        if (goal.Score < config.AutonomyEncounterScoreThreshold)
            return;

        var duration = ResolveDetourDuration(goal, availableMinutes);
        if (duration <= 0)
            return;

        remainingGoals.RemoveAt(0);
        var startTime = windowStart;
        var endTime = AddMinutes(startTime, duration);
        var targetLocation = ResolveTargetLocation(goal, snapshot);
        blocks.Add(new AutonomyPlanBlock
        {
            BlockId = $"{npcId}:{goal.GoalType}:detour:{blockCount + 1}",
            Type = ResolveBlockType(goal.GoalType),
            TargetNpcId = goal.TargetNpcId,
            TargetLocation = targetLocation,
            TargetTile = Point.Zero,
            Reason = goal.Reason,
            StartTime = startTime,
            EndTime = endTime
        });
        blockCount += 1;
    }

    private static bool ShouldAddReturnHome(NpcContextSnapshot snapshot, List<AutonomyPlanBlock> anchors)
    {
        if (string.IsNullOrWhiteSpace(snapshot.HomeLocation))
            return false;
        if (anchors.Count == 0)
            return true;

        var latestAnchor = anchors.OrderByDescending(block => block.StartTime).First();
        return latestAnchor.StartTime < 2000
            && !string.Equals(latestAnchor.TargetLocation, snapshot.HomeLocation, StringComparison.OrdinalIgnoreCase);
    }

    private static AutonomyPlanBlock? FindNextAnchor(NpcDailyPlan plan, int startIndex)
    {
        for (var i = Math.Max(0, startIndex); i < plan.Blocks.Count; i++)
        {
            var block = plan.Blocks[i];
            if (block.Type is AutonomyPlanBlockType.BaseAnchor or AutonomyPlanBlockType.RequiredDuty or AutonomyPlanBlockType.ReturnHome)
                return block;
        }

        return null;
    }

    private static int ResolveDetourDuration(ScoredAutonomyGoal goal, int availableMinutes)
    {
        var target = goal.GoalType.Equals("visit_npc", StringComparison.OrdinalIgnoreCase) ? 30 : 20;
        var maxAllowed = Math.Max(0, availableMinutes - 20);
        return Math.Min(target, maxAllowed);
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

    private string ResolveTargetLocation(ScoredAutonomyGoal goal, NpcContextSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(goal.TargetLocation))
            return goal.TargetLocation;

        if (goal.GoalType.Equals("rest", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(snapshot.HomeLocation))
        {
            return snapshot.HomeLocation;
        }

        return _destinationRegistryService.ResolveFallbackLocation(snapshot.CurrentLocation, Game1.locations).LocationId;
    }

    private static bool TryReadScheduleLocation(object scheduleEntry, out string locationId)
    {
        locationId = string.Empty;

        foreach (var memberName in ScheduleLocationMemberCandidates)
        {
            if (TryReadMember(scheduleEntry, memberName, out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
            {
                locationId = text;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadScheduleTile(object scheduleEntry, out Point tile)
    {
        tile = Point.Zero;

        foreach (var memberName in ScheduleTileMemberCandidates)
        {
            if (!TryReadMember(scheduleEntry, memberName, out var value) || value is null)
                continue;

            if (value is Point point)
            {
                tile = point;
                return true;
            }

            if (value is Vector2 vector)
            {
                tile = new Point((int)vector.X, (int)vector.Y);
                return true;
            }

            // SDV 1.6: SchedulePathDescription stores the route as List<Point>
            if (value is IList<Point> routePoints && routePoints.Count > 0)
            {
                tile = routePoints[routePoints.Count - 1];
                return true;
            }
        }

        return false;
    }

    private static bool TryReadMember(object instance, string memberName, out object? value)
    {
        value = null;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is not null)
        {
            value = property.GetValue(instance);
            return true;
        }

        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is not null)
        {
            value = field.GetValue(instance);
            return true;
        }

        return false;
    }

    private static int DiffMinutes(int startTime, int endTime)
    {
        var start = ((startTime / 100) * 60) + (startTime % 100);
        var end = ((endTime / 100) * 60) + (endTime % 100);
        return Math.Max(0, end - start);
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
