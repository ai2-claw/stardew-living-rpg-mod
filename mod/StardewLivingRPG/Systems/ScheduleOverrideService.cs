using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewLivingRPG.Systems;

public sealed class ScheduleOverrideService
{
    private readonly NpcWalkabilityService _walkabilityService;
    private readonly Dictionary<string, Dictionary<int, SchedulePathDescription>> _vanillaBackups = new(StringComparer.OrdinalIgnoreCase);

    public ScheduleOverrideService(NpcWalkabilityService walkabilityService)
    {
        _walkabilityService = walkabilityService;
    }

    public void ApplyDailyOverride(NPC npc, NpcDailyPlan plan)
    {
        if (npc is null || plan is null || plan.Blocks.Count == 0 || npc.Schedule is null)
            return;

        if (!_vanillaBackups.ContainsKey(npc.Name))
            _vanillaBackups[npc.Name] = new Dictionary<int, SchedulePathDescription>(npc.Schedule);

        foreach (var block in plan.Blocks)
        {
            if (!IsDetourBlock(block) || string.IsNullOrWhiteSpace(block.TargetLocation) || block.TargetTile == Point.Zero)
                continue;

            if (!TryResolveSafeScheduleTile(npc, block.TargetLocation, block.TargetTile, out var safeTile))
                continue;

            block.TargetTile = safeTile;

            npc.Schedule.Remove(block.StartTime);
            npc.Schedule[block.StartTime] = new SchedulePathDescription(
                new Stack<Point>(),
                2,
                block.TargetLocation,
                string.Empty,
                string.Empty,
                safeTile);
        }
    }

    public void RestoreAllSchedules()
    {
        foreach (var npcName in _vanillaBackups.Keys.ToArray())
        {
            var npc = Game1.getCharacterFromName(npcName);
            if (npc is null || npc.Schedule is null)
                continue;

            npc.Schedule.Clear();
            foreach (var kvp in _vanillaBackups[npcName])
                npc.Schedule[kvp.Key] = kvp.Value;
        }
    }

    public void RestoreVanillaSchedule(NPC npc)
    {
        RestoreVanillaSchedule(npc, releaseBackup: true);
    }

    public void RestoreVanillaSchedule(NPC npc, bool releaseBackup)
    {
        if (npc is null || npc.Schedule is null)
            return;

        if (_vanillaBackups.TryGetValue(npc.Name, out var backup))
        {
            npc.Schedule.Clear();
            foreach (var kvp in backup)
                npc.Schedule[kvp.Key] = kvp.Value;
            if (releaseBackup)
                _vanillaBackups.Remove(npc.Name);
        }
    }

    public void CommitVanillaRestore(string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return;

        _vanillaBackups.Remove(npcName);
    }

    public void PatchSingleEntry(NPC npc, int gameTime, string locationId, Point tile, int facing = 2)
    {
        if (npc is null || string.IsNullOrWhiteSpace(locationId) || npc.Schedule is null || tile == Point.Zero)
            return;

        if (!TryResolveSafeScheduleTile(npc, locationId, tile, out var safeTile))
            return;

        npc.Schedule[gameTime] = new SchedulePathDescription(
            new Stack<Point>(),
            facing,
            locationId,
            string.Empty,
            string.Empty,
            safeTile);
    }

    public void ClearAllBackups()
    {
        _vanillaBackups.Clear();
    }

    public bool HasOverride(string npcName)
    {
        return _vanillaBackups.ContainsKey(npcName);
    }

    private bool TryResolveSafeScheduleTile(NPC npc, string locationId, Point tile, out Point safeTile)
    {
        safeTile = tile;
        var location = Game1.getLocationFromName(locationId);
        if (location is null)
            return false;

        if (_walkabilityService.IsTileWalkable(location, tile, npc))
            return true;

        if (_walkabilityService.TryFindNearestWalkableTile(location, tile, 16, npc, out safeTile))
            return true;

        return false;
    }

    private static bool IsDetourBlock(AutonomyPlanBlock block)
    {
        return block.Type is AutonomyPlanBlockType.Wander
            or AutonomyPlanBlockType.VisitNpc
            or AutonomyPlanBlockType.Socialize
            or AutonomyPlanBlockType.Errand
            or AutonomyPlanBlockType.ObserveEvent
            or AutonomyPlanBlockType.Rest;
    }
}
