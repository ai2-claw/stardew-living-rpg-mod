using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcMaterializationService
{
    private readonly ModConfig _config;
    private readonly NpcWalkabilityService _walkabilityService;

    public NpcMaterializationService(ModConfig config, NpcWalkabilityService walkabilityService)
    {
        _config = config;
        _walkabilityService = walkabilityService;
    }

    public int ReconcileMap(
        GameLocation location,
        IEnumerable<AutonomyRuntimeState> allRuntimes,
        NpcAutonomyExecutionService executionService)
    {
        if (location is null || string.IsNullOrWhiteSpace(location.Name))
            return 0;

        var materialized = 0;
        var locationName = location.Name;

        foreach (var runtime in allRuntimes)
        {
            if (!string.Equals(runtime.ExpectedLocationId, locationName, StringComparison.OrdinalIgnoreCase))
                continue;

            var npc = Game1.getCharacterFromName(runtime.NpcId);
            if (npc is null)
                continue;

            var activeBlock = runtime.ActivePlan is not null
                && runtime.ActiveBlockIndex >= 0
                && runtime.ActiveBlockIndex < runtime.ActivePlan.Blocks.Count
                ? runtime.ActivePlan.Blocks[runtime.ActiveBlockIndex]
                : null;

            var routeMinutes = activeBlock?.Route?.TotalEstimatedMinutes ?? 0;
            if (routeMinutes > 0 && runtime.OffscreenProgressMinutes < routeMinutes)
                continue;

            var targetTile = runtime.ExpectedTile != Point.Zero ? runtime.ExpectedTile : Point.Zero;
            if (activeBlock is not null && activeBlock.TargetTile != Point.Zero)
                targetTile = activeBlock.TargetTile;

            var exactOnly = activeBlock?.RequiresExactTile ?? false;
            if (targetTile == Point.Zero || !TryFindSafeTile(location, targetTile, npc, exactOnly, out var safeTile))
                continue;

            var farmerTile = Game1.player?.Tile ?? Vector2.Zero;
            if (Vector2.Distance(farmerTile, new Vector2(safeTile.X, safeTile.Y)) < 2f)
                continue;

            if (npc.currentLocation is not null
                && string.Equals(npc.currentLocation.Name, locationName, StringComparison.OrdinalIgnoreCase))
            {
                var distance = Vector2.Distance(npc.Tile, new Vector2(safeTile.X, safeTile.Y));
                if (distance > 8f)
                {
                    executionService.WarpNpcTo(npc, locationName, safeTile);
                    materialized++;
                }
            }
            else
            {
                executionService.WarpNpcTo(npc, locationName, safeTile);
                materialized++;
            }
        }

        return materialized;
    }

    public bool TryFindSafeTile(GameLocation location, Point preferredTile, NPC npc, bool exactOnly, out Point safeTile)
    {
        if (preferredTile == Point.Zero)
        {
            safeTile = Point.Zero;
            return false;
        }

        if (exactOnly)
        {
            safeTile = preferredTile;
            return _walkabilityService.IsTileWalkable(location, preferredTile, npc);
        }

        return _walkabilityService.TryFindNearestWalkableTile(location, preferredTile, _config.AutonomyMaterializationMaxRadius, npc, out safeTile);
    }
}
