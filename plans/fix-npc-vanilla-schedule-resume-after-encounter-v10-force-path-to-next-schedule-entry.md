# Fix NPC Vanilla Schedule Resume After Encounter (v10 - Force Path to Next Schedule Entry)

## Problem Summary

After v9 implementation, encounters complete successfully but NPCs remain stuck because:

**Root Cause:** `checkSchedule(Game1.timeOfDay)` only creates a `PathFindController` when there's a schedule entry at the **current time**. When an encounter completes at time 920 but the NPC's next schedule entry is at 1230:
- Vanilla `checkSchedule(920)` returns without creating a controller
- `HasVanillaResumeState(npc)` returns `false` (no controller, not moving)
- NPCs stay frozen until their next schedule time (300+ ticks)

**Why SVE NPCs work:** Their encounters happen to complete at times that align with their schedule entries (e.g., time 620/620), so `checkSchedule` creates controllers successfully.

**Evidence from logs:**
```
[23:52:59] [REBIND] Shane reset complete: next_schedule_time=1230, controller=null, isMoving=False
[23:52:59] [REBIND] Alex reset complete: next_schedule_time=1300, controller=null, isMoving=False
```

Both NPCs have `next_schedule_time` in the future, but no controller is created.

## Solution: Force Path to Next Schedule Entry

After calling `checkSchedule`, if no controller is created BUT there's a future schedule entry:
1. Parse the next schedule entry to extract target location and position
2. Manually create a `PathFindController` to send the NPC there immediately
3. Set up state so vanilla scheduling takes over when they arrive

## Implementation

### File: `mod/StardewLivingRPG/ModEntry.cs`

#### 1. Add helper method to parse schedule entry target location (~line 15371)

Add after `DescribeScheduleDestination`:

```csharp
private static bool TryParseScheduleEntryTarget(
    NPC npc,
    int scheduleTime,
    out string? targetLocationName,
    out Vector2? targetTile)
{
    targetLocationName = null;
    targetTile = null;

    if (npc.Schedule is null || !npc.Schedule.TryGetValue(scheduleTime, out var scheduleEntry))
        return false;

    // Try to get target location name
    var locationCandidates = new[] { "targetLocationName", "TargetLocationName", "locationName", "LocationName", "location", "Location", "destination", "Destination" };
    foreach (var candidate in locationCandidates)
    {
        if (TryGetMemberValue(scheduleEntry, candidate, out var locValue) && locValue is not null)
        {
            targetLocationName = locValue.ToString();
            break;
        }
    }

    // Try to get target tile position
    var tileCandidates = new[] { "endPoint", "EndPoint", "targetTile", "TargetTile", "tile", "Tile", "point", "Point" };
    foreach (var candidate in tileCandidates)
    {
        if (TryGetMemberValue(scheduleEntry, candidate, out var tileValue) && tileValue is Vector2 v)
        {
            targetTile = v;
            break;
        }
    }

    return targetLocationName is not null;
}
```

#### 2. Add method to create path controller to schedule destination (~line 15390)

```csharp
private bool TryForcePathToNextScheduleEntry(NPC npc, int nextScheduleTime, string encounterId)
{
    if (!TryParseScheduleEntryTarget(npc, nextScheduleTime, out var targetLocationName, out var targetTile))
    {
        Monitor.Log($"Autonomy: [FORCE_PATH] {npc.Name} unable to parse schedule entry at {nextScheduleTime}, skipping force path.", LogLevel.Warn);
        return false;
    }

    // Get target location
    var targetLocation = targetLocationName is not null
        ? Game1.getLocationFromName(targetLocationName)
        : npc.currentLocation;

    if (targetLocation is null)
    {
        Monitor.Log($"Autonomy: [FORCE_PATH] {npc.Name} target location '{targetLocationName}' not found.", LogLevel.Warn);
        return false;
    }

    // Use a default tile if not specified (center of location or spawn point)
    var finalTargetTile = targetTile ?? new Vector2(targetLocation.Map.Layers[0].LayerWidth / 2, targetLocation.Map.Layers[0].LayerHeight / 2);

    // Create PathFindController using reflection
    try
    {
        var controllerType = Type.GetType("StardewValley.PathFindController, Stardew Valley");
        if (controllerType is null)
        {
            Monitor.Log($"Autonomy: [FORCE_PATH] {npc.Name} PathFindController type not found.", LogLevel.Warn);
            return false;
        }

        // Create PathFindController(NPC npc, GameLocation location, Vector2 targetPosition)
        var controller = Activator.CreateInstance(controllerType, npc, targetLocation, finalTargetTile);

        if (controller is not null)
        {
            npc.controller = controller;
            TrySetMemberValue(npc, "lastAttemptedSchedule", nextScheduleTime);
            TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);

            Monitor.Log(
                $"Autonomy: [FORCE_PATH] {npc.Name} forced path to {targetLocationName} at ({finalTargetTile.X},{finalTargetTile.Y}) for schedule time {nextScheduleTime} after encounter {encounterId}.",
                LogLevel.Debug);
            return true;
        }
    }
    catch (Exception ex)
    {
        Monitor.Log($"Autonomy: [FORCE_PATH] {npc.Name} failed to create PathFindController: {ex.Message}", LogLevel.Warn);
    }

    return false;
}
```

