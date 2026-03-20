# Fix Encounter Resume by Driving Vanilla `checkSchedule(...)` Until Real Movement Exists

## Summary
The latest log proves the encounter completion path is working and the bug is in the vanilla resume worker, not the bubble gate:

- encounters do reach `Player2 encounter ... completed`
- `[HANDOFF]` and `[REBIND]` run
- `TryLoadSchedule()` succeeds
- the code then logs `resumed=true` even when `controller=null` and `isMoving=False`

The v7 fix should therefore do two things together:

1. replace the fake `currentScheduleDelay=0.001` trigger with a direct reflective call to vanilla `checkSchedule(...)`
2. stop treating “schedule loaded/reset” as success; keep the NPC in the pending resume queue until vanilla actually produces movement/controller state or the day genuinely has no future schedule work

This stays vanilla-driven: no custom controller creation, no custom movement, no teleport/warp fallback.

## Key Changes
- In `mod/StardewLivingRPG/ModEntry.cs`, change `TryRebindVanillaScheduleAtCurrentTime(...)` so it no longer sets `currentScheduleDelay`.
  - Keep `npc.ClearSchedule()` + `npc.TryLoadSchedule()` on the first attempt.
  - Keep resetting `lastAttemptedSchedule = -1`.
  - Reset `previousEndPoint` to the NPC’s actual tile after schedule load and before invoking vanilla.
  - Immediately invoke vanilla `checkSchedule(...)` via reflection instead of relying on the one-shot delay field.

- Add a private helper that invokes vanilla `checkSchedule` safely.
  - Prefer an instance overload that takes a single `int timeOfDay`.
  - Fallback to a parameterless overload if the single-int overload is unavailable.
  - Catch reflection/invocation failures and log them at debug/warn level without crashing.
  - Return the method label used, for example `checkSchedule(int)` or `checkSchedule()`.

- Fix the pending resume worker so success means actual vanilla resume state, not just “rebind executed”.
  - `TryProcessPendingVanillaEncounterResumes(...)` must only remove a pending NPC when `HasVanillaResumeState(npc)` becomes true.
  - Do not return `"ScheduleRebound"` just because `TryLoadSchedule()` succeeded.
  - If `checkSchedule(...)` ran but left `controller=null`, `temporaryController=null`, and `isMoving=False`, keep the NPC pending.

- Make retries time-aware instead of one-shot.
  - On attempt 1: reload schedule, reset state, call `checkSchedule(...)`.
  - On later attempts: do not clear/reload schedule again unless the schedule is missing.
  - Retry `checkSchedule(...)` when `Game1.timeOfDay` changes, because the main bug is that the next 10-minute vanilla slot is never processed after encounter release.
  - Store the last attempted `timeOfDay` in `PendingVanillaEncounterResume` so the worker does not spam `checkSchedule(...)` every tick at the same clock time.

- Track the next relevant schedule key for diagnostics and retry semantics.
  - After `TryLoadSchedule()`, inspect `npc.Schedule`.
  - Record the next future schedule key `> Game1.timeOfDay`, if one exists.
  - If the NPC is between slots, keep the pending resume alive through that future key instead of declaring success or failure early.
  - If there is no future schedule key left for the day, allow the worker to stop retrying after one final `checkSchedule(...)` pass and log that there was no later schedule slot.

- Correct the logs so they reflect reality.
  - Replace the current “returned ... resumed=true” log when no movement exists.
  - Log one of:
    - actual success: controller/temp controller/movement present
    - waiting: schedule loaded, no movement yet, next retry at later `timeOfDay`
    - terminal no-future-slot: schedule loaded, no later slot today
    - failure: reflection call failed or repeated retries never produced movement
  - Include:
    - `check_schedule_invoked`
    - `check_schedule_method`
    - `last_attempt_time`
    - `next_schedule_time`
    - `controller`
    - `temporary_controller`
    - `isMoving`

## Interfaces / Types
- No public API changes.
- Extend `PendingVanillaEncounterResume` with:
  - `LastAttemptedTimeOfDay`
  - `NextScheduleTime`
  - `CheckScheduleInvoked`
  - `CheckScheduleMethod`
- Keep `HasVanillaResumeState(...)` as the single success predicate for removing pending resume work.

## Test Plan
- Early-day cancel before first schedule slot:
  - example: encounter releases at `600`, first vanilla slot is `750`
  - first rebind may legitimately create no controller
  - worker must stay pending
  - at `750`, it must call `checkSchedule(...)` again and vanilla should take over

- Between-slot completion:
  - example: encounter releases at `750` with schedule keys `610,700,800...`
  - first rebind may only settle the NPC to the `700` endpoint
  - worker must retry at `800`
  - NPC should then acquire controller/movement for the `800` route

- Missed-slot completion:
  - example: encounter releases at `920` after an earlier active slot
  - first `checkSchedule(...)` should produce movement/controller immediately from the NPC’s actual post-encounter position

- Logging regression checks:
  - no more `resumed=true` when `controller=null`, `temporary_controller=false`, and `isMoving=False`
  - success logs must correspond to real movement/controller state
  - waiting logs must show the next retry time when between schedule slots

- Scope regression checks:
  - no custom `PathFindController` creation
  - no warp/teleport fallback
  - no changes to bubble behavior or GMCM defaults

## Assumptions
- The latest `smapi-logs.md` is authoritative and shows the real failing path.
- The core bug is not encounter completion anymore; it is the resume worker’s one-shot trigger plus false-success condition.
- Direct reflective `checkSchedule(...)` counts as “sticking to vanilla schedule/pathfinding” because the mod still does not build or own the path itself.
