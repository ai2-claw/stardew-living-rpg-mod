# Fix: Pathfinding BFS Flood Causing Per-Tick Lag

## Problem

Heavy in-game lag persists despite logging being disabled. The lag correlates with NPCs that fail to resolve schedule destinations after encounters.

**Root cause:** When an NPC cannot reach its schedule target, the resume loop calls [TryResolveReachableSameMapEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18452-18485) which enumerates up to **80 candidate tiles** (radius 4). For each candidate, [TryCanUseVanillaEncounterResumeTarget](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18519-18526) instantiates a [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18949-18978) via **reflection** which runs a full **BFS pathfinding search**.

When the target is genuinely unreachable (e.g., walkability still rejects valid tiles), all 80 candidates fail. Because the loop sets `NextAttemptTick = currentTick + 1`, this 80-BFS sweep runs **every single game tick** (~60/s).

**Observed in logs**: 6-7 NPCs stuck simultaneously (Robin, Demetrius, Caroline, Morrow, Sam, Alex, Shane), producing **480-560 BFS instantiations per tick** — a crippling CPU load.

Secondary contributor: [TryPlanNextCrossMapFallbackLeg](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18616-18656) calls `BuildGraph(Game1.locations)` on every cross-map resolution attempt, adding graph construction overhead.

---

## Proposed Changes

### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

#### Fix 1: Cache failed target resolution per schedule slot

Add a `HashSet<string>` of `"{location}:{targetX},{targetY}"` keys that have already exhaustively failed resolution. Skip re-attempts for the same target until the active schedule slot changes.

On the [PendingVanillaEncounterResume](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#539-615) class, add:
```csharp
public HashSet<string> FailedResolutionTargets { get; } = new(StringComparer.OrdinalIgnoreCase);
```

In [TryStartSameMapActiveSlotFallback](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17882-17948) (line ~17882), at the top, check:
```diff
+var targetKey = $"{pending.ActiveTargetLocation}:{targetTile.X},{targetTile.Y}";
+if (pending.FailedResolutionTargets.Contains(targetKey))
+    return false;
+
 if (!TryResolveReachableSameMapEncounterResumeTarget(...))
 {
+    pending.FailedResolutionTargets.Add(targetKey);
     Monitor.Log(...);
     return false;
 }
```

Clear the set when the active schedule slot changes (in the slot rollover logic, ~line 16263):
```diff
 if (pending.ActiveScheduleTime != previousActiveScheduleTime)
 {
     pending.LastRejectedResumeTimeOfDay = null;
     pending.LastRejectedResumeReason = string.Empty;
     pending.RejectedResumeCountForCurrentSlot = 0;
+    pending.FailedResolutionTargets.Clear();
 }
```

#### Fix 2: Cache failed departure tile resolution per location

Same pattern in [TryResolveCrossMapLegDepartureTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18657-18675) (line 18657):
```diff
+var departureKey = $"{npc.currentLocation.Name}:{requestedTile.X},{requestedTile.Y}";
+if (pending.FailedResolutionTargets.Contains(departureKey))
+{
+    departureTile = Point.Zero;
+    return false;
+}

 foreach (var candidate in EnumerateEncounterResumeMovementTargetCandidates(requestedTile, 4))
 {
     ...
 }

+pending.FailedResolutionTargets.Add(departureKey);
 return false;
```

This requires routing `pending` through to [TryResolveCrossMapLegDepartureTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18657-18675). Two call sites:
- Line 17968: already has `pending` in scope
- Line 18664: in departure tile resolution from the cross-map leg start

#### Fix 3: Throttle non-fallback retry to once per game time step

In the dispatch loop (line ~15960), when the NPC has no fallback and is in the "waiting" state, increase `NextAttemptTick` to avoid re-running expensive resolution every tick:

```diff
-pending.NextAttemptTick = currentTick + 1;
+pending.NextAttemptTick = currentTick + 300; // ~5 seconds at 60fps
```

This only applies to the final "waiting" branch. The every-tick check for fallback progress (`currentTick + 1`) is correct and should remain.

#### Fix 4: Cache world topology graph per tick

In [TryPlanNextCrossMapFallbackLeg](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18616-18656) (line 18627):

```diff
-var graph = _worldTopologyService.BuildGraph(Game1.locations);
+var graph = GetOrBuildCachedWorldGraph();
```

Add a simple one-tick cache:
```csharp
private object? _cachedWorldGraph;
private ulong _cachedWorldGraphTick;

private object GetOrBuildCachedWorldGraph()
{
    var currentTick = _lastObservedTick;
    if (_cachedWorldGraph is not null && _cachedWorldGraphTick == currentTick)
        return _cachedWorldGraph;
    _cachedWorldGraph = _worldTopologyService!.BuildGraph(Game1.locations);
    _cachedWorldGraphTick = currentTick;
    return _cachedWorldGraph;
}
```

---

## Expected Impact

| Fix | Reduction |
|-----|-----------|
| Failed target resolution cache | Eliminates 80-BFS sweep after first failure per slot |
| Failed departure tile cache | Eliminates repeated dead-end route planning |
| Retry throttle (5s) | Reduces re-entry frequency from 60/s to 0.2/s for waiting NPCs |
| Graph cache | Prevents duplicate graph builds in same tick |

Combined: **~99.5% reduction** in unnecessary pathfinding computation for stuck NPCs.

---

## Verification Plan

### Build
```bash
dotnet build StardewLivingRPG.csproj
```

### Manual Verification
1. Trigger encounters for NPCs whose targets are on modded maps (Arthur in Downhill, Robin in ScienceHouse)
2. Verify no noticeable lag during cross-map NPC walks
3. Verify NPCs that can reach their targets still do so correctly
4. Verify NPCs with genuinely unreachable targets stop retrying immediately and don't re-attempt until the next schedule slot
