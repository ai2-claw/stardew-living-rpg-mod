using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;
using xTile.Layers;
using xTile.Tiles;

namespace StardewLivingRPG.Systems;

public sealed class NpcWalkabilityService
{
    private const int TileSize = 64;
    private static readonly IndoorOverlayBlockerRule[] KnownIndoorOverlayBlockers =
    {
        // SVE Saloon bar counter tiles are drawn from generic town interior sheets.
        new("Saloon", "Buildings", 22, 15, "towninterior", 762),
        new("Saloon", "Buildings", 23, 15, "towninterior", 763),
        new("Saloon", "Buildings", 22, 16, "towninterior", 762),
        new("Saloon", "Buildings", 23, 16, "towninterior", 763),
        new("Saloon", "Buildings", 22, 17, "towninterior", 794),
        new("Saloon", "Buildings", 23, 17, "towninterior", 795),
        new("Saloon", "Buildings", 24, 17, "spring_z_extras", 482),
        new("Saloon", "Buildings", 25, 17, "towninterior", 1472),
        new("Saloon", "Buildings", 26, 17, "towninterior", 1473),
        new("Saloon", "Buildings", 27, 17, "towninterior", 894),
        new("Saloon", "Buildings", 28, 17, "towninterior", 895),
        new("Saloon", "Buildings", 27, 18, "towninterior", 926),
        new("Saloon", "Buildings", 28, 18, "towninterior", 927),
        new("Saloon", "Front", 22, 13, "towninterior", 666),
        new("Saloon", "Front", 23, 13, "towninterior", 667),
        new("Saloon", "Front", 22, 14, "towninterior", 698),
        new("Saloon", "Front", 23, 14, "towninterior", 699),
        new("Saloon", "Front", 22, 15, "towninterior", 730),
        new("Saloon", "Front", 23, 15, "towninterior", 731),
        new("Saloon", "Front", 22, 16, "towninterior", 762),
        new("Saloon", "Front", 23, 16, "towninterior", 763),
        new("Saloon", "Front", 24, 16, "spring_z_extras", 457),
        new("Saloon", "Front", 25, 14, "towninterior", 1376),
        new("Saloon", "Front", 26, 14, "towninterior", 1377),
        new("Saloon", "Front", 27, 14, "towninterior", 756),
        new("Saloon", "Front", 25, 15, "towninterior", 1408),
        new("Saloon", "Front", 26, 15, "towninterior", 1409),
        new("Saloon", "Front", 27, 15, "towninterior", 757),
        new("Saloon", "Front", 25, 16, "towninterior", 1440),
        new("Saloon", "Front", 26, 16, "towninterior", 1441),
        new("Saloon", "Front", 27, 16, "towninterior", 862),
        new("Saloon", "Front", 28, 16, "towninterior", 863),

        // SVE indoor furniture/fixture clusters that are drawn from generic overlay sheets.
        new("SeedShop", "Buildings", 35, 14, "towninterior", 87),
        new("SeedShop", "Buildings", 36, 14, "towninterior", 119),
        new("SeedShop", "Buildings", 37, 14, "towninterior", 138),
        new("SeedShop", "Buildings", 38, 14, "towninterior", 118),
        new("SeedShop", "Buildings", 39, 14, "towninterior", 86),
        new("SeedShop", "Buildings", 35, 15, "towninterior", 119),
        new("SeedShop", "Buildings", 36, 15, "towninterior", 819),
        new("SeedShop", "Buildings", 38, 15, "towninterior", 819),
        new("SeedShop", "Buildings", 39, 15, "towninterior", 118),
        new("SeedShop", "Buildings", 35, 16, "towninterior", 819),
        new("SeedShop", "Buildings", 36, 16, "towninterior", 784),
        new("SeedShop", "Buildings", 37, 16, "towninterior", 785),
        new("SeedShop", "Buildings", 38, 16, "towninterior", 786),
        new("SeedShop", "Buildings", 39, 16, "towninterior", 819),
        new("SeedShop", "Buildings", 36, 17, "towninterior", 816),
        new("SeedShop", "Buildings", 37, 17, "towninterior", 817),
        new("SeedShop", "Buildings", 38, 17, "towninterior", 818),
        new("SeedShop", "Buildings", 34, 18, "towninterior", 819),
        new("SeedShop", "Buildings", 40, 18, "towninterior", 819),
        new("SeedShop", "Buildings", 36, 19, "towninterior", 886),
        new("SeedShop", "Buildings", 37, 19, "towninterior", 886),
        new("SeedShop", "Buildings", 38, 19, "towninterior", 886),
        new("SeedShop", "Buildings", 34, 20, "towninterior", 819),
        new("SeedShop", "Buildings", 40, 20, "towninterior", 819),
        new("SeedShop", "Buildings", 36, 21, "towninterior", 886),
        new("SeedShop", "Buildings", 37, 21, "towninterior", 886),
        new("SeedShop", "Buildings", 38, 21, "towninterior", 886),
        new("SeedShop", "Front", 35, 14, "towninterior", 755),
        new("SeedShop", "Front", 36, 14, "towninterior", 787),
        new("SeedShop", "Front", 38, 14, "towninterior", 787),
        new("SeedShop", "Front", 39, 14, "towninterior", 755),
        new("SeedShop", "Front", 35, 15, "towninterior", 787),
        new("SeedShop", "Front", 36, 15, "towninterior", 752),
        new("SeedShop", "Front", 37, 15, "towninterior", 753),
        new("SeedShop", "Front", 38, 15, "towninterior", 754),
        new("SeedShop", "Front", 39, 15, "towninterior", 787),
        new("SeedShop", "Front", 34, 16, "towninterior", 755),
        new("SeedShop", "Front", 40, 16, "towninterior", 755),
        new("SeedShop", "Front", 34, 17, "towninterior", 787),
        new("SeedShop", "Front", 40, 17, "towninterior", 787),
        new("SeedShop", "Front", 34, 18, "towninterior", 755),
        new("SeedShop", "Front", 40, 18, "towninterior", 755),
        new("SeedShop", "Front", 34, 19, "towninterior", 787),
        new("SeedShop", "Front", 40, 19, "towninterior", 787),
        new("SeedShop", "Front", 35, 20, "towninterior", 755),
        new("SeedShop", "Front", 39, 20, "towninterior", 755),
        new("SeedShop", "Front", 35, 21, "towninterior", 787),
        new("SeedShop", "Front", 39, 21, "towninterior", 787),

        new("JojaMart", "Buildings", 22, 22, "towninterior", 154),
        new("JojaMart", "Buildings", 23, 22, "towninterior", 591),
        new("JojaMart", "Buildings", 27, 22, "towninterior", 591),
        new("JojaMart", "Buildings", 28, 22, "towninterior", 591),
        new("JojaMart", "Buildings", 29, 22, "towninterior", 1890),
        new("JojaMart", "Buildings", 22, 23, "towninterior", 186),
        new("JojaMart", "Buildings", 23, 23, "towninterior", 80),
        new("JojaMart", "Buildings", 27, 23, "towninterior", 80),
        new("JojaMart", "Buildings", 28, 23, "towninterior", 80),
        new("JojaMart", "Buildings", 29, 23, "towninterior", 1890),
        new("JojaMart", "Buildings", 21, 24, "towninterior", 120),
        new("JojaMart", "Buildings", 22, 24, "towninterior", 218),
        new("JojaMart", "Buildings", 23, 24, "towninterior", 112),
        new("JojaMart", "Buildings", 24, 24, "towninterior", 112),
        new("JojaMart", "Buildings", 25, 24, "towninterior", 486),
        new("JojaMart", "Buildings", 26, 24, "towninterior", 487),
        new("JojaMart", "Buildings", 27, 24, "towninterior", 488),
        new("JojaMart", "Buildings", 28, 24, "towninterior", 112),
        new("JojaMart", "Buildings", 29, 24, "towninterior", 1890),
        new("JojaMart", "Buildings", 22, 25, "towninterior", 1179),
        new("JojaMart", "Buildings", 23, 25, "towninterior", 768),
        new("JojaMart", "Buildings", 27, 25, "towninterior", 520),
        new("JojaMart", "Buildings", 29, 25, "towninterior", 1890),
        new("JojaMart", "Buildings", 23, 26, "towninterior", 768),
        new("JojaMart", "Buildings", 28, 26, "towninterior", 2147),
        new("JojaMart", "Buildings", 29, 26, "towninterior", 1890),
        new("JojaMart", "Buildings", 23, 27, "towninterior", 864),
        new("JojaMart", "Buildings", 29, 27, "towninterior", 1890),
        new("JojaMart", "Buildings", 27, 28, "towninterior", 579),
        new("JojaMart", "Buildings", 28, 28, "towninterior", 580),
        new("JojaMart", "Buildings", 29, 28, "towninterior", 1922),
        new("JojaMart", "Front", 21, 22, "towninterior", 56),
        new("JojaMart", "Front", 22, 22, "towninterior", 1426),
        new("JojaMart", "Front", 23, 22, "towninterior", 1426),
        new("JojaMart", "Front", 24, 22, "towninterior", 1426),
        new("JojaMart", "Front", 25, 22, "towninterior", 1426),
        new("JojaMart", "Front", 26, 22, "towninterior", 1426),
        new("JojaMart", "Front", 27, 22, "towninterior", 1426),
        new("JojaMart", "Front", 28, 22, "towninterior", 1426),
        new("JojaMart", "Front", 21, 23, "towninterior", 88),
        new("JojaMart", "Front", 22, 23, "towninterior", 215),
        new("JojaMart", "Front", 24, 23, "towninterior", 80),
        new("JojaMart", "Front", 25, 23, "towninterior", 80),
        new("JojaMart", "Front", 26, 23, "towninterior", 80),
        new("JojaMart", "Front", 23, 24, "towninterior", 488),
        new("JojaMart", "Front", 28, 25, "towninterior", 2115),
        new("JojaMart", "Front", 21, 28, "towninterior", 331),
        new("JojaMart", "Front", 22, 28, "towninterior", 1921),
        new("JojaMart", "Front", 23, 28, "towninterior", 1921),
        new("JojaMart", "Front", 24, 28, "towninterior", 1921),
        new("JojaMart", "Front", 27, 28, "towninterior", 676),
        new("JojaMart", "Front", 28, 28, "towninterior", 649),
        new("JojaMart", "AlwaysFront", 22, 21, "towninterior", 1892),
        new("JojaMart", "AlwaysFront", 23, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 24, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 25, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 26, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 27, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 28, 21, "towninterior", 1857),
        new("JojaMart", "AlwaysFront", 21, 28, "towninterior", 1921),
        new("JojaMart", "AlwaysFront", 25, 28, "towninterior", 1921),
        new("JojaMart", "AlwaysFront", 26, 28, "towninterior", 1921),
        new("JojaMart", "AlwaysFront", 27, 28, "towninterior", 1921),
        new("JojaMart", "AlwaysFront", 28, 28, "towninterior", 1921)
    };

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

