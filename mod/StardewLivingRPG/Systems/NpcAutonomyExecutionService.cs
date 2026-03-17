using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewLivingRPG.Systems;

public enum MovementTickResult
{
    InProgress,
    Arrived,
    Stuck,
    WaitingForLoad,
    BlockComplete,
    HardLockYield
}

public sealed class NpcAutonomyExecutionService
{
    private readonly ModConfig _config;
    private readonly NpcWalkabilityService _walkabilityService;
    private readonly Dictionary<string, int> _stuckTickCountByNpcId = new(StringComparer.OrdinalIgnoreCase);

    public NpcAutonomyExecutionService(ModConfig config, NpcWalkabilityService walkabilityService)
    {
        _config = config;
        _walkabilityService = walkabilityService;
    }

    public MovementTickResult Tick(NPC? npc, AutonomyRuntimeState runtime, AutonomyPlanBlock activeBlock, int gameTimeOfDay)
    {
        if (Game1.eventUp)
        {
            runtime.MovementPhase = "hard_lock_yield";
            return MovementTickResult.HardLockYield;
        }

        if (npc is not null && npc.currentLocation is not null)
            return TickLoadedNpc(npc, runtime, activeBlock);

        return TickUnloadedNpc(runtime, activeBlock);
    }

    public bool HasArrived(NPC? npc, AutonomyRuntimeState runtime, AutonomyPlanBlock activeBlock)
    {
        if (npc is not null
            && npc.currentLocation is not null
            && string.Equals(npc.currentLocation.Name, activeBlock.TargetLocation, StringComparison.OrdinalIgnoreCase))
        {
            if (activeBlock.TargetTile == Point.Zero)
            {
                if (activeBlock.RequiresExactTile)
                    return false;
                return true;
            }

            var distance = Vector2.Distance(npc.Tile, new Vector2(activeBlock.TargetTile.X, activeBlock.TargetTile.Y));
            if (distance <= 1.25f)
                return true;
        }

        if (string.Equals(runtime.ExpectedLocationId, activeBlock.TargetLocation, StringComparison.OrdinalIgnoreCase)
            && runtime.OffscreenProgressMinutes >= (activeBlock.Route?.TotalEstimatedMinutes ?? 0))
        {
            if (activeBlock.RequiresExactTile && activeBlock.TargetTile == Point.Zero)
                return false;
            return true;
        }

        return false;
    }

    public void WarpNpcTo(NPC npc, string locationId, Point tile)
    {
        if (npc is null || string.IsNullOrWhiteSpace(locationId))
            return;

        npc.controller = null;
        npc.Halt();
        Game1.warpCharacter(npc, locationId, new Vector2(tile.X, tile.Y));
    }

    public void ResetStuckTracking(string npcId)
    {
        _stuckTickCountByNpcId.Remove(npcId);
    }

