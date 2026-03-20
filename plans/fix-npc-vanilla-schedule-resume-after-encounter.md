# Plan: Fix NPC Vanilla Schedule Resume After Encounter

## TL;DR

After a Player2 NPC-to-NPC encounter completes, both participants fail to return to their vanilla schedule (10 retries exhausted, NPCs stranded). Root cause: `TryRebindVanillaScheduleAtCurrentTime` loads the schedule successfully but requires an immediate `PathFindController` as proof of success — which SDV never creates because the pre-computed schedule routes start from positions the NPCs are no longer at. Fix by: (1) directly pathfinding from the NPC's current position to their schedule destination, (2) relaxing the success gate, and (3) adding a warp fallback.

## Root Cause Analysis

**The schedule resume flow (lines 15022-15067 in ModEntry.cs):**
1. Clears controller, sets `followSchedule=true`, calls `npc.ClearSchedule()` + `npc.TryLoadSchedule()`
2. Checks `HasVanillaResumeState(npc)` — requires `controller != null || isMoving() || temporaryController != null`
3. This gate FAILS because `TryLoadSchedule()` populates `npc.Schedule` with pre-computed routes from the NPC's **day-start position**, not their current position. SDV doesn't create a controller for routes that don't start at the NPC's current tile.
4. Falls through to Path B: clones schedule with injected current-time entry mirrored from an earlier key. Same problem — the mirrored route was computed for the wrong origin position.
5. Returns empty string (failure) on every attempt.

**Self-defeating retry loop:** Each of 10 attempts (1 tick apart) calls `npc.controller = null; npc.ClearSchedule(); npc.TryLoadSchedule()` — destroying any work the game engine might have done between ticks.

**Key evidence from logs:**
- `checkSchedule=True` (old field name for `reloadTodayOk`) — schedule loaded fine
- `controller=False` — no PathFindController created
- `pathfind_attempted=False` — no direct pathfinding tried
- `preserved_route=True/False` — route was cloned but useless (wrong origin)

## Steps

### Phase 1: Add direct pathfinding from current position (primary fix)

1. **Add `ScheduleLocationMemberCandidates` array to ModEntry.cs** (line ~112, alongside existing `ScheduleTileMemberCandidates`). Copy the pattern from `NpcAutonomyPlannerService.cs:17`:
   ```
   { "targetLocationName", "TargetLocationName", "locationName", "LocationName", "location", "Location", "locationId", "LocationId" }
   ```

2. **Add helper `TryExtractScheduleDestination`** in ModEntry.cs (near line ~15210, before `HasVanillaResumeState`). Given a schedule dictionary and current time, find the active or next schedule entry and extract its target location name and target tile using the reflection pattern already used in `TryCloneScheduleWithCurrentEntry` and `NpcAutonomyPlannerService.TryReadScheduleLocation/TryReadScheduleTile`.

3. **Add helper `TryPathfindToScheduleDestination`** in ModEntry.cs. Given an NPC and a target location + tile:
   - If NPC is already on the target map: create `new PathFindController(npc, npc.currentLocation, targetTile, facingDirection)` and assign to `npc.controller`
   - If NPC is on a different map: use `Game1.warpCharacter(npc, targetLocationName, new Vector2(tile.X, tile.Y))` (existing pattern from `NpcAutonomyExecutionService:166`)
   - Return a descriptive method string on success, empty on failure

4. **Modify `TryRebindVanillaScheduleAtCurrentTime`** (line 15022): After both Path A and Path B fail the `HasVanillaResumeState` check, add a **Path C** that calls `TryExtractScheduleDestination` on the loaded schedule, then `TryPathfindToScheduleDestination`. Return `"PathFindController(direct)"` or `"WarpCharacter(schedule)"` on success.

### Phase 2: Prevent self-defeating retry (*depends on Phase 1*)

5. **Restructure `TryProcessPendingVanillaEncounterResumes`** (line 14958): On the first attempt, run the full TryRebind flow (Paths A/B/C). On subsequent attempts, *don't* clear and reload the schedule — just check `HasVanillaResumeState(npc)` to see if the game has processed the schedule since last attempt. Only re-run the full TryRebind if `npc.Schedule` is null.

### Phase 3: Warp fallback on exhaustion (*depends on Phase 1*)

6. **Add warp-to-schedule-destination fallback on max attempts** (line ~15007): In `TryProcessPendingVanillaEncounterResumes`, when `pending.Attempts >= EncounterVanillaResumeMaxAttempts` and the NPC still has no controller, extract the schedule destination and warp the NPC there as a last resort. Log at Warn level. This prevents NPCs from being permanently stranded.

### Phase 4: Improved logging (*parallel with any phase*)

7. **Add `pathfind_attempted` and `pathfind_method` fields** to `PendingVanillaEncounterResume` class and include them in the success/failure log messages. This makes future debugging easier and matches the diagnostic intent of the current log fields.

## Relevant Files

- [mod/StardewLivingRPG/ModEntry.cs](mod/StardewLivingRPG/ModEntry.cs) — All changes go here:
  - Line 112: Add `ScheduleLocationMemberCandidates` array
  - Lines 456-472: `PendingVanillaEncounterResume` class — add tracking fields
  - Lines 14958-15020: `TryProcessPendingVanillaEncounterResumes` — restructure retry, add warp fallback
  - Lines 15022-15067: `TryRebindVanillaScheduleAtCurrentTime` — add Path C
  - Near line 15210: Add `TryExtractScheduleDestination` + `TryPathfindToScheduleDestination` helpers

- [mod/StardewLivingRPG/Systems/NpcAutonomyPlannerService.cs](mod/StardewLivingRPG/Systems/NpcAutonomyPlannerService.cs) — Reference implementation:
  - Line 17: `ScheduleLocationMemberCandidates` array to copy
  - Line 336: `TryReadScheduleLocation` pattern to follow
  - Line 352: `TryReadScheduleTile` pattern to follow

- [mod/StardewLivingRPG/Systems/NpcAutonomyExecutionService.cs](mod/StardewLivingRPG/Systems/NpcAutonomyExecutionService.cs) — Reference:
  - Line 166: `Game1.warpCharacter` usage pattern

- [mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs](mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs) — Reference:
  - Line 37: `SchedulePathDescription` constructor (6-param form)

## Verification

1. Build with `dotnet build` — must compile clean
2. In-game test: trigger an NPC-to-NPC encounter (use `slrpg_demo_bootstrap` or wait for autonomous encounter), let it complete, verify both NPCs resume walking to their schedule destination within ~2 seconds
3. Check SMAPI console for: `"Autonomy: returned {npc} to vanilla schedule after encounter"` with `method=PathFindController(direct)` or `method=WarpCharacter(schedule)` — no more `failed to return` warnings
4. Verify cross-location case: if encounter happens outside the NPC's next schedule location, confirm warp works
5. Verify edge case: encounter at a time when no schedule entries exist (after last scheduled slot)

## Decisions

- **PathFindController is the primary fix, warp is fallback only.** Direct pathfinding looks natural; warping is jarring but prevents stranding.
- **Same-map vs cross-map branching:** Use `PathFindController` for same-map (natural walk), `Game1.warpCharacter` for cross-map (only safe option, follows existing NpcAutonomyExecutionService pattern).
- **Don't call `checkSchedule` via reflection.** Too fragile — private method with unknown side effects. Better to create PathFindController directly.
- **Keep the retry mechanism** but make subsequent attempts non-destructive (just check if NPC is moving).
- **Scope:** Only modifying the encounter-resume flow. Not touching encounter staging, face-to-face pinning, or schedule override paths.