        var buildingsLayer = location.Map.GetLayer("Buildings");
        if (location.IsOutdoors && buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
        {
            blockerDescription = "outdoor_buildings_layer";
            return false;
        }

        if (TryGetIndoorOverlayBlocker(location, tile, out blockerDescription))
            return false;

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

        var buildingsLayer = location.Map.GetLayer("Buildings");
        if (location.IsOutdoors && buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
            return false;
        if (TryGetIndoorOverlayBlocker(location, tile, out _))
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

    private static bool TryGetIndoorOverlayBlocker(GameLocation location, Point tile, out string? blockerDescription)
    {
        blockerDescription = null;
        if (location.IsOutdoors || location.Map is null)
            return false;

        if (TryGetKnownIndoorOverlayBlocker(location, location.Map.GetLayer("Buildings"), tile, "Buildings", out blockerDescription))
            return true;
        if (TryGetKnownIndoorOverlayBlocker(location, location.Map.GetLayer("Front"), tile, "Front", out blockerDescription))
            return true;
        if (TryGetKnownIndoorOverlayBlocker(location, location.Map.GetLayer("AlwaysFront"), tile, "AlwaysFront", out blockerDescription))
            return true;

        if (TryGetBlockingIndoorBuildingsTile(location.Map.GetLayer("Buildings"), tile, out blockerDescription))
            return true;
        if (TryGetBlockingIndoorFrontTile(location.Map.GetLayer("Front"), tile, out blockerDescription))
            return true;
        if (TryGetBlockingIndoorOverlayTile(location.Map.GetLayer("AlwaysFront"), tile, "AlwaysFront", blockByDefault: true, out blockerDescription))
            return true;

        return false;
    }

    private static bool TryGetBlockingIndoorBuildingsTile(Layer? layer, Point tile, out string? blockerDescription)
    {
        blockerDescription = null;
        if (!TryGetOverlayTile(layer, tile, out var overlayTile))
            return false;

        if (!IsIndoorBuildingsBlockerTileSheet(overlayTile.TileSheet))
            return false;

        blockerDescription = BuildOverlayBlockerDescription("Buildings", overlayTile);
        return true;
    }

    private static bool TryGetBlockingIndoorFrontTile(Layer? layer, Point tile, out string? blockerDescription)
    {
        blockerDescription = null;
        if (!TryGetOverlayTile(layer, tile, out var overlayTile))
            return false;

        if (!IsFurnitureLikeTileSheet(overlayTile.TileSheet))
            return false;

        blockerDescription = BuildOverlayBlockerDescription("Front", overlayTile);
        return true;
    }

    private static bool TryGetBlockingIndoorOverlayTile(Layer? layer, Point tile, string layerName, bool blockByDefault, out string? blockerDescription)
    {
        blockerDescription = null;
        if (!TryGetOverlayTile(layer, tile, out var overlayTile))
            return false;

        var tileSheet = overlayTile.TileSheet;
        if (blockByDefault)
        {
            if (IsIndoorPassThroughTileSheet(tileSheet))
                return false;
        }
        else if (!IsFurnitureLikeTileSheet(tileSheet))
        {
            return false;
        }

        blockerDescription = BuildOverlayBlockerDescription(layerName, overlayTile);
        return true;
    }

    private static bool TryGetKnownIndoorOverlayBlocker(GameLocation location, Layer? layer, Point tile, string layerName, out string? blockerDescription)
    {
        blockerDescription = null;
        if (!TryGetOverlayTile(layer, tile, out var overlayTile))
            return false;

        foreach (var rule in KnownIndoorOverlayBlockers)
        {
            if (!rule.Matches(location.Name, layerName, tile, overlayTile))
                continue;

            blockerDescription = BuildKnownOverlayBlockerDescription(location.Name, layerName, overlayTile);
            return true;
        }

        return false;
    }

    private static bool TryGetOverlayTile(Layer? layer, Point tile, out Tile overlayTile)
    {
        overlayTile = null!;
        if (layer is null || tile.X < 0 || tile.Y < 0 || tile.X >= layer.LayerWidth || tile.Y >= layer.LayerHeight)
            return false;

        overlayTile = layer.Tiles[tile.X, tile.Y];
        return overlayTile is not null;
    }

    private static bool IsFurnitureLikeTileSheet(TileSheet? tileSheet)
    {
        if (tileSheet is null)
            return false;

        return IsFurnitureLikeSheetName(tileSheet.Id) || IsFurnitureLikeSheetName(tileSheet.ImageSource);
    }

    private static bool IsFurnitureLikeSheetName(string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return false;

        return sheetName.Contains("Furniture", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("Craftables", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("Couch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIndoorBuildingsBlockerTileSheet(TileSheet? tileSheet)
    {
        if (tileSheet is null)
            return false;

        return IsFurnitureLikeTileSheet(tileSheet)
            || IsIndoorExtrasSheetName(tileSheet.Id)
            || IsIndoorExtrasSheetName(tileSheet.ImageSource);
    }

    private static bool IsIndoorExtrasSheetName(string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return false;

        return sheetName.Contains("spring_z_extras", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIndoorPassThroughTileSheet(TileSheet? tileSheet)
    {
        if (tileSheet is null)
            return false;

        return IsIndoorPassThroughSheetName(tileSheet.Id) || IsIndoorPassThroughSheetName(tileSheet.ImageSource);
    }

    private static bool IsIndoorPassThroughSheetName(string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return false;

        return sheetName.Contains("WallsAndFloors", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("paths", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("shadow", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildOverlayBlockerDescription(string layerName, Tile overlayTile)
    {
        var tileSheet = overlayTile.TileSheet;
        var sheetId = string.IsNullOrWhiteSpace(tileSheet?.Id) ? "none" : tileSheet.Id;
        var imageSource = string.IsNullOrWhiteSpace(tileSheet?.ImageSource) ? "none" : tileSheet.ImageSource;
        return $"indoor_overlay(layer={layerName}, sheet={sheetId}, image={imageSource}, index={overlayTile.TileIndex})";
    }

    private static string BuildKnownOverlayBlockerDescription(string locationName, string layerName, Tile overlayTile)
    {
        var tileSheet = overlayTile.TileSheet;
        var sheetId = string.IsNullOrWhiteSpace(tileSheet?.Id) ? "none" : tileSheet.Id;
        var imageSource = string.IsNullOrWhiteSpace(tileSheet?.ImageSource) ? "none" : tileSheet.ImageSource;
        return $"known_indoor_overlay(map={locationName}, layer={layerName}, sheet={sheetId}, image={imageSource}, index={overlayTile.TileIndex})";
    }

    private static bool MatchesSheetToken(TileSheet? tileSheet, string expectedToken)
    {
        return MatchesSheetToken(tileSheet?.Id, expectedToken) || MatchesSheetToken(tileSheet?.ImageSource, expectedToken);
    }

    private static bool MatchesSheetToken(string? rawSheetName, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(rawSheetName))
            return false;

        var normalized = Path.GetFileNameWithoutExtension(rawSheetName)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return normalized == expectedToken;
    }

    private readonly record struct IndoorOverlayBlockerRule(string MapName, string LayerName, int X, int Y, string SheetToken, int TileIndex)
    {
        public bool Matches(string? locationName, string layerName, Point tile, Tile overlayTile)
        {
            return string.Equals(locationName, MapName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(layerName, LayerName, StringComparison.Ordinal)
                && tile.X == X
                && tile.Y == Y
                && overlayTile.TileIndex == TileIndex
                && MatchesSheetToken(overlayTile.TileSheet, SheetToken);
        }
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
