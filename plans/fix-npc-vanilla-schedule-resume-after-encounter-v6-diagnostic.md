# Plan: Fix NPC Vanilla Schedule Resume After Encounter (v6 - Diagnostic)

## TL;DR

V4-V5 approaches failed because we don't understand why vanilla's `checkSchedule` isn't creating a controller for one NPC. Both NPCs report "ScheduleRebound" success, but one NPC remains stuck.

**New approach: Add comprehensive diagnostic logging to understand the actual failure mode, then fix based on findings.**

## Current Understanding

1. Both NPCs complete the encounter (user sees "Autonomy: Player2 encounter... completed")
2. Both NPCs are removed from pending resume queue (no "failed to return" warnings)
3. One NPC resumes normally, the other stays stuck
4. Same NPC always gets stuck (deterministic)
5. After some time, stuck NPC walks through walls in straight line -> characteristic of `previousEndPoint` being wrong origin for pathfinding

## Diagnostic Logging to Add

### Step 1: Add comprehensive logging to `TryRebindVanillaScheduleAtCurrentTime`

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 15043-15078

Add diagnostic logs BEFORE and AFTER each critical operation:

```csharp
private string TryRebindVanillaScheduleAtCurrentTime(NPC npc, ...)
{
    // ... existing init ...

    // LOG: Starting position and state
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} starting rebind at TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), controller={(npc.controller?.GetType().Name ?? "null")}, followSchedule={TryGetMemberValue(npc, "followSchedule", out var fs) ? fs : false}, map={npc.currentLocation?.Name ?? "null"}", LogLevel.Debug);

    npc.controller = null;
    TrySetMemberValue(npc, "temporaryController", null);
    TrySetMemberValue(npc, "followSchedule", true);
    ClearEncounterMovementBlockingState(npc);
    npc.ClearSchedule();

    // LOG: Before TryLoadSchedule
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} cleared schedule, calling TryLoadSchedule...", LogLevel.Debug);

    reloadTodayOk = npc.TryLoadSchedule();

    // LOG: After TryLoadSchedule
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} TryLoadSchedule returned={reloadTodayOk}, Schedule.Count={(npc.Schedule?.Count ?? 0)}, first_10min_keys={string.Join(",", npc.Schedule?.Keys.Take(5).Select(k => k.ToString()) ?? Array.Empty<string>())}", LogLevel.Debug);

    if (!reloadTodayOk || npc.Schedule is null || npc.Schedule.Count == 0)
    {
        Monitor.Log($"Autonomy: [REBIND] {npc.Name} ABORTING: no schedule loaded", LogLevel.Warn);
        return string.Empty;
    }

    // LOG: Current time vs schedule entries
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} current_time={Game1.timeOfDay}, entries_before_current={string.Join(",", npc.Schedule.Where(e => e.Key <= Game1.timeOfDay).Select(e => $"{e.Key}:{e.Value?.destination ?? "null"}").Take(5))}", LogLevel.Debug);

    TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
    TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);
    TrySetMemberValue(npc, "currentScheduleDelay", 0.001f);
    ClearEncounterMovementBlockingState(npc);

    // LOG: After state reset
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} reset complete: lastAttemptedSchedule=-1, previousEndPoint=({npc.TilePoint.X},{npc.TilePoint.Y}), currentScheduleDelay=0.001", LogLevel.Debug);

    return "ScheduleRebound";
}
```

### Step 2: Add logging to multi-tick verification

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 15003-15029

```csharp
else if (HasVanillaResumeState(npc))
{
    // LOG: Why this is considered successful
    var hasController = npc.controller is not null;
    var isMoving = npc.isMoving();
    var hasTempController = TryGetMemberValue(npc, "temporaryController", out var tempCtrl) && tempCtrl is not null;
    var controllerType = npc.controller?.GetType().Name ?? "null";
    Monitor.Log($"Autonomy: [REBIND] {npc.Name} marked successful via HasVanillaResumeState: controller={hasController}({controllerType}), isMoving={isMoving}, temporaryController={hasTempController}", LogLevel.Debug);
    resumeMethod = "VanillaSchedule(update)";
}

// In the success log (line 15024-15026), add more detail:
Monitor.Log(
    $"Autonomy: returned {npc.Name} to vanilla schedule after encounter {pending.EncounterId} (..., controller={npc.controller?.GetType().Name ?? "null"}, isMoving={npc.isMoving()}, TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), previousEndPoint={TryGetMemberValue(npc, "previousEndPoint", out var pep) is Point p ? $"({p.X},{p.Y})" : "null"}, lastAttemptedSchedule={TryGetMemberValue(npc, "lastAttemptedSchedule", out var las) ? las : "null"}, ...)",
    LogLevel.Debug);
```