    private MovementTickResult TickLoadedNpc(NPC npc, AutonomyRuntimeState runtime, AutonomyPlanBlock activeBlock)
    {
        var currentLocation = npc.currentLocation;
        if (currentLocation is null)
            return MovementTickResult.WaitingForLoad;

        runtime.MovementPhase = "walking";
        runtime.ExpectedLocationId = currentLocation.Name ?? runtime.ExpectedLocationId;
        runtime.ExpectedTile = new Point((int)npc.Tile.X, (int)npc.Tile.Y);

        if (HasArrived(npc, runtime, activeBlock))
        {
            runtime.MovementPhase = "arrived";
            runtime.CurrentSegmentTargetTile = Point.Zero;
            runtime.CurrentSegmentPath.Clear();
            runtime.NeedsLocalRepath = false;
            runtime.StationaryTicks = 0;
            runtime.OscillationTicks = 0;
            npc.controller = null;
            npc.Halt();
            ResetStuckTracking(runtime.NpcId);
            return MovementTickResult.Arrived;
        }

        if (activeBlock.Route is null || activeBlock.Route.Segments.Count == 0)
            return MoveWithinCurrentLocation(npc, runtime, activeBlock.TargetTile, "local_target", activeBlock.RequiresExactTile);

        if (runtime.ActiveRouteSegmentIndex < 0)
            runtime.ActiveRouteSegmentIndex = 0;

        if (runtime.ActiveRouteSegmentIndex >= activeBlock.Route.Segments.Count)
            return MoveWithinCurrentLocation(npc, runtime, activeBlock.TargetTile, "final_approach", activeBlock.RequiresExactTile);

        var segment = activeBlock.Route.Segments[runtime.ActiveRouteSegmentIndex];
        if (string.Equals(currentLocation.Name, segment.FromLocationId, StringComparison.OrdinalIgnoreCase))
        {
            var departureTile = segment.DepartureTile;
            if (departureTile != Point.Zero)
            {
                var departureDistance = Vector2.Distance(npc.Tile, new Vector2(departureTile.X, departureTile.Y));
                if (departureDistance > 0.75f)
                    return MoveWithinCurrentLocation(npc, runtime, departureTile, "segment_departure");
            }

            var destinationLocation = Game1.getLocationFromName(segment.ToLocationId);
            if (destinationLocation is null)
            {
                runtime.StuckReason = "missing_destination";
                return IncrementStuck(runtime);
            }

            var arrivalTile = segment.ArrivalTile != Point.Zero ? segment.ArrivalTile : activeBlock.TargetTile;
            if (!_walkabilityService.TryFindNearestWalkableTile(destinationLocation, arrivalTile, _config.AutonomyMaterializationMaxRadius, npc, out var safeArrivalTile))
            {
                runtime.StuckReason = "no_safe_tile";
                return IncrementStuck(runtime);
            }

            WarpNpcTo(npc, segment.ToLocationId, safeArrivalTile);
            segment.Status = RouteSegmentStatus.Completed;
            runtime.ActiveRouteSegmentIndex += 1;
            runtime.ExpectedLocationId = segment.ToLocationId;
            runtime.ExpectedTile = safeArrivalTile;
            runtime.CurrentSegmentTargetTile = Point.Zero;
            runtime.CurrentSegmentPath.Clear();
            runtime.NeedsLocalRepath = false;
            runtime.StationaryTicks = 0;
            runtime.OscillationTicks = 0;
            ResetStuckTracking(runtime.NpcId);
            return MovementTickResult.InProgress;
        }

        if (string.Equals(currentLocation.Name, activeBlock.TargetLocation, StringComparison.OrdinalIgnoreCase))
            return MoveWithinCurrentLocation(npc, runtime, activeBlock.TargetTile, "final_approach", activeBlock.RequiresExactTile);

        runtime.StuckReason = "route_desync";
        return IncrementStuck(runtime);
    }

