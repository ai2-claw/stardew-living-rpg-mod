using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;
using System.Reflection;

namespace StardewLivingRPG.Systems;

public sealed class NpcDutyRosterService
{
    private sealed record DutyAssignment(string RoleId, string NpcId, string LocationId, int StartTime, int EndTime);
    private static readonly string[] ScheduleLocationMemberCandidates = { "locationName", "LocationName", "location", "Location", "locationId", "LocationId" };
    private static readonly string[] ScheduleTileMemberCandidates = { "endPoint", "EndPoint", "targetTile", "TargetTile", "tile", "Tile", "point", "Point" };
    private static readonly Dictionary<string, Point> DutyFallbackTiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pierre:SeedShop"] = new Point(4, 15),
        ["Morris:JojaMart"] = new Point(11, 17),
        ["Gus:Saloon"] = new Point(10, 18),
        ["Clint:Blacksmith"] = new Point(3, 14),
        ["Harvey:Hospital"] = new Point(12, 13),
        ["Robin:ScienceHouse"] = new Point(6, 14)
    };

    private static readonly DutyAssignment[] Assignments =
    {
        new("general_store_keeper", "Pierre", "SeedShop", 900, 1700),
        new("joja_manager", "Morris", "JojaMart", 900, 1700),
        new("saloon_keeper", "Gus", "Saloon", 1200, 2400),
        new("blacksmith", "Clint", "Blacksmith", 900, 1600),
        new("doctor", "Harvey", "Hospital", 900, 1500),
        new("carpenter", "Robin", "ScienceHouse", 900, 1700)
    };

    public IReadOnlyList<AutonomyPlanBlock> BuildRequiredDutyBlocks(string npcId, IEnumerable<GameLocation> worldLocations)
    {
        var blocks = new List<AutonomyPlanBlock>();

        foreach (var assignment in Assignments)
        {
            if (!string.Equals(assignment.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
                continue;

            var location = worldLocations.FirstOrDefault(candidate =>
                candidate is not null
                && string.Equals(candidate.Name, assignment.LocationId, StringComparison.OrdinalIgnoreCase));
            if (location is null)
                continue;

            blocks.Add(new AutonomyPlanBlock
            {
                BlockId = $"{npcId}:duty:{assignment.RoleId}",
                Type = AutonomyPlanBlockType.RequiredDuty,
                DutyRoleId = assignment.RoleId,
                TargetLocation = assignment.LocationId,
                TargetTile = ResolveDutyTile(assignment, location),
                Reason = $"keep {assignment.RoleId.Replace('_', ' ')} staffed",
                StartTime = assignment.StartTime,
                EndTime = assignment.EndTime
            });
        }

        return blocks;
    }

    public bool IsRequiredDutyNpc(string npcId)
    {
        return Assignments.Any(assignment => string.Equals(assignment.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsRequiredDutyBlock(AutonomyPlanBlock? block)
    {
        return block?.Type == AutonomyPlanBlockType.RequiredDuty;
    }

    public bool TryResolveDutyRoleForWindow(string npcId, string locationId, int startTime, int endTime, out string dutyRoleId)
    {
        dutyRoleId = string.Empty;
        foreach (var assignment in Assignments)
        {
            if (!string.Equals(assignment.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(assignment.LocationId, locationId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (endTime <= assignment.StartTime || startTime >= assignment.EndTime)
                continue;

            dutyRoleId = assignment.RoleId;
            return true;
        }

        return false;
    }

    private static Point ResolveDutyTile(DutyAssignment assignment, GameLocation location)
    {
        var npc = Game1.getCharacterFromName(assignment.NpcId);
        if (npc is not null)
        {
            var scheduledTile = TryResolveScheduledDutyTile(npc, assignment.LocationId, assignment.StartTime, assignment.EndTime);
            if (scheduledTile != Point.Zero)
                return scheduledTile;

            if (npc.currentLocation is not null
                && string.Equals(npc.currentLocation.Name, location.Name, StringComparison.OrdinalIgnoreCase))
            {
                return new Point((int)npc.Tile.X, (int)npc.Tile.Y);
            }
        }

        if (DutyFallbackTiles.TryGetValue($"{assignment.NpcId}:{assignment.LocationId}", out var fallbackTile))
            return fallbackTile;

        return Point.Zero;
    }

    private static Point TryResolveScheduledDutyTile(NPC npc, string locationId, int startTime, int endTime)
    {
        if (npc.Schedule is null || npc.Schedule.Count == 0)
            return Point.Zero;

        foreach (var entry in npc.Schedule.OrderBy(candidate => candidate.Key))
        {
            if (entry.Key > endTime)
                break;

            if (entry.Key < startTime - 100)
                continue;

            if (!TryReadScheduleLocation(entry.Value, out var scheduledLocation)
                || !string.Equals(scheduledLocation, locationId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryReadScheduleTile(entry.Value, out var scheduledTile))
                return scheduledTile;
        }

        return Point.Zero;
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
}
