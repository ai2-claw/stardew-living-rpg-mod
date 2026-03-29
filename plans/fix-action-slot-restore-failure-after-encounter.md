# Fix: Action Slot Restore Fails After NPC Encounters

## Problem

After NPC encounters complete, the action slot restore process fails for every NPC with a scheduled behavior (e.g., `chloe_coffee`, `alex_football`). The rebind mechanism successfully clones the schedule entry and invokes `checkSchedule(int)`, but the NPC's sprite animation never starts. After 3 rebind attempts (one per game time unit), the system logs a warning and gives up, leaving the NPC standing idle at the correct tile.

### Root Cause

Stardew Valley's `checkSchedule` applies end-of-route behaviors through a `PathFindController` completion callback. The cloned schedule entry has an empty/consumed route because the NPC already traversed it to reach the tile. When `checkSchedule` processes an entry with zero waypoints, the `PathFindController` either isn't created or completes without triggering the end-of-route callback, so the behavior animation never starts.

Evidence from logs (line 1319):
- `arrival_rebind_method=schedule_entry_cloned+checkSchedule(int)` — clone + checkSchedule both succeed
- `arrival_rebind_degraded=False` — route clone even found route data
- `action_stable_ticks=0, action_confirm_method=unconfirmed` — sprite never changed
- Same pattern repeats at every attempt (times 800, 810, 820 for Chloe)

### Secondary Issue

The `EncounterSettleRecoveryMonitor` also requires `TryDetectVisibleScheduleActionState` to pass for stability (line 16068-16072). Even if we accept a degraded settle, the monitor will spin trying to recover an unrecoverable animation state.

## Fix Approach

Two surgical changes in `ModEntry.cs`:

### Change 1: Accept "position-only" settle when animation can't be restored

**File:** `mod/StardewLivingRPG/ModEntry.cs`

**Where:** The failure block at lines 16596-16608 in `TryHandleActiveSlotArrivalSettle`

**What:** When `ActionRebindAttemptCount >= EncounterArrivalActionMaxRebindAttempts` and the NPC is at the correct tile, facing the correct direction, with no controllers, and not moving -- accept this as a degraded settle instead of logging a failure. Add a new helper `IsArrivalPositionOnlySatisfied` that checks:
- NPC is at exact active target tile
- Not using temporary fallback
- Not moving, no controller, no temporary controller
- Facing matches expected direction (if set)

Replace the failure block with:
1. Try direct end-of-route behavior application via reflection (`doEndOfRouteBehavior` or `performTenMinuteUpdate`)
2. Check position-only satisfaction
3. If position-only is satisfied: log as degraded debug, start recovery monitor, complete
4. If not: keep the existing warning path

### Change 2: Relax recovery monitor stability check for degraded settles

**File:** `mod/StardewLivingRPG/ModEntry.cs`

**Where:** `IsNpcStableAfterEncounterSettle` at line 16053

**What:** When the pending entry settled in degraded mode (animation not confirmed), skip the `TryDetectVisibleScheduleActionState` requirement in the recovery monitor. The NPC being at the right location/tile IS the stable state for a degraded settle.

Add a `DegradedSettle` flag to `EncounterSettleRecoveryMonitor`. Set it when the settle was position-only. In `IsNpcStableAfterEncounterSettle`, skip the behavior visibility check when `DegradedSettle` is true.

## Files to Modify

- `mod/StardewLivingRPG/ModEntry.cs` (only file)
  - Add `IsArrivalPositionOnlySatisfied` static method (~20 lines, near line 16675)
  - Add `TryApplyDirectEndOfRouteBehavior` method (~40 lines, near line 16675)
  - Modify failure block in `TryHandleActiveSlotArrivalSettle` (lines 16596-16608)
  - Add `DegradedSettle` field to `EncounterSettleRecoveryMonitor` class
  - Set `DegradedSettle = true` in `StartEncounterSettleRecoveryMonitor` when caller indicates degraded
  - Modify `IsNpcStableAfterEncounterSettle` (lines 16068-16072) to skip visibility check when degraded

## Constants

No constant changes needed. Keep `EncounterArrivalActionMaxRebindAttempts = 3` -- the fix makes the failure path graceful rather than reducing attempts.

## Verification

1. Build: `dotnet build`
2. Launch game with SMAPI, trigger NPC encounters during schedule slots with behaviors
3. Check SMAPI console for the settle messages:
   - Should see "settled ... (degraded action restore: position-only)" instead of the WARN "action slot restore failed"
   - Recovery monitor should stabilize without spinning
4. NPCs should remain at correct tiles after encounters, not freeze or wander off
