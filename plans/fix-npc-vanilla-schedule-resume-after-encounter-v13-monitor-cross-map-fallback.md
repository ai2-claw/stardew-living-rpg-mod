# Fix NPC Vanilla Schedule Resume After Encounter (v13 - Monitor Cross-Map Fallback)

## Problem Summary

v12 enabled cross-map path finding, but NPCs like **Shane** are still stuck because they are never added to the monitor:

**Evidence from logs:**
```
[01:09:08] [FORCE_PATH] Shane forced cross-map from Town active-slot path after encounter enc_7
[01:09:08] [REBIND] Shane reset complete: ... fallback_used=True
```

But no `[MONITOR]` logs appear for Shane afterward, while other NPCs like Treyvon show:
```
[01:08:55] [MONITOR] Treyvon encounter=enc_6 tick=1: controller=PathFindController, isMoving=True...
```

## Root Cause

The monitoring code at lines 15071-15084 in `TryProcessPendingEncounterResumes` has a gap:

```csharp
if (usingTemporaryActiveSlotFallback)
{
    if (!pending.NextScheduleTime.HasValue && HasReachedActiveFallbackTarget(npc, pending))
    {
        // NPC reached target - remove from pending
        _pendingVanillaEncounterResumeByNpcId.Remove(npcId);
        continue;
    }

    pending.NextAttemptTick = currentTick + 1;
    continue;  // ← NPC stays in pending, NEVER added to monitor!
}
```

The guard condition at line 15053:
```csharp
if (HasVanillaResumeState(npc) && !usingTemporaryActiveSlotFallback)
```

Only adds NPCs to the monitor when `!usingTemporaryActiveSlotFallback` - meaning the NPC has **finished** using the fallback and now has vanilla resume state.

For **cross-map travel** (Town → JojaMart):
1. Shane gets a temporary fallback controller
2. He needs to walk to the Town exit, warp to JojaMart, then walk to destination
3. This takes time - he should be monitored during the journey
4. But he's never added to the monitor because he's still using the fallback controller

## Solution: Add Fallback NPCs to Monitor

Add NPCs using temporary active-slot fallback to the monitor so we can track their progress. The existing monitor already logs:
- `controller` type
- `isMoving` status
- `TilePoint` position changes
- `moved_from_initial` tracking

This will allow us to see cross-map NPCs making progress (walking to exit, warping, continuing to destination).

## Implementation

### File: `mod/StardewLivingRPG/ModEntry.cs`

#### Modify the fallback path at lines 15071-15084:

**BEFORE:**
```csharp
if (usingTemporaryActiveSlotFallback)
{
    if (!pending.NextScheduleTime.HasValue && HasReachedActiveFallbackTarget(npc, pending))
    {
        Monitor.Log(...);
        _pendingVanillaEncounterResumeByNpcId.Remove(npcId);
        continue;
    }

    pending.NextAttemptTick = currentTick + 1;
    continue;  // ← Never monitored!
}
```

**AFTER:**
```csharp
if (usingTemporaryActiveSlotFallback)
{
    // Add to monitor if not already monitored
    if (!_vanillaEncounterResumeMonitorByNpcId.ContainsKey(npcId))
    {
        _vanillaEncounterResumeMonitorByNpcId[npcId] = new VanillaEncounterResumeMonitor
        {
            NpcId = npcId,
            EncounterId = pending.EncounterId,
            InitialTilePoint = pending.InitialTilePoint,
            LoggedControllerTickCount = 0,
            NextLogTick = currentTick + 10
        };
        Monitor.Log(
            $"Autonomy: monitoring {npc.Name} active-slot fallback after encounter {pending.EncounterId} (active_schedule_time={pending.ActiveScheduleTime}, next_schedule_time={pending.NextScheduleTime}, active_target_location={pending.ActiveTargetLocation}, active_target_tile={DescribeNullablePoint(pending.ActiveTargetTile)}, controller={DescribeControllerType(npc)}, isMoving={npc.isMoving()}, TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), map={npc.currentLocation?.Name ?? "unknown"}, time={Game1.timeOfDay}).",
            LogLevel.Debug);
    }

    // Check completion conditions
    var reachedTarget = HasReachedActiveFallbackTarget(npc, pending);
    var reachedNextScheduleTime = pending.NextScheduleTime.HasValue && Game1.timeOfDay >= pending.NextScheduleTime.Value;

    if (reachedTarget || (reachedNextScheduleTime && pending.LastAttemptedTimeOfDay != Game1.timeOfDay))
    {
        // Time to hand off to vanilla or complete
        ClearTemporaryActiveSlotFallback(npc, pending);

        // Try vanilla checkSchedule for next slot
        if (reachedNextScheduleTime)
        {
            TryAdvanceVanillaScheduleAtCurrentTime(npc, pending, out var nextTime, out var checkInvoked, out var checkMethod);
            pending.NextScheduleTime = nextTime;
            pending.CheckScheduleInvoked |= checkInvoked;
            if (checkInvoked)
                pending.CheckScheduleMethod = checkMethod;
        }

        // If now has vanilla state, transition to normal monitoring
        if (HasVanillaResumeState(npc) && !IsUsingTemporaryActiveSlotFallback(npc, pending))
        {
            Monitor.Log(
                $"Autonomy: returned {npc.Name} to vanilla schedule after encounter {pending.EncounterId} (active_slot_fallback_complete, restored={pending.RestoredSchedule}, attempts={pending.Attempts}, controller={DescribeControllerType(npc)}, isMoving={npc.isMoving()}, TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), map={npc.currentLocation?.Name ?? "unknown"}, time={Game1.timeOfDay}).",
                LogLevel.Debug);
            _pendingVanillaEncounterResumeByNpcId.Remove(npcId);
            continue;
        }

        // If no next schedule and reached target, complete
        if (!pending.NextScheduleTime.HasValue && reachedTarget)
        {
            Monitor.Log(
                $"Autonomy: returned {npc.Name} to active schedule target after encounter {pending.EncounterId} (active_slot_fallback_complete, no_next_slot, controller={DescribeControllerType(npc)}, isMoving={npc.isMoving()}, TilePoint=({npc.TilePoint.X},{npc.TilePoint.Y}), map={npc.currentLocation?.Name ?? "unknown"}, time={Game1.timeOfDay}).",
                LogLevel.Debug);
            _pendingVanillaEncounterResumeByNpcId.Remove(npcId);
            _vanillaEncounterResumeMonitorByNpcId.Remove(npcId);
            continue;
        }
    }

    pending.NextAttemptTick = currentTick + 1;
    continue;
}
```

