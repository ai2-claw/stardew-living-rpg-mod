# Fix NPC Vanilla Schedule Resume After Encounter (v12 - Cross-Map Active-Slot Fallback)

## Summary

The current v11 fallback only works when the NPC's active schedule destination is on the same map as the encounter release tile. The latest logs show that this is why some NPCs still remain stuck after encounters:

- Shane releases in `Town`, but his active slot at `920` is `700:JojaMart`
- Sam releases in `SamHouse`, but his active slot at `930` is `900:Town`
- Vincent releases in `SamHouse`, but his active slot at `930` is `900:ArchaeologyHouse`

In all three cases, vanilla `checkSchedule(int)` creates no movement, and the current custom fallback is skipped only because of the same-map guard.

This plan corrects that gap, but keeps the earlier active-slot rule:

- do not fast-forward to the next future slot
- keep vanilla `checkSchedule(...)` as the first attempt
- if vanilla still produces no movement, allow a temporary fallback controller only to the **current active slot** destination
- support both same-map and cross-map active-slot fallback

This plan does **not** claim Alex is a movement failure. In the latest log, Alex's active slot target is his current tile in `Town`, so his correct behavior at `920` may be to remain there until the next schedule slot.

## Key Changes

### Active-slot fallback behavior

- In `mod/StardewLivingRPG/ModEntry.cs`, update `TryForcePathToActiveScheduleEntry(...)` so it no longer rejects cross-map active destinations.
- Remove the `npc.currentLocation.Name == pending.ActiveTargetLocation` requirement.
- Resolve the target map using `Game1.getLocationFromName(pending.ActiveTargetLocation)` and validate the destination tile on that target map, not on the NPC's current map.
- Create the temporary fallback `PathFindController` against the target location so the controller can handle same-map or cross-map travel using the same code path.

### No-op active-slot cases

- Before creating any fallback controller, detect whether the NPC is already effectively at the active-slot destination:
  - same target map as current map
  - current tile already at or within the existing arrival threshold of the active target tile
- In that case:
  - do not create a fallback controller
  - leave `fallback_used = false`
  - keep the pending resume worker alive until the next schedule time so vanilla can take over naturally
- This prevents false expectations in cases like Alex at `920`, where the active slot is `Town` and the target tile is already the current tile.

### Resume-worker integration

- Keep the current order unchanged:
  - restore/reload vanilla schedule
  - invoke vanilla `checkSchedule(...)`
  - only if vanilla still produces no controller or movement, try the active-slot fallback
- Continue treating the temporary fallback controller as non-terminal:
  - do not remove the pending resume immediately just because the fallback exists
  - keep monitoring until either vanilla takes over, the NPC reaches the active-slot target with no future slot today, or the next schedule time arrives
- When the next schedule time arrives:
  - clear any temporary active-slot fallback controller
  - invoke vanilla `checkSchedule(...)` again
  - let vanilla own the next route

### Logging and verification signals

- Update the `[FORCE_PATH]` log to distinguish:
  - `same-map active-slot path`
  - `cross-map from <currentMap> active-slot path`
- Add a debug log when fallback is intentionally skipped because the NPC is already at the active-slot destination.
- Keep existing active-slot telemetry:
  - `active_schedule_time`
  - `next_schedule_time`
  - `active_target_location`
  - `active_target_tile`
  - `fallback_used`

## Test Plan

1. Shane cross-map case:
   - release Alex->Shane encounter around `920` in `Town`
   - expect:
     - Shane log shows active slot `700:JojaMart`
     - `[FORCE_PATH] Shane forced cross-map from Town active-slot path ... location=JojaMart`
     - Shane walks to the exit, transitions maps, and proceeds toward JojaMart

2. Sam and Vincent cross-map cases:
   - release Sam->Vincent encounter around `930` in `SamHouse`
   - expect:
     - Sam gets cross-map fallback to `Town`
     - Vincent gets cross-map fallback to `ArchaeologyHouse`
     - neither remains stuck with `controller=null` and `isMoving=False`

3. Alex no-op active-slot case:
   - in the same Alex->Shane scenario, if Alex's active slot target is still his current tile in `Town`
   - expect:
     - no fallback controller is created for Alex just to path to the same tile
     - log indicates already-at-active-destination or equivalent skip
     - Alex remains pending until his next vanilla schedule slot instead of being treated as a walking recovery case

4. Same-map regression check:
   - same-map active-slot fallback cases like Abigail/SeedShop should still work exactly as before
   - no regression in same-map walkability validation

5. Boundary handoff:
   - if a cross-map fallback controller is still active when the next schedule time arrives
   - expect the fallback to be cleared and vanilla `checkSchedule(...)` retried for the new active slot

6. Verification:
   - `dotnet build` passes
   - no fallback ever targets the next future slot directly unless that future slot has become the active slot
   - no center-of-map or arbitrary destination fallback is introduced

## Assumptions

- The current remaining failures are primarily cross-map active-slot cases blocked by the same-map restriction.
- Reflective `PathFindController` creation is already acceptable in this subsystem as a bounded fallback, but only for the **current active slot** destination.
- Alex's latest log is not evidence of failed movement recovery, because his active slot target matches his current Town tile at release time.