#### 3. Modify `TryAdvanceVanillaScheduleAtCurrentTime` (~line 15223)

After the existing `checkSchedule` call, add force-path logic:

```csharp
private void TryAdvanceVanillaScheduleAtCurrentTime(
    NPC npc,
    string encounterId,
    out int? nextScheduleTime,
    out bool checkScheduleInvoked,
    out string checkScheduleMethod)
{
    nextScheduleTime = null;
    checkScheduleInvoked = false;
    checkScheduleMethod = "none";

    TrySetMemberValue(npc, "followSchedule", true);
    TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
    TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);
    ClearEncounterMovementBlockingState(npc);

    var beforeController = npc.controller;
    checkScheduleInvoked = TryCallCheckSchedule(npc, Game1.timeOfDay, out checkScheduleMethod);
    var afterController = npc.controller;

    nextScheduleTime = TryGetNextScheduleTime(npc, Game1.timeOfDay);

    // NEW: If checkSchedule didn't create a controller but there's a future schedule entry,
    // force-create a path to that next destination
    if (checkScheduleInvoked && nextScheduleTime.HasValue)
    {
        var hasControllerNow = npc.controller is not null
            || npc.isMoving()
            || (TryGetMemberValue(npc, "temporaryController", out var tempCtrl) && tempCtrl is not null);

        if (!hasControllerNow && beforeController == afterController)
        {
            Monitor.Log(
                $"Autonomy: [REBIND] {npc.Name} checkSchedule did not create controller at time {Game1.timeOfDay}, next entry at {nextScheduleTime.Value}. Attempting force path.",
                LogLevel.Debug);

            TryForcePathToNextScheduleEntry(npc, nextScheduleTime.Value, encounterId);
        }
    }

    Monitor.Log(
        $"Autonomy: [REBIND] {npc.Name} reset complete: lastAttemptedSchedule={DescribeMemberValue(npc, "lastAttemptedSchedule")}, previousEndPoint={DescribePointMember(npc, "previousEndPoint")}, check_schedule_invoked={checkScheduleInvoked}, check_schedule_method={checkScheduleMethod}, next_schedule_time={(nextScheduleTime?.ToString(CultureInfo.InvariantCulture) ?? "none")}.",
        LogLevel.Debug);
}
```

#### 4. Update call sites to pass `encounterId` (~line 15216)

```csharp
// In TryRebindVanillaScheduleAtCurrentTime
TryAdvanceVanillaScheduleAtCurrentTime(
    npc,
    encounterId,  // NEW parameter
    out nextScheduleTime,
    out checkScheduleInvoked,
    out checkScheduleMethod);
```

And update the method signature:

```csharp
private void TryAdvanceVanillaScheduleAtCurrentTime(
    NPC npc,
    string encounterId,  // NEW parameter
    out int? nextScheduleTime,
    out bool checkScheduleInvoked,
    out string checkScheduleMethod)
```

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | ~15371 | Add `TryParseScheduleEntryTarget` helper method |
| `ModEntry.cs` | ~15390 | Add `TryForcePathToNextScheduleEntry` method |
| `ModEntry.cs` | ~15223 | Modify `TryAdvanceVanillaScheduleAtCurrentTime` to add force-path logic |
| `ModEntry.cs` | ~15216 | Update call site to pass `encounterId` |

## Verification

1. **Build:** `dotnet build` must pass

2. **Manual test:**
   - Launch game via SMAPI
   - Trigger encounter with vanilla NPCs (Alex, Shane, Sam, Vincent)
   - Wait for encounter to complete
   - **Expected logs:**
     - `[FORCE_PATH] Shane forced path to JojaMart at (...) for schedule time 1230`
     - `returned Shane to vanilla schedule... controller=PathFindController`
   - **Expected behavior:**
     - BOTH NPCs should start moving within 1-2 seconds after encounter completes
     - NPCs should walk (not teleport) to their next scheduled destination
     - No NPCs should remain frozen

3. **Regression check:**
   - Verify SVE NPCs still work (they had working paths before)
   - Verify cancelled encounters still work correctly
   - Verify NPCs don't walk through walls

## Why This Will Work

1. **When `checkSchedule` succeeds** (entry at current time): Vanilla code creates controller → existing behavior preserved
2. **When `checkSchedule` fails** (no entry at current time but future entry exists):
   - We detect `beforeController == afterController` (no change)
   - We parse the next schedule entry to get destination
   - We manually create a `PathFindController` to send NPC there
   - Vanilla scheduling takes over once NPC arrives at destination

## Key Design Decisions

- **Use reflection for PathFindController construction:** Avoids direct dependency on internal SDV types that may change
- **Graceful degradation:** If force-path fails, log warning but don't crash - NPCs will wait for vanilla retry
- **Reuse existing schedule parsing patterns:** From `NpcAutonomyPlannerService.cs`
- **Default tile handling:** If schedule entry has no tile, use center of map as fallback (vanilla scheduling will refine on arrival)