#### Also update the second fallback path at lines 15156-15160 with similar logic:

**BEFORE:**
```csharp
if (usingTemporaryActiveSlotFallbackAfterAttempt)
{
    pending.NextAttemptTick = currentTick + 1;
    continue;
}
```

**AFTER:**
```csharp
if (usingTemporaryActiveSlotFallbackAfterAttempt)
{
    // Same monitoring logic as above - add to monitor if not already
    if (!_vanillaEncounterResumeMonitorByNpcId.ContainsKey(npcId))
    {
        _vanillaEncounterResumeMonitorByNpcId[npcId] = new VanillaEncounterResumeMonitor
        {
            NpcId = npcId,
            EncounterId = pending.EncounterId,
            InitialTilePoint = pending.InitialTilePoint,
            LoggedControllerTickCount = 0,
            NextLogTick = currentTick + 10
        };
    }

    pending.NextAttemptTick = currentTick + 1;
    continue;
}
```

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | 15071-15084 | Add monitoring for temporary fallback NPCs |
| `ModEntry.cs` | 15156-15160 | Add monitoring for post-attempt fallback NPCs |

## Verification

1. **Build:** `dotnet build` must pass

2. **Manual test:**
   - Trigger Alex->Shane encounter in Town (time ~910)
   - Wait for encounter to complete
   - **Expected logs:**
     ```
     [FORCE_PATH] Shane forced cross-map from Town active-slot path ... location=JojaMart
     [MONITOR] Shane encounter=enc_7 tick=1: controller=PathFindController, isMoving=True, TilePoint=(61,64), moved_from_initial=no...
     [MONITOR] Shane encounter=enc_7 tick=2: controller=PathFindController, isMoving=True, TilePoint=(60,64), moved_from_initial=yes...
     ...
     ```
   - **Expected behavior:**
     - Shane should walk toward the Town exit
     - Warp to JojaMart when reaching the exit
     - Continue walking to his destination (9,17)
     - Monitor logs should track his progress throughout

3. **Cross-map journey tracking:**
   - `TilePoint` should change as Shane walks
   - `moved_from_initial` should become `yes` after first step
   - When Shane warps, `map=` should change from `Town` to `JojaMart`
   - Monitor should show `isMoving=True` while walking

4. **Schedule boundary handoff:**
   - When time reaches `1230` (Shane's next schedule slot)
   - The temporary fallback should be cleared
   - Vanilla `checkSchedule` should be invoked for the new slot
   - A "returned to vanilla schedule" log should appear

## Why This Will Work

1. **Monitoring provides visibility:**
   - Currently we have no idea if Shane is walking, stuck, or making progress
   - Monitor logs will show controller type, isMoving status, position changes
   - We can see the cross-map journey happen in real-time

2. **Monitor already supports all we need:**
   - Logs `controller` type (confirms PathFindController is active)
   - Logs `isMoving` (confirms NPC is walking)
   - Logs `TilePoint` (confirms position changes)
   - Logs `moved_from_initial` (confirms progress)
   - Logs `map=` (will show warp from Town to JojaMart)

3. **Schedule boundary handoff:**
   - When next schedule time arrives, we clear the fallback
   - Try vanilla `checkSchedule` for the new slot
   - Either vanilla takes over, or we log why not
   - Prevents NPC from being stranded on a stale fallback path

## Key Design Decisions

- **Reuse existing monitor:** No new monitor structure needed - the existing `VanillaEncounterResumeMonitor` works for both vanilla and fallback cases
- **Monitor logs 5 ticks then auto-removes:** This is fine - if NPC is still walking after 5 ticks, they should have vanilla state or be handed off at next schedule boundary
- **Same map hint in log:** The `cross-map from {currentMap}` or `same-map` hint helps distinguish cases
- **Schedule boundary triggers handoff:** Even if NPC hasn't reached destination yet, we clear fallback at next schedule time so vanilla can take over for the new slot
