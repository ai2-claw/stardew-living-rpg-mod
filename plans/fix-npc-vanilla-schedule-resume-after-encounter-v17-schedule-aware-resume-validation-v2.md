# Fix Encounter Resume False-Positives with Schedule-Aware Resume Validation

## Summary
Some NPCs occasionally end up walking to unscheduled spots after an encounter because the resume worker accepts any live controller or movement state as success, even when that state is inconsistent with the active schedule slot.

The fix is to make encounter resume success schedule-aware:
- validate the live movement state against the current active schedule slot before declaring success;
- reject clearly wrong resume states, like a source-map NPC holding a remote-map endpoint tile;
- immediately fall back to the existing active-slot recovery path for the current slot when a bad resume state is detected.

This stays narrow: it changes resume validation, not schedule content or the core fallback systems.

## Key Changes
- In `ModEntry.cs`, replace direct `HasVanillaResumeState(...)` success checks in the pending encounter-resume worker with a new validator such as `EvaluateResumeStateForActiveSchedule(...)`.
- The validator should return one of three outcomes:
  - `valid`
  - `invalid`
  - `unknown`
- Use `valid` to finalize encounter resume, `invalid` to clear the bad state and re-enter fallback, and `unknown` to keep waiting without accepting success.

- For same-map active slots, treat resume as valid when one of these is true:
  - the NPC is already at the resolved active target tile on the active target map;
  - the current endpoint matches the resolved active target tile;
  - the mod’s own same-map active-slot fallback controller is active.
- For same-map active slots, treat resume as invalid when vanilla creates a controller whose endpoint clearly does not match the resolved active target tile.

- For cross-map active slots, treat resume as valid when one of these is true:
  - the mod’s own cross-map fallback leg is active;
  - the NPC is on the source map and the current endpoint matches the expected first departure/transition leg tile;
  - the NPC has already reached the active target map.
- For cross-map active slots, treat resume as invalid when:
  - the NPC is still on the source map, but `previousEndPoint` matches the remote final target tile for another map;
  - or the current endpoint is otherwise incompatible with the planned first leg.
- This specifically covers the Morrow bug where `map=Town` but `previousEndPoint=(15,20)` belonged to `SeedShop`.

- Do not rely on `previousEndPoint` alone.
  - Evaluate in this order:
    - whether the current controller is the mod’s temporary fallback controller;
    - whether the NPC is on the expected map for the current stage;
    - whether `previousEndPoint` matches the expected valid tile for that stage;
    - whether the state is ambiguous and should remain pending.
- If the active schedule target cannot be resolved from the schedule entry, enter degraded mode:
  - log that target extraction was unreadable;
  - fall back to the existing generic `HasVanillaResumeState(...)` behavior for that NPC only;
  - do not clear vanilla state in degraded mode.

- On invalid resume state:
  - clear the bad state:
    - `npc.controller = null`
    - clear `temporaryController`
  - keep `followSchedule = true`
  - immediately re-enter the existing active-slot fallback for the current active slot:
    - same-map fallback if the active slot is on the current map
    - topology-guided cross-map leg fallback otherwise
  - do not fast-forward to the next future slot unless it has actually become active

- Add bounded rejection tracking to avoid thrashing if vanilla keeps recreating the same bad controller:
  - extend `PendingVanillaEncounterResume` with:
    - `LastRejectedResumeTimeOfDay`
    - `LastRejectedResumeReason`
    - `RejectedResumeCountForCurrentSlot`
  - only reject/clear the same bad resume shape once per `timeOfDay` unless the movement state materially changes
  - after rejection, the fallback system becomes the preferred recovery path for that slot window

- Update diagnostics:
  - add `resume_validation=valid|invalid|unknown|degraded`
  - add `resume_mismatch_reason`
  - include `active_target_location`, `active_target_tile`, `map`, `previousEndPoint`, and for cross-map slots the expected first-leg departure tile
  - only log `returned ... to vanilla schedule` after `valid`
  - add an explicit log when a wrong-target vanilla resume is rejected and replaced by active-slot fallback

## Interfaces / Types
- No public API changes.
- Add one private helper in `ModEntry.cs` to evaluate live resume state against the active schedule slot.
- Add small private pending-state fields for rejection throttling and diagnostics.
- Keep existing fallback controllers, cross-map legs, and schedule-entry target extraction helpers.

## Test Plan
- Morrow regression:
  - encounter ends in `Town` before `1220`
  - active slot is `1200 SeedShop 10,23`, next slot `1330 SeedShop 15,20`
  - if vanilla later creates a controller while Morrow is still on `Town` with `previousEndPoint=(15,20)`, the worker must reject it as invalid and resume cross-map recovery instead of accepting success

- Other occasional NPC cases:
  - verify at least two more NPCs with cross-map active slots after encounters
  - expected:
    - no success log while the NPC remains on the wrong map with a remote final endpoint
    - the worker either keeps the correct fallback leg active or re-enters it after rejecting the bad state

- Same-map cases:
  - correct same-map vanilla controller remains accepted
  - wrong same-map endpoint is rejected and replaced with same-map fallback
  - safe resolved fallback tiles still count as valid, even if they differ from the raw schedule tile

- Ambiguous / unreadable schedule-entry case:
  - if target extraction fails, the worker logs degraded validation and falls back to generic vanilla success logic without clearing controllers repeatedly

- Retry-throttle case:
  - if vanilla recreates the same wrong controller multiple times at the same clock time, the worker should not thrash every tick
  - one rejection per `timeOfDay` per bad state shape is enough before fallback takes over

- Verification:
  - `dotnet build` passes
  - no schedule content-pack changes are required
  - no GMCM/config changes are introduced

## Assumptions
- The occasional bug class shares one root cause: false-positive resume acceptance for movement states that do not actually match the active slot.
- The existing active-slot fallback systems are the right recovery path once these false positives are rejected.
- It is acceptable to override a bad vanilla resume attempt by clearing the invalid controller and restarting current-slot fallback, because the current behavior strands NPCs at unscheduled locations.
