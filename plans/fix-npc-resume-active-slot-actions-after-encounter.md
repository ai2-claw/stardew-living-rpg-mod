# Restore Active-Slot Schedule Actions After Encounter Resume

## Summary
Fix encounter resume so NPCs do not just return to the right tile; they must also re-enter the active vanilla schedule action at that tile, including correct facing and any schedule-driven animation or special frame state.

The fix stays vanilla-first:
- keep the existing temporary movement fallback for getting the NPC to the active slot;
- once the NPC is at the active-slot destination, explicitly hand control back to vanilla for that active slot;
- do not keep NPCs in a position-only "arrived but idle" state until the next schedule time.

## Key Changes
- In `mod/StardewLivingRPG/ModEntry.cs`, add an active-slot arrival handoff step:
  - trigger it for both cases:
    - NPC was already at the active target when the encounter ended;
    - NPC reached the active target through same-map or cross-map fallback;
  - clear the temporary fallback controller;
  - keep `followSchedule = true`;
  - reset `lastAttemptedSchedule = -1`;
  - set `previousEndPoint` to the actual active target tile;
  - inject an exact `Game1.timeOfDay` schedule entry that mirrors the active slot with its full vanilla payload;
  - immediately invoke vanilla `checkSchedule(Game1.timeOfDay)` and parameterless `checkSchedule()` fallback if needed.

- Add a helper that clones the full active `SchedulePathDescription`, not just location/tile:
  - preserve route data if present;
  - preserve facing direction;
  - preserve end/action/behavior strings;
  - preserve target location and endpoint;
  - never rebuild arrival handoff entries with hardcoded facing `2` unless full metadata is genuinely unavailable.

- Change pending-resume success criteria:
  - travel completion is not enough;
  - do not remove the pending worker just because the NPC reached the active target tile;
  - only treat the handoff as successful once vanilla has resumed the active slot in a concrete way:
    - controller/temporary controller exists, or
    - movement resumes under vanilla, or
    - no movement is expected and the active-slot facing/action state was applied.

- Add a dedicated “waiting for active-slot action” state:
  - when fallback reaches the active target before the next schedule boundary, stay pending and run the arrival handoff immediately;
  - if the handoff fails, retry once per in-game time change until the next schedule key;
  - if the next schedule key arrives first, stop trying to restore the old active slot and hand over to the new slot normally.

- Improve extraction and diagnostics:
  - log `active_schedule_time`, `active_target_location`, `active_target_tile`, `active_facing`, `active_behavior`, `arrival_rebind_invoked`, and `arrival_rebind_method`;
  - add a degraded log when full schedule-entry cloning is not possible and the code must fall back to tile+facing only.

## Test Plan
- Already-at-target case:
  - Alex/Treyvon/Beckett-style encounters where the NPC is already on the active-slot tile when released.
  - Expected: immediate arrival handoff; no idle waiting until the next schedule slot.

- Same-map arrival case:
  - NPC reaches the active tile on the same map after fallback.
  - Expected: after arrival, vanilla starts the active-slot facing/animation instead of leaving the NPC standing neutrally.

- Cross-map arrival case:
  - Shane reaches JojaMart and then `(9,17)` before `1230`.
  - Expected: he starts the active JojaMart slot behavior after arrival, not just pathing there and waiting.

- Animated-frame cases:
  - Clint hammering, Morrow cards, Shane stacking goods.
  - Expected: if those behaviors are encoded in the active schedule entry, they resume after encounter release and arrival.

- Boundary case:
  - NPC reaches the active target just before the next schedule time.
  - Expected: active-slot arrival handoff runs only if still before the next slot; otherwise the next slot takes precedence.

- Failure-path case:
  - full schedule-entry clone data is partially unavailable.
  - Expected: log degraded handoff, restore at least facing if possible, and do not spin forever.

## Assumptions
- The root bug is “arrival without vanilla action handoff,” not pathing anymore.
- Vanilla `checkSchedule(...)` is the correct place to restore schedule-end actions and special animation states.
- Temporary custom controllers are acceptable only for transit to the active slot; final facing/animation restoration must be vanilla-owned.