    private MovementTickResult MoveWithinCurrentLocation(NPC npc, AutonomyRuntimeState runtime, Point preferredTargetTile, string movementPhase, bool exactOnly = false)
    {
        if (preferredTargetTile == Point.Zero)
            return MovementTickResult.InProgress;

        var location = npc.currentLocation;
        if (location is null)
            return MovementTickResult.WaitingForLoad;

        Point targetTile;
        if (exactOnly)
        {
            if (!_walkabilityService.IsTileWalkable(location, preferredTargetTile, npc))
            {
                runtime.StuckReason = "blocked_target";
                return IncrementStuck(runtime);
            }

            targetTile = preferredTargetTile;
        }
        else if (!_walkabilityService.TryFindNearestWalkableTile(location, preferredTargetTile, 2, npc, out targetTile))
        {
            runtime.StuckReason = "blocked_target";
            return IncrementStuck(runtime);
        }

        runtime.MovementPhase = movementPhase;
        runtime.ExpectedLocationId = location.Name ?? runtime.ExpectedLocationId;
        runtime.ExpectedTile = targetTile;

        var currentTile = new Point((int)npc.Tile.X, (int)npc.Tile.Y);
        var previousTile = runtime.LastKnownTile;
        var movedThisTick = currentTile != previousTile;
        if (!movedThisTick)
            runtime.StationaryTicks += 1;
        else
            runtime.StationaryTicks = 0;

        if (currentTile == runtime.PreviousKnownTile && currentTile != previousTile)
            runtime.OscillationTicks += 1;
        else if (movedThisTick)
            runtime.OscillationTicks = 0;

        runtime.PreviousKnownTile = previousTile;
        runtime.LastKnownTile = currentTile;

        if (Vector2.Distance(npc.Tile, new Vector2(targetTile.X, targetTile.Y)) <= 1.25f)
        {
            npc.controller = null;
            npc.Halt();
            runtime.CurrentSegmentTargetTile = Point.Zero;
            runtime.StationaryTicks = 0;
            runtime.OscillationTicks = 0;
            ResetStuckTracking(runtime.NpcId);
            return MovementTickResult.Arrived;
        }

        if (npc.controller is null
            || runtime.NeedsLocalRepath
            || runtime.CurrentSegmentTargetTile != targetTile)
        {
            npc.Halt();
            npc.controller = new PathFindController(npc, location, targetTile, 2);
            runtime.CurrentSegmentTargetTile = targetTile;
            runtime.NeedsLocalRepath = false;
        }

        if (movedThisTick || npc.isMoving())
        {
            if (_walkabilityService.IsNearWarpTile(location, currentTile, 1))
                runtime.StuckReason = string.Empty;
            ResetStuckTracking(runtime.NpcId);
            return MovementTickResult.InProgress;
        }

        if (_walkabilityService.IsNearWarpTile(location, currentTile, 1)
            && runtime.StationaryTicks >= Math.Max(20, _config.AutonomyStuckDetectionTicks / 6))
        {
            runtime.StuckReason = "doorway_cluster";
            return IncrementStuck(runtime, repathFirst: true);
        }

        if (runtime.OscillationTicks >= 4)
        {
            runtime.StuckReason = "micro_oscillation";
            return IncrementStuck(runtime, repathFirst: true);
        }

        return IncrementStuck(runtime, repathFirst: true);
    }

    private MovementTickResult TickUnloadedNpc(AutonomyRuntimeState runtime, AutonomyPlanBlock activeBlock)
    {
        runtime.MovementPhase = "offscreen";
        var route = activeBlock.Route;
        if (route is null)
            return MovementTickResult.InProgress;

        runtime.OffscreenProgressMinutes++;

        var cumulativeMinutes = 0;
        foreach (var segment in route.Segments)
        {
            cumulativeMinutes += segment.EstimatedMinutes;
            if (runtime.OffscreenProgressMinutes < cumulativeMinutes)
            {
                runtime.ExpectedLocationId = segment.FromLocationId;
                runtime.ExpectedTile = segment.DepartureTile;
                break;
            }

            runtime.ExpectedLocationId = segment.ToLocationId;
            runtime.ExpectedTile = segment.ArrivalTile;
            segment.Status = RouteSegmentStatus.Completed;
        }

        if (runtime.OffscreenProgressMinutes >= route.TotalEstimatedMinutes)
        {
            runtime.ExpectedLocationId = route.DestinationLocationId;
            runtime.ExpectedTile = activeBlock.TargetTile != Point.Zero ? activeBlock.TargetTile : route.FinalArrivalTile;
            runtime.MovementPhase = "arrived";
            return MovementTickResult.Arrived;
        }

        return MovementTickResult.WaitingForLoad;
    }

    private MovementTickResult IncrementStuck(AutonomyRuntimeState runtime, bool repathFirst = false)
    {
        _stuckTickCountByNpcId.TryGetValue(runtime.NpcId, out var stuckTicks);
        stuckTicks++;
        _stuckTickCountByNpcId[runtime.NpcId] = stuckTicks;

        if (repathFirst && stuckTicks >= Math.Max(20, _config.AutonomyStuckDetectionTicks / 4))
            runtime.NeedsLocalRepath = true;

        if (stuckTicks < _config.AutonomyStuckDetectionTicks)
            return MovementTickResult.InProgress;

        runtime.RetryCount++;
        _stuckTickCountByNpcId[runtime.NpcId] = 0;
        runtime.MovementPhase = "stuck";
        return runtime.RetryCount > 3 ? MovementTickResult.Stuck : MovementTickResult.InProgress;
    }
}
