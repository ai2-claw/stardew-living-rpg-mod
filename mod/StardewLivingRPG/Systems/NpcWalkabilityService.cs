using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace StardewLivingRPG.Systems;

public sealed class NpcWalkabilityService
{
    private const int TileSize = 64;

    public bool IsTileWalkable(GameLocation? location, Point tile, Character? actor = null)
    {
        return IsTileWalkableCore(location, tile, actor, ignoreTransientOccupants: false, out _);
    }

    public bool IsTileStructurallyWalkable(GameLocation? location, Point tile, Character? actor = null)
    {
        return IsTileWalkableCore(location, tile, actor, ignoreTransientOccupants: true, out _);
    }

    public bool IsTileStructurallyWalkable(GameLocation? location, Point tile, Character? actor, out string? blockerDescription)
    {
        return IsTileWalkableCore(location, tile, actor, ignoreTransientOccupants: true, out blockerDescription);
    }

    private static bool IsTileWalkableCore(GameLocation? location, Point tile, Character? actor, bool ignoreTransientOccupants, out string? blockerDescription)
    {
        blockerDescription = null;
        if (location is null || location.Map is null)
            return false;
        if (tile.X < 0 || tile.Y < 0)
            return false;

        var layer = location.Map.Layers.Count > 0 ? location.Map.Layers[0] : null;
        if (layer is null || tile.X >= layer.LayerWidth || tile.Y >= layer.LayerHeight)
            return false;

        // Reject void tiles — interior maps have large black areas with no painted Back layer tile
        var backLayer = location.Map.GetLayer("Back");
        if (backLayer?.Tiles[tile.X, tile.Y] is null)
            return false;

        if (HasImpassableBuildingsTile(location, tile))
        {
            blockerDescription = "buildings_layer";
            return false;
        }

        var tileVector = new Vector2(tile.X, tile.Y);
        var tileLocation = new xTile.Dimensions.Location(tile.X * TileSize, tile.Y * TileSize);
        var tileViewport = new xTile.Dimensions.Rectangle(0, 0, TileSize, TileSize);
        if (!location.isTilePassable(tileLocation, tileViewport))
            return false;
        if (HasNoPathProperty(location, tile))
            return false;

        var collisionRect = BuildCollisionRect(tile);
        if (IsBlockedByCollision(location, collisionRect, actor, ignoreTransientOccupants))
            return false;

        if (location.Objects.TryGetValue(tileVector, out var obj)
            && obj is SObject placedObject
            && !placedObject.isPassable())
        {
            return false;
        }

        if (location.terrainFeatures.TryGetValue(tileVector, out var terrainFeature)
            && terrainFeature is TerrainFeature feature
            && !feature.isPassable(actor))
        {
            return false;
        }

        foreach (var largeTerrainFeature in location.largeTerrainFeatures)
        {
            if (largeTerrainFeature.getBoundingBox().Intersects(collisionRect))
                return false;
        }

        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.occupiesTile(tile.X, tile.Y))
                return false;
        }

        foreach (var furniture in location.furniture)
        {
            if (furniture is Furniture placedFurniture
                && placedFurniture.GetBoundingBox().Intersects(collisionRect))
            {
                return false;
            }
        }

        if (!ignoreTransientOccupants
            && location.characters.Any(character =>
                character is not null
                && !ReferenceEquals(character, actor)
                && (int)character.Tile.X == tile.X
                && (int)character.Tile.Y == tile.Y))
        {
            return false;
        }

        if (!ignoreTransientOccupants
            && Game1.player is not null
            && !ReferenceEquals(Game1.player, actor)
            && (int)Game1.player.Tile.X == tile.X
            && (int)Game1.player.Tile.Y == tile.Y)
        {
            return false;
        }

        // Reject tiles that fall inside building footprints
        foreach (var building in location.buildings)
        {
            if (building is null)
                continue;
            var bx = building.tileX.Value;
            var by = building.tileY.Value;
            var bw = building.tilesWide.Value;
            var bh = building.tilesHigh.Value;
            if (tile.X >= bx && tile.X < bx + bw && tile.Y >= by && tile.Y < by + bh)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Lenient walkability check for staging tiles.
    /// Ignores transient character/player occupancy while still honoring
    /// structural collision so crowded but open tiles remain stageable.
    /// </summary>
    public bool IsTileStageable(GameLocation? location, Point tile)
    {
        if (location is null || location.Map is null)
            return false;
        if (tile.X < 0 || tile.Y < 0)
            return false;

        var layer = location.Map.Layers.Count > 0 ? location.Map.Layers[0] : null;
        if (layer is null || tile.X >= layer.LayerWidth || tile.Y >= layer.LayerHeight)
            return false;

        var backLayer = location.Map.GetLayer("Back");
        if (backLayer?.Tiles[tile.X, tile.Y] is null)
            return false;

        if (HasImpassableBuildingsTile(location, tile))
            return false;

        var tileLocation = new xTile.Dimensions.Location(tile.X * TileSize, tile.Y * TileSize);
        var tileViewport = new xTile.Dimensions.Rectangle(0, 0, TileSize, TileSize);
        if (!location.isTilePassable(tileLocation, tileViewport))
            return false;
        if (HasNoPathProperty(location, tile))
            return false;

        var collisionRect = BuildCollisionRect(tile);
        if (IsBlockedByCollision(location, collisionRect, actor: null, ignoreTransientOccupants: true))
            return false;

        var tileVector = new Vector2(tile.X, tile.Y);

        if (location.Objects.TryGetValue(tileVector, out var obj)
            && obj is SObject placedObject
            && !placedObject.isPassable())
        {
            return false;
        }

        if (location.terrainFeatures.TryGetValue(tileVector, out var terrainFeature)
            && terrainFeature is TerrainFeature feature
            && !feature.isPassable())
        {
            return false;
        }

        foreach (var largeTerrainFeature in location.largeTerrainFeatures)
        {
            if (largeTerrainFeature.getBoundingBox().Intersects(collisionRect))
                return false;
        }

        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.occupiesTile(tile.X, tile.Y))
                return false;
        }

        foreach (var furniture in location.furniture)
        {
            if (furniture is Furniture placedFurniture
                && placedFurniture.GetBoundingBox().Intersects(collisionRect))
            {
                return false;
            }
        }

        // Reject tiles that fall inside building footprints
        foreach (var building in location.buildings)
        {
            if (building is null)
                continue;
            var bx = building.tileX.Value;
            var by = building.tileY.Value;
            var bw = building.tilesWide.Value;
            var bh = building.tilesHigh.Value;
            if (tile.X >= bx && tile.X < bx + bw && tile.Y >= by && tile.Y < by + bh)
                return false;
        }

        return true;
    }

    public bool IsNearWarpTile(GameLocation? location, Point tile, int radius = 0)
    {
        return location is not null
            && location.warps.Any(warp => Math.Abs(warp.X - tile.X) <= radius && Math.Abs(warp.Y - tile.Y) <= radius);
    }

    public bool IsNearEntranceTile(GameLocation? location, Point tile, int radius = 0)
    {
        if (location is null)
            return false;

        if (location.warps.Any(warp => Math.Abs(warp.X - tile.X) <= radius && Math.Abs(warp.Y - tile.Y) <= radius))
            return true;

        if (location.doors?.Pairs is null)
            return false;

        foreach (var door in location.doors.Pairs)
        {
            if (Math.Abs(door.Key.X - tile.X) <= radius && Math.Abs(door.Key.Y - tile.Y) <= radius)
                return true;
        }

        return false;
    }

    public bool IsNpcOverlappingAnyNpc(NPC? npc, GameLocation? location = null)
    {
        if (npc is null)
            return false;

        var currentLocation = location ?? npc.currentLocation;
        if (currentLocation is null)
            return false;

        var npcBounds = npc.GetBoundingBox();
        foreach (var otherNpc in currentLocation.characters)
        {
            if (otherNpc is null || ReferenceEquals(otherNpc, npc))
                continue;

            if (otherNpc.GetBoundingBox().Intersects(npcBounds))
                return true;
        }

        return false;
    }

    public bool TryFindNearestWalkableTile(GameLocation? location, Point preferredTile, int maxRadius, Character? actor, out Point safeTile)
    {
        return TryFindNearestTile(location, preferredTile, maxRadius, actor, useStructuralWalkability: false, out safeTile);
    }

    public bool TryFindNearestStructurallyWalkableTile(GameLocation? location, Point preferredTile, int maxRadius, Character? actor, out Point safeTile)
    {
        return TryFindNearestTile(location, preferredTile, maxRadius, actor, useStructuralWalkability: true, out safeTile);
    }

    private bool TryFindNearestTile(GameLocation? location, Point preferredTile, int maxRadius, Character? actor, bool useStructuralWalkability, out Point safeTile)
    {
        safeTile = Point.Zero;
        if (location is null)
            return false;

        if (IsTileWalkableForSearch(location, preferredTile, actor, useStructuralWalkability))
        {
            safeTile = preferredTile;
            return true;
        }

        for (var radius = 1; radius <= maxRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;

                    var candidate = new Point(preferredTile.X + dx, preferredTile.Y + dy);
                    if (!IsTileWalkableForSearch(location, candidate, actor, useStructuralWalkability))
                        continue;

                    safeTile = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsTileWalkableForSearch(GameLocation location, Point tile, Character? actor, bool useStructuralWalkability)
    {
        return useStructuralWalkability
            ? IsTileStructurallyWalkable(location, tile, actor)
            : IsTileWalkable(location, tile, actor);
    }

    private static Rectangle BuildCollisionRect(Point tile)
    {
        return new Rectangle((tile.X * TileSize) + 8, (tile.Y * TileSize) + 16, TileSize - 16, TileSize - 24);
    }

    private static bool IsBlockedByCollision(GameLocation location, Rectangle collisionRect, Character? actor, bool ignoreTransientOccupants)
    {
        if (!location.isCollidingPosition(collisionRect, Game1.viewport, actor))
            return false;
        if (!ignoreTransientOccupants)
            return true;

        return !IsCollisionOnlyTransientOccupant(location, collisionRect, actor);
    }

    private static bool IsCollisionOnlyTransientOccupant(GameLocation location, Rectangle collisionRect, Character? actor)
    {
        if (location.characters.Any(character =>
                character is not null
                && !ReferenceEquals(character, actor)
                && character.GetBoundingBox().Intersects(collisionRect)))
        {
            return true;
        }

        return Game1.player is not null
            && !ReferenceEquals(Game1.player, actor)
            && Game1.player.GetBoundingBox().Intersects(collisionRect);
    }

    private static bool HasNoPathProperty(GameLocation location, Point tile)
    {
        return HasBlockingProperty(location, tile, "Back")
            || HasBlockingProperty(location, tile, "Buildings")
            || HasBlockingProperty(location, tile, "Front");
    }

    private static bool HasBlockingProperty(GameLocation location, Point tile, string layerName)
    {
        return !string.IsNullOrWhiteSpace(location.doesTileHaveProperty(tile.X, tile.Y, "NoPath", layerName))
            || string.Equals(location.doesTileHaveProperty(tile.X, tile.Y, "NPCBarrier", layerName), "T", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location.doesTileHaveProperty(tile.X, tile.Y, "TouchAction", layerName), "Door", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasImpassableBuildingsTile(GameLocation location, Point tile)
    {
        var buildingsLayer = location.Map?.GetLayer("Buildings");
        if (buildingsLayer is null
            || tile.X < 0
            || tile.Y < 0
            || tile.X >= buildingsLayer.LayerWidth
            || tile.Y >= buildingsLayer.LayerHeight)
        {
            return false;
        }

        var buildingsTile = buildingsLayer.Tiles[tile.X, tile.Y];
        var hasTileIndexPassable = buildingsTile is not null
            && buildingsTile.TileIndexProperties.TryGetValue("Passable", out var tileIndexPassable)
            && string.Equals(tileIndexPassable?.ToString(), "T", StringComparison.OrdinalIgnoreCase);
        return buildingsTile is not null
            && !hasTileIndexPassable
            && !string.Equals(
                location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Buildings"),
                "T",
                StringComparison.OrdinalIgnoreCase);
    }

    public bool HasLineOfSight(GameLocation? location, Point tileA, Point tileB)
    {
        if (location?.Map is null)
            return false;

        var buildingsLayer = location.Map.GetLayer("Buildings");
        if (buildingsLayer is null)
            return true;

        var x0 = tileA.X;
        var y0 = tileA.Y;
        var x1 = tileB.X;
        var y1 = tileB.Y;
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            if (!(x0 == tileA.X && y0 == tileA.Y) && !(x0 == tileB.X && y0 == tileB.Y))
            {
                if (x0 < 0 || y0 < 0 || x0 >= buildingsLayer.LayerWidth || y0 >= buildingsLayer.LayerHeight)
                    return false;
                if (buildingsLayer.Tiles[x0, y0] is not null)
                    return false;
            }

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return true;
    }
}
