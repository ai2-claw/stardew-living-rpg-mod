# Fix NPC Post-Encounter Walkability Rejection and Waypoint Oscillation Lag

Two bugs were introduced by the recent walkability fix. Both have clear root causes and surgical fixes.

## Root Cause Analysis

### Bug 1: Caroline stuck at Town (43,57)

**Log evidence** (line 2779):
```
[REBIND] Caroline could not resolve a reachable movement target for schedule 1330 at (23,71) in Town.
```

**Cause**: [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18522-18560) (line 18522) validates every tile in the PathFindController path against [IsTileStructurallyWalkable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#23-27). This method checks Buildings layer tiles, building footprints, furniture, terrain features, objects, `isCollidingPosition`, `isTilePassable`, [NoPath](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#351-357)/`NPCBarrier` properties, and more.

The problem is that Stardew's [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18894-18923) generates valid NPC-walkable paths that intentionally route through tiles our service considers "unwalkable" (e.g. tiles inside building footprints that have valid warp doors, or tiles with passable objects NPCs can walk over). The path validation was too aggressive — it was meant to catch SVE plant-box tiles (which have a Buildings-layer tile) but ended up rejecting many legitimate paths.

**Fix**: Replace [IsTileStructurallyWalkable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#23-27) in the path validation loop with a lighter check that only rejects tiles with a non-null Buildings-layer tile. This is the original intent of the fix (prevent NPCs from pathing through solid walls/objects) without the false negatives from the full battery of checks.

### Bug 2: Arthur infinite waypoint oscillation (lag)

**Log evidence**: Arthur oscillates between waypoints (34,40) → (35,40) → (36,40) → (34,40) → ... every ~0.5s for 3+ minutes, generating hundreds of `[FORCE_PATH]` log lines and PathFindController allocations every second.

**Cause**: [TryAdvanceSameMapActiveSlotFallback](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18005-18127) (line 18005) only tracks 2 previous waypoints (`LastReachedSameMapWaypoint` + `PreviousReachedSameMapWaypoint`), and [TryResolveReachableSameMapEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18410-18443) (line 18431) only excludes those 2 from candidates. A 3-tile cycle (A→B→C→A) slips through because when at C, only B and A are excluded — but A was 2 hops ago and gets re-selected.

Additionally, `SameMapWaypointHopCount` is incremented on every hop but never checked against any limit, so unreachable targets just oscillate forever.

**Fix**: Add a hop-count ceiling (e.g. 12 hops). When exceeded, clear the fallback to break the cycle — identical to how `FallbackRepathCount >= EncounterVanillaResumeFallbackRepathLimit` already works.

## Proposed Changes

### NPC Encounter Resume Path Validation

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

**Change 1 — Relax path validation** (~line 18540-18555):

Replace the [IsTileStructurallyWalkable](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs#23-27) check in [TryCreateVanillaEncounterResumeController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18522-18560) with a lighter Buildings-layer-only check. Instead of calling the full walkability service, just check `location.Map.GetLayer("Buildings")?.Tiles[tile.X, tile.Y] is not null`. This keeps the original safety (no pathing through walls) without rejecting valid paths.

```diff
-        var walkabilityService = _walkabilityService;
-        if (walkabilityService is null)
-        {
-            controller = null;
-            return false;
-        }
-
-        foreach (var pathTile in controllerPath)
-        {
-            if (walkabilityService.IsTileStructurallyWalkable(location, pathTile, npc))
-                continue;
-
-            controller = null;
-            pathLength = 0;
-            return false;
-        }
+        var buildingsLayer = location.Map?.GetLayer("Buildings");
+        if (buildingsLayer is not null)
+        {
+            foreach (var pathTile in controllerPath)
+            {
+                if (pathTile.X >= 0 && pathTile.Y >= 0
+                    && pathTile.X < buildingsLayer.LayerWidth
+                    && pathTile.Y < buildingsLayer.LayerHeight
+                    && buildingsLayer.Tiles[pathTile.X, pathTile.Y] is not null)
+                {
+                    controller = null;
+                    pathLength = 0;
+                    return false;
+                }
+            }
+        }
```

**Change 2 — Add waypoint hop-count ceiling** (~line 18012-18022):

In [TryAdvanceSameMapActiveSlotFallback](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18005-18127), after `SameMapWaypointHopCount` is incremented, add a check: if hop count exceeds a reasonable limit (12), clear the fallback. This breaks any N-tile cycle.

```diff
             pending.SameMapWaypointHopCount += 1;
+
+            if (pending.SameMapWaypointHopCount > 12)
+            {
+                LogRuntimeThrottled(
+                    $"autonomy:same-map-waypoint-hop-limit:{npc.Name}:{pending.EncounterId}",
+                    TimeSpan.FromSeconds(20),
+                    $"Autonomy: [FORCE_PATH] {npc.Name} exceeded waypoint hop limit for target {DescribeNullablePoint(pending.ActiveTargetTile)} after {pending.SameMapWaypointHopCount} hops; clearing fallback.",
+                    LogLevel.Trace);
+                ClearTemporaryActiveSlotFallback(npc, pending);
+                return;
+            }
+
             ClearTemporaryActiveSlotFallbackController(npc, pending);
```

## Verification Plan

### Automated Tests

No existing unit tests in this project. The build itself is the primary automated check:

```powershell
dotnet build d:\talk\Stardew Mod\stardew-living-rpg-mod\mod\StardewLivingRPG\StardewLivingRPG.csproj
```

### Manual Verification

> [!IMPORTANT]
> These fixes address specific in-game scenarios that require playtesting with the mod loaded in SMAPI. Please verify the following after building:

1. **Load a save with SVE installed** and advance to a time where Caroline has a cross-map schedule entry (around 13:30).
2. **Check Caroline's movement**: She should leave SeedShop, arrive in Town at (43,57), and walk to her scheduled destination (23,71) without getting stuck.
3. **Check for lag**: Watch NPCs with `approach_target` waypoints (like Arthur on Downhill). They should NOT oscillate between 2-3 tiles indefinitely. If an NPC can't reach its target, it should give up after ~12 hops instead of looping.
4. **Check SMAPI console**: No new `exceeded waypoint hop limit` messages should trigger for NPCs that successfully reach their destinations. Only unreachable targets should trigger it.
5. **Pam/Saloon counter check**: Ensure NPCs still don't stand on top of counters or walk through SVE plant boxes (the original fix this builds on).
