using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewLivingRPG.Systems;

public sealed class ScheduleOverrideService
{
    private readonly Dictionary<string, Dictionary<int, SchedulePathDescription>> _vanillaBackups = new(StringComparer.OrdinalIgnoreCase);

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

            npc.Schedule.Remove(block.StartTime);
            npc.Schedule[block.StartTime] = new SchedulePathDescription(
                new Stack<Point>(new[] { block.TargetTile }),
                2,
                block.TargetLocation,
                string.Empty,
                string.Empty,
                block.TargetTile);
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
        if (npc is null || npc.Schedule is null)
            return;

        if (_vanillaBackups.TryGetValue(npc.Name, out var backup))
        {
            npc.Schedule.Clear();
            foreach (var kvp in backup)
                npc.Schedule[kvp.Key] = kvp.Value;
            _vanillaBackups.Remove(npc.Name);
        }
    }

    public void PatchSingleEntry(NPC npc, int gameTime, string locationId, Point tile, int facing = 2)
    {
        if (npc is null || string.IsNullOrWhiteSpace(locationId) || npc.Schedule is null || tile == Point.Zero)
            return;

        npc.Schedule[gameTime] = new SchedulePathDescription(
            new Stack<Point>(new[] { tile }),
            facing,
            locationId,
            string.Empty,
            string.Empty,
            tile);
    }

    public void ClearAllBackups()
    {
        _vanillaBackups.Clear();
    }

    public bool HasOverride(string npcName)
    {
        return _vanillaBackups.ContainsKey(npcName);
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
