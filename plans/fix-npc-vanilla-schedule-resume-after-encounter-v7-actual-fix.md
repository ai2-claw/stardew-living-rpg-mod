# Plan: Fix NPC Vanilla Schedule Resume After Encounter (v7 - Actual Fix)

## TL;DR

The diagnostic logs revealed the real issue: **`currentScheduleDelay=0.001` only triggers `checkSchedule` ONCE**. After processing schedule entries up to current time, vanilla sets `currentScheduleDelay=0`, and `checkSchedule` is never called again when the next schedule entry time arrives.

**The fix: Don't use `currentScheduleDelay` trigger. Instead, directly call vanilla's `checkSchedule` method via reflection to process entries immediately.**

## What the Logs Revealed

```
[22:39:14] Victor HANDOFF: TilePoint=(23,19)
[22:39:14] Victor REBIND: TilePoint=(23,19), schedule_count=9, entries: 610,700,800...
[22:39:14] Victor reset: lastAttemptedSchedule=-1, previousEndPoint=(31,5), currentScheduleDelay=0.001
[22:39:14] Victor MONITOR tick=1: TilePoint=(31,5), controller=null, isMoving=False
[22:39:14] Victor MONITOR tick=2..5: same - stuck at (31,5)
```

**Analysis:**
1. Schedule loaded successfully: `610, 700, 800, 910...`
2. Current time: `750` - between schedule entries (700 and 800)
3. `previousEndPoint=(31,5)` - vanilla processed the 700 entry and moved Victor there
4. **Victor is waiting for next entry at 800** - this is correct vanilla behavior
5. **But `checkSchedule` is never called at 800** - so Victor waits forever

## Root Cause

### Why `currentScheduleDelay` Doesn't Work

Vanilla SDV's `NPC.update()` logic (simplified):

```csharp
// SDV NPC.update()
if (currentScheduleDelay > 0)
{
    currentScheduleDelay -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (currentScheduleDelay <= 0)
    {
        checkSchedule(timeOfDay);  // Called ONCE
        currentScheduleDelay = 0;
    }
}
```

**The problem:**
- We set `currentScheduleDelay = 0.001` to trigger `checkSchedule` immediately
- `checkSchedule` runs, processes entries up to current time (750)
- It processes the 700 entry, moves NPC to destination
- No entry for 750, so it sets `currentScheduleDelay = 0`
- **At time 800, `checkSchedule` is NOT called** because `currentScheduleDelay` is still 0

In vanilla SDV, `checkSchedule` is also called when:
- Time changes (10-minute intervals)
- NPC changes locations
- Certain other events occur

But after our encounter completion, these vanilla triggers don't fire, so NPCs wait indefinitely.

## The Fix

### Option 1: Direct `checkSchedule` Call (Recommended)

Instead of using `currentScheduleDelay` trigger, **directly call `checkSchedule` via reflection** to process the next schedule entry immediately:

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 15071-15078

```csharp
private string TryRebindVanillaScheduleAtCurrentTime(NPC npc, ...)
{
    // ... existing code up to line 15070 ...

    // Reset the internal schedule-processing gate
    TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
    // Set origin to current position for route computation
    TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);

    // REMOVED: TrySetMemberValue(npc, "currentScheduleDelay", 0.001f);

    // NEW: Directly call checkSchedule to process the next schedule entry
    TryCallCheckSchedule(npc, Game1.timeOfDay);

    ClearEncounterMovementBlockingState(npc);
    return "ScheduleRebound";
}

// Add new helper method:
private static void TryCallCheckSchedule(NPC npc, int timeOfDay)
{
    var method = npc.GetType().GetMethod("checkSchedule",
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.InvokeMethod);

    if (method is not null)
    {
        try
        {
            method.Invoke(npc, new object[] { timeOfDay });
        }
        catch (Exception ex)
        {
            // Log error but don't crash
        }
    }
}
```

**Why this works:**
- `checkSchedule` is called immediately when we rebind
- It processes the next schedule entry (e.g., the 700 entry for Victor at time 750)
- Creates a `PathFindController` to route the NPC to that destination
- NPC starts moving immediately
- Future schedule entries are handled by vanilla's normal timing

### Option 2: Process Multiple Entries (Alternative)

If there are gaps between schedule entries, we might need to process multiple entries until we find one that's in the future:

```csharp
// Process entries until we find one that creates a controller
var maxAttempts = 5;
var attempts = 0;
while (npc.controller is null && attempts < maxAttempts)
{
    TryCallCheckSchedule(npc, Game1.timeOfDay);
    attempts++;
    // Small delay to let the controller initialize
    if (npc.controller is not null)
        break;
}
```

## Additional Fix Needed

Looking at the logs, the NPCs' positions are changing between REBIND and MONITOR tick 1. This suggests vanilla's `checkSchedule` is moving them immediately. We should capture this in logs.

Also, we should add logic to handle the case where the current time is AFTER all schedule entries (late day encounters).

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | 15071-15078 | Remove `currentScheduleDelay`, add `TryCallCheckSchedule` call |
| `ModEntry.cs` | New method | Add `TryCallCheckSchedule` helper method |
| `ModEntry.cs` | 15080-15090 | (Optional) Add multi-entry processing logic |

## Verification

1. **Build:** `dotnet build` from `mod/StardewLivingRPG/` - must compile clean

2. **Manual test:**
   - Launch game via SMAPI
   - Run `slrpg_demo_bootstrap` to trigger encounter
   - Wait for encounter to complete
   - **Expected:**
     - NPCs should start moving within 1 second after encounter
     - MONITOR logs should show `controller=PathFindController` within a few ticks
     - `isMoving=True` within a few ticks
     - NPCs should reach their schedule destinations and continue moving

3. **Regression check:**
   - Verify NPCs don't walk through walls (controller should have valid path)
   - Verify NPCs reach their correct destinations

## Why This Will Work

1. **Direct `checkSchedule` call** processes schedule entries immediately
2. **Creates `PathFindController`** for the next valid schedule entry
3. **NPC starts moving** instead of waiting indefinitely
4. **Vanilla's normal schedule processing** takes over after the rebind
