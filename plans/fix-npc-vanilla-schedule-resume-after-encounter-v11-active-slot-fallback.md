# Fix Encounter Resume by Targeting the Active Vanilla Slot, Not the Next One

## Summary
The v10 idea should be corrected, not implemented as written.

The latest logs show the stuck NPCs already have an active schedule slot before the current time:
- Shane at `920` still has active slot `700:JojaMart`
- Sam at `930` still has active slot `900:Town`

So the fix must resume them toward the **current active vanilla destination**, not fast-forward them to the next future slot (`1230`, `1500`, etc). Your JojaMart-at-9:50 / Seed-Shop-at-11:00 example is the right rule: if there is still time left in the current slot window, the NPC should go to JojaMart first.

Recommended approach:
1. keep vanilla rebind as the first path
2. if vanilla still produces no movement, allow a **temporary same-map controller only to the active slot destination**
3. keep the worker alive until the next vanilla schedule time so vanilla can take over again cleanly

## Key Changes
- In `ModEntry.cs`, replace the "next future slot" fallback idea with **active-slot recovery**:
  - add a helper to resolve:
    - `activeScheduleTime` = latest schedule key `<= Game1.timeOfDay`
    - `nextScheduleTime` = earliest schedule key `> Game1.timeOfDay`
  - treat the active slot as valid until `nextScheduleTime` arrives
  - do not send the NPC directly to the next future slot just because `checkSchedule(...)` returned no controller

- Improve schedule target extraction using the same data shapes already recognized elsewhere in the repo:
  - location candidates should include `targetLocationName`, `locationName`, `locationId`, etc.
  - tile/endpoint candidates should support:
    - `Point`
    - `Vector2`
    - `IList<Point>` / route lists, using the last point as the destination
  - do not use "center of map" as a fallback destination
  - if no valid destination can be resolved, log and skip the custom fallback

- Keep vanilla-owned resume as the first attempt:
  - on first encounter-release attempt:
    - clear encounter hold state
    - restore vanilla schedule if needed
    - clear `controller` / `temporaryController`
    - set `followSchedule = true`
    - `ClearSchedule()` + `TryLoadSchedule()`
    - reset `lastAttemptedSchedule = -1`
    - set `previousEndPoint` to the NPC's actual current tile
    - invoke reflective `checkSchedule(Game1.timeOfDay)` or parameterless fallback
  - if that creates a vanilla controller/movement, keep current success behavior

- Add a narrow fallback only when vanilla still fails:
  - fallback is allowed only if:
    - there is an active slot
    - current time is still before the next slot, or there is no next slot
    - the active slot target location equals the NPC's current map
    - a valid active-slot destination tile can be resolved
  - then create a temporary `PathFindController` to the **active slot** destination tile
  - validate the destination tile first with the existing walkability logic or nearest-walkable search
  - set `previousEndPoint` to the resolved active-slot destination, not the NPC's current tile
  - record that this was a temporary active-slot fallback, not a vanilla-created controller

- Keep the pending resume worker alive after the temporary fallback:
  - do not treat "temporary active-slot controller exists" as terminal success
  - keep monitoring until one of:
    - the NPC reaches the active-slot destination
    - vanilla later produces its own controller/movement
    - `Game1.timeOfDay` reaches the next schedule slot
  - when the next schedule slot time arrives:
    - if the NPC is still on the temporary active-slot controller, clear it
    - invoke vanilla `checkSchedule(...)` again so vanilla can take over for the next slot
  - this avoids the NPC getting stranded on a custom fallback path past the next real schedule boundary

- Update telemetry so logs distinguish:
  - `active_schedule_time`
  - `next_schedule_time`
  - `active_target_location`
  - `active_target_tile`
  - `fallback_used`
  - `fallback_kind=active_slot_same_map`
  - whether the controller is vanilla-created or temporary-fallback-created

## Interfaces / Types
- No public API changes.
- Extend `PendingVanillaEncounterResume` with fields needed for the active-slot fallback lifecycle:
  - `ActiveScheduleTime`
  - `ActiveTargetLocation`
  - `ActiveTargetTile`
  - `UsedTemporaryActiveSlotFallback`
  - `FallbackControllerStartedAtTime`
- Keep `HasVanillaResumeState(...)` for vanilla success detection, but do not let a temporary fallback controller remove the pending worker immediately.

## Test Plan
- Missed active-slot case:
  - example: `930 JojaMart`, released at `950`, next slot `1100 SeedShop`
  - expected:
    - first try vanilla `checkSchedule(...)`
    - if vanilla produces no controller, fallback sends NPC to `JojaMart`, not `SeedShop`
    - at `1100`, vanilla is invoked again and takes over for the Seed Shop slot

- Current failing cases from logs:
  - Shane at `920` should resume toward active `700:JojaMart`, not future `1230`
  - Sam at `930` should resume toward active `900:Town`, not future `1500`

- Same-map fallback safety:
  - fallback only runs when target map equals current map
  - target tile is validated as walkable or nearest-walkable
  - no map-center fallback, no arbitrary destination

- Cross-map case:
  - no custom same-map fallback is created
  - worker keeps retrying vanilla-only resume at time boundaries
  - log clearly shows fallback was skipped because the active slot target is on a different map

- Schedule-boundary handoff:
  - if NPC is still on the temporary active-slot controller when the next slot time arrives, the controller is cleared and vanilla `checkSchedule(...)` is retried
  - NPC should transition onto the next vanilla schedule rather than stay on the temporary fallback path

- Verification:
  - `dotnet build` passes
  - no fallback path ever targets the next future slot directly unless that future slot has become the active slot
  - no center-of-map destination fallback remains

## Assumptions
- The user's intended behavior is: resume toward the destination of the currently active vanilla slot if that slot window is still in effect.
- A temporary custom controller is acceptable only as a bounded fallback to the **active** slot, not as a replacement for vanilla scheduling and not as a shortcut to the next future slot.
- Cross-map recovery should remain vanilla-driven unless later evidence shows a separate targeted fix is needed.