### Step 3: Add logging to track `checkSchedule` execution

Since we can't directly hook `checkSchedule`, add periodic logging in the update loop to monitor when controllers are created:

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** Update loop (find where `OnUpdateTicked` is)

Add a new method to poll NPC state after encounter completion:

```csharp
// In _pendingVanillaEncounterResumeByNpcId, add a field to track for logging:
public class PendingVanillaEncounterResume
{
    // ... existing fields ...
    public Point InitialTilePoint { get; set; }
    public int LoggedControllerTickCount { get; set; }
}

// In TryProcessPendingVanillaEncounterResumes, add monitoring:
if (pending.Attempts == 1)
{
    pending.InitialTilePoint = npc.TilePoint;
}

// Every 10 ticks after rebinding, log controller status:
if (!string.IsNullOrWhiteSpace(resumeMethod) && pending.LoggedControllerTickCount < 5)
{
    pending.LoggedControllerTickCount++;
    Monitor.Log($"Autonomy: [MONITOR] {npc.Name} tick={pending.LoggedControllerTickCount}: controller={(npc.controller?.GetType().Name ?? "null")}, isMoving={npc.isMoving()}, TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), moved_from_initial={(!npc.TilePoint.Equals(pending.InitialTilePoint) ? "yes" : "no")}", LogLevel.Debug);
}
```

### Step 4: Add logging to `HandoffNpcToVanillaAfterEncounter`

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14932-14959

```csharp
private void HandoffNpcToVanillaAfterEncounter(NPC? npc, string encounterId, string phase)
{
    if (npc is null)
    {
        Monitor.Log($"Autonomy: [HANDOFF] Skipping null NPC for encounter {encounterId}", LogLevel.Warn);
        return;
    }

    Monitor.Log($"Autonomy: [HANDOFF] {npc.Name} starting handoff: TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), controller={(npc.controller?.GetType().Name ?? "null")}, followSchedule={TryGetMemberValue(npc, "followSchedule", out var fs) ? fs : false}, time={Game1.timeOfDay}", LogLevel.Debug);

    // ... rest of method ...
}
```

## Expected Output (What to Look For)

After adding these logs and running `slrpg_demo_bootstrap`, look for:

1. **Schedule loading differences** between the two NPCs
   - Does one have `Schedule.Count=0`?
   - Are the schedule entries different?
   - Does one have no entries after current time?

2. **Position anomalies**
   - Is `TilePoint` different between NPCs?
   - Is `previousEndPoint` being set to wrong value?

3. **Controller creation differences**
   - Does one NPC get a `PathFindController` and the other doesn't?
   - Is the stuck NPC's controller null or a different type?

4. **Movement tracking**
   - Does the stuck NPC's `TilePoint` ever change?
   - Does `isMoving` return true for the stuck NPC?

5. **Timing issues**
   - Is `lastAttemptedSchedule` being updated after the rebind?

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | 15043-15078 | Add diagnostic logs to `TryRebindVanillaScheduleAtCurrentTime` |
| `ModEntry.cs` | 15003-15029 | Add diagnostic logs to `HasVanillaResumeState` branch |
| `ModEntry.cs` | 14932-14959 | Add diagnostic logs to `HandoffNpcToVanillaAfterEncounter` |
| `ModEntry.cs` | `PendingVanillaEncounterResume` class | Add `InitialTilePoint` and `LoggedControllerTickCount` fields |
| `ModEntry.cs` | `TryProcessPendingVanillaEncounterResumes` | Add periodic controller monitoring |

## Verification

1. **Build:** `dotnet build` from `mod/StardewLivingRPG/` - must compile clean

2. **Manual test:**
   - Launch game via SMAPI
   - Run `slrpg_demo_bootstrap` to trigger encounter
   - Watch SMAPI console for detailed diagnostic logs
   - Compare logs between the two NPCs to find differences

3. **Analysis:**
   - Share the logs and identify the specific failure mode
   - Based on findings, implement targeted fix in v7

## Next Steps After Diagnostics

Based on what the logs reveal, the fix will likely be one of:

1. **If schedule is empty** -> Fix schedule loading/backup restore
2. **If position is wrong** -> Fix `previousEndPoint` calculation or use different position source
3. **If controller not created** -> Force create `PathFindController` directly
4. **If `checkSchedule` not called** -> Use different trigger mechanism
5. **If timing issue** -> Add delay before/after rebind

## Status

- Implemented on 2026-03-20.
- Added `[HANDOFF]`, `[REBIND]`, and post-success `[MONITOR]` diagnostics in `ModEntry.cs`.
- Added per-NPC resume monitoring after successful rebind so the next five sampled ticks show controller, movement, tile, `previousEndPoint`, and `followSchedule` state.
