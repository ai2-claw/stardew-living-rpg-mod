# Fix Remaining NPC Post-Encounter Walkability Issues

Two distinct bugs persist after the initial walkability refactor.

## Bug 1: Caroline walks through plant box on Town 40,57

**Symptom**: Caroline's post-encounter path traverses a non-walkable tile (SVE plant box on `Buildings` layer) on the outdoor Town map.

**Root Cause**: [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18522-18543) creates a [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18877-18906) and reads the resulting path, but **never validates each path tile** against our [IsTileStructurallyWalkable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#23-27). Vanilla's internal A* uses its own collision checks which evidently miss this SVE tile. Our walkability correctly rejects it (outdoor `Buildings` check catches it), but we never apply that check to the path.

**Fix**: After reading the controller path in [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18522-18543), validate each tile against [IsTileStructurallyWalkable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#23-27). Reject the path if any tile fails.

### Changes

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

In [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L18522-L18542) — after the [TryReadControllerPath](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18907-18920) success check (line 18540), add a loop that validates each tile in `controllerPath` against `_walkabilityService.IsTileStructurallyWalkable(location, pathTile, npc)`. If any tile fails, set `controller = null` and `return false`.

```csharp
// After pathLength = controllerPath.Count;
foreach (var pathNode in controllerPath)
{
    var pathTile = new Point(pathNode.X, pathNode.Y);
    if (!_walkabilityService.IsTileStructurallyWalkable(location, pathTile, npc))
    {
        controller = null;
        pathLength = 0;
        return false;
    }
}
```

This causes [TryResolveReachableSameMapEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18410-18443) to try the next candidate tile instead, naturally steering the NPC around the blocked tile.

---

## Bug 2: Pam stands on Saloon bar counter tile 8,18

**Symptom**: Pam stands on bar counter tile 8,18 instead of floor tile 7,18.

**Root Cause**: The `Buildings` layer check in [IsTileWalkableCore](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#L45-L50) is guarded by `location.IsOutdoors`. The Saloon bar counter sits on the `Buildings` layer but is never rejected indoors. [IsTileStageable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#134-221) has the same guard.

**Fix**: Remove the `IsOutdoors` guard. Any non-null `Buildings` tile = non-walkable regardless of indoor/outdoor.

### Changes

#### [MODIFY] [NpcWalkabilityService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs)

In [IsTileWalkableCore](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#28-133) (lines 45-50):

```diff
 var buildingsLayer = location.Map.GetLayer("Buildings");
-if (location.IsOutdoors && buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
+if (buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
 {
-    blockerDescription = "outdoor_buildings_layer";
+    blockerDescription = "buildings_layer";
     return false;
 }
```

Same in [IsTileStageable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#134-221) (lines 154-156):

```diff
 var buildingsLayer = location.Map.GetLayer("Buildings");
-if (location.IsOutdoors && buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
+if (buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
     return false;
```

## Verification Plan

### Build
```
cd d:\talk\Stardew Mod\stardew-living-rpg-mod\mod\StardewLivingRPG
dotnet build
```

### In-Game Testing
1. SVE Saloon: Pam encounter near bar → verify she does NOT land on tile 8,18
2. SVE Town: Caroline encounter → verify path does NOT traverse tile 40,57
3. Vanilla interiors (SeedShop, Hospital): verify NPCs still navigate correctly
4. Regression: no NPCs stuck without valid walkable tiles
