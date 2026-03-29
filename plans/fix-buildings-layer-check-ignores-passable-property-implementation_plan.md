# Fix: Buildings Layer Check Ignores Passable Property

## Problem

NPCs cannot reach their vanilla schedule destinations after encounters. MarlonFay oscillates near [(27,19)](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#45-52) in Custom_AdventurerSummit, Arthur oscillates near [(35,39)](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#45-52) in Downhill. Both are valid schedule destinations the game's own [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18931-18960) can reach.

**Root cause:** Two code paths blanket-reject ALL tiles with a non-null Buildings layer tile, without checking the `Passable` tile property:

1. `NpcWalkabilityService.IsTileWalkableCore` (line 46) — `buildingsLayer?.Tiles[tile.X, tile.Y] is not null` → reject
2. `ModEntry.TryCreateVanillaEncounterResumeController` (lines 18573-18591) — same blanket Buildings tile check on the path

Many modded maps (Custom_AdventurerSummit, Downhill, etc.) use the Buildings layer for visual decoration (bridges, archways, overlay elements) and set `Passable: T` on those tiles so NPCs can walk through them. Vanilla's [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18931-18960) respects this property. Our code does not.

This was introduced in the [fix-remaining-npc-post-encounter-walkability-issues plan](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/plans/fix-remaining-npc-post-encounter-walkability-issues-implementation_plan.md) which removed the `IsOutdoors` guard and added path validation.

> [!IMPORTANT]
> The oscillation loop and `HopLimitExhaustedTarget` guard bypass are **downstream symptoms**. The correct fix is to make NPC pathing actually reach the target tile. The `HopLimitExhaustedTarget` deferral fix from the previous plan is a nice safety net but not the primary fix.

---

## Proposed Changes

### [MODIFY] [NpcWalkabilityService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs)

#### Fix 1: Respect `Passable` property on Buildings tiles (line 46)

Check whether the Buildings tile has `Passable: T` set. If so, allow the tile.

```diff
 var buildingsLayer = location.Map.GetLayer("Buildings");
-if (buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
+var buildingsTile = buildingsLayer?.Tiles[tile.X, tile.Y];
+if (buildingsTile is not null
+    && !string.Equals(
+        buildingsTile.TileIndexProperties?.GetValue("Passable")?.ToString(),
+        "T",
+        StringComparison.OrdinalIgnoreCase)
+    && !string.Equals(
+        location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Buildings"),
+        "T",
+        StringComparison.OrdinalIgnoreCase))
 {
     blockerDescription = "buildings_layer";
     return false;
 }
```

This checks both the tile-index property (baked into the tilesheet) and the tile-instance property (set per-tile in the map), matching how vanilla evaluates Buildings passability.

#### Fix 2: Same fix in [IsTileStageable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#134-221) (line 154-156)

Apply the same `Passable` property check:

```diff
 var buildingsLayer = location.Map.GetLayer("Buildings");
-if (buildingsLayer?.Tiles[tile.X, tile.Y] is not null)
+var buildingsTile = buildingsLayer?.Tiles[tile.X, tile.Y];
+if (buildingsTile is not null
+    && !string.Equals(
+        buildingsTile.TileIndexProperties?.GetValue("Passable")?.ToString(),
+        "T",
+        StringComparison.OrdinalIgnoreCase)
+    && !string.Equals(
+        location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Buildings"),
+        "T",
+        StringComparison.OrdinalIgnoreCase))
     return false;
```

---

### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

#### Fix 3: Respect `Passable` in path validation (lines 18573-18591)

[TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18555-18597) does the same blanket Buildings rejection on path tiles. Apply the same `Passable` property check:

```diff
 var buildingsLayer = location.Map?.GetLayer("Buildings");
 if (buildingsLayer is not null)
 {
     foreach (var pathTile in controllerPath)
     {
         if (pathTile.X < 0 || pathTile.Y < 0
             || pathTile.X >= buildingsLayer.LayerWidth
             || pathTile.Y >= buildingsLayer.LayerHeight)
         {
             continue;
         }

-        if (buildingsLayer.Tiles[pathTile.X, pathTile.Y] is null)
-            continue;
-
-        controller = null;
-        pathLength = 0;
-        return false;
+        var bTile = buildingsLayer.Tiles[pathTile.X, pathTile.Y];
+        if (bTile is not null
+            && !string.Equals(
+                bTile.TileIndexProperties?.GetValue("Passable")?.ToString(),
+                "T",
+                StringComparison.OrdinalIgnoreCase)
+            && !string.Equals(
+                location.doesTileHaveProperty(pathTile.X, pathTile.Y, "Passable", "Buildings"),
+                "T",
+                StringComparison.OrdinalIgnoreCase))
+        {
+            controller = null;
+            pathLength = 0;
+            return false;
+        }
     }
 }
```

#### Fix 4: Also defer `HopLimitExhaustedTarget` clear (line 16263)

While the Passable fix is the primary fix, the `HopLimitExhaustedTarget` guard should still persist across rollovers when the target tile doesn't change (safety net for genuinely unreachable tiles):

```diff
 if (pending.ActiveScheduleTime != previousActiveScheduleTime)
 {
     pending.LastRejectedResumeTimeOfDay = null;
     pending.LastRejectedResumeReason = string.Empty;
     pending.RejectedResumeCountForCurrentSlot = 0;
-    pending.HopLimitExhaustedTarget = null;
 }
```

After target tile resolution (~line 16294):

```diff
+if (pending.ActiveScheduleTime != previousActiveScheduleTime
+    && pending.HopLimitExhaustedTarget.HasValue
+    && pending.ActiveTargetTile.HasValue
+    && pending.HopLimitExhaustedTarget.Value != pending.ActiveTargetTile.Value)
+{
+    pending.HopLimitExhaustedTarget = null;
+}
```

#### Fix 5: Throttle FORCE_PATH waypoint advancement log (~line 18079)

```diff
-                Monitor.Log(
-                    $"Autonomy: [FORCE_PATH] {npc.Name} advanced same-map active-slot fallback ...",
-                    LogLevel.Debug);
+                LogRuntimeThrottled(
+                    $"autonomy:same-map-waypoint-advance:{npc.Name}:{pending.EncounterId}",
+                    TimeSpan.FromSeconds(5),
+                    $"Autonomy: [FORCE_PATH] {npc.Name} advanced same-map active-slot fallback ...",
+                    LogLevel.Trace);
```

---

## Verification Plan

### Build
```bash
dotnet build StardewLivingRPG.csproj
```

### Manual Verification
1. Trigger encounters for MarlonFay (Custom_AdventurerSummit) and Arthur (Downhill)
2. Confirm NPCs path **directly to target tiles** without oscillation
3. Confirm no FORCE_PATH log spam
4. Confirm NPCs still don't walk through actual impassable tiles (e.g., SVE plant boxes on Town, Saloon bar counter)
