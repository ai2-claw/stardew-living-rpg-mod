using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

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
        // Anchor / duty blocks are 100% vanilla-driven — never touch the NPC
        if (activeBlock.Type is AutonomyPlanBlockType.BaseAnchor or AutonomyPlanBlockType.RequiredDuty)
        {
            runtime.MovementPhase = "vanilla_driving";
            return MovementTickResult.InProgress;
        }

        if (Game1.eventUp)
        {
            runtime.MovementPhase = "hard_lock_yield";
            return MovementTickResult.HardLockYield;
        }

        if (npc is not null && npc.currentLocation is not null)
            return TickLoadedNpc(npc, runtime, activeBlock);

        return TickUnloadedNpc(runtime, activeBlock);
    }

    /// <summary>
    /// Resolve a walkable tile for a block whose TargetTile is Point.Zero.
    /// Uses warp entry point or map center as seed, then spiral-searches outward.
    /// Updates the block in-place so subsequent ticks reuse the resolved tile.
    /// </summary>
    public bool TryResolveFallbackTile(GameLocation location, NPC npc, AutonomyPlanBlock block)
    {
        if (location is null || block.TargetTile != Point.Zero)
            return block.TargetTile != Point.Zero;

        // Prefer the warp arrival tile nearest to an entry point
        var seedTile = FindWarpEntryOrMapCenter(location);
        if (_walkabilityService.TryFindNearestWalkableTile(location, seedTile, 8, npc, out var safeTile))
        {
            block.TargetTile = safeTile;
            return true;
        }

        return false;
    }

    private static Point FindWarpEntryOrMapCenter(GameLocation location)
    {
        // Try the first warp tile that leads into this location from another map
        if (location.warps.Count > 0)
        {
            var warp = location.warps[0];
            return new Point(warp.X, Math.Max(0, warp.Y - 1));
        }

        // Fall back to the first tile that has Back layer data (avoids interior void areas)
        var backLayer = location.Map?.GetLayer("Back");
        if (backLayer is not null)
        {
            var cx = backLayer.LayerWidth / 2;
            var cy = backLayer.LayerHeight / 2;
            // Spiral outward from center to find a tile with painted Back data
            for (var r = 0; r <= Math.Max(backLayer.LayerWidth, backLayer.LayerHeight); r++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    for (var dy = -r; dy <= r; dy++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r && r > 0)
                            continue;
                        var tx = cx + dx;
                        var ty = cy + dy;
                        if (tx >= 0 && ty >= 0 && tx < backLayer.LayerWidth && ty < backLayer.LayerHeight
                            && backLayer.Tiles[tx, ty] is not null)
                        {
                            return new Point(tx, ty);
                        }
                    }
                }
            }
        }

        return new Point(10, 10); // absolute last resort
    }

    public bool HasArrived(NPC? npc, AutonomyRuntimeState runtime, AutonomyPlanBlock activeBlock)
    {
        if (npc is not null
            && npc.currentLocation is not null
            && string.Equals(npc.currentLocation.Name, activeBlock.TargetLocation, StringComparison.OrdinalIgnoreCase))
        {
            if (activeBlock.TargetTile == Point.Zero)
            {
                // Try to resolve a fallback tile now that we're on the right map
                if (TryResolveFallbackTile(npc.currentLocation, npc, activeBlock))
                    return Vector2.Distance(npc.Tile, new Vector2(activeBlock.TargetTile.X, activeBlock.TargetTile.Y)) <= 1.25f;
                // If we still can't resolve and it's not exact-required, consider arrived
                if (!activeBlock.RequiresExactTile)
                    return true;
                return false;
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

        // Validate the tile is walkable; spiral-search if not (prevents void placement)
        var destination = Game1.getLocationFromName(locationId);
        if (destination is not null && !_walkabilityService.IsTileWalkable(destination, tile, npc))
        {
            if (_walkabilityService.TryFindNearestWalkableTile(destination, tile, 16, npc, out var safeTile))
                tile = safeTile;
            else
            {
                // Last resort: find any walkable tile near a warp entry
                var seed = FindWarpEntryOrMapCenter(destination);
                if (_walkabilityService.TryFindNearestWalkableTile(destination, seed, 16, npc, out var fallback))
                    tile = fallback;
            }
        }

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

        runtime.MovementPhase = "vanilla_schedule";
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
            ResetStuckTracking(runtime.NpcId);
            return MovementTickResult.Arrived;
        }

        // Loaded NPC movement stays vanilla-owned.
        // The mod does not assign path controllers or drive loaded detour traversal.
        return MovementTickResult.InProgress;
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
}
