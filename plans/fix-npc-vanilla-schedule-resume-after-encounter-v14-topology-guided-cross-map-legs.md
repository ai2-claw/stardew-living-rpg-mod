# Fix Cross-Map Encounter Resume by Using Topology-Guided Same-Map Legs

## Summary

The latest logs and in-game report point to a different root cause than v13:

- v12 now creates cross-map fallback controllers for Shane, Sam, and Vincent
- but some of those NPCs still appear visually stuck after release
- the repo already has a world-topology graph and route planner, while the current cross-map fallback still tries to create one opaque `PathFindController` directly against the remote target map

The real fix is to stop using a single remote-map controller for cross-map fallback. Instead:

- keep vanilla `checkSchedule(...)` as the first attempt
- keep same-map active-slot fallback exactly as-is
- replace cross-map fallback with **topology-guided same-map legs**
- drive the NPC to the next exit/door on the current map first
- after the warp/map change, create the next leg
- when the NPC reaches the target map, switch back to the existing same-map active-slot fallback to the final target tile

This uses only short same-map controllers, which the logs already show behave reliably.

## Key Changes

### Cross-map fallback strategy

- In `mod/StardewLivingRPG/ModEntry.cs`, split active-slot fallback into:
  - same-map target fallback: unchanged
  - cross-map target fallback: new staged route behavior
- Remove the current cross-map behavior that creates a `PathFindController` directly against the final target map.
- For cross-map active slots:
  - build the topology graph from `_worldTopologyService`
  - use `_routePlannerService` or `_worldTopologyService.FindRoute(...)` to resolve a route from `npc.currentLocation.Name` to `pending.ActiveTargetLocation`
  - take only the **first route segment**
  - create a same-map controller to that segment’s `DepartureTile` on the NPC’s current map
  - do not point the controller at the final remote destination tile until the NPC is actually on the target map

### Pending resume state

- Extend `PendingVanillaEncounterResume` with private state for staged cross-map fallback:
  - `FallbackMode` or equivalent (`none`, `same_map_target`, `cross_map_leg`)
  - `FallbackLegFromLocation`
  - `FallbackLegToLocation`
  - `FallbackLegDepartureTile`
  - `FallbackLegArrivalTile`
  - `FallbackLastObservedTile`
  - `FallbackLastObservedLocation`
  - `FallbackLastProgressTick`
  - `FallbackLegRetryCount`
- Keep the existing `TemporaryFallbackController` and `UsedTemporaryActiveSlotFallback` fields, but reinterpret them as “current fallback leg controller,” not “full trip controller.”

### Fallback progression and stale-leg recovery

- In the pending resume worker:
  - if the NPC is on a same-map fallback, keep current behavior
  - if the NPC is on a cross-map leg fallback:
    - observe tile and map changes while the fallback controller is active
    - update `FallbackLastProgressTick` whenever tile or map changes
    - when the NPC changes maps to the segment destination:
      - clear the old leg controller
      - if the new map equals `pending.ActiveTargetLocation`, switch to the existing same-map active-slot fallback toward `pending.ActiveTargetTile`
      - otherwise plan the next route segment and create the next same-map leg controller
- Add bounded stale-leg recovery:
  - if a cross-map leg controller exists but the NPC shows no tile or map progress for a fixed timeout, treat the leg as inert
  - recreate the current leg controller once
  - if it is still inert after one retry, clear the fallback controller, log a warning, and leave the NPC pending for the next vanilla `checkSchedule(...)` time boundary instead of spinning forever
- Default chosen:
  - stale timeout: `180` ticks
  - one recreate retry per route leg

### Logging and monitoring

- Do not treat fallback monitoring as the fix, but add targeted logs so the new state machine is diagnosable:
  - `CrossMapLeg(start)` with `from`, `to`, `departure_tile`, `arrival_tile`
  - `CrossMapLeg(progress)` when the NPC changes tile/map
  - `CrossMapLeg(warped)` when the NPC reaches the next map
  - `CrossMapLeg(stale)` when a leg stops making progress
  - `CrossMapLeg(retry)` when the current leg controller is recreated
  - `CrossMapLeg(target_map)` when the NPC reaches the final map and switches to same-map target fallback
- Keep the existing `[HANDOFF]`, `[REBIND]`, and final return/failure logs.
- Do not implement v13’s duplicate schedule-boundary handoff logic; keep the existing next-schedule-time clearing path as the single owner of that behavior.

## Interfaces / Types

- No public API changes.
- Reuse existing private services already in `ModEntry`:
  - `WorldTopologyService`
  - `NpcRoutePlannerService`
- Add only private/internal pending-state fields and helper methods in `ModEntry.cs` for:
  - planning the next cross-map leg
  - detecting cross-map fallback progress
  - detecting stale fallback legs
  - advancing from one leg to the next after a warp

## Test Plan

1. Shane cross-map release:
   - trigger Alex->Shane in `Town` around `910`
   - expect:
     - vanilla `checkSchedule(...)` still runs first
     - fallback starts a first leg from `Town` toward the correct exit for `JojaMart`
     - Shane visibly walks toward the exit
     - after the warp, fallback switches to same-map target path inside `JojaMart`
     - Shane no longer remains frozen in Town

2. Sam and Vincent cross-map release:
   - trigger Sam->Vincent in `SamHouse` around `930`
   - expect:
     - Sam gets a route leg toward `Town`
     - Vincent gets a route leg toward `ArchaeologyHouse`
     - both walk out of `SamHouse` instead of remaining in place with a remote-map controller

3. Same-map regression:
   - same-map active-slot fallback cases such as Abigail/SeedShop still use the existing same-map logic unchanged
   - Alex no-op active-slot case still does not create a pointless controller when he is already at the active target tile

4. Stale-leg recovery:
   - if a cross-map leg controller fails to move the NPC for `180` ticks
   - expect:
     - one leg controller recreation
     - then a warning and fallback suspension if still inert
     - no infinite “pending with controller but no progress” loop

5. Schedule boundary handoff:
   - if the next schedule time arrives during any fallback leg
   - expect:
     - the active fallback leg is cleared
     - vanilla `checkSchedule(...)` remains the owner of the new active-slot rebind
     - no duplicate handoff path is introduced

6. Verification:
   - `dotnet build` passes
   - no fallback ever targets the next future schedule slot directly unless it becomes the active slot
   - no direct remote-map `PathFindController` creation remains for cross-map active-slot fallback

## Assumptions

- Your in-game observation of “still visually stuck” is authoritative even though the logs suggest some distance changes.
- The current remote-map fallback controller is the unreliable part of the system, not the active-slot rule itself.
- Temporary custom controllers are acceptable here only as bounded recovery for the **current active slot**, and cross-map recovery should be decomposed into reliable same-map legs rather than one opaque cross-map controller.
