# Fix NPC Post-Encounter Walkability on Modded Maps

NPCs after encounters are not detecting non-walkable tiles from modded maps (e.g. SVE). The current [map_paths.json](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/assets/map_paths.json) mask is a band-aid. Vanilla pathfinding handles modded tiles correctly because it delegates to the game's own tile collision APIs. Our custom walkability service reimplements collision detection with fragile heuristics.

## Root Cause Analysis

`NpcWalkabilityService.IsTileWalkableCore` has **three layers of custom indoor tile blocking** that bypass the game engine:

1. **`KnownIndoorOverlayBlockers`** (lines 14-164) — a hardcoded lookup table of ~150 specific tile coordinates + tile-sheet IDs for Saloon, SeedShop, and JojaMart. Only covers known vanilla+SVE tiles.

2. **[TryGetIndoorOverlayBlocker](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#479-501)** (lines 479-500) — heuristic-based indoor tile blocking that checks [Buildings](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#502-514)/[Front](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#515-527)/`AlwaysFront` layers using tile-sheet name matching ([IsFurnitureLikeTileSheet](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#577-584), [IsIndoorBuildingsBlockerTileSheet](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#595-604)). Misses any tile-sheet that doesn't contain "Furniture", "Craftables", "Couch", or "spring_z_extras" in its name.

3. **[EncounterBadTileMaskService](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/EncounterBadTileMaskService.cs#7-71)** + [map_paths.json](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/assets/map_paths.json) — a pre-baked per-map binary mask that marks tiles as bad. Only covers maps manually added to the JSON. Doesn't scale.

**Why vanilla works**: The game's [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#19148-19177) internally calls `GameLocation.isCollidingPosition()` which checks **all** tile properties, passability flags, and object/furniture collision regardless of tile-sheet origin. Our code already calls `location.isTilePassable()` and `location.isCollidingPosition()` (lines 211, 683) — but the custom indoor overlay blocker runs **before** those checks and produces false negatives (marks legit walkable tiles as blocked) or false positives (misses blocking tiles from unknown tile-sheets).

## Proposed Changes

### Walkability Service

#### [MODIFY] [NpcWalkabilityService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs)

**Replace the indoor overlay heuristic system** with the game's native `isCollidingPosition` check, which already correctly handles all modded map content.

Changes to [IsTileWalkableCore](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#181-289):
- **Remove** the [TryGetIndoorOverlayBlocker](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#479-501) call (line 205-206) and all indoor-specific tile-sheet heuristic methods
- **Remove** the `KnownIndoorOverlayBlockers` table (lines 14-164)
- **Keep** the outdoor [Buildings](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#502-514) layer check (lines 199-203) since outdoor buildings tiles are a reliable vanilla indicator
- **Keep** `isTilePassable` check (line 211) — this handles [Back](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#21310-21341) layer passability
- **Keep** `isCollidingPosition` check (line 683) — this is the same API vanilla uses
- **Remove** all helper methods that are now dead code: [TryGetIndoorOverlayBlocker](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#479-501), [TryGetBlockingIndoorBuildingsTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#502-514), [TryGetBlockingIndoorFrontTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#515-527), [TryGetBlockingIndoorOverlayTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#528-548), [TryGetKnownIndoorOverlayBlocker](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#549-566), [TryGetOverlayTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#567-576), [IsFurnitureLikeTileSheet](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#577-584), [IsFurnitureLikeSheetName](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#585-594), [IsIndoorBuildingsBlockerTileSheet](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#595-604), [IsIndoorExtrasSheetName](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#605-612), [IsIndoorPassThroughTileSheet](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#613-620), [IsIndoorPassThroughSheetName](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#621-630), [BuildOverlayBlockerDescription](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#631-638), [BuildKnownOverlayBlockerDescription](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#639-646), [MatchesSheetToken](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#647-651) (both overloads), [IndoorOverlayBlockerRule](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#663-675)

Same changes to [IsTileStageable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#290-379) (lines 313-314).

`blockerDescription` output will lose the detailed indoor overlay info, but the structural walkability check still provides a boolean answer which is what callers need.

---

### Encounter Bad Tile Mask Removal

#### [DELETE] [EncounterBadTileMaskService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/EncounterBadTileMaskService.cs)

No longer needed. The game APIs handle all maps.

#### [DELETE] [map_paths.json](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/assets/map_paths.json)

The pre-baked mask data file.

---

### Callsite Cleanup

#### [MODIFY] [NpcFaceToFaceService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs)

- Remove [EncounterBadTileMaskService](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/EncounterBadTileMaskService.cs#7-71) field and constructor parameter
- Remove [IsMaskedBadTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/EncounterBadTileMaskService.cs#28-40) calls in [TryStage](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs#48-109) (lines 70-74)

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

- Remove `_encounterBadTileMaskService` field and instantiation
- Remove constructor parameter from [NpcFaceToFaceService](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs#9-436) creation
- Remove all `_encounterBadTileMaskService?.IsMaskedBadTile(...)` and `_encounterBadTileMaskService?.HasActiveMask(...)` guard calls (~6 call sites at lines 18423, 18649, 18710, 18789, 18947)
- Remove [TryResolveMaskedSameMapEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18449-18535) method and the masked BFS route builder ([TryBuildMaskedSameMapDetourRoute](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18572-18632), [CanTraverseMaskedSameMapRouteTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18633-18657), [EnumerateMaskedSameMapNeighborTiles](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18658-18663), [TryFindEncounterResumeBadMaskTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18782-18812)) — the vanilla [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#19148-19177) already does correct A* pathfinding that respects all map collision
- Simplify [TryResolveReachableSameMapEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18412-18448) to always use the non-masked code path
- Simplify [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18753-18781) to remove the bad-mask tile scan on the path

## User Review Required

> [!IMPORTANT]
> This removes the entire [EncounterBadTileMaskService](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/EncounterBadTileMaskService.cs#7-71) infrastructure including [map_paths.json](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/assets/map_paths.json). The replacement relies entirely on the game's own `isTilePassable()` + `isCollidingPosition()` + [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#19148-19177) which vanilla Stardew uses internally and which already handles modded map content correctly.

> [!WARNING]
> The `KnownIndoorOverlayBlockers` table was originally added to catch SVE tiles that don't have collision set in xTile tile properties (visual-only overlay tiles that look like furniture but have no `Passable: false` property). If any such tiles still exist, the game APIs won't catch them either. However, this would be a bug in the mod's map data — the game itself wouldn't stop the player from walking through them either. If this is a concern, we can add a lighter-weight fallback that checks only the [Buildings](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#502-514) layer for indoor locations (any non-null [Buildings](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#502-514) tile indoors = blocked), which is simpler and more universal than tile-sheet name matching.

## Verification Plan

### Build Verification
```
cd d:\talk\Stardew Mod\stardew-living-rpg-mod\mod\StardewLivingRPG
dotnet build
```

### Manual Testing (in-game with SVE installed)
1. Load a save with SVE enabled
2. Go to the Saloon and wait for two NPCs to encounter each other
3. After the encounter ends, verify neither NPC walks into non-walkable tiles (bar counter, furniture areas)
4. Repeat in SeedShop and other SVE-modified indoor locations
5. Test outdoor locations (Town, Forest) where SVE adds extra map objects
6. Verify NPCs still pathfind correctly to their schedule destinations after encounters
